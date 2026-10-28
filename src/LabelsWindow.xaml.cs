using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace HatchGrouper
{
    /// <summary>Window for previewing and exporting printable worker installation labels to Word (.docx).</summary>
    public partial class LabelsWindow : Window
    {
        private readonly List<NicheDetailRow> _allItems;
        private readonly LengthUnit _displayUnit;

        public LabelsWindow(List<NicheDetailRow> items, LengthUnit displayUnit)
        {
            InitializeComponent();
            _allItems = items ?? new List<NicheDetailRow>();
            _displayUnit = displayUnit;

            Title = Loc.T("labels.win.title");
            ExportDocxBtn.Content = Loc.T("labels.btn.docx");
            CopyTextBtn.Content = Loc.T("labels.btn.copy");
            SortLabel.Text = Loc.T("labels.sort.label");
            EntranceFilterLabel.Text = Loc.T("labels.filter.entrance");
            CloseBtn.Content = Loc.T("btn.close");
            BottomHintText.Text = Loc.T("labels.bottom.hint");

            // Setup Sort ComboBox
            SortComboBox.Items.Add(Loc.T("labels.sort.entrance"));
            SortComboBox.Items.Add(Loc.T("labels.sort.box"));
            SortComboBox.SelectedIndex = 0;

            // Setup Entrance ComboBox
            EntranceComboBox.Items.Add(Loc.T("labels.filter.all"));
            var entrances = _allItems.Select(n => n.Entrance).Distinct().OrderBy(e => e).ToList();
            foreach (var ent in entrances)
            {
                EntranceComboBox.Items.Add(Loc.T("labels.entrance.item", ent));
            }
            EntranceComboBox.SelectedIndex = 0;

            RenderCards();
        }

        private List<NicheDetailRow> GetFilteredAndSorted()
        {
            var list = _allItems.AsEnumerable();

            // Entrance filter
            if (EntranceComboBox.SelectedIndex > 0)
            {
                var entrances = _allItems.Select(n => n.Entrance).Distinct().OrderBy(e => e).ToList();
                int targetEnt = entrances[EntranceComboBox.SelectedIndex - 1];
                list = list.Where(n => n.Entrance == targetEnt);
            }

            // Sort
            if (SortComboBox.SelectedIndex == 1)
            {
                // Group by box size (GroupNo), then entrance, then floor
                list = list.OrderBy(n => n.GroupNo)
                           .ThenBy(n => n.Entrance)
                           .ThenBy(n => n.NicheNo);
            }
            else
            {
                // Order by entrance, then floor
                list = list.OrderBy(n => n.Entrance)
                           .ThenBy(n => n.NicheNo);
            }

            return list.ToList();
        }

        private void FilterOrSort_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (LabelsContainer == null) return;
            RenderCards();
        }

        private void RenderCards()
        {
            var items = GetFilteredAndSorted();
            TotalCountText.Text = Loc.T("labels.total.count", items.Count);

            LabelsContainer.Children.Clear();
            string u = UnitHelper.Name(_displayUnit);

            foreach (var item in items)
            {
                LabelsContainer.Children.Add(CreateLabelBadge(item, u));
            }
        }

        private FrameworkElement CreateLabelBadge(NicheDetailRow item, string unit)
        {
            var card = new Border
            {
                Width = 420,
                Margin = new Thickness(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                SnapsToDevicePixels = true
            };

            var sp = new StackPanel();

            // Header banner
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF9, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xBA, 0xE6, 0xFD)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(12, 8, 12, 8)
            };

            var headerDock = new DockPanel();
            var idBadge = new TextBlock
            {
                Text = item.Id,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(idBadge, Dock.Right);
            headerDock.Children.Add(idBadge);

            var locationText = new TextBlock
            {
                Text = $"{Loc.T("labels.field.entrance")}: {item.Entrance}    {Loc.T("labels.field.floor")}: {item.NicheNo}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0x03, 0x69, 0xA1)),
                VerticalAlignment = VerticalAlignment.Center
            };
            headerDock.Children.Add(locationText);
            header.Child = headerDock;
            sp.Children.Add(header);

            // Body content
            var body = new StackPanel { Margin = new Thickness(12, 10, 12, 10) };

            // Box drawing size (prominent)
            var boxPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            boxPanel.Children.Add(new TextBlock
            {
                Text = $"{Loc.T("labels.field.box")}: ",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                VerticalAlignment = VerticalAlignment.Center
            });
            boxPanel.Children.Add(new TextBlock
            {
                Text = $"{item.BoxDrawing} {Loc.T("unit.mm")}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)),
                VerticalAlignment = VerticalAlignment.Center
            });
            if (!string.IsNullOrEmpty(item.Box) && item.Box != item.BoxDrawing)
            {
                boxPanel.Children.Add(new TextBlock
                {
                    Text = $"  ({Loc.T("labels.field.exact")}: {item.Box} {unit})",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            body.Children.Add(boxPanel);

            // Wall niche size
            var nichePanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 6) };
            nichePanel.Children.Add(new TextBlock
            {
                Text = $"{Loc.T("labels.field.niche")}: ",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                VerticalAlignment = VerticalAlignment.Center
            });
            nichePanel.Children.Add(new TextBlock
            {
                Text = $"{item.Niche} {unit}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                VerticalAlignment = VerticalAlignment.Center
            });
            body.Children.Add(nichePanel);

            // Hatch door & Trim
            var hatchPanel = new TextBlock
            {
                Text = $"{Loc.T("labels.field.hatch")}: {item.Hatch} {unit}   |   {Loc.T("labels.field.trim")}: {item.WithTrim} {unit}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            body.Children.Add(hatchPanel);

            // Footer row: Group & Drawing code/note
            var footerDock = new DockPanel();
            var grpBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xCC, 0xFB, 0xF1)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = item.GroupName,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E))
                }
            };
            footerDock.Children.Add(grpBadge);

            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                var noteBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = $"{Loc.T("labels.field.note")}: {item.Note}",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09))
                    }
                };
                footerDock.Children.Add(noteBadge);
            }

            body.Children.Add(footerDock);
            sp.Children.Add(body);
            card.Child = sp;
            return card;
        }

        private void ExportDocx_Click(object sender, RoutedEventArgs e)
        {
            var items = GetFilteredAndSorted();
            if (items.Count == 0) return;

            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = Loc.T("filter.docx"),
                    DefaultExt = ".docx",
                    FileName = "Installation_Labels.docx"
                };

                if (dlg.ShowDialog(this) != true) return;

                DocxLabelWriter.Export(dlg.FileName, items, _displayUnit);

                var res = MessageBox.Show(this,
                    Loc.T("labels.export.success", dlg.FileName) + "\n\n" + Loc.T("labels.open.prompt"),
                    Loc.T("labels.win.title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.T("err.prefix") + ex.Message, Loc.T("err.title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyText_Click(object sender, RoutedEventArgs e)
        {
            var items = GetFilteredAndSorted();
            if (items.Count == 0) return;

            string u = UnitHelper.Name(_displayUnit);
            var sb = new StringBuilder();

            foreach (var item in items)
            {
                sb.AppendLine("══════════════════════════════════════════════════════");
                sb.AppendLine($"{Loc.T("labels.field.entrance").ToUpperInvariant()}: {item.Entrance}    {Loc.T("labels.field.floor").ToUpperInvariant()}: {item.NicheNo}    ({item.Id})");
                sb.AppendLine($"• {Loc.T("labels.field.box")}: {item.BoxDrawing} {Loc.T("unit.mm")} ({item.Box} {u})");
                sb.AppendLine($"• {Loc.T("labels.field.niche")}: {item.Niche} {u}");
                sb.AppendLine($"• {Loc.T("labels.field.hatch")}: {item.Hatch} {u}  |  {Loc.T("labels.field.trim")}: {item.WithTrim} {u}");
                string noteSuffix = !string.IsNullOrWhiteSpace(item.Note) ? $"  [{Loc.T("labels.field.note")}: {item.Note}]" : "";
                sb.AppendLine($"• {item.GroupName}{noteSuffix}");
            }
            sb.AppendLine("══════════════════════════════════════════════════════");

            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, Loc.T("labels.copied.msg", items.Count), Loc.T("labels.win.title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
