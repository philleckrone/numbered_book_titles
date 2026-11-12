using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NumberedBookTitles
{
    public sealed class FixReport
    {
        /// <summary>Book code and number of lines changed, in book order.</summary>
        public readonly List<KeyValuePair<string, int>> Fixed = new List<KeyValuePair<string, int>>();
        public readonly List<string> NoPermission = new List<string>();
        public readonly List<string> Busy = new List<string>();
        public readonly List<string> Failed = new List<string>();
        /// <summary>Books whose header changed after the scan, so they were left alone.</summary>
        public readonly List<string> Changed = new List<string>();
        public readonly List<string> Backups = new List<string>();
        public string HistoryError;
        /// <summary>True when no history point could be made, so nothing was changed.</summary>
        public bool Stopped;

        public int Total
        {
            get { return Fixed.Sum(kv => kv.Value); }
        }

        public int BooksChanged
        {
            get { return Fixed.Count; }
        }
    }

    /// <summary>
    /// Changes the ticked header lines the way Paratext treats edits: books whose first chapter
    /// the user may edit are changed, the others are left alone and reported. The header is in
    /// chapter 1 (from \id to \c 2), so that is the only chapter written.
    /// </summary>
    public sealed class FixRunner
    {
        public const int HeaderChapter = 1;

        private readonly IBookSource source;
        private readonly Dictionary<int, bool> canEdit = new Dictionary<int, bool>();

        /// <summary>Message for a book Paratext accepted but did not store as written (set from Strings).</summary>
        public string NotSavedText = "not saved";

        public FixRunner(IBookSource source)
        {
            this.source = source;
        }

        public bool CanEdit(int bookNum)
        {
            bool result;
            if (!canEdit.TryGetValue(bookNum, out result))
            {
                try { result = source.CanEdit(bookNum, HeaderChapter); }
                catch (Exception) { result = false; }
                canEdit[bookNum] = result;
            }
            return result;
        }

        /// <summary>The number of books with a chosen fix the user may edit.</summary>
        public int BooksToChange(IEnumerable<TitleIssue> chosen)
        {
            return chosen.Where(i => i.CanFix && CanEdit(i.Book.Number)).Select(i => i.Book.Number).Distinct().Count();
        }

        /// <summary>
        /// Changes the chosen lines in every book the user may edit. Each changed book is first
        /// saved whole as Backup_BOOK_time.bak (UTF-8). historyMessage is first recorded as a
        /// project history point; if that fails, nothing is changed (report.Stopped).
        /// </summary>
        public FixReport Run(IEnumerable<TitleIssue> chosen, string historyMessage)
        {
            var report = new FixReport();
            var byBook = new SortedDictionary<int, List<TitleIssue>>();
            foreach (TitleIssue issue in chosen)
            {
                if (!issue.CanFix) continue;
                List<TitleIssue> list;
                if (!byBook.TryGetValue(issue.Book.Number, out list))
                {
                    list = new List<TitleIssue>();
                    byBook[issue.Book.Number] = list;
                }
                list.Add(issue);
            }

            // Books the user may not edit are reported, never written.
            var editable = new List<List<TitleIssue>>();
            foreach (var kv in byBook)
            {
                if (CanEdit(kv.Key)) editable.Add(kv.Value);
                else report.NoPermission.Add(kv.Value[0].Book.Code);
            }
            if (editable.Count == 0) return report;

            // A history point is required: without one nothing is changed.
            string historyError;
            bool made;
            try { made = source.MakeHistoryPoint(historyMessage, out historyError); }
            catch (Exception ex)
            {
                made = false;
                historyError = ex.Message;
            }
            if (!made)
            {
                report.HistoryError = historyError ?? "";
                report.Stopped = true;
                return report;
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            foreach (List<TitleIssue> issues in editable)
            {
                BookId book = issues[0].Book;
                string old = TitleScanner.ReadChapter1(source, book.Number);
                if (old == null || old != issues[0].Titles.ChapterText)
                {
                    report.Changed.Add(book.Code);
                    continue;
                }
                string text = TitleScanner.Apply(old, issues);
                if (text == old) continue;

                string backupError;
                string backup = SaveBackup(book, stamp, out backupError);
                if (backup == null)
                {
                    report.Failed.Add(book.Code + ": " + backupError);
                    continue;
                }

                string error;
                WriteOutcome outcome = source.WriteChapter(book.Number, HeaderChapter, text, out error);
                if (outcome == WriteOutcome.Written)
                {
                    // Paratext normalizes what it stores: make sure every changed line holds its new text.
                    string now = TitleScanner.ReadChapter1(source, book.Number);
                    if (now == null || (now != text && !AllStored(book, now, issues)))
                    {
                        report.Failed.Add(book.Code + ": " + NotSavedText);
                        report.Backups.Add(backup);
                        continue;
                    }
                    report.Fixed.Add(new KeyValuePair<string, int>(book.Code, issues.Count));
                    report.Backups.Add(backup);
                    continue;
                }
                if (outcome == WriteOutcome.NoPermission)
                    report.NoPermission.Add(book.Code);
                else if (outcome == WriteOutcome.Busy)
                    report.Busy.Add(book.Code);
                else
                    report.Failed.Add(book.Code + ": " + error);
                try { File.Delete(backup); }
                catch (Exception) { }
            }
            return report;
        }

        private static bool AllStored(BookId book, string text, List<TitleIssue> issues)
        {
            BookTitles now = TitleScanner.ReadTitles(book, text);
            foreach (TitleIssue i in issues)
            {
                string want = TitleScanner.CollapseWs(i.NewValue);
                if (i.Field == TitleField.Mt)
                {
                    if (!now.MtLines.Any(l => l.Marker == i.Target.Marker && l.Value == want)) return false;
                }
                else
                {
                    MarkerLine line;
                    if (!now.Lines.TryGetValue(i.Field, out line) || line.Value != want) return false;
                }
            }
            return true;
        }

        private string SaveBackup(BookId book, string stamp, out string error)
        {
            error = null;
            try
            {
                string text = source.GetBook(book.Number);
                if (text == null)
                {
                    error = "empty book";
                    return null;
                }
                string dir = source.BackupDirectory;
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "Backup_" + book.Code + "_" + stamp + ".bak");
                File.WriteAllText(path, text, new UTF8Encoding(false));
                return path;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }
    }
}
