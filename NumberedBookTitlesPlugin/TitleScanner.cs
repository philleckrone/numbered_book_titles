using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace NumberedBookTitles
{
    /// <summary>How a numbered book's number is written in a title field.</summary>
    public enum NumberStyle
    {
        Digit,          // 1 John
        DigitNbsp,      // 1 + non-breaking space + John
        DigitNoSpace,   // 1John
        DigitPeriod,    // 1. John
        DigitSuffix,    // 1st John, 1.o Juan, 1a Juan
        Roman,          // I John
        AfterDigit,     // John 1
        AfterRoman,     // John I
        Word,           // First John
    }

    /// <summary>The fields compared, in the order of the CMS report's columns.</summary>
    public enum TitleField
    {
        H,
        Toc1,
        Toc2,
        Toc3,
        Mt,
        BookNamesLong,
        BookNamesShort,
        BookNamesAbbr,
    }

    /// <summary>One marker line of the header: \h, \toc1, \toc2, \toc3 or one \mt level.</summary>
    public sealed class MarkerLine
    {
        /// <summary>The marker without backslash: h, toc1, mt2, ...</summary>
        public string Marker;
        /// <summary>Where the marker's text starts in the chapter text, and its length.</summary>
        public int ValueStart;
        public int ValueLength;
        /// <summary>The text with runs of spaces/tabs collapsed and trimmed (as the CMS check reads it).</summary>
        public string Value;
    }

    /// <summary>The title fields of one numbered book.</summary>
    public sealed class BookTitles
    {
        public BookId Book;
        /// <summary>Chapter 1 as read for the scan (it holds the header); null when the book has no text.</summary>
        public string ChapterText;
        /// <summary>The value each field is compared with (null or empty = absent).</summary>
        public readonly Dictionary<TitleField, string> Values = new Dictionary<TitleField, string>();
        /// <summary>The marker line behind each text field (BookNames.xml fields have none).</summary>
        public readonly Dictionary<TitleField, MarkerLine> Lines = new Dictionary<TitleField, MarkerLine>();
        /// <summary>Every \mt, \mt1, \mt2, ... line in document order.</summary>
        public readonly List<MarkerLine> MtLines = new List<MarkerLine>();

        public string Value(TitleField field)
        {
            string v;
            return Values.TryGetValue(field, out v) ? v : null;
        }

        /// <summary>The number in the book code: 1, 2 or 3.</summary>
        public int Number
        {
            get { return Book.Code[0] - '0'; }
        }
    }

    /// <summary>The comparison of one field across the numbered books.</summary>
    public sealed class FieldResult
    {
        public TitleField Field;
        /// <summary>The style most books use; null when two or more styles tie.</summary>
        public NumberStyle? Usual;
        public readonly Dictionary<NumberStyle, int> Counts = new Dictionary<NumberStyle, int>();
        /// <summary>Book number -> style, for the books that differ (every book when there is a tie).</summary>
        public readonly Dictionary<int, NumberStyle> Outliers = new Dictionary<int, NumberStyle>();

        public int Total
        {
            get { return Counts.Values.Sum(); }
        }
    }

    /// <summary>One book whose field writes the number differently from the usual style.</summary>
    public sealed class TitleIssue
    {
        public BookTitles Titles;
        public FieldResult Result;
        public TitleField Field;
        /// <summary>The value compared (as in the CMS report).</summary>
        public string Text;
        public NumberStyle Style;
        /// <summary>The part that shows the number: "1st", "Primero", "II".</summary>
        public string Token;
        /// <summary>The marker line a fix changes; null for BookNames.xml (report only).</summary>
        public MarkerLine Target;
        /// <summary>The suggested new text of the target line, or null when none can be worked out.</summary>
        public string Suggestion;
        /// <summary>The text the target line gets when the row is fixed (the suggestion, or what the user typed).</summary>
        public string NewValue;
        public bool Selected;

        public BookId Book
        {
            get { return Titles.Book; }
        }

        public bool IsBookNames
        {
            get { return Field >= TitleField.BookNamesLong; }
        }

        /// <summary>A fix is possible: a marker line, and a new text that differs from the current one.</summary>
        public bool CanFix
        {
            get
            {
                return Target != null && !string.IsNullOrEmpty(NewValue) && NewValue.Trim().Length > 0
                    && TitleScanner.CollapseWs(NewValue) != Target.Value;
            }
        }
    }

    public sealed class ScanResult
    {
        public readonly List<BookTitles> Books = new List<BookTitles>();
        public readonly List<FieldResult> Results = new List<FieldResult>();
        public readonly List<TitleIssue> Issues = new List<TitleIssue>();
        /// <summary>As counted by the CMS check: one per differing book, one per field with a tie.</summary>
        public int IssueCount;
        public string BookNamesPath;
        public bool BookNamesFound;

        public FieldResult ResultFor(TitleField field)
        {
            return Results.FirstOrDefault(r => r.Field == field);
        }
    }

    /// <summary>
    /// Finds how the numbered books (1SA ... 3JN) write their number in \h, \toc1, \toc2, \toc3,
    /// \mt and the BookNames.xml names. A port of the "Numbered books" section of the CMS check
    /// Report of TOC (CMS\toc_examiner\toc_examiner.py): each field on its own, the most common
    /// style is the usual one, and every book that uses another style is listed. On top of the
    /// check, each listed marker line gets a suggested fix in the usual style.
    /// </summary>
    public static class TitleScanner
    {
        public static readonly TitleField[] Fields =
        {
            TitleField.H, TitleField.Toc1, TitleField.Toc2, TitleField.Toc3, TitleField.Mt,
            TitleField.BookNamesLong, TitleField.BookNamesShort, TitleField.BookNamesAbbr,
        };

        /// <summary>The CMS check's header expression: bare \h, \toc1-3, \mt with at most one digit.</summary>
        private static readonly Regex HeaderMarker = new Regex(@"^\\(toc1|toc2|toc3|mt[0-9]?|h)[ \t]+([^\r\n]*)", RegexOptions.Multiline);
        private static readonly Regex WsRun = new Regex(@"[ \t]+");

        private static readonly Regex LeadDigit = new Regex(@"^(\d+)(.*)$", RegexOptions.Singleline);
        // Ordinal endings: English 1st/2nd/3rd, Spanish 1er/1ro/1ra/1o/1a/1.o, French 1er/1re/2e/2eme,
        // and the ordinal indicators U+00AA/U+00BA/U+00B0. Only lowercase endings from this list
        // count, so "1Juan" or a \toc3 like "1Jn" is a number with no space, not an ending.
        private static readonly Regex Suffix = new Regex(
            @"^\.?[\u00AA\u00BA\u00B0]|^\.?(?:st|nd|rd|th|er|re|ere|\u00E8re|eme|\u00E8me|e|ra|ro|o|a)(?=[ \u00A0.]|$)");
        private static readonly Regex LeadRoman = new Regex(@"^(I{1,3})[ \u00A0]");
        private static readonly Regex TrailDigit = new Regex(@"[ \u00A0](\d+)$");
        private static readonly Regex TrailRoman = new Regex(@"[ \u00A0](I{1,3})$");
        private static readonly Regex FirstWord = new Regex(@"^\S+");

        private static readonly char[] Spaces = { ' ', '\u00A0' };

        public static bool IsNumberedBook(BookId book)
        {
            return book.Number >= 1 && book.Number <= 66 && book.Code.Length == 3 && book.Code[0] >= '1' && book.Code[0] <= '3';
        }

        public static bool IsDigitFamily(NumberStyle style)
        {
            return style != NumberStyle.Word;
        }

        // ------------------------------------------------------------ text helpers (as the CMS check)

        /// <summary>CR removed, LF as a space, runs of spaces/tabs as one space, then trimmed.</summary>
        public static string CollapseWs(string s)
        {
            if (s == null) return null;
            return WsRun.Replace(s.Replace("\r", "").Replace("\n", " "), " ").Trim();
        }

        /// <summary>A literal // (a soft line break) counts as a space.</summary>
        public static string StripSoftBreaks(string s)
        {
            return CollapseWs(s.Replace("//", " "));
        }

        // ------------------------------------------------------------ styles

        /// <summary>How the number is written in the text, and the part that shows it.</summary>
        public static NumberStyle StyleOf(string text, out string token)
        {
            Match m = LeadDigit.Match(text);
            if (m.Success)
            {
                string digits = m.Groups[1].Value, rest = m.Groups[2].Value;
                Match sm = Suffix.Match(rest);
                if (sm.Success)
                {
                    token = digits + sm.Value;
                    return NumberStyle.DigitSuffix;
                }
                if (rest.StartsWith(".", StringComparison.Ordinal))
                {
                    token = digits + ".";
                    return NumberStyle.DigitPeriod;
                }
                token = digits;
                if (rest.StartsWith("\u00A0", StringComparison.Ordinal)) return NumberStyle.DigitNbsp;
                if (rest.StartsWith(" ", StringComparison.Ordinal) || rest.Length == 0) return NumberStyle.Digit;
                return NumberStyle.DigitNoSpace;
            }
            m = LeadRoman.Match(text);
            if (m.Success)
            {
                token = m.Groups[1].Value;
                return NumberStyle.Roman;
            }
            m = TrailDigit.Match(text);
            if (m.Success)
            {
                token = m.Groups[1].Value;
                return NumberStyle.AfterDigit;
            }
            m = TrailRoman.Match(text);
            if (m.Success)
            {
                token = m.Groups[1].Value;
                return NumberStyle.AfterRoman;
            }
            m = FirstWord.Match(text);
            token = m.Success ? m.Value : text;
            return NumberStyle.Word;
        }

        public static NumberStyle StyleOf(string text)
        {
            string token;
            return StyleOf(text, out token);
        }

        /// <summary>The text without the part that shows the number (the book's name, mostly).</summary>
        public static string NamePart(string text, NumberStyle style)
        {
            string token;
            StyleOf(text, out token);
            switch (style)
            {
                case NumberStyle.Digit:
                case NumberStyle.DigitNbsp:
                case NumberStyle.DigitNoSpace:
                case NumberStyle.DigitPeriod:
                case NumberStyle.DigitSuffix:
                case NumberStyle.Roman:
                case NumberStyle.Word:
                    return text.Substring(Math.Min(token.Length, text.Length)).TrimStart().TrimStart(Spaces);
                case NumberStyle.AfterDigit:
                case NumberStyle.AfterRoman:
                    return text.Substring(0, Math.Max(0, text.Length - token.Length)).TrimEnd(Spaces);
            }
            return text;
        }

        private static string Roman(int n)
        {
            return n == 1 ? "I" : n == 2 ? "II" : "III";
        }

        // ------------------------------------------------------------ scanning

        /// <summary>Reads every numbered book of the source and compares the fields.</summary>
        public static ScanResult Scan(IBookSource source)
        {
            var result = new ScanResult();
            Dictionary<string, string[]> bookNames = LoadBookNames(source.ProjectDirectory, result);
            foreach (BookId book in source.Books.Where(IsNumberedBook).OrderBy(b => b.Number))
            {
                string text = ReadChapter1(source, book.Number);
                if (text == null) continue;
                BookTitles titles = ReadTitles(book, text);
                string[] names;
                if (bookNames != null && bookNames.TryGetValue(book.Code, out names))
                {
                    TitleField[] bn = { TitleField.BookNamesLong, TitleField.BookNamesShort, TitleField.BookNamesAbbr };
                    for (int i = 0; i < 3; i++)
                    {
                        string v = StripSoftBreaks(names[i] ?? "");
                        if (v.Length > 0) titles.Values[bn[i]] = v;
                    }
                }
                result.Books.Add(titles);
            }
            Compare(result);
            foreach (TitleIssue issue in result.Issues)
            {
                issue.Suggestion = Suggest(issue, result);
                issue.NewValue = issue.Suggestion;
            }
            return result;
        }

        /// <summary>Chapter 1 (from \id to \c 2); a book without \c is all chapter 1.</summary>
        public static string ReadChapter1(IBookSource source, int bookNum)
        {
            string text = source.GetChapter(bookNum, 1);
            if (text == null) text = source.GetBook(bookNum);
            return text;
        }

        /// <summary>The header markers of one book, read as the CMS check reads them.</summary>
        public static BookTitles ReadTitles(BookId book, string text)
        {
            var titles = new BookTitles();
            titles.Book = book;
            titles.ChapterText = text;
            foreach (Match m in HeaderMarker.Matches(text))
            {
                var line = new MarkerLine();
                line.Marker = m.Groups[1].Value;
                line.ValueStart = m.Groups[2].Index;
                line.ValueLength = m.Groups[2].Length;
                line.Value = CollapseWs(m.Groups[2].Value);
                switch (line.Marker)
                {
                    case "h": AddFirst(titles, TitleField.H, line, line.Value); break;
                    case "toc1": AddFirst(titles, TitleField.Toc1, line, StripSoftBreaks(line.Value)); break;
                    case "toc2": AddFirst(titles, TitleField.Toc2, line, line.Value); break;
                    case "toc3": AddFirst(titles, TitleField.Toc3, line, line.Value); break;
                    default: titles.MtLines.Add(line); break;
                }
            }
            // \mt levels joined in document order (some books put \mt2 before \mt1).
            if (titles.MtLines.Count > 0)
            {
                string mt = string.Join(" ", titles.MtLines.Where(l => l.Value.Length > 0).Select(l => l.Value).ToArray());
                titles.Values[TitleField.Mt] = StripSoftBreaks(mt);
            }
            return titles;
        }

        private static void AddFirst(BookTitles titles, TitleField field, MarkerLine line, string value)
        {
            if (titles.Lines.ContainsKey(field)) return;
            titles.Lines[field] = line;
            titles.Values[field] = value;
        }

        /// <summary>BookNames.xml: code -> { long, short, abbr }, or null when the file is missing or unreadable.</summary>
        private static Dictionary<string, string[]> LoadBookNames(string projectDir, ScanResult result)
        {
            if (string.IsNullOrEmpty(projectDir)) return null;
            string path = Path.Combine(projectDir, "BookNames.xml");
            result.BookNamesPath = path;
            if (!File.Exists(path)) return null;
            var names = new Dictionary<string, string[]>(StringComparer.Ordinal);
            try
            {
                var doc = new XmlDocument();
                doc.XmlResolver = null;
                doc.Load(path);
                if (doc.DocumentElement == null) return null;
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    var el = node as XmlElement;
                    if (el == null || el.Name != "book") continue;
                    string code = el.GetAttribute("code");
                    if (string.IsNullOrEmpty(code)) continue;
                    names[code] = new[] { el.GetAttribute("long"), el.GetAttribute("short"), el.GetAttribute("abbr") };
                }
            }
            catch (Exception)
            {
                return null;
            }
            result.BookNamesFound = true;
            return names;
        }

        /// <summary>The comparison of each field, as the CMS check's _evaluate_numbered.</summary>
        private static void Compare(ScanResult result)
        {
            foreach (TitleField field in Fields)
            {
                var styles = new Dictionary<int, NumberStyle>();
                foreach (BookTitles t in result.Books)
                {
                    string v = t.Value(field);
                    if (!string.IsNullOrEmpty(v)) styles[t.Book.Number] = StyleOf(v);
                }
                if (styles.Count < 2) continue;
                var r = new FieldResult();
                r.Field = field;
                foreach (NumberStyle st in styles.Values)
                {
                    int n;
                    r.Counts.TryGetValue(st, out n);
                    r.Counts[st] = n + 1;
                }
                int top = r.Counts.Values.Max();
                List<NumberStyle> leaders = r.Counts.Where(kv => kv.Value == top).Select(kv => kv.Key).ToList();
                if (leaders.Count == 1)
                {
                    r.Usual = leaders[0];
                    foreach (var kv in styles)
                        if (kv.Value != r.Usual.Value) r.Outliers[kv.Key] = kv.Value;
                    result.IssueCount += r.Outliers.Count;
                }
                else
                {
                    // No usual style: every book in the field is listed, and the field counts as one issue.
                    foreach (var kv in styles) r.Outliers[kv.Key] = kv.Value;
                    result.IssueCount += 1;
                }
                if (r.Outliers.Count == 0) continue;
                result.Results.Add(r);
                foreach (int bookNum in r.Outliers.Keys.OrderBy(n => n))
                {
                    BookTitles t = result.Books.First(b => b.Book.Number == bookNum);
                    var issue = new TitleIssue();
                    issue.Titles = t;
                    issue.Result = r;
                    issue.Field = field;
                    issue.Text = t.Value(field);
                    string token;
                    issue.Style = StyleOf(issue.Text, out token);
                    issue.Token = token;
                    issue.Target = TargetLine(t, field, issue.Style);
                    result.Issues.Add(issue);
                }
            }
        }

        /// <summary>
        /// The marker line a fix changes. For \mt it is the \mt line that holds the number: the
        /// first one, or the last one when the number comes after the name.
        /// </summary>
        private static MarkerLine TargetLine(BookTitles t, TitleField field, NumberStyle style)
        {
            if (field == TitleField.Mt)
            {
                List<MarkerLine> lines = t.MtLines.Where(l => l.Value.Length > 0).ToList();
                if (lines.Count == 0) return null;
                return style == NumberStyle.AfterDigit || style == NumberStyle.AfterRoman ? lines[lines.Count - 1] : lines[0];
            }
            MarkerLine line;
            return t.Lines.TryGetValue(field, out line) ? line : null;
        }

        // ------------------------------------------------------------ suggestions

        /// <summary>
        /// The target line rewritten in the usual style, or null when it cannot be worked out safely:
        /// a tie, a word that would need to be invented, or a word in front of something that is not
        /// clearly the book's name.
        /// </summary>
        public static string Suggest(TitleIssue issue, ScanResult result)
        {
            if (issue.Target == null || !issue.Result.Usual.HasValue) return null;
            NumberStyle usual = issue.Result.Usual.Value;
            BookTitles t = issue.Titles;
            string value = issue.Target.Value;
            NumberStyle style = StyleOf(value);
            if (style == usual) return null;
            string name = NamePart(value, style);
            if (name.Length == 0) return null;
            int n = t.Number;

            if (usual == NumberStyle.Word)
            {
                // \toc1 normally repeats the \mt title, so when one of them uses a word, the other can copy it.
                bool singleMt = t.MtLines.Count(l => l.Value.Length > 0) == 1;
                if (issue.Field == TitleField.Toc1)
                {
                    string mt = t.Value(TitleField.Mt);
                    if (!string.IsNullOrEmpty(mt) && StyleOf(mt) == NumberStyle.Word) return mt;
                }
                if (issue.Field == TitleField.Mt && singleMt)
                {
                    string toc1 = t.Value(TitleField.Toc1);
                    if (!string.IsNullOrEmpty(toc1) && StyleOf(toc1) == NumberStyle.Word) return toc1;
                }
                string word = SiblingToken(issue, result, NumberStyle.Word);
                return word == null ? null : word + " " + name;
            }

            // From a word to a digit: only when what follows the word is the book's name, as another
            // field of the same book shows it with a digit (so "Primera carta a los Corintios" is left alone).
            if (style == NumberStyle.Word && !NameConfirmed(t, StripSoftBreaks(name)))
                return null;

            switch (usual)
            {
                case NumberStyle.Digit: return n + " " + name;
                case NumberStyle.DigitNbsp: return n + "\u00A0" + name;
                case NumberStyle.DigitNoSpace: return n + name;
                case NumberStyle.DigitPeriod: return n + ". " + name;
                case NumberStyle.Roman: return Roman(n) + " " + name;
                case NumberStyle.AfterDigit: return name + " " + n;
                case NumberStyle.AfterRoman: return name + " " + Roman(n);
                case NumberStyle.DigitSuffix:
                {
                    string token = SiblingToken(issue, result, NumberStyle.DigitSuffix);
                    return token == null ? null : token + " " + name;
                }
            }
            return null;
        }

        /// <summary>
        /// The number's token ("1st", "Primera") that the other books with the same number use in
        /// this field in the given style, when they all use the same one; otherwise null.
        /// </summary>
        private static string SiblingToken(TitleIssue issue, ScanResult result, NumberStyle style)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (BookTitles other in result.Books)
            {
                if (other == issue.Titles || other.Number != issue.Titles.Number) continue;
                string v = other.Value(issue.Field);
                if (string.IsNullOrEmpty(v)) continue;
                string token;
                if (StyleOf(v, out token) == style) tokens.Add(token);
            }
            return tokens.Count == 1 ? tokens.First() : null;
        }

        /// <summary>True when another field of the book writes this name after a number (not a word).</summary>
        private static bool NameConfirmed(BookTitles t, string name)
        {
            foreach (var kv in t.Values)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                NumberStyle style = StyleOf(kv.Value);
                if (style == NumberStyle.Word) continue;
                if (string.Equals(StripSoftBreaks(NamePart(kv.Value, style)), name, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------ fixing

        /// <summary>The chapter text with each issue's target line set to its new text.</summary>
        public static string Apply(string chapterText, IEnumerable<TitleIssue> issues)
        {
            string text = chapterText;
            foreach (TitleIssue i in issues.Where(i => i.CanFix).OrderByDescending(i => i.Target.ValueStart))
                text = text.Substring(0, i.Target.ValueStart) + i.NewValue.Trim() +
                    text.Substring(i.Target.ValueStart + i.Target.ValueLength);
            return text;
        }
    }
}
