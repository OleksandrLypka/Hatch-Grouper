using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HatchGrouper
{
    public partial class MainWindow : Window
    {
        private List<GroupRow> _rows = new();
        private List<NicheGroup> _lastGroups = new();
        private Settings _lastSettings = new();
        private LengthUnit _inputUnit = LengthUnit.Cm;
        private LengthUnit _paramUnit = LengthUnit.Mm;
        private LengthUnit _displayUnit = LengthUnit.Cm;
        private bool _separateTolerance = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplySettings(SettingsStore.Load());
            InputBox.Text = ExampleFor(_inputUnit);
            Closing += (_, _) => SaveSettingsQuietly();
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

                _rows = groups.Select((g, i) => ToRow(g, i + 1, _displayUnit)).ToList();
                ResultGrid.ItemsSource = _rows;

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

                var data = new ProjectData { InputText = InputBox.Text, Settings = ReadSettings() };
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
                        r.No.ToString(), r.Count.ToString(), Csv(r.Niche), Csv(r.Box),
                        Csv(r.Hatch), Csv(r.MaxGap), Csv(r.Lap), Csv(r.Composition.Replace("\n", " / ")),
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

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var current = new Settings
            {
                InputUnit = _inputUnit,
                ParamUnit = _paramUnit,
                DisplayUnit = _displayUnit,
                SeparateTolerance = _separateTolerance,
                Language = Loc.Current,
            };
            var dlg = new SettingsWindow(current) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            var inputChanged = dlg.InputUnit != _inputUnit;
            var paramChanged = dlg.ParamUnit != _paramUnit;
            var displayChanged = dlg.DisplayUnit != _displayUnit;
            var tolModeChanged = dlg.SeparateTolerance != _separateTolerance;
            var languageChanged = dlg.SelectedLanguage != Loc.Current;
            if (!inputChanged && !paramChanged && !displayChanged && !tolModeChanged && !languageChanged) return;

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
            int gapW = ReadLength(GapBox, "name.gap");
            int trimW = ReadLength(TrimBox, "name.trim");
            int lapW = ReadLength(LapBox, "name.lap");
            int hatch = ReadLength(HatchBox, "name.hatch");

            int gapH = _separateTolerance ? ReadLength(GapHBox, "name.gap.h") : gapW;
            int trimH = _separateTolerance ? ReadLength(TrimHBox, "name.trim.h") : trimW;
            int lapH = _separateTolerance ? ReadLength(LapHBox, "name.lap.h") : lapW;

            return new Settings
            {
                InputUnit = _inputUnit,
                ParamUnit = _paramUnit,
                DisplayUnit = _displayUnit,
                SeparateTolerance = _separateTolerance,
                Language = Loc.Current,
                GapMm = gapW,
                TrimMm = trimW,
                MinLapMm = lapW,
                HatchOffsetMm = hatch,
                GapHMm = gapH,
                TrimHMm = trimH,
                MinLapHMm = lapH,
            };
        }

        private void ApplySettings(Settings s)
        {
            _inputUnit = s.InputUnit;
            _paramUnit = s.ParamUnit;
            _displayUnit = s.DisplayUnit;
            _separateTolerance = s.SeparateTolerance;
            Loc.Current = s.Language;
            GapBox.Text = UnitHelper.Format(s.GapMm, _paramUnit);
            TrimBox.Text = UnitHelper.Format(s.TrimMm, _paramUnit);
            LapBox.Text = UnitHelper.Format(s.MinLapMm, _paramUnit);
            HatchBox.Text = UnitHelper.Format(s.HatchOffsetMm, _paramUnit);
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
            LapLabel.Text = Loc.T("p.lap", parU);
            HatchLabel.Text = Loc.T("p.hatch", parU);
            GapHLabel.Text = Loc.T("p.gap", parU);
            TrimHLabel.Text = Loc.T("p.trim", parU);
            LapHLabel.Text = Loc.T("p.lap", parU);

            GroupButton.Content = Loc.T("btn.group");
            VisualizerButton.Content = Loc.T("btn.visualize");
            OpenButton.Content = Loc.T("btn.open");
            SaveButton.Content = Loc.T("btn.save");
            ExportButton.Content = Loc.T("btn.export");
            SettingsButton.Content = Loc.T("btn.settings");
            HelpButton.Content = Loc.T("btn.help");

            ResultGrid.Columns[0].Header = Loc.T("col.no");
            ResultGrid.Columns[1].Header = Loc.T("col.count");
            ResultGrid.Columns[2].Header = Loc.T("col.niche", outU);
            ResultGrid.Columns[3].Header = Loc.T("col.box", outU);
            ResultGrid.Columns[4].Header = Loc.T("col.hatch", outU);
            ResultGrid.Columns[5].Header = Loc.T("col.maxgap", outU);
            ResultGrid.Columns[6].Header = Loc.T("col.lap", outU);
            ResultGrid.Columns[7].Header = Loc.T("col.comp", outU);
        }

        /// <summary>Saves the current values on exit; invalid fields are skipped silently.</summary>
        private void SaveSettingsQuietly()
        {
            try { SettingsStore.Save(ReadSettings()); }
            catch { }
        }

        private static void SaveSettingsQuietly(Settings settings)
        {
            try { SettingsStore.Save(settings); }
            catch { }
        }

        /// <summary>Parses a non-negative length typed in <paramref name="unit"/>; the result is whole millimetres.</summary>
        private static bool TryParseLength(string text, LengthUnit unit, out int mm)
        {
            mm = 0;
            if (!double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                || v < 0 || v > 1_000_000)
                return false;
            var raw = v * (unit == LengthUnit.Cm ? 10 : 1);
            mm = (int)Math.Round(raw);
            return Math.Abs(raw - mm) < 1e-6; // 0.55 cm would be 5.5 mm: not allowed
        }

        private int ReadLength(TextBox box, string nameKey)
        {
            if (TryParseLength(box.Text, _paramUnit, out int mm)) return mm;

            var text = box.Text.Trim();
            box.Focus();
            box.SelectAll();
            throw new UserException(
                Loc.T(_paramUnit == LengthUnit.Cm ? "err.field.cm" : "err.field.mm", Loc.T(nameKey)) + "\n" +
                (text.Length == 0 ? Loc.T("err.field.empty") : Loc.T("err.field.now", text)));
        }

        private void ConvertParamBoxes(LengthUnit from, LengthUnit to)
        {
            foreach (var box in new[] { GapBox, TrimBox, LapBox, HatchBox, GapHBox, TrimHBox, LapHBox })
                if (TryParseLength(box.Text, from, out int mm))
                    box.Text = UnitHelper.Format(mm, to);
        }

        /// <summary>Refuses results that cannot be built physically (box or hatch of zero size).</summary>
        private static void CheckGroups(List<NicheGroup> groups, Settings s)
        {
            foreach (var g in groups)
            {
                if (g.HatchW > 0 && g.HatchH > 0) continue;
                throw new UserException(Loc.T("err.toosmall",
                    UnitHelper.Format(g.MinW, s.InputUnit), UnitHelper.Format(g.MinH, s.InputUnit),
                    UnitHelper.Name(s.InputUnit), Math.Max(s.GapMm, s.EffGapHMm), s.HatchOffsetMm));
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

        private static string Range(int minMm, int maxMm, LengthUnit unit) =>
            minMm == maxMm
                ? UnitHelper.Format(minMm, unit)
                : UnitHelper.Format(minMm, unit) + "–" + UnitHelper.Format(maxMm, unit);

        private static GroupRow ToRow(NicheGroup g, int no, LengthUnit unit)
        {
            // Composition: one line per entrance, equal sizes are collapsed into "size (N шт)"
            var lines = g.Niches
                .GroupBy(n => n.Entrance)
                .OrderBy(e => e.Key)
                .Select(e =>
                {
                    var sizes = e.GroupBy(n => (n.WidthMm, n.HeightMm))
                                 .OrderBy(s => s.Key.WidthMm).ThenBy(s => s.Key.HeightMm)
                                 .Select(s => UnitHelper.Format(s.Key.WidthMm, unit) + "×" + UnitHelper.Format(s.Key.HeightMm, unit) +
                                              (s.Count() > 1 ? Loc.T("comp.pcs", s.Count()) : ""));
                    return Loc.T("comp.entrance", e.Key) + string.Join(", ", sizes);
                });

            return new GroupRow
            {
                No = no,
                Count = g.Niches.Count,
                Niche = Range(g.MinW, g.MaxW, unit) + " × " + Range(g.MinH, g.MaxH, unit),
                Box = $"{UnitHelper.Format(g.BoxW, unit)} × {UnitHelper.Format(g.BoxH, unit)}",
                Hatch = $"{UnitHelper.Format(g.HatchW, unit)} × {UnitHelper.Format(g.HatchH, unit)}",
                MaxGap = UnitHelper.Format(g.MaxGapMm, unit),
                Lap = UnitHelper.Format(g.LapMm, unit),
                Composition = string.Join("\n", lines),
            };
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
