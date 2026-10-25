using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HatchGrouper
{
    public partial class VisualizerWindow : Window
    {
        private readonly List<NicheGroup> _groups;
        private readonly Settings _settings;
        private int? _highlightedGroup = null;

        // Base world bounds (mm) calculated from all niches
        private double _baseMinX, _baseMaxX, _baseMinY, _baseMaxY;
        // Current visible world bounds (mm) updated by zoom and pan
        private double _viewMinX, _viewMaxX, _viewMinY, _viewMaxY;
        private bool _hasInitializedBounds = false;

        // Drag-to-pan state
        private bool _isDragging = false;
        private Point _lastDragPoint;

        // Modern distinct colors for groups
        private static readonly Color[] Palette = new[]
        {
            Color.FromRgb(0x25, 0x63, 0xEB), // Blue
            Color.FromRgb(0xD9, 0x77, 0x06), // Amber
            Color.FromRgb(0x05, 0x96, 0x69), // Emerald
            Color.FromRgb(0x7C, 0x3A, 0xED), // Violet
            Color.FromRgb(0xDC, 0x26, 0x26), // Red
            Color.FromRgb(0x08, 0x91, 0xB2), // Cyan
            Color.FromRgb(0xEA, 0x58, 0x0C), // Orange
            Color.FromRgb(0x4F, 0x46, 0xE5), // Indigo
            Color.FromRgb(0xDB, 0x27, 0x77), // Pink
            Color.FromRgb(0x65, 0xA3, 0x0D), // Lime
            Color.FromRgb(0x02, 0x84, 0xC7), // Sky
            Color.FromRgb(0x93, 0x33, 0xEA), // Purple
        };

        public VisualizerWindow(List<NicheGroup> groups, Settings settings)
        {
            InitializeComponent();
            _groups = groups ?? new List<NicheGroup>();
            _settings = settings ?? new Settings();

            Title = Loc.T("vis.title");
            LegendHeader.Text = Loc.T("vis.legend");
            ResetFilterBtn.Content = Loc.T("vis.showall");
            ResetZoomBtn.ToolTip = Loc.T("vis.zoom.reset");
            HintText.Text = Loc.T("vis.hint");

            var u = UnitHelper.Name(_settings.DisplayUnit);
            LegendSub.Text = Loc.T("status.result",
                _groups.Sum(g => g.Niches.Count),
                _groups.Count,
                _settings.ToleranceWMm == _settings.ToleranceHMm
                    ? UnitHelper.Format(_settings.ToleranceWMm, _settings.DisplayUnit)
                    : $"{UnitHelper.Format(_settings.ToleranceWMm, _settings.DisplayUnit)} × {UnitHelper.Format(_settings.ToleranceHMm, _settings.DisplayUnit)}",
                u,
                _settings.MaxGapWMm == _settings.MaxGapHMm
                    ? UnitHelper.Format(_settings.MaxGapWMm, _settings.DisplayUnit)
                    : $"{UnitHelper.Format(_settings.MaxGapWMm, _settings.DisplayUnit)} × {UnitHelper.Format(_settings.MaxGapHMm, _settings.DisplayUnit)}");

            CalculateBaseBounds();
            ResetView();
            BuildLegend();

            ChartHost.SizeChanged += (_, _) => Redraw();
            Loaded += (_, _) => Redraw();
        }

        private Color GetColor(int index) => Palette[index % Palette.Length];

        private void CalculateBaseBounds()
        {
            var allNiches = _groups.SelectMany(g => g.Niches).ToList();
            if (allNiches.Count == 0)
            {
                _baseMinX = 0; _baseMaxX = 1000;
                _baseMinY = 0; _baseMaxY = 1000;
                return;
            }

            int minW = allNiches.Min(n => n.WidthMm);
            int maxW = allNiches.Max(n => n.WidthMm);
            int minH = allNiches.Min(n => n.HeightMm);
            int maxH = allNiches.Max(n => n.HeightMm);

            int padW = Math.Max(35, (int)((maxW - minW) * 0.12));
            int padH = Math.Max(35, (int)((maxH - minH) * 0.12));

            _baseMinX = minW - padW;
            _baseMaxX = maxW + padW;
            _baseMinY = minH - padH;
            _baseMaxY = maxH + padH;
            _hasInitializedBounds = true;
        }

        private void ResetView()
        {
            _viewMinX = _baseMinX;
            _viewMaxX = _baseMaxX;
            _viewMinY = _baseMinY;
            _viewMaxY = _baseMaxY;
        }

        private void BuildLegend()
        {
            LegendPanel.Children.Clear();
            var u = UnitHelper.Name(_settings.DisplayUnit);

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                int groupIndex = i;
                var color = GetColor(i);

                var card = new Border
                {
                    Background = _highlightedGroup == i ? new SolidColorBrush(Color.FromArgb(0x28, color.R, color.G, color.B)) : Brushes.White,
                    BorderBrush = _highlightedGroup == i ? new SolidColorBrush(color) : new SolidColorBrush(Color.FromRgb(0xEA, 0xEA, 0xEE)),
                    BorderThickness = new Thickness(_highlightedGroup == i ? 2 : 1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = Cursors.Hand
                };

                var sp = new StackPanel();

                // Header with color badge
                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                var badge = new Border
                {
                    Width = 14,
                    Height = 14,
                    CornerRadius = new CornerRadius(7),
                    Background = new SolidColorBrush(color),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var titleText = new TextBlock
                {
                    Text = $"{Loc.T("vis.group.title", i + 1)} ({g.Niches.Count} {Loc.T("col.count")})",
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                headerRow.Children.Add(badge);
                headerRow.Children.Add(titleText);
                sp.Children.Add(headerRow);

                // Box & Hatch sizes
                string boxStr = $"{UnitHelper.Format(g.BoxW, _settings.DisplayUnit)} × {UnitHelper.Format(g.BoxH, _settings.DisplayUnit)} {u}";
                string hatchStr = $"{UnitHelper.Format(g.HatchW, _settings.DisplayUnit)} × {UnitHelper.Format(g.HatchH, _settings.DisplayUnit)} {u}";
                sp.Children.Add(new TextBlock { Text = $"{Loc.T("vis.box.label", boxStr)}", FontSize = 12 });
                sp.Children.Add(new TextBlock { Text = $"{Loc.T("vis.hatch.label", hatchStr)}", FontSize = 12, Foreground = Brushes.DimGray });

                // Niche sizes summary
                string spanStr = g.MinW == g.MaxW && g.MinH == g.MaxH
                    ? $"{UnitHelper.Format(g.MinW, _settings.DisplayUnit)} × {UnitHelper.Format(g.MinH, _settings.DisplayUnit)} {u}"
                    : $"{UnitHelper.Format(g.MinW, _settings.DisplayUnit)}–{UnitHelper.Format(g.MaxW, _settings.DisplayUnit)} × {UnitHelper.Format(g.MinH, _settings.DisplayUnit)}–{UnitHelper.Format(g.MaxH, _settings.DisplayUnit)} {u}";
                sp.Children.Add(new TextBlock { Text = Loc.T("vis.niches.label", spanStr), FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 0) });

                card.Child = sp;

                card.MouseDown += (_, _) =>
                {
                    _highlightedGroup = _highlightedGroup == groupIndex ? null : groupIndex;
                    ResetFilterBtn.Visibility = _highlightedGroup.HasValue ? Visibility.Visible : Visibility.Collapsed;
                    BuildLegend();
                    Redraw();
                };

                LegendPanel.Children.Add(card);
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            _highlightedGroup = null;
            ResetFilterBtn.Visibility = Visibility.Collapsed;
            BuildLegend();
            Redraw();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomByFactor(1.3, ChartHost.ActualWidth / 2, ChartHost.ActualHeight / 2);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomByFactor(0.77, ChartHost.ActualWidth / 2, ChartHost.ActualHeight / 2);
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
            Redraw();
        }

        private void ChartHost_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.25 : 0.8;
            var pos = e.GetPosition(ChartHost);
            ZoomByFactor(factor, pos.X, pos.Y);
        }

        private void ZoomByFactor(double factor, double mouseScreenX, double mouseScreenY)
        {
            if (!_hasInitializedBounds) return;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;
            if (width < 100 || height < 100) return;

            double leftPad = 65, rightPad = 30, topPad = 30, bottomPad = 50;
            double plotW = Math.Max(10, width - leftPad - rightPad);
            double plotH = Math.Max(10, height - topPad - bottomPad);

            double clampedX = Math.Clamp(mouseScreenX, leftPad, leftPad + plotW);
            double clampedY = Math.Clamp(mouseScreenY, topPad, topPad + plotH);

            double currentSpanX = _viewMaxX - _viewMinX;
            double currentSpanY = _viewMaxY - _viewMinY;

            double mouseWorldX = _viewMinX + (clampedX - leftPad) / plotW * currentSpanX;
            double mouseWorldY = _viewMinY + (height - bottomPad - clampedY) / plotH * currentSpanY;

            double newSpanX = currentSpanX / factor;
            double newSpanY = currentSpanY / factor;

            // Prevent over-zooming (e.g. less than 15 mm or more than 4x base span)
            double baseSpanX = _baseMaxX - _baseMinX;
            double baseSpanY = _baseMaxY - _baseMinY;
            if (newSpanX < 15 || newSpanX > baseSpanX * 4) return;
            if (newSpanY < 15 || newSpanY > baseSpanY * 4) return;

            double ratioX = (mouseWorldX - _viewMinX) / currentSpanX;
            double ratioY = (mouseWorldY - _viewMinY) / currentSpanY;

            _viewMinX = mouseWorldX - ratioX * newSpanX;
            _viewMaxX = _viewMinX + newSpanX;
            _viewMinY = mouseWorldY - ratioY * newSpanY;
            _viewMaxY = _viewMinY + newSpanY;

            Redraw();
        }

        private void ChartHost_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
            {
                _isDragging = true;
                _lastDragPoint = e.GetPosition(ChartHost);
                ChartHost.CaptureMouse();
                ChartHost.Cursor = Cursors.SizeAll;
            }
        }

        private void ChartHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var currentPoint = e.GetPosition(ChartHost);
            double deltaX = currentPoint.X - _lastDragPoint.X;
            double deltaY = currentPoint.Y - _lastDragPoint.Y;
            _lastDragPoint = currentPoint;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;
            double leftPad = 65, rightPad = 30, topPad = 30, bottomPad = 50;
            double plotW = Math.Max(10, width - leftPad - rightPad);
            double plotH = Math.Max(10, height - topPad - bottomPad);

            double worldDeltaX = deltaX / plotW * (_viewMaxX - _viewMinX);
            double worldDeltaY = -deltaY / plotH * (_viewMaxY - _viewMinY);

            _viewMinX -= worldDeltaX;
            _viewMaxX -= worldDeltaX;
            _viewMinY -= worldDeltaY;
            _viewMaxY -= worldDeltaY;

            Redraw();
        }

        private void ChartHost_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ChartHost.ReleaseMouseCapture();
                ChartHost.Cursor = Cursors.Arrow;
            }
        }

        private void Redraw()
        {
            ChartCanvas.Children.Clear();
            if (_groups.Count == 0 || !_hasInitializedBounds) return;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;
            if (width < 100 || height < 100) return;

            double leftPad = 65;
            double rightPad = 30;
            double topPad = 30;
            double bottomPad = 50;

            double plotW = Math.Max(10, width - leftPad - rightPad);
            double plotH = Math.Max(10, height - topPad - bottomPad);

            double MapX(double mm) => leftPad + (mm - _viewMinX) / (_viewMaxX - _viewMinX) * plotW;
            double MapY(double mm) => height - bottomPad - (mm - _viewMinY) / (_viewMaxY - _viewMinY) * plotH;

            // 1. Draw Grid Lines and Coordinate Labels
            DrawGrid(_viewMinX, _viewMaxX, _viewMinY, _viewMaxY, MapX, MapY, leftPad, rightPad, topPad, bottomPad, width, height);

            // Container canvas with clipping so zoomed items don't bleed into axis areas
            var plotContent = new Canvas
            {
                Width = plotW,
                Height = plotH,
                Clip = new RectangleGeometry(new Rect(0, 0, plotW, plotH))
            };
            Canvas.SetLeft(plotContent, leftPad);
            Canvas.SetTop(plotContent, topPad);
            ChartCanvas.Children.Add(plotContent);

            double PlotMapX(double mm) => (mm - _viewMinX) / (_viewMaxX - _viewMinX) * plotW;
            double PlotMapY(double mm) => plotH - (mm - _viewMinY) / (_viewMaxY - _viewMinY) * plotH;

            // 2. Draw Group Bounding Boxes
            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                var color = GetColor(i);
                bool isFocus = !_highlightedGroup.HasValue || _highlightedGroup.Value == i;
                double opacity = _highlightedGroup.HasValue ? (isFocus ? 0.35 : 0.04) : 0.20;
                double strokeOpacity = _highlightedGroup.HasValue ? (isFocus ? 1.0 : 0.15) : 0.85;

                double x1 = PlotMapX(g.MinW);
                double x2 = PlotMapX(g.MaxW);
                double y1 = PlotMapY(g.MaxH);
                double y2 = PlotMapY(g.MinH);

                double rectX = Math.Min(x1, x2);
                double rectY = Math.Min(y1, y2);
                double rectW = Math.Max(Math.Abs(x2 - x1), 18);
                double rectH = Math.Max(Math.Abs(y2 - y1), 18);

                if (Math.Abs(x2 - x1) < 18) rectX -= (18 - Math.Abs(x2 - x1)) / 2;
                if (Math.Abs(y2 - y1) < 18) rectY -= (18 - Math.Abs(y2 - y1)) / 2;

                var rect = new Rectangle
                {
                    Width = rectW,
                    Height = rectH,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B)),
                    Stroke = new SolidColorBrush(Color.FromArgb((byte)(strokeOpacity * 255), color.R, color.G, color.B)),
                    StrokeThickness = isFocus && _highlightedGroup.HasValue ? 2.5 : 1.5,
                    RadiusX = 5,
                    RadiusY = 5,
                    ToolTip = $"{Loc.T("vis.group.title", i + 1)} ({g.Niches.Count} {Loc.T("col.count")})\n" +
                              $"{Loc.T("col.box", "")}: {UnitHelper.Format(g.BoxW, _settings.DisplayUnit)} × {UnitHelper.Format(g.BoxH, _settings.DisplayUnit)}\n" +
                              $"{Loc.T("col.hatch", "")}: {UnitHelper.Format(g.HatchW, _settings.DisplayUnit)} × {UnitHelper.Format(g.HatchH, _settings.DisplayUnit)}"
                };

                Canvas.SetLeft(rect, rectX);
                Canvas.SetTop(rect, rectY);
                plotContent.Children.Add(rect);

                // Badge with group number
                if (isFocus)
                {
                    var label = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B)),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 1, 4, 1),
                        Child = new TextBlock
                        {
                            Text = $"№{i + 1}",
                            Foreground = Brushes.White,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold
                        }
                    };
                    Canvas.SetLeft(label, rectX + 3);
                    Canvas.SetTop(label, rectY + 3);
                    plotContent.Children.Add(label);
                }
            }

            // 3. Draw Points for Niche Sizes
            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                var color = GetColor(i);
                bool isFocus = !_highlightedGroup.HasValue || _highlightedGroup.Value == i;
                double pointOpacity = _highlightedGroup.HasValue ? (isFocus ? 1.0 : 0.15) : 1.0;

                var distinctSizes = g.Niches
                    .GroupBy(n => (n.WidthMm, n.HeightMm))
                    .Select(group => new
                    {
                        W = group.Key.WidthMm,
                        H = group.Key.HeightMm,
                        Count = group.Count(),
                        Entrances = string.Join(", ", group.Select(n => n.Entrance).Distinct().OrderBy(e => e))
                    });

                foreach (var pt in distinctSizes)
                {
                    double px = PlotMapX(pt.W);
                    double py = PlotMapY(pt.H);
                    double radius = pt.Count > 1 ? 8 : 6;

                    var dot = new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = new SolidColorBrush(Color.FromArgb((byte)(pointOpacity * 255), color.R, color.G, color.B)),
                        Stroke = Brushes.White,
                        StrokeThickness = 1.5,
                        Cursor = Cursors.Hand,
                        ToolTip = Loc.T("vis.point.tooltip",
                            UnitHelper.Format(pt.W, _settings.DisplayUnit),
                            UnitHelper.Format(pt.H, _settings.DisplayUnit),
                            UnitHelper.Name(_settings.DisplayUnit),
                            pt.Count,
                            pt.Entrances,
                            i + 1)
                    };

                    Canvas.SetLeft(dot, px - radius);
                    Canvas.SetTop(dot, py - radius);
                    plotContent.Children.Add(dot);

                    // Point size text label
                    if (isFocus)
                    {
                        string countTag = pt.Count > 1 ? $" ({pt.Count})" : "";
                        var text = new TextBlock
                        {
                            Text = $"{UnitHelper.Format(pt.W, _settings.DisplayUnit)}×{UnitHelper.Format(pt.H, _settings.DisplayUnit)}{countTag}",
                            FontSize = 9.5,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x38)),
                            FontWeight = FontWeights.SemiBold
                        };
                        Canvas.SetLeft(text, px + radius + 3);
                        Canvas.SetTop(text, py - 6);
                        plotContent.Children.Add(text);
                    }
                }
            }
        }

        private void DrawGrid(double minX, double maxX, double minY, double maxY,
            Func<double, double> mapX, Func<double, double> mapY,
            double leftPad, double rightPad, double topPad, double bottomPad,
            double canvasW, double canvasH)
        {
            var u = UnitHelper.Name(_settings.DisplayUnit);
            var gridPen = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xF2));
            var axisPen = new SolidColorBrush(Color.FromRgb(0xBB, 0xBB, 0xC4));

            double stepX = CalculateNiceStep(maxX - minX);
            double stepY = CalculateNiceStep(maxY - minY);

            double startX = Math.Ceiling(minX / stepX) * stepX;
            for (double x = startX; x <= maxX; x += stepX)
            {
                double px = mapX(x);
                if (px < leftPad || px > canvasW - rightPad) continue;

                var line = new Line
                {
                    X1 = px, Y1 = topPad,
                    X2 = px, Y2 = canvasH - bottomPad,
                    Stroke = gridPen,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                ChartCanvas.Children.Add(line);

                var txt = new TextBlock
                {
                    Text = UnitHelper.Format(x, _settings.DisplayUnit),
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Center,
                    Width = 60
                };
                Canvas.SetLeft(txt, px - 30);
                Canvas.SetTop(txt, canvasH - bottomPad + 6);
                ChartCanvas.Children.Add(txt);
            }

            double startY = Math.Ceiling(minY / stepY) * stepY;
            for (double y = startY; y <= maxY; y += stepY)
            {
                double py = mapY(y);
                if (py < topPad || py > canvasH - bottomPad) continue;

                var line = new Line
                {
                    X1 = leftPad, Y1 = py,
                    X2 = canvasW - rightPad, Y2 = py,
                    Stroke = gridPen,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                ChartCanvas.Children.Add(line);

                var txt = new TextBlock
                {
                    Text = UnitHelper.Format(y, _settings.DisplayUnit),
                    FontSize = 10,
                    Foreground = Brushes.Gray,
                    TextAlignment = TextAlignment.Right,
                    Width = 50
                };
                Canvas.SetLeft(txt, leftPad - 55);
                Canvas.SetTop(txt, py - 7);
                ChartCanvas.Children.Add(txt);
            }

            // Axes border lines
            var xAxis = new Line
            {
                X1 = leftPad, Y1 = canvasH - bottomPad,
                X2 = canvasW - rightPad, Y2 = canvasH - bottomPad,
                Stroke = axisPen, StrokeThickness = 1.5
            };
            ChartCanvas.Children.Add(xAxis);

            var yAxis = new Line
            {
                X1 = leftPad, Y1 = topPad,
                X2 = leftPad, Y2 = canvasH - bottomPad,
                Stroke = axisPen, StrokeThickness = 1.5
            };
            ChartCanvas.Children.Add(yAxis);

            // Axis Title: X (Width)
            var xTitle = new TextBlock
            {
                Text = Loc.T("vis.width", u),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.DimGray
            };
            Canvas.SetLeft(xTitle, leftPad + (canvasW - leftPad - rightPad) / 2 - 40);
            Canvas.SetTop(xTitle, canvasH - bottomPad + 26);
            ChartCanvas.Children.Add(xTitle);

            // Axis Title: Y (Height)
            var yTitle = new TextBlock
            {
                Text = Loc.T("vis.height", u),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.DimGray,
                RenderTransform = new RotateTransform(-90)
            };
            Canvas.SetLeft(yTitle, 12);
            Canvas.SetTop(yTitle, topPad + (canvasH - topPad - bottomPad) / 2 + 35);
            ChartCanvas.Children.Add(yTitle);
        }

        private static double CalculateNiceStep(double range)
        {
            if (range <= 0) return 50;
            double rough = range / 7.0;
            double[] candidates = { 5, 10, 20, 50, 100, 200, 500, 1000 };
            return candidates.OrderBy(c => Math.Abs(c - rough)).First();
        }
    }
}
