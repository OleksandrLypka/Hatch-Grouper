using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HatchGrouper
{
    /// <summary>One wall niche. All sizes are stored in millimetres.</summary>
    public class Niche
    {
        public double WidthMm { get; set; }
        public double HeightMm { get; set; }
        public int Entrance { get; set; }
        public int NicheNo { get; set; } // number of the niche inside its entrance ("1)" in the list); shown as "entrance.no"
    }

    /// <summary>Unit of length shown to / entered by the user. Internally everything is millimetres.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LengthUnit { Cm, Mm }

    /// <summary>Rounding mode for the drawing box size.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BoxRounding { Nearest, Up, Down }

    /// <summary>Error whose text is written for the end user and can be shown as is.</summary>
    public class UserException : Exception
    {
        public UserException(string message) : base(message) { }
    }

    /// <summary>Program version (shown in the window title). Set in HatchGrouper.csproj.</summary>
    public static class AppInfo
    {
        public const string Author = "Oleksandr Lypka";

        public static string VersionText
        {
            get
            {
                var info = typeof(AppInfo).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;
                if (!string.IsNullOrEmpty(info))
                {
                    int plusIdx = info.IndexOf('+');
                    return plusIdx > 0 ? info.Substring(0, plusIdx) : info;
                }
                var v = typeof(AppInfo).Assembly.GetName().Version;
                return v == null ? "0.3-beta" : $"{v.Major}.{v.Minor}-beta";
            }
        }
    }

    public static class UnitHelper
    {
        public static string Name(LengthUnit unit) => Loc.T(unit == LengthUnit.Cm ? "unit.cm" : "unit.mm");

        /// <summary>Formats a length given in millimetres in the requested unit (no unit suffix).</summary>
        public static string Format(double mm, LengthUnit unit) =>
            (unit == LengthUnit.Cm ? mm / 10.0 : mm).ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>All tunable parameters. Change them here or in the UI, never inside the algorithm.</summary>
    public class Settings
    {
        public LengthUnit InputUnit { get; set; } = LengthUnit.Cm;   // unit of niche sizes in the input text
        public LengthUnit ParamUnit { get; set; } = LengthUnit.Mm;   // unit of the parameter fields
        public LengthUnit DisplayUnit { get; set; } = LengthUnit.Cm; // unit of sizes in the result table / CSV
        public AppLanguage Language { get; set; } = AppLanguage.En; // interface language, English by default (an app preference, not part of a project)
        public bool SeparateTolerance { get; set; } = false;         // false = symmetric on all sides (default); true = separate W and H
        public BoxRounding BoxRounding { get; set; } = BoxRounding.Nearest; // rounding for the drawing box size

        public double GapMm { get; set; } = 5;            // box-to-niche gap per side (width / all sides)
        public double TrimMm { get; set; } = 60;          // nominal trim protrusion per side (width / all sides)
        public double TrimDiffWMm { get; set; } = 116.2; // trim width - box width (sheet metal bend offset)
        public double TrimDiffHMm { get; set; } = 117.8; // trim height - box height (sheet metal bend offset)
        public double MinLapMm { get; set; } = 25;        // minimal trim overlap on the wall (width / all sides)
        public double HatchDiffWMm { get; set; } = 131.6; // hatch width = trim width - HatchDiffWMm
        public double HatchDiffHMm { get; set; } = 131.2; // hatch height = trim height - HatchDiffHMm
        public double DepthMm { get; set; } = 80;         // box depth
        public double EarMm { get; set; } = 1;            // flat mounting tabs/ears protrusion per side (top, bottom, sides; 0 allowed, standard 1)

        // Height (top/bottom) parameters, used only when SeparateTolerance is true
        public double GapHMm { get; set; } = 5;
        public double TrimHMm { get; set; } = 60;
        public double MinLapHMm { get; set; } = 25;

        [JsonIgnore] public double EffGapHMm => SeparateTolerance ? GapHMm : GapMm;
        [JsonIgnore] public double EffTrimHMm => SeparateTolerance ? TrimHMm : TrimMm;
        [JsonIgnore] public double EffMinLapHMm => SeparateTolerance ? MinLapHMm : MinLapMm;

        /// <summary>Largest gap per side that the trim can still hide (width, height).</summary>
        [JsonIgnore] public double MaxGapWMm => Math.Round(TrimMm - MinLapMm, 2);
        [JsonIgnore] public double MaxGapHMm => Math.Round(EffTrimHMm - EffMinLapHMm, 2);
        [JsonIgnore] public double MaxGapMm => MaxGapWMm;

        /// <summary>
        /// Allowed size spread inside a group (width and height).
        /// Gap per side to box body = GapMm + EarMm + spread / 2, so spread &lt;= 2 * (MaxGap - (GapMm + EarMm)).
        /// </summary>
        [JsonIgnore] public double ToleranceWMm => Math.Round(2 * (MaxGapWMm - (GapMm + EarMm)), 1);
        [JsonIgnore] public double ToleranceHMm => Math.Round(2 * (MaxGapHMm - (EffGapHMm + EarMm)), 1);
        [JsonIgnore] public double ToleranceMm => ToleranceWMm;

        // Backward compatibility for projects saved with older TrimMm property
        [JsonPropertyName("TrimMm")]
        public double? LegacyTrimMm
        {
            get => null;
            set
            {
                if (value.HasValue && value.Value > 0 && TrimDiffWMm == 116.2)
                {
                    TrimDiffWMm = Math.Round(value.Value * 2, 1);
                    TrimDiffHMm = Math.Round(value.Value * 2, 1);
                }
            }
        }
    }

    /// <summary>A set of niches that share one box size.</summary>
    public class NicheGroup
    {
        public List<Niche> Niches { get; set; } = new();
        public double MinW, MaxW, MinH, MaxH;
        public double BoxW, BoxH, TrimW, TrimH;
        public double HatchW, HatchH;
        public double DepthMm;
        public double MaxGapMm, LapMm;

        public void Compute(Settings s)
        {
            MinW = Niches.Min(n => n.WidthMm);  MaxW = Niches.Max(n => n.WidthMm);
            MinH = Niches.Min(n => n.HeightMm); MaxH = Niches.Max(n => n.HeightMm);

            double nominalBoxW = MinW - 2 * (s.GapMm + s.EarMm);
            double nominalBoxH = MinH - 2 * (s.EffGapHMm + s.EarMm);

            TrimW = Math.Round(nominalBoxW + 2 * s.TrimMm, 1);
            TrimH = Math.Round(nominalBoxH + 2 * s.EffTrimHMm, 1);

            BoxW = Math.Round(TrimW - s.TrimDiffWMm, 1);
            BoxH = Math.Round(TrimH - s.TrimDiffHMm, 1);

            HatchW = Math.Round(TrimW - s.HatchDiffWMm, 1);
            HatchH = Math.Round(TrimH - s.HatchDiffHMm, 1);

            DepthMm = s.DepthMm;

            double gapW = (MaxW - BoxW) / 2.0;
            double gapH = (MaxH - BoxH) / 2.0;
            MaxGapMm = Math.Round(Math.Max(gapW, gapH), 1);

            double lapW = (TrimW - MaxW) / 2.0;
            double lapH = (TrimH - MaxH) / 2.0;
            LapMm = Math.Round(Math.Min(lapW, lapH), 1);
        }
    }

    /// <summary>Saved project file: the raw input text plus the settings.</summary>
    public class ProjectData
    {
        public string InputText { get; set; } = "";
        public Settings Settings { get; set; } = new();
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    /// <summary>One display row of the result table.</summary>
    public class GroupRow : INotifyPropertyChanged
    {
        public int No { get; set; }
        public int Count { get; set; }
        public string Niche { get; set; }
        public string Box { get; set; }
        public string BoxDrawing { get; set; } // Rounded drawing size with depth, e.g. "852 × 768 × 80"
        public string WithTrim { get; set; }
        public string Hatch { get; set; }
        public string MaxGap { get; set; }
        public string Lap { get; set; }

        private string _note = "";
        public string Note
        {
            get => _note;
            set
            {
                if (_note != value)
                {
                    _note = value ?? "";
                    OnPropertyChanged();
                }
            }
        }
        public string Composition { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>One item in the ordered niche-by-niche specification.</summary>
    public class NicheDetailRow : INotifyPropertyChanged
    {
        public string Id { get; set; }        // "(1.1)"
        public int Entrance { get; set; }
        public int NicheNo { get; set; }
        public string Niche { get; set; }     // "84 × 89"
        public string Box { get; set; }       // "70.8 × 102.5"
        public string BoxDrawing { get; set; } // "708 × 1025 × 80"
        public string WithTrim { get; set; }  // "82.8 × 114.5"
        public string Hatch { get; set; }     // "69.6 × 101.4"
        public string GroupName { get; set; } // "Група 1"
        public int GroupNo { get; set; }      // 1

        private string _note = "";
        public string Note
        {
            get => _note;
            set
            {
                if (_note != value)
                {
                    _note = value ?? "";
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Parses text pasted from the customer document.</summary>
    public static class NicheParser
    {
        // "84 x 89", "87 x105", "- 91х85" (Latin x, Cyrillic х, or the multiplication sign)
        private static readonly Regex SizeRx = new(@"(\d+(?:[.,]\d+)?)\s*[xXхХ×]\s*(\d+(?:[.,]\d+)?)");
        // "(2 під’їзд)" / "(2 entrance)" / "(2 klatka)" / "Entrance 2" switches the current entrance
        private static readonly Regex EntranceRx = new(@"(\d+)\s*(?:під|entr|klatk)|(?:під|entr|klatk)\S*\s*(\d+)", RegexOptions.IgnoreCase);
        // Niche number at the start of a line: "1)", "12.", "3 )" (but not "84.5 x 89")
        private static readonly Regex NumberRx = new(@"^\s*(\d+)\s*[\).](?!\d)");

        /// <summary>
        /// Input sizes are in <paramref name="unit"/>; the result is in millimetres.
        /// Non-empty lines that are neither a size nor an entrance header are added to <paramref name="skipped"/>.
        /// </summary>
        public static List<Niche> Parse(string text, LengthUnit unit, List<string> skipped = null)
        {
            var result = new List<Niche>();
            int entrance = 1;
            int ordinal = 0; // position of the niche inside the current entrance (used when the line has no "N)" number)
            foreach (var line in text.Split('\n'))
            {
                var size = SizeRx.Match(line);
                if (size.Success)
                {
                    ordinal++;
                    var num = NumberRx.Match(line);
                    result.Add(new Niche
                    {
                        WidthMm = ToMm(size.Groups[1].Value, unit),
                        HeightMm = ToMm(size.Groups[2].Value, unit),
                        Entrance = entrance,
                        NicheNo = num.Success && num.Index + num.Length <= size.Index ? int.Parse(num.Groups[1].Value) : ordinal,
                    });
                    continue;
                }
                var e = EntranceRx.Match(line);
                if (e.Success)
                {
                    entrance = int.Parse(e.Groups[1].Success ? e.Groups[1].Value : e.Groups[2].Value);
                    ordinal = 0;
                    continue;
                }
                if (skipped != null && !string.IsNullOrWhiteSpace(line)) skipped.Add(line.Trim());
            }
            return result;
        }

        private static double ToMm(string value, LengthUnit unit)
        {
            var raw = double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture) * (unit == LengthUnit.Cm ? 10.0 : 1.0);
            return Math.Round(raw, 1);
        }
    }
}
