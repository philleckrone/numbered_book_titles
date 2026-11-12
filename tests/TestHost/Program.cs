using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NumberedBookTitles;

namespace NumberedBookTitlesTests
{
    /// <summary>
    /// Test host for the Numbered Book Titles plugin code, run on a folder of SFM files (the folder
    /// is the "project folder", with its BookNames.xml):
    ///   TestHost scan FOLDER                     the rows of the CMS report's "Numbered books" table
    ///   TestHost suggest FOLDER                  book, field, line, now and suggestion for each row
    ///   TestHost fix FOLDER EDITSPEC BACKUPDIR   changes every row that has a suggestion, then scans again
    ///   TestHost form FOLDER EDITSPEC LANG BACKUPDIR
    /// EDITSPEC: ALL, NONE, or e.g. 1TH,1PE (books the user may edit).
    /// </summary>
    internal static class Program
    {
        /// <summary>The CMS check's style keys, used to order the counts as it does.</summary>
        private static readonly string[] PyKeys =
        {
            "digit", "digit_nbsp", "digit_nospace", "digit_period", "digit_suffix",
            "roman", "after_digit", "after_roman", "word",
        };

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: TestHost scan|suggest|fix|form FOLDER ...");
                return 2;
            }
            // A WinExe has no console to set an encoding on: write UTF-8 to whatever stdout is.
            var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
            stdout.AutoFlush = true;
            Console.SetOut(stdout);
            string folder = args[1];
            switch (args[0])
            {
                case "scan":
                {
                    ScanResult r = TitleScanner.Scan(new FolderBookSource(folder, "TEST", "ALL", null));
                    PrintCmsRows(r);
                    return 0;
                }
                case "suggest":
                {
                    ScanResult r = TitleScanner.Scan(new FolderBookSource(folder, "TEST", "ALL", null));
                    foreach (TitleIssue i in r.Issues)
                        Console.WriteLine(string.Join("\t", new[] {
                            i.Book.Code, i.Field.ToString(), i.Target != null ? i.Target.Marker : "-",
                            Show(i.Target != null ? i.Target.Value : i.Text), Show(i.Suggestion ?? "(none)") }));
                    return 0;
                }
                case "fix":
                {
                    var source = new FolderBookSource(folder, "TEST", args[2], args[3]);
                    ScanResult r = TitleScanner.Scan(source);
                    var runner = new FixRunner(source);
                    List<TitleIssue> chosen = r.Issues.Where(i => i.CanFix).ToList();
                    FixReport report = runner.Run(chosen, "test history point");
                    Console.WriteLine("CHOSEN {0}", chosen.Count);
                    foreach (var kv in report.Fixed)
                        Console.WriteLine("FIXED {0} {1}", kv.Key, kv.Value);
                    Console.WriteLine("TOTAL {0} BOOKS {1}", report.Total, report.BooksChanged);
                    Console.WriteLine("STOPPED " + report.Stopped + " " + (report.HistoryError ?? ""));
                    Console.WriteLine("NOPERMISSION " + string.Join(", ", report.NoPermission.ToArray()));
                    Console.WriteLine("BUSY " + string.Join(", ", report.Busy.ToArray()));
                    Console.WriteLine("CHANGED " + string.Join(", ", report.Changed.ToArray()));
                    Console.WriteLine("FAILED " + string.Join(" | ", report.Failed.ToArray()));
                    Console.WriteLine("HISTORY " + string.Join(" | ", source.HistoryPoints.ToArray()));
                    Console.WriteLine("WRITES " + string.Join(", ", source.Writes.ToArray()));
                    Console.WriteLine("BACKUPS " + string.Join(", ", report.Backups.Select(p => Path.GetFileName(p).Substring(0, 11)).ToArray()));
                    Console.WriteLine("AFTER");
                    PrintCmsRows(TitleScanner.Scan(new FolderBookSource(folder, "TEST", "ALL", null)));
                    return 0;
                }
                case "form":
                {
                    var source = new FolderBookSource(folder, Path.GetFileName(Path.GetFullPath(folder).TrimEnd('\\')), args[2], args[4]);
                    Application.EnableVisualStyles();
                    var form = new NumberedBookTitlesForm(source, Strings.For(args[3]));
                    // So the UI test can check that double-clicking a row asks Paratext to go to the book.
                    form.FormClosed += delegate { Console.WriteLine("GOTO " + string.Join(", ", source.GoTos.ToArray())); };
                    Application.Run(form);
                    return 0;
                }
            }
            Console.Error.WriteLine("unknown mode " + args[0]);
            return 2;
        }

        private static string Show(string s)
        {
            return s.Replace('\u00A0', '~');
        }

        private static string He(string s)
        {
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>The summary line and the table rows exactly as toc_examiner.py writes them.</summary>
        private static void PrintCmsRows(ScanResult r)
        {
            Strings s = Strings.English();
            Console.WriteLine("<p>{0} numbered book(s), {1} inconsistency(ies) found.</p>", r.Books.Count, r.IssueCount);
            foreach (TitleIssue i in r.Issues)
            {
                FieldResult fr = i.Result;
                string usual;
                if (fr.Usual.HasValue)
                    usual = string.Format("{0} ({1} of {2} books)", He(s.StyleName(fr.Usual.Value)), fr.Counts[fr.Usual.Value], fr.Total);
                else
                    usual = "No usual style: " + He(string.Join(", ", fr.Counts
                        .OrderByDescending(kv => kv.Value).ThenBy(kv => PyKeys[(int)kv.Key], StringComparer.Ordinal)
                        .Select(kv => kv.Value + " \u00D7 " + s.StyleName(kv.Key)).ToArray()));
                Console.WriteLine("<tr><td class=\"bk\">{0}</td><td>{1}</td><td>&quot;{2}&quot;</td><td>{3} (&quot;{4}&quot;)</td><td>{5}</td></tr>",
                    He(i.Book.Code), He(s.FieldName(i.Field)), He(i.Text), He(s.StyleName(i.Style)), He(i.Token), usual);
            }
        }
    }
}
