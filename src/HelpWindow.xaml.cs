using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HatchGrouper
{
    /// <summary>Window with the formulas and a plain explanation, filled with the user's current numbers.</summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow(Settings s)
        {
            InitializeComponent();
            Title = Loc.T("help.title");
            CloseButton.Content = Loc.T("btn.close");

            AddHeader(Loc.T("help.h.what"));
            AddText(Loc.T("help.what"));
            AddHeader(Loc.T("help.h.params"));
            AddText(Loc.T("help.params"));
            AddHeader(Loc.T("help.h.how"));
            AddText(Loc.T("help.how"));
            AddHeader(Loc.T("help.h.formulas"));
            AddFormula(Loc.T("help.formulas"));

            // The same formulas as in Models.cs (NicheGroup.Compute), with a sample niche of 840 x 890 mm
            double boxW = Math.Round(840 - 2 * (s.GapMm + s.EarMm), 2), boxH = Math.Round(890 - 2 * (s.EffGapHMm + s.EarMm), 2);
            double trimW = Math.Round(boxW + 2 * s.TrimMm, 2), trimH = Math.Round(boxH + 2 * s.EffTrimHMm, 2);
            double hatchW = Math.Round(trimW - s.HatchDiffWMm, 2), hatchH = Math.Round(trimH - s.HatchDiffHMm, 2);
            AddHeader(Loc.T("help.h.example"));
            AddFormula(Loc.T("help.example",
                s.GapMm, s.TrimMm, s.MinLapMm,
                s.HatchDiffWMm.ToString("0.#", CultureInfo.InvariantCulture),
                s.MaxGapWMm, s.ToleranceWMm, boxW, boxH,
                hatchW.ToString("0.#", CultureInfo.InvariantCulture),
                hatchH.ToString("0.#", CultureInfo.InvariantCulture),
                s.HatchDiffHMm.ToString("0.#", CultureInfo.InvariantCulture),
                s.EarMm, trimW, trimH));
            if (s.ToleranceWMm < 0 || s.ToleranceHMm < 0)
                AddText(Loc.T("help.example.negative"), Brushes.Firebrick);

            AddHeader(Loc.T("help.h.table"));
            AddText(Loc.T("help.table"));

            AddText("\n" + Loc.T("set.version", AppInfo.VersionText) + "  |  " + Loc.T("set.author", AppInfo.Author), Brushes.Gray);
        }

        private void AddHeader(string text) => Body.Children.Add(new TextBlock
        {
            Text = text, FontWeight = FontWeights.Bold, FontSize = 15, Margin = new Thickness(0, 12, 0, 4),
        });

        private void AddText(string text, Brush color = null) => Body.Children.Add(new TextBlock
        {
            Text = text, TextWrapping = TextWrapping.Wrap, Foreground = color ?? Brushes.Black,
        });

        /// <summary>Formulas in a monospaced block so that the "=" signs line up.</summary>
        private void AddFormula(string text) => Body.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3)),
            Padding = new Thickness(8),
            Child = new TextBlock { Text = text, FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap },
        });

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
