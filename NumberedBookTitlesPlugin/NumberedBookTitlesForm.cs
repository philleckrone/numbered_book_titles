using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NumberedBookTitles
{
    /// <summary>
    /// Lists the numbered books whose header writes the number differently from the usual style
    /// of that field, and changes the ticked lines in the books the user may edit.
    /// </summary>
    public sealed class NumberedBookTitlesForm : Form
    {
        private static readonly Color Red = Color.FromArgb(0x8A, 0x1C, 0x00);
        private static readonly Color OddBack = Color.FromArgb(0xFF, 0xDD, 0xDD);
        private static readonly Color UsualBack = Color.FromArgb(0xEE, 0xEE, 0xEE);

        private readonly IBookSource source;
        private readonly Strings s;
        private FixRunner runner;
        private ScanResult scan;
        private List<TitleIssue> issues = new List<TitleIssue>();
        /// <summary>True while the grids are being filled (no model updates then).</summary>
        private bool filling;

        private readonly Label lblProject = new Label();
        private readonly Label lblIntro = new Label();
        private readonly Label lblSummary = new Label();
        private readonly Label lblWarning = new Label();
        private readonly TabControl tabs = new TabControl();
        private readonly DataGridView grid = new DataGridView();
        private readonly DataGridView matrix = new DataGridView();
        private readonly Label lblCounts = new Label();
        private readonly Button btnTick = new Button();
        private readonly Button btnUntick = new Button();
        private readonly Button btnGoTo = new Button();
        private readonly Button btnFix = new Button();
        private readonly Button btnRescan = new Button();
        private readonly Button btnClose = new Button();
        private readonly Label lblResults = new Label();
        private readonly TextBox txtResults = new TextBox();

        public NumberedBookTitlesForm(IBookSource source, Strings strings)
        {
            this.source = source;
            s = strings;
            BuildLayout();
            Shown += delegate { Rescan(); };
        }

        // ------------------------------------------------------------ layout

        private void BuildLayout()
        {
            Text = s.Title + " " + Strings.ToolVersion + " - " + source.ProjectName;
            Name = "NumberedBookTitlesForm";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9f);
            // As large as the screen allows, up to the size that shows every column.
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1250, area.Width - 40), Math.Min(720, area.Height - 60));
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(10);
            table.ColumnCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 9; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles[4] = new RowStyle(SizeType.Percent, 72f);
            table.RowStyles[8] = new RowStyle(SizeType.Percent, 28f);

            lblProject.AutoSize = true;
            lblProject.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            lblProject.Text = string.Format(s.ProjectLabel, source.ProjectName);
            lblProject.Margin = new Padding(3, 0, 3, 4);

            ConfigureWrappingLabel(lblIntro, table);
            lblIntro.Text = s.Intro;

            ConfigureWrappingLabel(lblSummary, table);
            lblSummary.Name = "Summary";
            lblSummary.Font = new Font(Font, FontStyle.Bold);
            lblSummary.Margin = new Padding(3, 8, 3, 2);

            ConfigureWrappingLabel(lblWarning, table);
            lblWarning.ForeColor = Red;
            lblWarning.Name = "Warning";
            lblWarning.Visible = false;

            tabs.Dock = DockStyle.Fill;
            tabs.Name = "Tabs";
            var pageIssues = new TabPage(s.TabIssues);
            pageIssues.Name = "IssuesPage";
            pageIssues.Padding = new Padding(3);
            var pageAll = new TabPage(s.TabAll);
            pageAll.Name = "AllPage";
            pageAll.Padding = new Padding(3);
            BuildGrid();
            pageIssues.Controls.Add(grid);
            BuildMatrix();
            var hint = new Label();
            hint.Dock = DockStyle.Top;
            hint.AutoSize = true;
            hint.ForeColor = SystemColors.GrayText;
            hint.Text = s.AllHint;
            hint.Padding = new Padding(0, 2, 0, 4);
            pageAll.Controls.Add(matrix);
            pageAll.Controls.Add(hint);
            tabs.TabPages.Add(pageIssues);
            tabs.TabPages.Add(pageAll);

            lblCounts.AutoSize = true;
            lblCounts.Name = "Counts";
            lblCounts.Margin = new Padding(3, 4, 3, 2);

            var buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 6, 0, 6);
            SetupButton(btnGoTo, s.GoToButton, "GoTo");
            SetupButton(btnTick, s.SelectAll, "TickAll");
            SetupButton(btnUntick, s.ClearAll, "UntickAll");
            SetupButton(btnFix, string.Format(s.FixButton, 0), "Fix");
            btnFix.Font = new Font(Font, FontStyle.Bold);
            SetupButton(btnRescan, s.Rescan, "Rescan");
            SetupButton(btnClose, s.Close, "Close");
            buttons.Controls.Add(btnGoTo);
            buttons.Controls.Add(btnTick);
            buttons.Controls.Add(btnUntick);
            buttons.Controls.Add(btnFix);
            buttons.Controls.Add(btnRescan);
            buttons.Controls.Add(btnClose);
            btnGoTo.Click += delegate { GoToSelected(); };
            btnTick.Click += delegate { TickAll(true); };
            btnUntick.Click += delegate { TickAll(false); };
            btnFix.Click += delegate { FixTicked(); };
            btnRescan.Click += delegate { Rescan(); };
            btnClose.Click += delegate { Close(); };

            ConfigureWrappingLabel(lblResults, table);
            lblResults.Font = new Font(Font, FontStyle.Bold);
            lblResults.Text = s.ResultsLabel;
            lblResults.Margin = new Padding(3, 4, 3, 2);

            txtResults.Multiline = true;
            txtResults.ReadOnly = true;
            txtResults.ScrollBars = ScrollBars.Both;
            txtResults.WordWrap = false;
            txtResults.Dock = DockStyle.Fill;
            txtResults.Name = "Results";
            txtResults.AccessibleName = s.ResultsLabel.Split(':')[0];
            txtResults.BackColor = SystemColors.Window;

            table.Controls.Add(lblProject, 0, 0);
            table.Controls.Add(lblIntro, 0, 1);
            table.Controls.Add(lblSummary, 0, 2);
            table.Controls.Add(lblWarning, 0, 3);
            table.Controls.Add(tabs, 0, 4);
            table.Controls.Add(lblCounts, 0, 5);
            table.Controls.Add(buttons, 0, 6);
            table.Controls.Add(lblResults, 0, 7);
            table.Controls.Add(txtResults, 0, 8);
            Controls.Add(table);
        }

        private void ConfigureWrappingLabel(Label label, TableLayoutPanel table)
        {
            label.AutoSize = true;
            label.Margin = new Padding(3, 2, 3, 2);
            // Wraps at the window's width, so a long text never widens the layout past the window.
            // Set again once the window is shown, because auto-scaling also scales MaximumSize.
            label.MaximumSize = new Size(Math.Max(200, ClientSize.Width - 50), 0);
            EventHandler fit = delegate
            {
                label.MaximumSize = new Size(Math.Max(200, table.ClientSize.Width - table.Padding.Horizontal - 10), 0);
            };
            table.SizeChanged += fit;
            Load += fit;
        }

        private static void SetupButton(Button b, string text, string name)
        {
            b.Text = text;
            b.Name = name;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(8, 2, 8, 2);
            b.Margin = new Padding(0, 0, 8, 0);
        }

        private static void SetupGrid(DataGridView g, string name)
        {
            g.Name = name;
            g.Dock = DockStyle.Fill;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible = false;
            g.MultiSelect = false;
            g.BackgroundColor = SystemColors.Window;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            g.DataError += delegate(object sender, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; };
        }

        private void BuildGrid()
        {
            SetupGrid(grid, "Differences");
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            var check = new DataGridViewCheckBoxColumn();
            check.Name = "Fix";
            check.HeaderText = s.ColFix;
            check.Width = 62;
            check.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(check);
            AddTextColumn(grid, "Book", s.ColBook, 45, true);
            AddTextColumn(grid, "Field", s.ColField, 185, true);
            AddTextColumn(grid, "Line", s.ColLine, 45, true);
            AddTextColumn(grid, "Now", s.ColNow, 190, true);
            AddTextColumn(grid, "Style", s.ColStyle, 200, true);
            AddTextColumn(grid, "Usual", s.ColUsual, 200, true);
            AddTextColumn(grid, "ChangeTo", s.ColChangeTo, 200, false);
            AddTextColumn(grid, "Permission", s.ColPermission, 80, true);
            // Long texts (the usual style with its counts) are in full in the tooltip.
            grid.ShowCellToolTips = true;
            grid.Columns["ChangeTo"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns["ChangeTo"].DefaultCellStyle.BackColor = Color.FromArgb(0xFF, 0xFF, 0xF0);
            grid.Columns["Permission"].DefaultCellStyle.ForeColor = Red;

            grid.CellBeginEdit += delegate(object sender, DataGridViewCellCancelEventArgs e)
            {
                if (e.RowIndex < 0 || e.RowIndex >= issues.Count) return;
                TitleIssue issue = issues[e.RowIndex];
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col == "Fix" && !CanTick(issue)) e.Cancel = true;
                if (col == "ChangeTo" && !CanEditText(issue)) e.Cancel = true;
            };
            // The placeholder text is never put into the editing box.
            grid.EditingControlShowing += delegate(object sender, DataGridViewEditingControlShowingEventArgs e)
            {
                int row = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1;
                if (row >= 0 && row < issues.Count && grid.Columns[grid.CurrentCell.ColumnIndex].Name == "ChangeTo")
                    e.Control.Text = issues[row].NewValue ?? "";
            };
            // A click on a tick box ticks it at once; a click on "Change to" starts editing.
            grid.CellClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0 || e.RowIndex >= issues.Count) return;
                if (grid.Columns[e.ColumnIndex].Name == "ChangeTo" && CanEditText(issues[e.RowIndex])) grid.BeginEdit(true);
            };
            grid.CurrentCellDirtyStateChanged += delegate
            {
                if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += OnCellValueChanged;
            grid.CellFormatting += OnCellFormatting;
            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0 || e.RowIndex >= issues.Count) return;
                string col = grid.Columns[e.ColumnIndex].Name;
                if (col == "Fix" || col == "ChangeTo") return;
                GoTo(issues[e.RowIndex].Book);
            };
        }

        private void BuildMatrix()
        {
            SetupGrid(matrix, "AllBooks");
            matrix.ReadOnly = true;
            matrix.SelectionMode = DataGridViewSelectionMode.CellSelect;
            matrix.ShowCellToolTips = true;
            matrix.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                var book = matrix.Rows[e.RowIndex].Tag as BookId;
                if (book != null) GoTo(book);
            };
        }

        private static void AddTextColumn(DataGridView g, string name, string header, int width, bool readOnly)
        {
            var col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = header;
            col.Width = width;
            col.ReadOnly = readOnly;
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            g.Columns.Add(col);
        }

        /// <summary>The book of the current row on the visible tab.</summary>
        private void GoToSelected()
        {
            if (tabs.SelectedIndex == 0)
            {
                if (grid.CurrentCell != null && grid.CurrentCell.RowIndex < issues.Count)
                    GoTo(issues[grid.CurrentCell.RowIndex].Book);
            }
            else if (matrix.CurrentCell != null)
            {
                var book = matrix.Rows[matrix.CurrentCell.RowIndex].Tag as BookId;
                if (book != null) GoTo(book);
            }
        }

        private void GoTo(BookId book)
        {
            // Verse 0 of chapter 1 is the book's header and introduction.
            try { source.GoTo(book.Number, 1, 0); }
            catch (Exception) { }
        }

        // ------------------------------------------------------------ rows

        private bool CanEditText(TitleIssue issue)
        {
            return issue.Target != null && runner != null && runner.CanEdit(issue.Book.Number);
        }

        private bool CanTick(TitleIssue issue)
        {
            return issue.CanFix && CanEditText(issue);
        }

        private string UsualText(FieldResult r)
        {
            if (!r.Usual.HasValue)
            {
                string counts = string.Join(", ", r.Counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                    .Select(kv => kv.Value + " \u00D7 " + s.StyleName(kv.Key)).ToArray());
                return string.Format(s.NoUsual, counts);
            }
            return string.Format(s.UsualCount, s.StyleName(r.Usual.Value), r.Counts[r.Usual.Value], r.Total);
        }

        private void FillGrid()
        {
            filling = true;
            grid.EndEdit();
            grid.Rows.Clear();
            foreach (TitleIssue i in issues)
            {
                int row = grid.Rows.Add();
                DataGridViewCellCollection c = grid.Rows[row].Cells;
                c["Fix"].Value = i.Selected;
                c["Book"].Value = i.Book.Code;
                c["Field"].Value = s.FieldName(i.Field);
                c["Line"].Value = i.Target != null ? "\\" + i.Target.Marker : "";
                c["Now"].Value = i.Target != null ? i.Target.Value : i.Text;
                c["Style"].Value = s.StyleName(i.Style) + "  (" + i.Token + ")";
                c["Usual"].Value = UsualText(i.Result);
                c["ChangeTo"].Value = i.NewValue ?? "";
                c["Permission"].Value = i.Target != null && !runner.CanEdit(i.Book.Number) ? s.CannotEdit : "";
            }
            filling = false;
        }

        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (filling || e.RowIndex < 0 || e.RowIndex >= issues.Count) return;
            TitleIssue issue = issues[e.RowIndex];
            DataGridViewCell cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            switch (grid.Columns[e.ColumnIndex].Name)
            {
                case "Fix":
                    issue.Selected = CanTick(issue) && cell.Value is bool && (bool)cell.Value;
                    break;
                case "ChangeTo":
                    string text = cell.Value as string;
                    issue.NewValue = string.IsNullOrEmpty(text) ? null : text;
                    // Typing a new text ticks the row; clearing it unticks it.
                    issue.Selected = CanTick(issue);
                    filling = true;
                    grid.Rows[e.RowIndex].Cells["Fix"].Value = issue.Selected;
                    filling = false;
                    break;
            }
            grid.InvalidateRow(e.RowIndex);
            UpdateCounts();
        }

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= issues.Count) return;
            TitleIssue issue = issues[e.RowIndex];
            string col = grid.Columns[e.ColumnIndex].Name;
            if (col == "ChangeTo")
            {
                if (issue.Target == null)
                {
                    e.Value = s.BookNamesFix;
                    e.CellStyle.ForeColor = SystemColors.GrayText;
                    e.CellStyle.BackColor = SystemColors.Window;
                    e.FormattingApplied = true;
                }
                else if (string.IsNullOrEmpty(issue.NewValue))
                {
                    e.Value = CanEditText(issue) ? s.NoSuggestion : "";
                    e.CellStyle.ForeColor = SystemColors.GrayText;
                    e.FormattingApplied = true;
                }
                else if (issue.NewValue.IndexOf('\u00A0') >= 0)
                {
                    // Show a non-breaking space, which otherwise looks like a space.
                    e.Value = issue.NewValue.Replace('\u00A0', '\u00B7');
                    e.FormattingApplied = true;
                }
                return;
            }
            if (col == "Now" && e.Value is string && ((string)e.Value).IndexOf('\u00A0') >= 0)
            {
                e.Value = ((string)e.Value).Replace('\u00A0', '\u00B7');
                e.FormattingApplied = true;
            }
            if (col != "Permission" && issue.Target != null && !runner.CanEdit(issue.Book.Number))
                e.CellStyle.ForeColor = SystemColors.GrayText;
        }

        private void FillMatrix()
        {
            matrix.Rows.Clear();
            matrix.Columns.Clear();
            if (scan == null) return;
            List<TitleField> fields = TitleScanner.Fields.Where(f => scan.Books.Any(b => !string.IsNullOrEmpty(b.Value(f)))).ToList();
            AddTextColumn(matrix, "Book", s.ColBook, 60, true);
            foreach (TitleField f in fields)
                AddTextColumn(matrix, f.ToString(), s.FieldName(f), 170, true);

            int usualRow = matrix.Rows.Add();
            DataGridViewRow ur = matrix.Rows[usualRow];
            ur.DefaultCellStyle.BackColor = UsualBack;
            ur.DefaultCellStyle.Font = new Font(Font, FontStyle.Italic);
            ur.Cells[0].Value = s.UsualRow;
            for (int i = 0; i < fields.Count; i++)
            {
                FieldResult r = scan.ResultFor(fields[i]);
                string text;
                if (r != null)
                    text = r.Usual.HasValue ? s.StyleName(r.Usual.Value) : s.NoUsualShort;
                else
                {
                    // No differences: every book uses the same style.
                    BookTitles any = scan.Books.FirstOrDefault(b => !string.IsNullOrEmpty(b.Value(fields[i])));
                    text = any != null ? s.StyleName(TitleScanner.StyleOf(any.Value(fields[i]))) : "";
                }
                ur.Cells[i + 1].Value = text;
                ur.Cells[i + 1].ToolTipText = r != null ? UsualText(r) : text;
            }

            foreach (BookTitles b in scan.Books)
            {
                int row = matrix.Rows.Add();
                DataGridViewRow mr = matrix.Rows[row];
                mr.Tag = b.Book;
                mr.Cells[0].Value = b.Book.Code;
                mr.Cells[0].Style.Font = new Font(Font, FontStyle.Bold);
                for (int i = 0; i < fields.Count; i++)
                {
                    DataGridViewCell cell = mr.Cells[i + 1];
                    string v = b.Value(fields[i]);
                    if (string.IsNullOrEmpty(v))
                    {
                        cell.Value = s.Missing;
                        cell.Style.ForeColor = SystemColors.GrayText;
                        continue;
                    }
                    cell.Value = v.Replace('\u00A0', '\u00B7');
                    NumberStyle style = TitleScanner.StyleOf(v);
                    cell.ToolTipText = s.StyleName(style);
                    FieldResult r = scan.ResultFor(fields[i]);
                    if (r != null && r.Outliers.ContainsKey(b.Book.Number))
                        cell.Style.BackColor = OddBack;
                }
            }
        }

        // ------------------------------------------------------------ scanning

        private static string Key(TitleIssue i)
        {
            return i.Book.Number + ":" + (int)i.Field;
        }

        private void Rescan()
        {
            Cursor old = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            lblCounts.Text = string.Format(s.Scanning, source.ProjectName);
            lblCounts.Refresh();
            try
            {
                // Ticks and typed texts survive a rescan for rows still there with the same line text.
                var kept = issues.Where(i => i.Target != null)
                    .ToDictionary(i => Key(i) + "|" + i.Target.Value, i => i);
                runner = new FixRunner(source);
                runner.NotSavedText = s.NotSaved;
                scan = TitleScanner.Scan(source);
                issues = scan.Issues;
                foreach (TitleIssue i in issues)
                {
                    TitleIssue before;
                    if (i.Target != null && kept.TryGetValue(Key(i) + "|" + i.Target.Value, out before))
                    {
                        i.NewValue = before.NewValue;
                        i.Selected = before.Selected && CanTick(i);
                    }
                }
                lblSummary.Text = string.Format(s.Summary, scan.Books.Count, scan.IssueCount);
                FillGrid();
                FillMatrix();
                UpdateWarning();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(s.Error, ex.Message), s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor.Current = old;
                UpdateCounts();
            }
        }

        private void UpdateWarning()
        {
            var lines = new List<string>();
            if (scan.Books.Count < 2) lines.Add(s.NoNumberedBooks);
            else if (issues.Count == 0) lines.Add(s.NoProblems);
            if (!scan.BookNamesFound) lines.Add(s.BookNamesMissing);
            if (issues.Any(i => i.Target != null && !runner.CanEdit(i.Book.Number))) lines.Add(s.SomeNotEditable);
            lblWarning.Text = string.Join("\n", lines.ToArray());
            lblWarning.ForeColor = issues.Count == 0 ? SystemColors.ControlText : Red;
            lblWarning.Visible = lines.Count > 0;
        }

        private void UpdateCounts()
        {
            int ticked = issues.Count(i => i.Selected && CanTick(i));
            lblCounts.Text = string.Format(s.Counts, issues.Count, issues.Count(CanEditText), ticked);
            btnFix.Text = string.Format(s.FixButton, ticked);
            btnFix.Enabled = ticked > 0;
            btnTick.Enabled = btnUntick.Enabled = issues.Any(CanTick);
        }

        private void TickAll(bool value)
        {
            grid.EndEdit();
            filling = true;
            for (int r = 0; r < issues.Count; r++)
            {
                issues[r].Selected = value && CanTick(issues[r]);
                grid.Rows[r].Cells["Fix"].Value = issues[r].Selected;
            }
            filling = false;
            UpdateCounts();
        }

        // ------------------------------------------------------------ fixing

        private void FixTicked()
        {
            grid.EndEdit();
            List<TitleIssue> chosen = issues.Where(i => i.Selected && CanTick(i)).ToList();
            if (chosen.Count == 0)
            {
                MessageBox.Show(this, s.NothingSelected, s.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string question = string.Format(s.Confirm, chosen.Count, runner.BooksToChange(chosen), source.BackupDirectory);
            if (MessageBox.Show(this, question, s.ConfirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Cursor old = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            FixReport report;
            try
            {
                report = runner.Run(chosen, string.Format(s.HistoryMessage, chosen.Count));
            }
            catch (Exception ex)
            {
                Cursor.Current = old;
                MessageBox.Show(this, string.Format(s.Error, ex.Message), s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Cursor.Current = old;
            if (report.Stopped)
            {
                // No history point, so nothing was changed: say so and leave the list as it is.
                string stopped = string.Format(s.HistoryStopped, report.HistoryError);
                txtResults.Text = stopped.Replace("\r\n", "\n").Replace("\n", "\r\n");
                MessageBox.Show(this, stopped, s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtResults.Text = FormatReport(report);
            Rescan();
        }

        private string FormatReport(FixReport report)
        {
            var sb = new StringBuilder();
            foreach (var kv in report.Fixed)
                sb.AppendLine(string.Format(s.ResFixed, kv.Key, kv.Value));
            if (report.Fixed.Count > 0)
                sb.AppendLine(string.Format(s.ResTotal, report.Total, report.BooksChanged));
            else
                sb.AppendLine(s.ResNothing);
            if (report.NoPermission.Count > 0)
                sb.AppendLine(string.Format(s.ResNoPermission, string.Join(", ", report.NoPermission.ToArray())));
            if (report.Busy.Count > 0)
                sb.AppendLine(string.Format(s.ResBusy, string.Join(", ", report.Busy.ToArray())));
            if (report.Changed.Count > 0)
                sb.AppendLine(string.Format(s.ResChanged, string.Join(", ", report.Changed.ToArray())));
            foreach (string f in report.Failed)
                sb.AppendLine(string.Format(s.ResFailed, f));
            if (report.Backups.Count > 0)
                sb.AppendLine(string.Format(s.ResBackups, source.BackupDirectory));
            return sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }
}
