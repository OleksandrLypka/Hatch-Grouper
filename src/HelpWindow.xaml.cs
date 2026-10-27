using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HatchGrouper
{
    /// <summary>Window with formulas, plain explanation, and visual CAD guide tutorial.</summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow(Settings s)
        {
            InitializeComponent();
            Title = Loc.T("help.title");
            CloseButton.Content = Loc.T("btn.close");

            TabFormulas.Header = Loc.T("help.tab.formulas");
            TabCadGuide.Header = Loc.T("help.tab.cad");

            BuildFormulasTab(s);
            BuildCadGuideTab();
        }

        private void BuildFormulasTab(Settings s)
        {
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
            double trimW = Math.Round(boxW + s.TrimDiffWMm, 2), trimH = Math.Round(boxH + s.TrimDiffHMm, 2);
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

        private void BuildCadGuideTab()
        {
            // Intro Card
            CadBody.Children.Add(CreateCadCard(
                Loc.T("help.cad.intro.title"),
                Loc.T("help.cad.intro.body"),
                isIntro: true));

            // Step 1: Outer trim dimensions (968 x 885)
            var imgTrimW = CreateImageCard("cad_trim_width.png", Loc.T("help.cad.s1.cap1"));
            var imgTrimH = CreateImageCard("cad_trim_height.png", Loc.T("help.cad.s1.cap2"));
            var rowStep1 = CreateTwoColumnRow(imgTrimW, imgTrimH);
            CadBody.Children.Add(CreateCadCard(
                Loc.T("help.cad.s1.title"),
                Loc.T("help.cad.s1.body"),
                visuals: new FrameworkElement[] { rowStep1 }));

            // Step 2: Inner box body dimensions (851.8 x 767.2)
            var imgBoxW = CreateImageCard("cad_box_width.png", Loc.T("help.cad.s2.cap1"));
            var imgBoxH = CreateImageCard("cad_box_height.png", Loc.T("help.cad.s2.cap2"));
            var rowStep2 = CreateTwoColumnRow(imgBoxW, imgBoxH);
            CadBody.Children.Add(CreateCadCard(
                Loc.T("help.cad.s2.title"),
                Loc.T("help.cad.s2.body"),
                visuals: new FrameworkElement[] { rowStep2 }));

            // Step 3: Calculation of bend offsets (TrimDiff)
            var formulaBox = CreateFormulaTipBox(Loc.T("help.cad.s3.formula"));
            CadBody.Children.Add(CreateCadCard(
                Loc.T("help.cad.s3.title"),
                Loc.T("help.cad.s3.body"),
                visuals: new FrameworkElement[] { formulaBox }));

            // Step 4: Clear opening and hatch door
            var imgHatch = CreateImageCard("cad_hatch_door.png", Loc.T("help.cad.s4.cap1"), maxHeight: 260);
            CadBody.Children.Add(CreateCadCard(
                Loc.T("help.cad.s4.title"),
                Loc.T("help.cad.s4.body"),
                visuals: new FrameworkElement[] { imgHatch }));

            // Step 5: Mounting tabs and box depth
            CadBody.Children.Add(CreateCadCard(
                Loc.T("help.cad.s5.title"),
                Loc.T("help.cad.s5.body")));
        }

        private Border CreateCadCard(string title, string description, bool isIntro = false, FrameworkElement[] visuals = null)
        {
            var card = new Border
            {
                Background = isIntro ? new SolidColorBrush(Color.FromRgb(0xF0, 0xF9, 0xFF)) : Brushes.White,
                BorderBrush = isIntro ? new SolidColorBrush(Color.FromRgb(0xBA, 0xE6, 0xFD)) : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = isIntro ? 15 : 14,
                Foreground = isIntro ? new SolidColorBrush(Color.FromRgb(0x03, 0x69, 0xA1)) : new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            sp.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12.5,
                LineHeight = 19,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55)),
                Margin = new Thickness(0, 0, 0, (visuals != null && visuals.Length > 0) ? 10 : 0)
            });

            if (visuals != null)
            {
                foreach (var v in visuals)
                {
                    if (v != null) sp.Children.Add(v);
                }
            }

            card.Child = sp;
            return card;
        }

        private FrameworkElement CreateImageCard(string fileName, string caption, double maxHeight = 220)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
                Margin = new Thickness(4),
                Padding = new Thickness(6)
            };

            var sp = new StackPanel();
            try
            {
                var uri = new Uri($"pack://application:,,,/Resources/CadGuide/{fileName}", UriKind.Absolute);
                var bmp = new BitmapImage(uri);
                var img = new Image
                {
                    Source = bmp,
                    MaxHeight = maxHeight,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                sp.Children.Add(img);
            }
            catch
            {
                // Fallback in case resource cannot be loaded
            }

            if (!string.IsNullOrEmpty(caption))
            {
                sp.Children.Add(new TextBlock
                {
                    Text = caption,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            border.Child = sp;
            return border;
        }

        private FrameworkElement CreateTwoColumnRow(FrameworkElement left, FrameworkElement right)
        {
            var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 4, 0, 4) };
            if (left != null) grid.Children.Add(left);
            if (right != null) grid.Children.Add(right);
            return grid;
        }

        private FrameworkElement CreateFormulaTipBox(string text)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF2, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xC7, 0xD2, 0xFE)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock
                {
                    Text = text,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x31, 0x2E, 0x81)),
                    TextWrapping = TextWrapping.Wrap
                }
            };
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
