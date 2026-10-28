using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace HatchGrouper
{
    public partial class MainWindow : Window
    {
        private List<GroupRow> _rows = new();
        private List<NicheDetailRow> _nicheRows = new();
        private List<NicheGroup> _lastGroups = new();
        private Settings _lastSettings = new();
        private LengthUnit _inputUnit = LengthUnit.Cm;
        private LengthUnit _paramUnit = LengthUnit.Mm;
        private LengthUnit _displayUnit = LengthUnit.Cm;
        private BoxRounding _boxRounding = BoxRounding.Nearest;
        private bool _separateTolerance = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplySettings(SettingsStore.Load());
            InputBox.Text = ExampleFor(_inputUnit);
            Closing += (_, _) => SaveSettingsQuietly();

            // Auto-save settings whenever user finishes editing any parameter field
            RoutedEventHandler onParamLostFocus = (_, _) => SaveSettingsQuietly();
            GapBox.LostFocus += onParamLostFocus;
            TrimBox.LostFocus += onParamLostFocus;
            TrimDiffWBox.LostFocus += onParamLostFocus;
            TrimDiffHBox.LostFocus += onParamLostFocus;
            LapBox.LostFocus += onParamLostFocus;
            HatchDiffWBox.LostFocus += onParamLostFocus;
            HatchDiffHBox.LostFocus += onParamLostFocus;
            DepthBox.LostFocus += onParamLostFocus;
            EarBox.LostFocus += onParamLostFocus;
            GapHBox.LostFocus += onParamLostFocus;
            TrimHBox.LostFocus += onParamLostFocus;
            LapHBox.LostFocus += onParamLostFocus;
        }

        // Example shown on start so that users see the expected input format (sizes in cm).
        // @E1 / @E2 are replaced with the localised entrance header.
        private const string ExampleTemplate =
            "@E1\r\n" +
            "1)\t84 x 89\r\n2)\t86 x 89\r\n3)\t73 x 104\r\n4)\t76 x 107\r\n5)\t96 x 105\r\n6)\t96 x 112\r\n" +
            "7)\t91 x 108\r\n8)\t98 x 107\r\n9)\t98 x 108\r\n10)\t87 x105\r\n11)\t89 x 103\r\n12)\t85 x 89\r\n" +
            "13)\t87 x 90\r\n14)\t92 x 94\r\n15)\t90 x 91\r\n16)\t91 x 85\r\n17)\t92 x 85\r\n18)\t87 x 90\r\n" +
            "@E2\r\n" +
            "1)\t100 x 105\r\n2)\t96 x 90\r\n3)\t92 x 95\r\n4)\t93 x 79\r\n5)\t87 x 78\r\n6)\t91 x 80\r\n" +
            "7)\t93 x 81\r\n8)\t95 x 79\r\n9)\t84 x 86\r\n10)\t85 x 87\r\n11)\t94 x 105\r\n12)\t106 x 100\r\n" +
            "13)\t100 x 103\r\n14)\t81 x 107\r\n15)\t85 x 104\r\n16)\t93 x 101\r\n17)\t95 x 98\r\n18)\t83 x 87";

        /// <summary>The example in the requested unit (the same sizes, x10 for millimetres) and the current language.</summary>
        private static string ExampleFor(LengthUnit unit)
        {
            var text = ExampleTemplate
                .Replace("@E1", Loc.T("example.entrance", 1))
                .Replace("@E2", Loc.T("example.entrance", 2));
            return unit == LengthUnit.Cm
                ? text
                : Regex.Replace(text, @"(\d+)\s*x\s*(\d+)",
                    m => $"{int.Parse(m.Groups[1].Value) * 10} x {int.Parse(m.Groups[2].Value) * 10}");
        }

        // ---- Buttons ----

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = ReadSettings();
                var skipped = new List<string>();
                var niches = NicheParser.Parse(InputBox.Text, settings.InputUnit, skipped);
                if (niches.Count == 0)
                    throw new UserException(Loc.T("err.noniches", UnitHelper.Name(settings.InputUnit)));

                var groups = Grouper.Build(niches, settings);
                CheckGroups(groups, settings);
                SaveSettingsQuietly(settings);

                _lastGroups = groups;
                _lastSettings = settings;

                var oldNotesByBox = _rows?.Where(r => !string.IsNullOrWhiteSpace(r.Note))
                                          .ToDictionary(r => r.Box, r => r.Note) ?? new Dictionary<string, string>();
                var oldNotesByNo = _rows?.Where(r => !string.IsNullOrWhiteSpace(r.Note))
                                         .ToDictionary(r => r.No, r => r.Note) ?? new Dictionary<int, string>();

                _rows = groups.Select((g, i) => ToRow(g, i + 1, _displayUnit, settings.BoxRounding)).ToList();
                foreach (var row in _rows)
                {
                    if (oldNotesByBox.TryGetValue(row.Box, out var n)) row.Note = n;
                    else if (oldNotesByNo.TryGetValue(row.No, out var n2)) row.Note = n2;
                    row.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(GroupRow.Note) && s is GroupRow grp && _nicheRows != null)
                        {
                            foreach (var nr in _nicheRows.Where(r => r.GroupNo == grp.No))
                                if (nr.Note != grp.Note) nr.Note = grp.Note;
                        }
                    };
                }
                ResultGrid.ItemsSource = _rows;

                // Build ordered specification list (1.1, 1.2, 1.3... 2.1...)
                var nicheList = new List<NicheDetailRow>();
                for (int i = 0; i < groups.Count; i++)
                {
                    var g = groups[i];
                    var row = _rows[i];
                    foreach (var n in g.Niches)
                    {
                        var detail = new NicheDetailRow
                        {
                            Id = $"({n.Entrance}.{n.NicheNo})",
                            Entrance = n.Entrance,
                            NicheNo = n.NicheNo,
                            Niche = $"{UnitHelper.Format(n.WidthMm, _displayUnit)} × {UnitHelper.Format(n.HeightMm, _displayUnit)}",
                            Box = $"{UnitHelper.Format(g.BoxW, _displayUnit)} × {UnitHelper.Format(g.BoxH, _displayUnit)}",
                            BoxDrawing = row.BoxDrawing,
                            WithTrim = $"{UnitHelper.Format(g.TrimW, _displayUnit)} × {UnitHelper.Format(g.TrimH, _displayUnit)}",
                            Hatch = $"{UnitHelper.Format(g.HatchW, _displayUnit)} × {UnitHelper.Format(g.HatchH, _displayUnit)}",
                            GroupNo = i + 1,
                            GroupName = Loc.T("vis.group.title", i + 1),
                            Note = row.Note ?? "",
                        };
                        detail.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(NicheDetailRow.Note) && s is NicheDetailRow nr)
                            {
                                var grp = _rows?.FirstOrDefault(r => r.No == nr.GroupNo);
                                if (grp != null && grp.Note != nr.Note) grp.Note = nr.Note;
                                if (_nicheRows != null)
                                {
                                    foreach (var sibling in _nicheRows.Where(r => r.GroupNo == nr.GroupNo && r != nr))
                                        if (sibling.Note != nr.Note) sibling.Note = nr.Note;
                                }
                            }
                        };
                        nicheList.Add(detail);
                    }
                }
                _nicheRows = nicheList;
                UpdateNichesGridSorting();
                NichesCountLabel.Text = Loc.T("status.niches.count", _nicheRows.Count);

                var u = UnitHelper.Name(_displayUnit);
                string tolText = settings.ToleranceWMm == settings.ToleranceHMm
                    ? UnitHelper.Format(settings.ToleranceWMm, _displayUnit)
                    : $"{UnitHelper.Format(settings.ToleranceWMm, _displayUnit)} × {UnitHelper.Format(settings.ToleranceHMm, _displayUnit)}";
                string maxGapText = settings.MaxGapWMm == settings.MaxGapHMm
                    ? UnitHelper.Format(settings.MaxGapWMm, _displayUnit)
                    : $"{UnitHelper.Format(settings.MaxGapWMm, _displayUnit)} × {UnitHelper.Format(settings.MaxGapHMm, _displayUnit)}";

                var status = new StringBuilder(Loc.T("status.result",
                    niches.Count, groups.Count, tolText, u, maxGapText));
                if (skipped.Count > 0)
                    status.Append(Loc.T("status.skipped", skipped.Count, skipped[0]));
                if (niches.Any(n => Math.Min(n.WidthMm, n.HeightMm) < 200 || Math.Max(n.WidthMm, n.HeightMm) > 3000))
                    status.Append(Loc.T("status.unusual", UnitHelper.Name(settings.InputUnit)));
                StatusText.Text = status.ToString();
            }
            catch (Exception ex)
            {
                Fail(Loc.T("err.group"), ex);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = Loc.T("filter.project"), DefaultExt = ".hatch.json" };
                if (dlg.ShowDialog() != true) return;

                var notes = _rows?.Where(r => !string.IsNullOrWhiteSpace(r.Note))
                                 .ToDictionary(r => r.Box, r => r.Note) ?? new Dictionary<string, string>();
                var data = new ProjectData { InputText = InputBox.Text, Settings = ReadSettings(), Notes = notes };
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                File.WriteAllText(dlg.FileName, JsonSerializer.Serialize(data, options), Encoding.UTF8);
                StatusText.Text = Loc.T("status.saved", dlg.FileName);
            }
            catch (Exception ex)
            {
                Fail(Loc.T("err.save"), ex);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = Loc.T("filter.project") };
                if (dlg.ShowDialog() != true) return;

                var data = JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(dlg.FileName, Encoding.UTF8));
                if (data == null || data.InputText == null)
                    throw new UserException(Loc.T("err.emptyfile"));
                var loaded = data.Settings ?? new Settings();
                loaded.Language = Loc.Current; // the language is an app preference: a project must not change it
                InputBox.Text = data.InputText;
                ApplySettings(loaded);
                Group_Click(sender, e);
                if (data.Notes != null && data.Notes.Count > 0 && _rows != null)
                {
                    foreach (var row in _rows)
                    {
                        if (data.Notes.TryGetValue(row.Box, out var note))
                            row.Note = note;
                    }
                    ResultGrid.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                Fail(Loc.T("err.open"), ex);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rows.Count == 0) { StatusText.Text = Loc.T("status.pressgroup"); return; }
                var dlg = new SaveFileDialog { Filter = Loc.T("filter.csv"), DefaultExt = ".csv" };
                if (dlg.ShowDialog() != true) return;

                // ';' separator and UTF-8 BOM so that Ukrainian Excel opens the file correctly
                var sb = new StringBuilder();
                sb.AppendLine(Loc.T("csv.header", UnitHelper.Name(_displayUnit)));
                foreach (var r in _rows)
                {
                    sb.AppendLine(string.Join(";", new[]
                    {
                        r.No.ToString(), r.Count.ToString(), Csv(r.Niche), Csv(r.Box), Csv(r.BoxDrawing),
                        Csv(r.WithTrim), Csv(r.Hatch), Csv(r.MaxGap), Csv(r.Lap),
                        Csv(r.Note), Csv(r.Composition.Replace("\n", " / ")),
                    }));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                StatusText.Text = Loc.T("status.exported", dlg.FileName);
            }
            catch (Exception ex)
            {
                Fail(Loc.T("err.export"), ex);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            // The help only shows numbers: invalid fields must not block it, so fall back to the defaults
            Settings settings;
            try { settings = ReadSettings(); }
            catch (UserException) { settings = new Settings(); }
            new HelpWindow(settings) { Owner = this }.ShowDialog();
        }

        private void Visualizer_Click(object sender, RoutedEventArgs e)
        {
            if (_lastGroups.Count == 0)
            {
                Group_Click(sender, e);
            }

            if (_lastGroups.Count == 0)
            {
                MessageBox.Show(this, Loc.T("vis.empty"), Loc.T("vis.title"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new VisualizerWindow(_lastGroups, _lastSettings) { Owner = this }.Show();
        }

        private void Extremes_Click(object sender, RoutedEventArgs e)
        {
            if (_lastGroups.Count == 0)
            {
                Group_Click(sender, e);
            }

            if (_lastGroups.Count == 0)
            {
                MessageBox.Show(this, Loc.T("ext.empty"), Loc.T("ext.title"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int selectedIndex = ResultGrid.SelectedIndex >= 0 ? ResultGrid.SelectedIndex : 0;
            new ExtremeCasesWindow(_lastGroups, _lastSettings, selectedIndex) { Owner = this }.Show();
        }

        private void NicheList_Click(object sender, RoutedEventArgs e)
        {
            if (_rows.Count == 0)
            {
                Group_Click(sender, e);
            }

            if (_rows.Count == 0)
            {
                StatusText.Text = Loc.T("status.pressgroup");
                return;
            }

            ViewsTabControl.SelectedItem = NichesTabItem;
        }

        private void CopyNicheList_Click(object sender, RoutedEventArgs e)
        {
            var items = GetSortedNicheRows();
            if (items.Count == 0) return;
            var u = UnitHelper.Name(_displayUnit);
            var lines = items.Select(n =>
                string.IsNullOrWhiteSpace(n.Note)
                    ? $"{n.Id} {Loc.T("niche.word")} {n.Niche} {u} — {n.BoxDrawing}"
                    : $"{n.Id} {Loc.T("niche.word")} {n.Niche} {u} — {n.BoxDrawing} [{n.Note}]");
            Clipboard.SetText(string.Join(Environment.NewLine, lines));
            StatusText.Text = Loc.T("status.niches.copied", items.Count);
        }

        private void ExportNiches_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var items = GetSortedNicheRows();
                if (items.Count == 0) return;
                var dlg = new SaveFileDialog
                {
                    Filter = Loc.T("filter.csv"),
                    DefaultExt = ".csv",
                    FileName = "niches_specification.csv"
                };
                if (dlg.ShowDialog() != true) return;

                var u = UnitHelper.Name(_displayUnit);
                var sb = new StringBuilder();
                sb.AppendLine(Loc.T("csv.niches.header", u));
                foreach (var r in items)
                {
                    sb.AppendLine(string.Join(";", new[]
                    {
                        Csv(r.Id),
                        Csv(r.Niche),
                        Csv(r.Box),
                        Csv(r.BoxDrawing),
                        Csv(r.WithTrim),
                        Csv(r.Hatch),
                        Csv(r.GroupName),
                        Csv(r.Note)
                    }));
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                StatusText.Text = Loc.T("status.exported", dlg.FileName);
            }
            catch (Exception ex)
            {
                Fail(Loc.T("err.export"), ex);
            }
        }

        private void Labels_Click(object sender, RoutedEventArgs e)
        {
            if (_nicheRows == null || _nicheRows.Count == 0)
            {
                Group_Click(sender, e);
            }

            if (_nicheRows == null || _nicheRows.Count == 0)
            {
                StatusText.Text = Loc.T("status.pressgroup");
                return;
            }

            var items = GetSortedNicheRows();
            new LabelsWindow(items, _displayUnit) { Owner = this }.ShowDialog();
        }

        private void GroupByBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateNichesGridSorting();
            SaveSettingsQuietly();
        }

        private void UpdateNichesGridSorting()
        {
            if (_nicheRows == null || _nicheRows.Count == 0) return;
            NichesGrid.ItemsSource = GetSortedNicheRows();
        }

        private List<NicheDetailRow> GetSortedNicheRows()
        {
            if (_nicheRows == null || _nicheRows.Count == 0) return new List<NicheDetailRow>();
            bool byBox = GroupByBoxCheckBox?.IsChecked == true;
            return byBox
                ? _nicheRows.OrderBy(n => n.GroupNo).ThenBy(n => n.Entrance).ThenBy(n => n.NicheNo).ToList()
                : _nicheRows.OrderBy(n => n.Entrance).ThenBy(n => n.NicheNo).ToList();
        }

        private void ResultGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultGrid.CurrentColumn != null && ResultGrid.CurrentColumn.Header?.ToString() == Loc.T("col.note"))
                return;

            if (ResultGrid.SelectedItem is GroupRow row && _lastGroups.Count > 0)
            {
                int groupIdx = Math.Clamp(row.No - 1, 0, _lastGroups.Count - 1);
                new ExtremeCasesWindow(_lastGroups, _lastSettings, groupIdx) { Owner = this }.Show();
            }
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not DataGrid grid) return;

            var cell = FindVisualParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell != null && cell.Column != null)
            {
                grid.SelectedItem = cell.DataContext;
                cell.Focus();
                grid.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
            }
            else
            {
                e.Handled = true;
            }
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.OriginalSource is TextBox tb && tb.SelectionLength > 0)
                {
                    return;
                }

                if (sender is DataGrid grid && grid.CurrentCell.IsValid && grid.CurrentCell.Item != null)
                {
                    string text = GetCellText(grid, grid.CurrentCell);
                    try
                    {
                        if (!string.IsNullOrEmpty(text))
                            Clipboard.SetText(text);
                        else
                            Clipboard.Clear();
                        StatusText.Text = Loc.T("status.cell.copied", text);
                        e.Handled = true;
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void CopyCell_Click(object sender, RoutedEventArgs e)
        {
            var grid = (sender as MenuItem)?.Parent is ContextMenu cm && cm.PlacementTarget is DataGrid dg
                ? dg
                : (ViewsTabControl.SelectedItem == NichesTabItem ? NichesGrid : ResultGrid);

            if (grid.CurrentCell.IsValid && grid.CurrentCell.Item != null)
            {
                string text = GetCellText(grid, grid.CurrentCell);
                try
                {
                    if (!string.IsNullOrEmpty(text))
                        Clipboard.SetText(text);
                    else
                        Clipboard.Clear();
                    StatusText.Text = Loc.T("status.cell.copied", text);
                }
                catch
                {
                }
            }
        }

        private void CopyRow_Click(object sender, RoutedEventArgs e)
        {
            var grid = (sender as MenuItem)?.Parent is ContextMenu cm && cm.PlacementTarget is DataGrid dg
                ? dg
                : (ViewsTabControl.SelectedItem == NichesTabItem ? NichesGrid : ResultGrid);

            var item = grid.SelectedItem ?? grid.CurrentCell.Item;
            if (item == null) return;

            var values = new List<string>();
            foreach (var col in grid.Columns.OrderBy(c => c.DisplayIndex))
            {
                var cellInfo = new DataGridCellInfo(item, col);
                values.Add(GetCellText(grid, cellInfo).Replace("\r", "").Replace("\n", " / "));
            }

            var line = string.Join("\t", values);
            try
            {
                if (!string.IsNullOrEmpty(line))
                    Clipboard.SetText(line);
                else
                    Clipboard.Clear();
                StatusText.Text = Loc.T("status.row.copied");
            }
            catch
            {
            }
        }

        private string GetCellText(DataGrid grid, DataGridCellInfo cellInfo)
        {
            if (!cellInfo.IsValid || cellInfo.Item == null || cellInfo.Column == null)
                return string.Empty;

            var element = cellInfo.Column.GetCellContent(cellInfo.Item);
            if (element != null)
            {
                if (element is TextBlock tb) return tb.Text ?? "";
                if (element is TextBox txt) return txt.Text ?? "";

                var childTxt = FindVisualChild<TextBox>(element);
                if (childTxt != null) return childTxt.Text ?? "";

                var childTb = FindVisualChild<TextBlock>(element);
                if (childTb != null) return childTb.Text ?? "";
            }

            // Fallback: binding reflection
            if (cellInfo.Column is DataGridBoundColumn boundCol &&
                boundCol.Binding is System.Windows.Data.Binding binding &&
                !string.IsNullOrEmpty(binding.Path?.Path))
            {
                var prop = cellInfo.Item.GetType().GetProperty(binding.Path.Path);
                return prop?.GetValue(cellInfo.Item)?.ToString() ?? "";
            }

            var noteProp = cellInfo.Item.GetType().GetProperty("Note");
            if (noteProp != null && (cellInfo.Column.Header?.ToString() == Loc.T("col.note") || cellInfo.Column is DataGridTemplateColumn))
            {
                return noteProp.GetValue(cellInfo.Item)?.ToString() ?? "";
            }

            return string.Empty;
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                if (child is Visual || child is System.Windows.Media.Media3D.Visual3D)
                {
                    child = VisualTreeHelper.GetParent(child);
                }
                else
                {
                    child = LogicalTreeHelper.GetParent(child);
                }
            }
            return null;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            if (parent is not Visual && parent is not System.Windows.Media.Media3D.Visual3D) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var current = new Settings
            {
                InputUnit = _inputUnit,
                ParamUnit = _paramUnit,
                DisplayUnit = _displayUnit,
                SeparateTolerance = _separateTolerance,
                BoxRounding = _boxRounding,
                Language = Loc.Current,
            };
            var dlg = new SettingsWindow(current) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            var inputChanged = dlg.InputUnit != _inputUnit;
            var paramChanged = dlg.ParamUnit != _paramUnit;
            var displayChanged = dlg.DisplayUnit != _displayUnit;
            var tolModeChanged = dlg.SeparateTolerance != _separateTolerance;
            var roundingChanged = dlg.BoxRounding != _boxRounding;
            var languageChanged = dlg.SelectedLanguage != Loc.Current;
            if (!inputChanged && !paramChanged && !displayChanged && !tolModeChanged && !roundingChanged && !languageChanged) return;

            // Keep the untouched example in sync with the chosen input unit and language
            bool isExample = InputBox.Text == ExampleFor(_inputUnit);

            // When switching from symmetric to separate W/H, copy the current W boxes into H boxes
            if (!_separateTolerance && dlg.SeparateTolerance)
            {
                GapHBox.Text = GapBox.Text;
                TrimHBox.Text = TrimBox.Text;
                LapHBox.Text = LapBox.Text;
            }

            // Re-express the parameter fields in the new unit (values that are not valid numbers stay as typed)
            if (paramChanged) ConvertParamBoxes(_paramUnit, dlg.ParamUnit);

            _inputUnit = dlg.InputUnit;
            _paramUnit = dlg.ParamUnit;
            _displayUnit = dlg.DisplayUnit;
            _separateTolerance = dlg.SeparateTolerance;
            _boxRounding = dlg.BoxRounding;
            Loc.Current = dlg.SelectedLanguage;
            if (isExample) InputBox.Text = ExampleFor(_inputUnit);
            UpdateTexts();
            SaveSettingsQuietly();

            if (inputChanged)
            {
                // The old list was entered in another unit: the old result is no longer valid
                _rows = new List<GroupRow>();
                _lastGroups = new List<NicheGroup>();
                ResultGrid.ItemsSource = null;
                StatusText.Text = Loc.T("status.unitchanged");
            }
            else if (_rows.Count > 0)
            {
                Group_Click(sender, e); // redraw the result in the new units / tolerance mode / language
            }
            else
            {
                StatusText.Text = "";
            }
        }

        // ---- Helpers ----

        private Settings ReadSettings()
        {
            double gapW = ReadLengthDouble(GapBox, "name.gap");
            double trimW = ReadLengthDouble(TrimBox, "name.trim");
            double trimDiffW = ReadLengthDouble(TrimDiffWBox, "name.trim.w");
            double trimDiffH = ReadLengthDouble(TrimDiffHBox, "name.trim.h.diff");
            double lapW = ReadLengthDouble(LapBox, "name.lap");
            double hatchDiffW = ReadLengthDouble(HatchDiffWBox, "name.hatch.w");
            double hatchDiffH = ReadLengthDouble(HatchDiffHBox, "name.hatch.h");
            double depth = ReadLengthDouble(DepthBox, "name.depth");
            double ear = ReadLengthDouble(EarBox, "name.ear");

            double gapH = _separateTolerance ? ReadLengthDouble(GapHBox, "name.gap.h") : gapW;
            double trimH = _separateTolerance ? ReadLengthDouble(TrimHBox, "name.trim.h") : trimW;
            double lapH = _separateTolerance ? ReadLengthDouble(LapHBox, "name.lap.h") : lapW;

            return new Settings
            {
                InputUnit = _inputUnit,
                ParamUnit = _paramUnit,
                DisplayUnit = _displayUnit,
                SeparateTolerance = _separateTolerance,
                BoxRounding = _boxRounding,
                Language = Loc.Current,
                GapMm = gapW,
                TrimMm = trimW,
                TrimDiffWMm = trimDiffW,
                TrimDiffHMm = trimDiffH,
                MinLapMm = lapW,
                HatchDiffWMm = hatchDiffW,
                HatchDiffHMm = hatchDiffH,
                DepthMm = depth,
                EarMm = ear,
                GapHMm = gapH,
                TrimHMm = trimH,
                MinLapHMm = lapH,
                GroupNichesByBox = GroupByBoxCheckBox?.IsChecked == true,
            };
        }

        private void ApplySettings(Settings s)
        {
            _inputUnit = s.InputUnit;
            _paramUnit = s.ParamUnit;
            _displayUnit = s.DisplayUnit;
            _separateTolerance = s.SeparateTolerance;
            _boxRounding = s.BoxRounding;
            Loc.Current = s.Language;
            if (GroupByBoxCheckBox != null)
                GroupByBoxCheckBox.IsChecked = s.GroupNichesByBox;
            GapBox.Text = UnitHelper.Format(s.GapMm, _paramUnit);
            TrimBox.Text = UnitHelper.Format(s.TrimMm, _paramUnit);
            TrimDiffWBox.Text = UnitHelper.Format(s.TrimDiffWMm, _paramUnit);
            TrimDiffHBox.Text = UnitHelper.Format(s.TrimDiffHMm, _paramUnit);
            LapBox.Text = UnitHelper.Format(s.MinLapMm, _paramUnit);
            HatchDiffWBox.Text = UnitHelper.Format(s.HatchDiffWMm, _paramUnit);
            HatchDiffHBox.Text = UnitHelper.Format(s.HatchDiffHMm, _paramUnit);
            DepthBox.Text = UnitHelper.Format(s.DepthMm, _paramUnit);
            EarBox.Text = UnitHelper.Format(s.EarMm, _paramUnit);
            GapHBox.Text = UnitHelper.Format(s.EffGapHMm, _paramUnit);
            TrimHBox.Text = UnitHelper.Format(s.EffTrimHMm, _paramUnit);
            LapHBox.Text = UnitHelper.Format(s.EffMinLapHMm, _paramUnit);
            UpdateTexts();
        }

        /// <summary>Puts the chosen language and units into every label, button and table header.</summary>
        private void UpdateTexts()
        {
            var inU = UnitHelper.Name(_inputUnit);
            var parU = UnitHelper.Name(_paramUnit);
            var outU = UnitHelper.Name(_displayUnit);

            Title = Loc.T("title") + " v" + AppInfo.VersionText;
            InputLabel.Text = Loc.T("input.label", inU);

            WidthRowPrefix.Text = Loc.T("p.width.prefix");
            HeightRowPrefix.Text = Loc.T("p.height.prefix");
            WidthRowPrefix.Visibility = _separateTolerance ? Visibility.Visible : Visibility.Collapsed;
            HeightParamsPanel.Visibility = _separateTolerance ? Visibility.Visible : Visibility.Collapsed;

            GapLabel.Text = Loc.T("p.gap", parU);
            TrimLabel.Text = Loc.T("p.trim", parU);
            TrimDiffLabel.Text = Loc.T("p.trim.diff", parU);
            LapLabel.Text = Loc.T("p.lap", parU);
            HatchLabel.Text = Loc.T("p.hatch", parU);
            DepthLabel.Text = Loc.T("p.depth", parU);
            EarLabel.Text = Loc.T("p.ear", parU);
            GapHLabel.Text = Loc.T("p.gap", parU);
            TrimHLabel.Text = Loc.T("p.trim", parU);
            LapHLabel.Text = Loc.T("p.lap", parU);

            GroupButton.Content = Loc.T("btn.group");
            VisualizerButton.Content = Loc.T("btn.visualize");
            ExtremesButton.Content = Loc.T("btn.extremes");
            NicheListButton.Content = Loc.T("btn.niches");
            OpenButton.Content = Loc.T("btn.open");
            SaveButton.Content = Loc.T("btn.save");
            ExportButton.Content = Loc.T("btn.export");
            SettingsButton.Content = Loc.T("btn.settings");
            if (LabelsMainButton != null) LabelsMainButton.Content = Loc.T("btn.labels");
            HelpButton.Content = Loc.T("btn.help");

            GroupsTabItem.Header = Loc.T("tab.groups");
            NichesTabItem.Header = Loc.T("tab.niches");
            CopyNicheListButton.Content = Loc.T("btn.copy.niches");
            ExportNichesButton.Content = Loc.T("btn.export.niches");
            if (LabelsButton != null) LabelsButton.Content = Loc.T("btn.labels");
            if (GroupByBoxCheckBox != null) GroupByBoxCheckBox.Content = Loc.T("chk.group.by.box");
            if (_nicheRows.Count > 0)
                NichesCountLabel.Text = Loc.T("status.niches.count", _nicheRows.Count);

            ResultGrid.Columns[0].Header = Loc.T("col.no");
            ResultGrid.Columns[1].Header = Loc.T("col.count");
            ResultGrid.Columns[2].Header = Loc.T("col.niche", outU);
            ResultGrid.Columns[3].Header = Loc.T("col.box", outU);
            ResultGrid.Columns[4].Header = Loc.T("col.box.drawing");
            ResultGrid.Columns[5].Header = Loc.T("col.trim", outU);
            ResultGrid.Columns[6].Header = Loc.T("col.hatch", outU);
            ResultGrid.Columns[7].Header = Loc.T("col.maxgap", outU);
            ResultGrid.Columns[8].Header = Loc.T("col.lap", outU);
            ResultGrid.Columns[9].Header = Loc.T("col.note");
            ResultGrid.Columns[10].Header = Loc.T("col.comp", outU);

            NichesGrid.Columns[0].Header = Loc.T("col.spec.id");
            NichesGrid.Columns[1].Header = Loc.T("col.spec.niche", outU);
            NichesGrid.Columns[2].Header = Loc.T("col.spec.box", outU);
            NichesGrid.Columns[3].Header = Loc.T("col.spec.box.drawing");
            NichesGrid.Columns[4].Header = Loc.T("col.trim", outU);
            NichesGrid.Columns[5].Header = Loc.T("col.hatch", outU);
            NichesGrid.Columns[6].Header = Loc.T("col.spec.group");
            NichesGrid.Columns[7].Header = Loc.T("col.note");

            if (CopyCellMenuItem != null) CopyCellMenuItem.Header = Loc.T("menu.copy.cell");
            if (CopyRowMenuItem != null) CopyRowMenuItem.Header = Loc.T("menu.copy.row");
            if (CopyNicheCellMenuItem != null) CopyNicheCellMenuItem.Header = Loc.T("menu.copy.cell");
            if (CopyNicheRowMenuItem != null) CopyNicheRowMenuItem.Header = Loc.T("menu.copy.row");
        }

        private Settings ReadSettingsSafe()
        {
            var s = _lastSettings != null ? new Settings
            {
                InputUnit = _lastSettings.InputUnit,
                ParamUnit = _lastSettings.ParamUnit,
                DisplayUnit = _lastSettings.DisplayUnit,
                SeparateTolerance = _lastSettings.SeparateTolerance,
                BoxRounding = _lastSettings.BoxRounding,
                Language = _lastSettings.Language,
                GapMm = _lastSettings.GapMm,
                TrimMm = _lastSettings.TrimMm,
                TrimDiffWMm = _lastSettings.TrimDiffWMm,
                TrimDiffHMm = _lastSettings.TrimDiffHMm,
                MinLapMm = _lastSettings.MinLapMm,
                HatchDiffWMm = _lastSettings.HatchDiffWMm,
                HatchDiffHMm = _lastSettings.HatchDiffHMm,
                DepthMm = _lastSettings.DepthMm,
                EarMm = _lastSettings.EarMm,
                GapHMm = _lastSettings.GapHMm,
                TrimHMm = _lastSettings.TrimHMm,
                MinLapHMm = _lastSettings.MinLapHMm,
                GroupNichesByBox = _lastSettings.GroupNichesByBox,
            } : new Settings();

            s.InputUnit = _inputUnit;
            s.ParamUnit = _paramUnit;
            s.DisplayUnit = _displayUnit;
            s.SeparateTolerance = _separateTolerance;
            s.BoxRounding = _boxRounding;
            s.Language = Loc.Current;
            s.GroupNichesByBox = GroupByBoxCheckBox?.IsChecked == true;

            if (TryParseLengthDouble(GapBox.Text, _paramUnit, out var g)) s.GapMm = g;
            if (TryParseLengthDouble(TrimBox.Text, _paramUnit, out var t)) s.TrimMm = t;
            if (TryParseLengthDouble(TrimDiffWBox.Text, _paramUnit, out var tdw)) s.TrimDiffWMm = tdw;
            if (TryParseLengthDouble(TrimDiffHBox.Text, _paramUnit, out var tdh)) s.TrimDiffHMm = tdh;
            if (TryParseLengthDouble(LapBox.Text, _paramUnit, out var l)) s.MinLapMm = l;
            if (TryParseLengthDouble(HatchDiffWBox.Text, _paramUnit, out var hdw)) s.HatchDiffWMm = hdw;
            if (TryParseLengthDouble(HatchDiffHBox.Text, _paramUnit, out var hdh)) s.HatchDiffHMm = hdh;
            if (TryParseLengthDouble(DepthBox.Text, _paramUnit, out var d)) s.DepthMm = d;
            if (TryParseLengthDouble(EarBox.Text, _paramUnit, out var ear)) s.EarMm = ear;

            if (_separateTolerance)
            {
                if (TryParseLengthDouble(GapHBox.Text, _paramUnit, out var gh)) s.GapHMm = gh;
                if (TryParseLengthDouble(TrimHBox.Text, _paramUnit, out var th)) s.TrimHMm = th;
                if (TryParseLengthDouble(LapHBox.Text, _paramUnit, out var lh)) s.MinLapHMm = lh;
            }
            else
            {
                s.GapHMm = s.GapMm;
                s.TrimHMm = s.TrimMm;
                s.MinLapHMm = s.MinLapMm;
            }

            return s;
        }

        /// <summary>Saves the current values on exit; invalid fields are skipped silently.</summary>
        private void SaveSettingsQuietly()
        {
            try
            {
                var s = ReadSettingsSafe();
                SettingsStore.Save(s);
            }
            catch { }
        }

        private static void SaveSettingsQuietly(Settings settings)
        {
            try { SettingsStore.Save(settings); }
            catch { }
        }

        /// <summary>Parses a non-negative length allowing tenths of a millimetre (e.g. 6.5 or 131.6 mm).</summary>
        private static bool TryParseLengthDouble(string text, LengthUnit unit, out double mm)
        {
            mm = 0;
            if (!double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                || v < 0 || v > 1_000_000)
                return false;
            var raw = v * (unit == LengthUnit.Cm ? 10 : 1);
            mm = Math.Round(raw, 1);
            return Math.Abs(raw - mm) < 1e-6;
        }

        private double ReadLengthDouble(TextBox box, string nameKey)
        {
            if (TryParseLengthDouble(box.Text, _paramUnit, out double mm)) return mm;

            var text = box.Text.Trim();
            box.Focus();
            box.SelectAll();
            throw new UserException(
                Loc.T(_paramUnit == LengthUnit.Cm ? "err.field.cm" : "err.field.mm", Loc.T(nameKey)) + "\n" +
                (text.Length == 0 ? Loc.T("err.field.empty") : Loc.T("err.field.now", text)));
        }

        private void ConvertParamBoxes(LengthUnit from, LengthUnit to)
        {
            foreach (var box in new[] { GapBox, TrimBox, TrimDiffWBox, TrimDiffHBox, LapBox, HatchDiffWBox, HatchDiffHBox, DepthBox, EarBox, GapHBox, TrimHBox, LapHBox })
                if (TryParseLengthDouble(box.Text, from, out double mm))
                    box.Text = UnitHelper.Format(mm, to);
        }

        /// <summary>Refuses results that cannot be built physically (box or hatch of zero size).</summary>
        private static void CheckGroups(List<NicheGroup> groups, Settings s)
        {
            foreach (var g in groups)
            {
                if (g.HatchW > 0 && g.HatchH > 0 && g.BoxW > 0 && g.BoxH > 0) continue;
                throw new UserException(Loc.T("err.toosmall",
                    UnitHelper.Format(g.MinW, s.InputUnit), UnitHelper.Format(g.MinH, s.InputUnit),
                    UnitHelper.Name(s.InputUnit), Math.Max(s.GapMm, s.EffGapHMm) + s.EarMm,
                    $"{s.HatchDiffWMm.ToString("0.#", CultureInfo.InvariantCulture)} / {s.HatchDiffHMm.ToString("0.#", CultureInfo.InvariantCulture)}"));
            }
        }

        /// <summary>Shows an error to the user in plain words (message box + status line).</summary>
        private void Fail(string action, Exception ex)
        {
            string detail = ex switch
            {
                UserException => ex.Message,
                JsonException => Loc.T("err.json"),
                UnauthorizedAccessException => Loc.T("err.access"),
                IOException => Loc.T("err.io", ex.Message),
                _ => Loc.T("err.unexpected", ex.Message),
            };
            // A UserException already explains everything; for others add what we were doing
            string message = ex is UserException ? detail : action + "\n\n" + detail;

            StatusText.Text = Loc.T("err.prefix") + message.Replace("\n\n", " ").Replace("\n", " ");
            MessageBox.Show(this, message, Loc.T("err.title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

        private static string Range(double minMm, double maxMm, LengthUnit unit) =>
            Math.Abs(minMm - maxMm) < 1e-6
                ? UnitHelper.Format(minMm, unit)
                : UnitHelper.Format(minMm, unit) + "–" + UnitHelper.Format(maxMm, unit);

        private static double RoundBox(double val, BoxRounding mode) => mode switch
        {
            BoxRounding.Up => Math.Ceiling(val - 1e-6),
            BoxRounding.Down => Math.Floor(val + 1e-6),
            _ => Math.Round(val, MidpointRounding.AwayFromZero)
        };

        private static GroupRow ToRow(NicheGroup g, int no, LengthUnit unit, BoxRounding rounding)
        {
            // Compact composition: equal sizes are collapsed into "WxH [1.1, 2.5]"
            var items = g.Niches
                .GroupBy(n => (n.WidthMm, n.HeightMm))
                .OrderBy(s => s.Key.WidthMm).ThenBy(s => s.Key.HeightMm)
                .Select(s =>
                {
                    var ids = s.OrderBy(n => n.Entrance).ThenBy(n => n.NicheNo)
                               .Select(n => $"{n.Entrance}.{n.NicheNo}");
                    return $"{UnitHelper.Format(s.Key.WidthMm, unit)}×{UnitHelper.Format(s.Key.HeightMm, unit)} [{string.Join(", ", ids)}]";
                });

            string depthStr = g.DepthMm.ToString("0.#", CultureInfo.InvariantCulture);
            string boxDrawing = $"{RoundBox(g.BoxW, rounding):0} × {RoundBox(g.BoxH, rounding):0} × {depthStr}";

            return new GroupRow
            {
                No = no,
                Count = g.Niches.Count,
                Niche = Range(g.MinW, g.MaxW, unit) + " × " + Range(g.MinH, g.MaxH, unit),
                Box = $"{UnitHelper.Format(g.BoxW, unit)} × {UnitHelper.Format(g.BoxH, unit)}",
                BoxDrawing = boxDrawing,
                WithTrim = $"{UnitHelper.Format(g.TrimW, unit)} × {UnitHelper.Format(g.TrimH, unit)}",
                Hatch = $"{UnitHelper.Format(g.HatchW, unit)} × {UnitHelper.Format(g.HatchH, unit)}",
                MaxGap = UnitHelper.Format(g.MaxGapMm, unit),
                Lap = UnitHelper.Format(g.LapMm, unit),
                Note = "",
                Composition = string.Join(", ", items),
            };
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
