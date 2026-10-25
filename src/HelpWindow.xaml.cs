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
            int boxW = 840 - 2 * s.GapMm, boxH = 890 - 2 * s.EffGapHMm;
            int hatchW = boxW - 2 * s.HatchOffsetMm, hatchH = boxH - 2 * s.HatchOffsetMm;
            AddHeader(Loc.T("help.h.example"));
            AddFormula(Loc.T("help.example", s.GapMm, s.TrimMm, s.MinLapMm, s.HatchOffsetMm,
                             s.MaxGapWMm, s.ToleranceWMm, boxW, boxH, hatchW, hatchH));
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
