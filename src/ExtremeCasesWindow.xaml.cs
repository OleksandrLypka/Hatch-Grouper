using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HatchGrouper
{
    public partial class ExtremeCasesWindow : Window
    {
        private readonly List<NicheGroup> _groups;
        private readonly Settings _settings;

        private enum ExtremeKind
        {
            Other,
            Smallest,   // both narrowest & lowest
            Narrowest,  // min W
            Lowest,     // min H
            Largest,    // both widest & tallest
            Widest,     // max W
            Tallest,    // max H
            EnvelopeMin, // theoretical min W x min H
            EnvelopeMax  // theoretical max W x max H
        }

        private class NicheCaseItem
        {
            public double W { get; set; }
            public double H { get; set; }
            public ExtremeKind Kind { get; set; }
            public int Count { get; set; }
            public string Ids { get; set; } = "";
            public string DisplayText { get; set; } = "";

            public override string ToString() => DisplayText;
        }

        public ExtremeCasesWindow(List<NicheGroup> groups, Settings settings, int selectedGroupIndex = 0)
        {
            InitializeComponent();
            _groups = groups ?? new List<NicheGroup>();
            _settings = settings ?? new Settings();

            Title = Loc.T("ext.title");
            CloseButton.Content = Loc.T("btn.close");

            GroupLabel.Text = Loc.T("ext.group.label");
            CaseLabel.Text = Loc.T("ext.case.label");

            var u = UnitHelper.Name(_settings.DisplayUnit);
            DimsHeader.Text = Loc.T("ext.h.dims", u);
            GapsHeader.Text = Loc.T("ext.h.gaps", u);
            SummaryHeader.Text = Loc.T("ext.h.summary");

            LblNiche.Text = Loc.T("ext.lbl.niche");
            LblBox.Text = Loc.T("ext.lbl.box");
            LblBoxEars.Text = Loc.T("ext.lbl.boxears", UnitHelper.Format(_settings.EarMm, _settings.DisplayUnit), u);
            LblTrim.Text = Loc.T("ext.lbl.trim");
            LblHatch.Text = Loc.T("ext.lbl.hatch");

            LblGapEar.Text = Loc.T("ext.lbl.gapear");
            LblGapBody.Text = Loc.T("ext.lbl.gapbody");
            LblLap.Text = Loc.T("ext.lbl.lap");
            LblHatchClr.Text = Loc.T("ext.lbl.hatchclr");

            LegWall.Text = Loc.T("ext.legend.wall");
            LegTrim.Text = Loc.T("ext.legend.trim");
            LegBox.Text = Loc.T("ext.legend.box");
            LegEars.Text = Loc.T("ext.legend.ears", UnitHelper.Format(_settings.EarMm, _settings.DisplayUnit), u);
            LegHatch.Text = Loc.T("ext.legend.hatch");

            GlobalConclusionText.Text = Loc.T("ext.rec.conclusion");

            PopulateGroups(selectedGroupIndex);
        }

        private void PopulateGroups(int initialIndex)
        {
            GroupCombo.Items.Clear();
            var u = UnitHelper.Name(_settings.DisplayUnit);
            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                string title = $"{Loc.T("vis.group.title", i + 1)} ({UnitHelper.Format(g.BoxW, _settings.DisplayUnit)} × {UnitHelper.Format(g.BoxH, _settings.DisplayUnit)} {u}, {g.Niches.Count} {Loc.T("col.count")})";
                GroupCombo.Items.Add(title);
            }

            if (GroupCombo.Items.Count > 0)
            {
                int index = Math.Clamp(initialIndex, 0, GroupCombo.Items.Count - 1);
                GroupCombo.SelectedIndex = index;
            }
        }

        private void GroupCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupCombo.SelectedIndex < 0 || GroupCombo.SelectedIndex >= _groups.Count) return;
            var g = _groups[GroupCombo.SelectedIndex];

            CaseCombo.Items.Clear();
            var u = UnitHelper.Name(_settings.DisplayUnit);

            var distinctSizes = g.Niches
                .GroupBy(n => (n.WidthMm, n.HeightMm))
                .Select(grp => new
                {
                    W = grp.Key.WidthMm,
                    H = grp.Key.HeightMm,
                    Count = grp.Count(),
                    Ids = string.Join(", ", grp.OrderBy(n => n.Entrance).ThenBy(n => n.NicheNo).Select(n => $"{n.Entrance}.{n.NicheNo}"))
                })
                .OrderBy(s => s.W).ThenBy(s => s.H)
                .ToList();

            // Find physical extreme niches in the group
            var narrowest = distinctSizes.Where(s => Math.Abs(s.W - g.MinW) < 1e-6).OrderBy(s => s.H).First();
            var lowest = distinctSizes.Where(s => Math.Abs(s.H - g.MinH) < 1e-6).OrderBy(s => s.W).First();
            var widest = distinctSizes.Where(s => Math.Abs(s.W - g.MaxW) < 1e-6).OrderByDescending(s => s.H).First();
            var tallest = distinctSizes.Where(s => Math.Abs(s.H - g.MaxH) < 1e-6).OrderByDescending(s => s.W).First();

            var addedKeys = new HashSet<(double W, double H)>();

            // 1. Minimum / Entry extremes
            if (narrowest.W == lowest.W && narrowest.H == lowest.H)
            {
                string dim = $"{UnitHelper.Format(narrowest.W, _settings.DisplayUnit)} × {UnitHelper.Format(narrowest.H, _settings.DisplayUnit)} {u} [{narrowest.Ids}]";
                CaseCombo.Items.Add(new NicheCaseItem
                {
                    W = narrowest.W,
                    H = narrowest.H,
                    Count = narrowest.Count,
                    Ids = narrowest.Ids,
                    Kind = ExtremeKind.Smallest,
                    DisplayText = Loc.T("ext.case.smallest", dim)
                });
                addedKeys.Add((narrowest.W, narrowest.H));
            }
            else
            {
                // Narrowest niche
                string dimW = $"{UnitHelper.Format(narrowest.W, _settings.DisplayUnit)} × {UnitHelper.Format(narrowest.H, _settings.DisplayUnit)} {u} [{narrowest.Ids}]";
                CaseCombo.Items.Add(new NicheCaseItem
                {
                    W = narrowest.W,
                    H = narrowest.H,
                    Count = narrowest.Count,
                    Ids = narrowest.Ids,
                    Kind = ExtremeKind.Narrowest,
                    DisplayText = Loc.T("ext.case.narrowest", dimW)
                });
                addedKeys.Add((narrowest.W, narrowest.H));

                // Lowest niche
                string dimH = $"{UnitHelper.Format(lowest.W, _settings.DisplayUnit)} × {UnitHelper.Format(lowest.H, _settings.DisplayUnit)} {u} [{lowest.Ids}]";
                CaseCombo.Items.Add(new NicheCaseItem
                {
                    W = lowest.W,
                    H = lowest.H,
                    Count = lowest.Count,
                    Ids = lowest.Ids,
                    Kind = ExtremeKind.Lowest,
                    DisplayText = Loc.T("ext.case.lowest", dimH)
                });
                addedKeys.Add((lowest.W, lowest.H));
            }

            // 2. Maximum / Lap extremes
            if (widest.W == tallest.W && widest.H == tallest.H)
            {
                if (!addedKeys.Contains((widest.W, widest.H)))
                {
                    string dim = $"{UnitHelper.Format(widest.W, _settings.DisplayUnit)} × {UnitHelper.Format(widest.H, _settings.DisplayUnit)} {u} [{widest.Ids}]";
                    CaseCombo.Items.Add(new NicheCaseItem
                    {
                        W = widest.W,
                        H = widest.H,
                        Count = widest.Count,
                        Ids = widest.Ids,
                        Kind = ExtremeKind.Largest,
                        DisplayText = Loc.T("ext.case.largest", dim)
                    });
                    addedKeys.Add((widest.W, widest.H));
                }
            }
            else
            {
                // Widest niche
                if (!addedKeys.Contains((widest.W, widest.H)))
                {
                    string dimW = $"{UnitHelper.Format(widest.W, _settings.DisplayUnit)} × {UnitHelper.Format(widest.H, _settings.DisplayUnit)} {u} [{widest.Ids}]";
                    CaseCombo.Items.Add(new NicheCaseItem
                    {
                        W = widest.W,
                        H = widest.H,
                        Count = widest.Count,
                        Ids = widest.Ids,
                        Kind = ExtremeKind.Widest,
                        DisplayText = Loc.T("ext.case.widest", dimW)
                    });
                    addedKeys.Add((widest.W, widest.H));
                }

                // Tallest niche
                if (!addedKeys.Contains((tallest.W, tallest.H)))
                {
                    string dimH = $"{UnitHelper.Format(tallest.W, _settings.DisplayUnit)} × {UnitHelper.Format(tallest.H, _settings.DisplayUnit)} {u} [{tallest.Ids}]";
                    CaseCombo.Items.Add(new NicheCaseItem
                    {
                        W = tallest.W,
                        H = tallest.H,
                        Count = tallest.Count,
                        Ids = tallest.Ids,
                        Kind = ExtremeKind.Tallest,
                        DisplayText = Loc.T("ext.case.tallest", dimH)
                    });
                    addedKeys.Add((tallest.W, tallest.H));
                }
            }

            // 3. Theoretical envelope corners (if not represented by physical niches)
            bool hasMinCorner = distinctSizes.Any(s => Math.Abs(s.W - g.MinW) < 1e-6 && Math.Abs(s.H - g.MinH) < 1e-6);
            if (!hasMinCorner)
            {
                string dim = $"{UnitHelper.Format(g.MinW, _settings.DisplayUnit)} × {UnitHelper.Format(g.MinH, _settings.DisplayUnit)} {u}";
                CaseCombo.Items.Add(new NicheCaseItem
                {
                    W = g.MinW,
                    H = g.MinH,
                    Kind = ExtremeKind.EnvelopeMin,
                    DisplayText = Loc.T("ext.case.envelope.min", dim)
                });
            }

            bool hasMaxCorner = distinctSizes.Any(s => Math.Abs(s.W - g.MaxW) < 1e-6 && Math.Abs(s.H - g.MaxH) < 1e-6);
            if (!hasMaxCorner && (Math.Abs(g.MaxW - g.MinW) > 1e-6 || Math.Abs(g.MaxH - g.MinH) > 1e-6))
            {
                string dim = $"{UnitHelper.Format(g.MaxW, _settings.DisplayUnit)} × {UnitHelper.Format(g.MaxH, _settings.DisplayUnit)} {u}";
                CaseCombo.Items.Add(new NicheCaseItem
                {
                    W = g.MaxW,
                    H = g.MaxH,
                    Kind = ExtremeKind.EnvelopeMax,
                    DisplayText = Loc.T("ext.case.envelope.max", dim)
                });
            }

            // 4. Other distinct niches in the group
            foreach (var size in distinctSizes)
            {
                if (addedKeys.Contains((size.W, size.H))) continue;

                string dim = $"{UnitHelper.Format(size.W, _settings.DisplayUnit)} × {UnitHelper.Format(size.H, _settings.DisplayUnit)} {u} [{size.Ids}] ({size.Count} {Loc.T("col.count")})";
                CaseCombo.Items.Add(new NicheCaseItem
                {
                    W = size.W,
                    H = size.H,
                    Count = size.Count,
                    Ids = size.Ids,
                    Kind = ExtremeKind.Other,
                    DisplayText = Loc.T("ext.case.other", dim)
                });
            }

            CaseCombo.SelectedIndex = 0;
        }

        private void CaseCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            if (GroupCombo.SelectedIndex < 0 || GroupCombo.SelectedIndex >= _groups.Count) return;
            if (CaseCombo.SelectedItem is not NicheCaseItem item) return;

            var g = _groups[GroupCombo.SelectedIndex];
            var u = UnitHelper.Name(_settings.DisplayUnit);

            double nicheW = item.W;
            double nicheH = item.H;

            // Dimensions
            ValNiche.Text = $"{UnitHelper.Format(nicheW, _settings.DisplayUnit)} × {UnitHelper.Format(nicheH, _settings.DisplayUnit)} {u}";
            ValBox.Text = $"{UnitHelper.Format(g.BoxW, _settings.DisplayUnit)} × {UnitHelper.Format(g.BoxH, _settings.DisplayUnit)} {u}";
            double boxEarsW = g.BoxW + 2 * _settings.EarMm;
            double boxEarsH = g.BoxH + 2 * _settings.EarMm;
            ValBoxEars.Text = $"{UnitHelper.Format(boxEarsW, _settings.DisplayUnit)} × {UnitHelper.Format(boxEarsH, _settings.DisplayUnit)} {u}";
            ValTrim.Text = $"{UnitHelper.Format(g.TrimW, _settings.DisplayUnit)} × {UnitHelper.Format(g.TrimH, _settings.DisplayUnit)} {u}";
            ValHatch.Text = $"{UnitHelper.Format(g.HatchW, _settings.DisplayUnit)} × {UnitHelper.Format(g.HatchH, _settings.DisplayUnit)} {u}";

            // Gaps & Laps
            double gapBodyW = (nicheW - g.BoxW) / 2.0;
            double gapBodyH = (nicheH - g.BoxH) / 2.0;
            double gapEarW = gapBodyW - _settings.EarMm;
            double gapEarH = gapBodyH - _settings.EarMm;

            double lapW = _settings.TrimMm - gapBodyW;
            double lapH = _settings.EffTrimHMm - gapBodyH;

            ValGapEar.Text = gapEarW == gapEarH
                ? $"{UnitHelper.Format(gapEarW, _settings.DisplayUnit)} {u}"
                : $"{UnitHelper.Format(gapEarW, _settings.DisplayUnit)} × {UnitHelper.Format(gapEarH, _settings.DisplayUnit)} {u}";

            ValGapBody.Text = gapBodyW == gapBodyH
                ? $"{UnitHelper.Format(gapBodyW, _settings.DisplayUnit)} {u}"
                : $"{UnitHelper.Format(gapBodyW, _settings.DisplayUnit)} × {UnitHelper.Format(gapBodyH, _settings.DisplayUnit)} {u}";

            ValLap.Text = lapW == lapH
                ? $"{UnitHelper.Format(lapW, _settings.DisplayUnit)} {u}"
                : $"{UnitHelper.Format(lapW, _settings.DisplayUnit)} × {UnitHelper.Format(lapH, _settings.DisplayUnit)} {u}";

            ValHatchClr.Text = $"{UnitHelper.Format(_settings.HatchDiffWMm, _settings.DisplayUnit)} × {UnitHelper.Format(_settings.HatchDiffHMm, _settings.DisplayUnit)} {u}";

            // Recommendations
            switch (item.Kind)
            {
                case ExtremeKind.Smallest:
                case ExtremeKind.EnvelopeMin:
                    CaseRecommendationText.Text = Loc.T("ext.rec.smallest",
                        UnitHelper.Format(Math.Min(gapEarW, gapEarH), _settings.DisplayUnit),
                        u,
                        UnitHelper.Format(Math.Min(gapBodyW, gapBodyH), _settings.DisplayUnit));
                    break;

                case ExtremeKind.Narrowest:
                    CaseRecommendationText.Text = Loc.T("ext.rec.narrowest",
                        UnitHelper.Format(gapEarW, _settings.DisplayUnit),
                        u,
                        UnitHelper.Format(gapBodyW, _settings.DisplayUnit));
                    break;

                case ExtremeKind.Lowest:
                    CaseRecommendationText.Text = Loc.T("ext.rec.lowest",
                        UnitHelper.Format(gapEarH, _settings.DisplayUnit),
                        u,
                        UnitHelper.Format(gapBodyH, _settings.DisplayUnit));
                    break;

                case ExtremeKind.Largest:
                case ExtremeKind.EnvelopeMax:
                    double minReqLapBoth = Math.Min(_settings.MinLapMm, _settings.EffMinLapHMm);
                    CaseRecommendationText.Text = Loc.T("ext.rec.largest",
                        UnitHelper.Format(Math.Min(lapW, lapH), _settings.DisplayUnit),
                        u,
                        UnitHelper.Format(minReqLapBoth, _settings.DisplayUnit));
                    break;

                case ExtremeKind.Widest:
                    CaseRecommendationText.Text = Loc.T("ext.rec.widest",
                        UnitHelper.Format(lapW, _settings.DisplayUnit),
                        u,
                        UnitHelper.Format(_settings.MinLapMm, _settings.DisplayUnit));
                    break;

                case ExtremeKind.Tallest:
                    CaseRecommendationText.Text = Loc.T("ext.rec.tallest",
                        UnitHelper.Format(lapH, _settings.DisplayUnit),
                        u,
                        UnitHelper.Format(_settings.EffMinLapHMm, _settings.DisplayUnit));
                    break;

                default:
                    double minReqLap = Math.Min(_settings.MinLapMm, _settings.EffMinLapHMm);
                    CaseRecommendationText.Text =
                        $"• {Loc.T("ext.lbl.gapear")} {UnitHelper.Format(Math.Min(gapEarW, gapEarH), _settings.DisplayUnit)} {u}\n" +
                        $"• {Loc.T("ext.lbl.lap")} {UnitHelper.Format(Math.Min(lapW, lapH), _settings.DisplayUnit)} {u} ({Loc.T("col.lap", "")}: {UnitHelper.Format(minReqLap, _settings.DisplayUnit)} {u}).";
                    break;
            }

            DrawDiagram(nicheW, nicheH, g);
        }

        private void DiagramCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDetails();
        }

        private void DrawDiagram(double nicheW, double nicheH, NicheGroup g)
        {
            DiagramCanvas.Children.Clear();
            double cWidth = DiagramCanvas.ActualWidth;
            double cHeight = DiagramCanvas.ActualHeight;
            if (cWidth < 50 || cHeight < 50) return;

            // Geometry in mm
            double trimW = g.TrimW;
            double trimH = g.TrimH;
            double boxW = g.BoxW;
            double boxH = g.BoxH;
            double ear = _settings.EarMm;
            double hatchW = g.HatchW;
            double hatchH = g.HatchH;

            // Overall extent to fit in canvas
            double maxModelW = Math.Max(trimW, nicheW) + 60;
            double maxModelH = Math.Max(trimH, nicheH) + 60;

            double scale = Math.Min((cWidth - 40) / maxModelW, (cHeight - 40) / maxModelH);
            if (scale <= 0) scale = 0.2;

            double cx = cWidth / 2.0;
            double cy = cHeight / 2.0;

            Rect ToCanvasRect(double wMm, double hMm) =>
                new Rect(cx - (wMm / 2.0) * scale, cy - (hMm / 2.0) * scale, wMm * scale, hMm * scale);

            // 1. Wall / Niche Opening (background / boundary)
            var nicheRect = ToCanvasRect(nicheW, nicheH);
            var nicheShape = new Rectangle
            {
                Width = nicheRect.Width,
                Height = nicheRect.Height,
                Stroke = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                StrokeThickness = 2.0,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Fill = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC))
            };
            Canvas.SetLeft(nicheShape, nicheRect.Left);
            Canvas.SetTop(nicheShape, nicheRect.Top);
            DiagramCanvas.Children.Add(nicheShape);

            // 2. Trim (outer flange)
            var trimRect = ToCanvasRect(trimW, trimH);
            var trimShape = new Rectangle
            {
                Width = trimRect.Width,
                Height = trimRect.Height,
                Stroke = new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)),
                StrokeThickness = 2.0,
                Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x02, 0x84, 0xC7))
            };
            Canvas.SetLeft(trimShape, trimRect.Left);
            Canvas.SetTop(trimShape, trimRect.Top);
            DiagramCanvas.Children.Add(trimShape);

            // 3. Mounting Ears (tabs protruding by ear mm)
            if (ear > 0)
            {
                var earBrush = new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
                double earVisualW = Math.Max(ear * scale, 3.5);
                double tabLength = 36 * scale;

                // Left tab
                var leftTab = new Rectangle { Width = earVisualW, Height = tabLength, Fill = earBrush };
                Canvas.SetLeft(leftTab, cx - (boxW / 2.0 + ear) * scale);
                Canvas.SetTop(leftTab, cy - tabLength / 2.0);
                DiagramCanvas.Children.Add(leftTab);

                // Right tab
                var rightTab = new Rectangle { Width = earVisualW, Height = tabLength, Fill = earBrush };
                Canvas.SetLeft(rightTab, cx + (boxW / 2.0) * scale);
                Canvas.SetTop(rightTab, cy - tabLength / 2.0);
                DiagramCanvas.Children.Add(rightTab);

                // Top tab
                var topTab = new Rectangle { Width = tabLength, Height = earVisualW, Fill = earBrush };
                Canvas.SetLeft(topTab, cx - tabLength / 2.0);
                Canvas.SetTop(topTab, cy - (boxH / 2.0 + ear) * scale);
                DiagramCanvas.Children.Add(topTab);

                // Bottom tab
                var bottomTab = new Rectangle { Width = tabLength, Height = earVisualW, Fill = earBrush };
                Canvas.SetLeft(bottomTab, cx - tabLength / 2.0);
                Canvas.SetTop(bottomTab, cy + (boxH / 2.0) * scale);
                DiagramCanvas.Children.Add(bottomTab);
            }

            // 4. Box body
            var boxRect = ToCanvasRect(boxW, boxH);
            var boxShape = new Rectangle
            {
                Width = boxRect.Width,
                Height = boxRect.Height,
                Stroke = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                StrokeThickness = 2.0,
                Fill = new SolidColorBrush(Color.FromArgb(0x28, 0x33, 0x41, 0x55))
            };
            Canvas.SetLeft(boxShape, boxRect.Left);
            Canvas.SetTop(boxShape, boxRect.Top);
            DiagramCanvas.Children.Add(boxShape);

            // 5. Hatch opening & door
            var hatchRect = ToCanvasRect(hatchW, hatchH);
            var hatchShape = new Rectangle
            {
                Width = hatchRect.Width,
                Height = hatchRect.Height,
                Stroke = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Color.FromArgb(0x50, 0xFE, 0xF0, 0x8A))
            };
            Canvas.SetLeft(hatchShape, hatchRect.Left);
            Canvas.SetTop(hatchShape, hatchRect.Top);
            DiagramCanvas.Children.Add(hatchShape);

            // 6. Center Labels
            var centerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var u = UnitHelper.Name(_settings.DisplayUnit);

            centerStack.Children.Add(new TextBlock
            {
                Text = $"{Loc.T("ext.lbl.box")} {UnitHelper.Format(boxW, _settings.DisplayUnit)} × {UnitHelper.Format(boxH, _settings.DisplayUnit)} {u}",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                TextAlignment = TextAlignment.Center
            });

            centerStack.Children.Add(new TextBlock
            {
                Text = $"{Loc.T("ext.lbl.hatch")} {UnitHelper.Format(hatchW, _settings.DisplayUnit)} × {UnitHelper.Format(hatchH, _settings.DisplayUnit)} {u}",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                TextAlignment = TextAlignment.Center
            });

            centerStack.Children.Add(new TextBlock
            {
                Text = $"{Loc.T("ext.lbl.trim")} {UnitHelper.Format(trimW, _settings.DisplayUnit)} × {UnitHelper.Format(trimH, _settings.DisplayUnit)} {u}",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x03, 0x69, 0xA1)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            });

            var centerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3),
                Child = centerStack
            };
            centerBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(centerBorder, cx - centerBorder.DesiredSize.Width / 2.0);
            Canvas.SetTop(centerBorder, cy - centerBorder.DesiredSize.Height / 2.0);
            DiagramCanvas.Children.Add(centerBorder);

            // 7. Dimension Callout: Niche Dimensions at the Top
            var nicheLabel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0xEE, 0xF8, 0xFA, 0xFC)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                Child = new TextBlock
                {
                    Text = $"{Loc.T("ext.lbl.niche")} {UnitHelper.Format(nicheW, _settings.DisplayUnit)} × {UnitHelper.Format(nicheH, _settings.DisplayUnit)} {u}",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55))
                }
            };
            nicheLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(nicheLabel, cx - nicheLabel.DesiredSize.Width / 2.0);
            Canvas.SetTop(nicheLabel, Math.Max(6, nicheRect.Top - nicheLabel.DesiredSize.Height - 4));
            DiagramCanvas.Children.Add(nicheLabel);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
