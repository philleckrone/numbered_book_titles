using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NumberedBookTitles;

namespace NumberedBookTitlesTests
{
    /// <summary>
    /// A project made of a folder of SFM files, for the tests. Chapters are cut the way Paratext
    /// cuts them (chapter 1 runs from \id to \c 2; a book without \c is all chapter 1). Edit
    /// rights come from a spec: ALL, NONE, or e.g. "TOB,XXA,HEB:2-3" (book, or book:chapters).
    /// </summary>
    public sealed class FolderBookSource : IBookSource
    {
        public static readonly string[] BookOrder = (
            "GEN EXO LEV NUM DEU JOS JDG RUT 1SA 2SA 1KI 2KI 1CH 2CH EZR NEH EST JOB PSA PRO ECC SNG ISA JER LAM " +
            "EZK DAN HOS JOL AMO OBA JON MIC NAM HAB ZEP HAG ZEC MAL MAT MRK LUK JHN ACT ROM 1CO 2CO GAL EPH PHP " +
            "COL 1TH 2TH 1TI 2TI TIT PHM HEB JAS 1PE 2PE 1JN 2JN 3JN JUD REV TOB JDT ESG WIS SIR BAR LJE S3Y SUS " +
            "BEL 1MA 2MA 3MA 4MA 1ES 2ES MAN PS2 ODA PSS JSA JDB TBS SST DNT BLT XXA XXB XXC XXD XXE XXF XXG FRT " +
            "BAK OTH 3ES EZA 5EZ 6EZ INT CNC GLO TDX NDX DAG PS3 2BA LBA JUB ENO 1MQ 2MQ 3MQ REP 4BA LAO").Split(' ');

        private static readonly Regex ChapterRegex = new Regex(@"\\c[ \t]+(\d+)");
        private readonly Encoding encoding = new UTF8Encoding(false);

        private readonly string name;
        private readonly string folder;
        private readonly Dictionary<int, string> files = new Dictionary<int, string>();
        private readonly List<BookId> books = new List<BookId>();
        private readonly Dictionary<string, HashSet<int>> editable;   // null = every book; empty set = whole book
        private readonly string backupDirectory;
        public readonly List<string> HistoryPoints = new List<string>();
        public readonly List<string> Writes = new List<string>();
        public readonly List<string> GoTos = new List<string>();

        public FolderBookSource(string folder, string projectName, string editSpec, string backupDir)
        {
            name = projectName;
            this.folder = Path.GetFullPath(folder);
            // Settings.xml <Encoding> (a code page; 65001 = UTF-8), as Paratext reads the files.
            string settings = Path.Combine(folder, "Settings.xml");
            if (File.Exists(settings))
            {
                Match enc = Regex.Match(File.ReadAllText(settings), @"<Encoding>(\d+)</Encoding>");
                if (enc.Success && enc.Groups[1].Value != "65001")
                    encoding = Encoding.GetEncoding(int.Parse(enc.Groups[1].Value));
            }
            backupDirectory = backupDir;
            // Book files are named <FileNamePrePart>...<FileNamePostPart> (e.g. URB...SFC, ...MZZ.PTW);
            // without a Settings.xml they are *.SFM.
            string pre = "", post = ".SFM";
            if (File.Exists(settings))
            {
                string xml = File.ReadAllText(settings);
                Match mp = Regex.Match(xml, @"<FileNamePrePart>([^<]*)</FileNamePrePart>");
                Match mq = Regex.Match(xml, @"<FileNamePostPart>([^<]*)</FileNamePostPart>");
                if (mp.Success) pre = mp.Groups[1].Value;
                if (mq.Success && mq.Groups[1].Value.Length > 0) post = mq.Groups[1].Value;
            }
            IEnumerable<string> bookFiles = Directory.GetFiles(folder).Where(p =>
            {
                string fn = Path.GetFileName(p);
                return fn.StartsWith(pre, StringComparison.OrdinalIgnoreCase) && fn.EndsWith(post, StringComparison.OrdinalIgnoreCase);
            });
            foreach (string path in bookFiles)
            {
                string first = File.ReadLines(path).FirstOrDefault() ?? "";
                Match m = Regex.Match(first, @"^\uFEFF?\\id\s+(\w{3})");
                if (!m.Success) continue;
                string code = m.Groups[1].Value.ToUpperInvariant();
                int index = Array.IndexOf(BookOrder, code);
                if (index < 0) continue;
                // Book files are named like 01GENPDT15.SFM; skip strays such as a
                // RegExBackup1.sfm that also starts with \id GEN.
                if (Path.GetFileNameWithoutExtension(path).ToUpperInvariant().IndexOf(code, StringComparison.Ordinal) < 0) continue;
                files[index + 1] = path;
            }
            foreach (int n in files.Keys.OrderBy(n => n))
                books.Add(new BookId(n, BookOrder[n - 1]));
            editable = ParseSpec(editSpec);
        }

