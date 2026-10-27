using System.Windows;

namespace HatchGrouper
{
    /// <summary>Dialog with the program settings (units of measurement, tolerance mode, language).</summary>
    public partial class SettingsWindow : Window
    {
        public LengthUnit InputUnit { get; private set; }
        public LengthUnit ParamUnit { get; private set; }
        public LengthUnit DisplayUnit { get; private set; }
        public bool SeparateTolerance { get; private set; }
        public BoxRounding BoxRounding { get; private set; }
        public AppLanguage SelectedLanguage { get; private set; }

        public SettingsWindow(Settings current)
        {
            InitializeComponent();

            Title = Loc.T("set.title");
            UnitsHeader.Text = Loc.T("set.units");
            InputUnitLabel.Text = Loc.T("set.input");
            ParamUnitLabel.Text = Loc.T("set.params");
            DisplayUnitLabel.Text = Loc.T("set.display");
            ToleranceLabel.Text = Loc.T("set.tolerance");
            RoundingLabel.Text = Loc.T("set.rounding");
            LanguageLabel.Text = Loc.T("set.language");
            NoteText.Text = Loc.T("set.note");
            VersionText.Text = Loc.T("set.version", AppInfo.VersionText) + "  |  " + Loc.T("set.author", AppInfo.Author);
            OkButton.Content = Loc.T("btn.ok");
            CancelButton.Content = Loc.T("btn.cancel");

            // Order of items = order of the enum values (Cm, Mm) / (false, true) / (Uk, En, Pl)
            foreach (var box in new[] { InputUnitBox, ParamUnitBox, DisplayUnitBox })
            {
                box.Items.Add(UnitHelper.Name(LengthUnit.Cm));
                box.Items.Add(UnitHelper.Name(LengthUnit.Mm));
            }
            ToleranceBox.Items.Add(Loc.T("set.tol.sym"));
            ToleranceBox.Items.Add(Loc.T("set.tol.sep"));
            RoundingBox.Items.Add(Loc.T("set.round.nearest"));
            RoundingBox.Items.Add(Loc.T("set.round.up"));
            RoundingBox.Items.Add(Loc.T("set.round.down"));
            LanguageBox.Items.Add("Українська");
            LanguageBox.Items.Add("English");
            LanguageBox.Items.Add("Polski");

            InputUnitBox.SelectedIndex = (int)current.InputUnit;
            ParamUnitBox.SelectedIndex = (int)current.ParamUnit;
            DisplayUnitBox.SelectedIndex = (int)current.DisplayUnit;
            ToleranceBox.SelectedIndex = current.SeparateTolerance ? 1 : 0;
            RoundingBox.SelectedIndex = (int)current.BoxRounding;
            LanguageBox.SelectedIndex = (int)current.Language;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            InputUnit = (LengthUnit)InputUnitBox.SelectedIndex;
            ParamUnit = (LengthUnit)ParamUnitBox.SelectedIndex;
            DisplayUnit = (LengthUnit)DisplayUnitBox.SelectedIndex;
            SeparateTolerance = ToleranceBox.SelectedIndex == 1;
            BoxRounding = (BoxRounding)RoundingBox.SelectedIndex;
            SelectedLanguage = (AppLanguage)LanguageBox.SelectedIndex;
            DialogResult = true;
        }
    }
}
