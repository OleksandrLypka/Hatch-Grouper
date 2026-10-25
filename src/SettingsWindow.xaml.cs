using System.Windows;
using System.Windows.Controls;

namespace HatchGrouper
{
    /// <summary>Dialog with the program settings (units of measurement, language).</summary>
    public partial class SettingsWindow : Window
    {
        public LengthUnit InputUnit { get; private set; }
        public LengthUnit ParamUnit { get; private set; }
        public LengthUnit DisplayUnit { get; private set; }
        public AppLanguage SelectedLanguage { get; private set; }

        public SettingsWindow(Settings current)
        {
            InitializeComponent();

            Title = Loc.T("set.title");
            UnitsHeader.Text = Loc.T("set.units");
            InputUnitLabel.Text = Loc.T("set.input");
            ParamUnitLabel.Text = Loc.T("set.params");
            DisplayUnitLabel.Text = Loc.T("set.display");
            LanguageLabel.Text = Loc.T("set.language");
            NoteText.Text = Loc.T("set.note");
            VersionText.Text = Loc.T("set.version", AppInfo.VersionText) + "  |  " + Loc.T("set.author", AppInfo.Author);
            OkButton.Content = Loc.T("btn.ok");
            CancelButton.Content = Loc.T("btn.cancel");

            // Order of items = order of the enum values (Cm, Mm) / (Uk, En)
            foreach (var box in new[] { InputUnitBox, ParamUnitBox, DisplayUnitBox })
            {
                box.Items.Add(UnitHelper.Name(LengthUnit.Cm));
                box.Items.Add(UnitHelper.Name(LengthUnit.Mm));
            }
            LanguageBox.Items.Add("Українська");
            LanguageBox.Items.Add("English");
            LanguageBox.Items.Add("Polski");

            InputUnitBox.SelectedIndex = (int)current.InputUnit;
            ParamUnitBox.SelectedIndex = (int)current.ParamUnit;
            DisplayUnitBox.SelectedIndex = (int)current.DisplayUnit;
            LanguageBox.SelectedIndex = (int)current.Language;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            InputUnit = (LengthUnit)InputUnitBox.SelectedIndex;
            ParamUnit = (LengthUnit)ParamUnitBox.SelectedIndex;
            DisplayUnit = (LengthUnit)DisplayUnitBox.SelectedIndex;
            SelectedLanguage = (AppLanguage)LanguageBox.SelectedIndex;
            DialogResult = true;
        }
    }
}