        private static Dictionary<string, HashSet<int>> ParseSpec(string spec)
        {
            if (string.IsNullOrEmpty(spec) || spec.Equals("ALL", StringComparison.OrdinalIgnoreCase)) return null;
            var result = new Dictionary<string, HashSet<int>>();
            if (spec.Equals("NONE", StringComparison.OrdinalIgnoreCase)) return result;
            foreach (string part in spec.Split(','))
            {
                string[] bc = part.Trim().Split(':');
                var chapters = new HashSet<int>();
                if (bc.Length > 1)
                {
                    string[] range = bc[1].Split('-');
                    int from = int.Parse(range[0]);
                    int to = range.Length > 1 ? int.Parse(range[1]) : from;
                    for (int c = from; c <= to; c++) chapters.Add(c);
                }
                result[bc[0].ToUpperInvariant()] = chapters;
            }
            return result;
        }

        public string ProjectName
        {
            get { return name; }
        }

        /// <summary>The folder itself, so the stylesheets and settings are looked up as for a project.</summary>
        public string ProjectDirectory
        {
            get { return folder; }
        }

        public IList<BookId> Books
        {
            get { return books; }
        }

        public string GetBook(int bookNum)
        {
            string path;
            return files.TryGetValue(bookNum, out path) ? File.ReadAllText(path, encoding) : null;
        }

        /// <summary>Start and end of each chapter in the book text (chapter 1 includes the header).</summary>
        private static Dictionary<int, Tuple<int, int>> ChapterSpans(string text)
        {
            var starts = new List<Tuple<int, int>>();
            foreach (Match m in ChapterRegex.Matches(text))
                starts.Add(Tuple.Create(int.Parse(m.Groups[1].Value), m.Index));
            var spans = new Dictionary<int, Tuple<int, int>>();
            if (starts.Count == 0)
            {
                spans[1] = Tuple.Create(0, text.Length);
                return spans;
            }
            for (int i = 0; i < starts.Count; i++)
            {
                int start = i == 0 ? 0 : starts[i].Item2;
                int end = i + 1 < starts.Count ? starts[i + 1].Item2 : text.Length;
                if (!spans.ContainsKey(starts[i].Item1))
                    spans[starts[i].Item1] = Tuple.Create(start, end);
            }
            return spans;
        }

        public string GetChapter(int bookNum, int chapter)
        {
            string text = GetBook(bookNum);
            if (text == null) return null;
            Tuple<int, int> span;
            if (!ChapterSpans(text).TryGetValue(chapter, out span)) return null;
            return text.Substring(span.Item1, span.Item2 - span.Item1);
        }

        public bool CanEdit(int bookNum, int chapter)
        {
            if (editable == null) return true;
            HashSet<int> chapters;
            if (!editable.TryGetValue(BookOrder[bookNum - 1], out chapters)) return false;
            if (chapters.Count == 0) return true;
            return chapter != 0 && chapters.Contains(chapter);
        }

        public WriteOutcome WriteChapter(int bookNum, int chapter, string usfm, out string error)
        {
            error = null;
            if (!CanEdit(bookNum, chapter)) return WriteOutcome.NoPermission;
            string text = GetBook(bookNum);
            Tuple<int, int> span;
            if (text == null || !ChapterSpans(text).TryGetValue(chapter, out span))
            {
                error = "no such chapter";
                return WriteOutcome.Failed;
            }
            string updated = text.Substring(0, span.Item1) + usfm + text.Substring(span.Item2);
            File.WriteAllText(files[bookNum], updated, encoding);
            Writes.Add(BookOrder[bookNum - 1] + " " + chapter);
            return WriteOutcome.Written;
        }

        /// <summary>Fails when the environment variable NBT_NOHISTORY is 1 (as when Paratext cannot commit).</summary>
        public bool MakeHistoryPoint(string message, out string error)
        {
            if (Environment.GetEnvironmentVariable("NBT_NOHISTORY") == "1")
            {
                error = "test: history point refused";
                return false;
            }
            error = null;
            HistoryPoints.Add(message);
            return true;
        }

        public string BackupDirectory
        {
            get { return backupDirectory; }
        }

        /// <summary>Records the request; with NBT_GOTOLOG set, also appends it to that file at once (for the UI test).</summary>
        public void GoTo(int bookNum, int chapter, int verse)
        {
            string entry = BookOrder[bookNum - 1] + " " + chapter + ":" + verse;
            GoTos.Add(entry);
            string log = Environment.GetEnvironmentVariable("NBT_GOTOLOG");
            if (!string.IsNullOrEmpty(log)) File.AppendAllText(log, entry + "\r\n");
        }
    }
}
