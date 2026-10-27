using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace HatchGrouper
{
    /// <summary>Full-resolution modal image viewer with zoom, pan, and fit controls.</summary>
    public partial class ImagePreviewWindow : Window
    {
        private bool _isDragging;
        private Point _lastPoint;
        private double _currentScale = 1.0;

        public ImagePreviewWindow(BitmapSource source, string title)
        {
            InitializeComponent();

            Title = string.IsNullOrWhiteSpace(title) ? Loc.T("img.preview.title") : title;
            HeaderTitle.Text = Title;

            ZoomInBtn.ToolTip = Loc.T("img.zoom.in");
            ZoomOutBtn.ToolTip = Loc.T("img.zoom.out");
            FitBtn.ToolTip = Loc.T("img.zoom.fit");
            ResetBtn.ToolTip = Loc.T("img.zoom.reset");
            BottomHintText.Text = Loc.T("img.hint");

            PreviewImage.Source = source;

            Loaded += (_, _) => FitZoom();
            SizeChanged += (_, _) =>
            {
                // When resized, keep centered if near fit
                if (Math.Abs(_currentScale - GetFitScale()) < 0.05)
                {
                    FitZoom();
                }
            };
        }

        private double GetFitScale()
        {
            if (PreviewImage.Source is not BitmapSource bmp || bmp.PixelWidth <= 0 || bmp.PixelHeight <= 0)
                return 1.0;

            double viewW = Math.Max(50, ViewportGrid.ActualWidth - 40);
            double viewH = Math.Max(50, ViewportGrid.ActualHeight - 40);

            double scaleX = viewW / bmp.PixelWidth;
            double scaleY = viewH / bmp.PixelHeight;
            return Math.Min(scaleX, scaleY);
        }

        private void FitZoom()
        {
            double fit = GetFitScale();
            SetZoom(fit, resetTranslation: true);
        }

        private void ResetZoom()
        {
            SetZoom(1.0, resetTranslation: true);
        }

        private void SetZoom(double scale, bool resetTranslation = false, Point? centerPoint = null)
        {
            scale = Math.Clamp(scale, 0.15, 8.0);
            double oldScale = _currentScale;
            _currentScale = scale;

            ImageScale.ScaleX = scale;
            ImageScale.ScaleY = scale;

            if (resetTranslation)
            {
                ImageTranslate.X = 0;
                ImageTranslate.Y = 0;
            }
            else if (centerPoint.HasValue && oldScale > 0)
            {
                Point center = centerPoint.Value;
                double viewCenterX = ViewportGrid.ActualWidth / 2.0;
                double viewCenterY = ViewportGrid.ActualHeight / 2.0;

                double mouseRelX = center.X - viewCenterX;
                double mouseRelY = center.Y - viewCenterY;

                double ratio = scale / oldScale;
                ImageTranslate.X = mouseRelX - (mouseRelX - ImageTranslate.X) * ratio;
                ImageTranslate.Y = mouseRelY - (mouseRelY - ImageTranslate.Y) * ratio;
            }

            ZoomLevelText.Text = $"{(int)Math.Round(_currentScale * 100)}%";
        }

        private void Zoom(double factor, Point? centerPoint = null)
        {
            SetZoom(_currentScale * factor, resetTranslation: false, centerPoint: centerPoint);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => Zoom(1.25);
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => Zoom(1.0 / 1.25);
        private void FitZoom_Click(object sender, RoutedEventArgs e) => FitZoom();
        private void ResetZoom_Click(object sender, RoutedEventArgs e) => ResetZoom();
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    // Double click toggles between Fit and 1:1
                    double fit = GetFitScale();
                    if (Math.Abs(_currentScale - fit) < 0.1)
                        ResetZoom();
                    else
                        FitZoom();
                    return;
                }

                _isDragging = true;
                _lastPoint = e.GetPosition(ViewportGrid);
                ViewportGrid.CaptureMouse();
                ViewportGrid.Cursor = Cursors.SizeAll;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                FitZoom();
            }
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point current = e.GetPosition(ViewportGrid);
            ImageTranslate.X += current.X - _lastPoint.X;
            ImageTranslate.Y += current.Y - _lastPoint.Y;
            _lastPoint = current;
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ViewportGrid.ReleaseMouseCapture();
                ViewportGrid.Cursor = Cursors.Arrow;
            }
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.2 : (1.0 / 1.2);
            Zoom(factor, e.GetPosition(ViewportGrid));
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
            else if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                Zoom(1.25);
            }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                Zoom(1.0 / 1.25);
            }
            else if (e.Key == Key.D0 || e.Key == Key.NumPad0)
            {
                FitZoom();
            }
            else if (e.Key == Key.D1 || e.Key == Key.NumPad1)
            {
                ResetZoom();
            }
        }
    }
}
