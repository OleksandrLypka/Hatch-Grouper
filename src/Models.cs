using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HatchGrouper
{
    /// <summary>One wall niche. All sizes are stored in millimetres.</summary>
    public class Niche
    {
        public int WidthMm { get; set; }
        public int HeightMm { get; set; }
        public int Entrance { get; set; }
    }

    /// <summary>Unit of length shown to / entered by the user. Internally everything is millimetres.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LengthUnit { Cm, Mm }

    /// <summary>Error whose text is written for the end user and can be shown as is.</summary>
    public class UserException : Exception
    {
        public UserException(string message) : base(message) { }
    }

    /// <summary>Program version (shown in the window title). Set in HatchGrouper.csproj, stays 0.1 until told otherwise.</summary>
    public static class AppInfo
    {
        public const string Author = "Oleksandr Lypka";

        public static string VersionText
        {
            get
            {
                var v = typeof(AppInfo).Assembly.GetName().Version;
                return v == null ? "0.1" : $"{v.Major}.{v.Minor}";
            }
        }
    }

    public static class UnitHelper
    {
        public static string Name(LengthUnit unit) => Loc.T(unit == LengthUnit.Cm ? "unit.cm" : "unit.mm");

        /// <summary>Formats a length given in millimetres in the requested unit (no unit suffix).</summary>
        public static string Format(double mm, LengthUnit unit) =>
            (unit == LengthUnit.Cm ? mm / 10.0 : mm).ToString("0.#", CultureInfo.InvariantCulture);
    }

    /// <summary>All tunable parameters. Change them here or in the UI, never inside the algorithm.</summary>
    public class Settings
    {
        public LengthUnit InputUnit { get; set; } = LengthUnit.Cm;   // unit of niche sizes in the input text
        public LengthUnit ParamUnit { get; set; } = LengthUnit.Mm;   // unit of the parameter fields (gap, trim, lap, hatch offset)
        public LengthUnit DisplayUnit { get; set; } = LengthUnit.Cm; // unit of sizes in the result table / CSV
        public AppLanguage Language { get; set; } = AppLanguage.En; // interface language, English by default (an app preference, not part of a project)

        public int GapMm { get; set; } = 5;          // box-to-niche gap per side
        public int TrimMm { get; set; } = 60;        // trim protrusion beyond the box per side
        public int MinLapMm { get; set; } = 25;      // minimal trim overlap on the wall (assumption, ask the designer)
        public int HatchOffsetMm { get; set; } = 60; // hatch is smaller than the box by this much per side

        /// <summary>Largest gap per side that the trim can still hide.</summary>
        [JsonIgnore] public int MaxGapMm => TrimMm - MinLapMm;

        /// <summary>
        /// Allowed size spread inside a group (per dimension).
        /// Gap per side = GapMm + spread / 2, so spread &lt;= 2 * (MaxGapMm - GapMm).
        /// </summary>
        [JsonIgnore] public int ToleranceMm => 2 * (MaxGapMm - GapMm);
    }

    /// <summary>A set of niches that share one box size.</summary>
    public class NicheGroup
    {
        public List<Niche> Niches { get; set; } = new();
        public int MinW, MaxW, MinH, MaxH;
        public int BoxW, BoxH, HatchW, HatchH;
        public double MaxGapMm, LapMm;

        public void Compute(Settings s)
        {
            MinW = Niches.Min(n => n.WidthMm);  MaxW = Niches.Max(n => n.WidthMm);
            MinH = Niches.Min(n => n.HeightMm); MaxH = Niches.Max(n => n.HeightMm);
            BoxW = MinW - 2 * s.GapMm;          // the box must fit the smallest niche
            BoxH = MinH - 2 * s.GapMm;
            HatchW = BoxW - 2 * s.HatchOffsetMm;
            HatchH = BoxH - 2 * s.HatchOffsetMm;
            MaxGapMm = s.GapMm + Math.Max(MaxW - MinW, MaxH - MinH) / 2.0;
            LapMm = s.TrimMm - MaxGapMm;
        }
    }

    /// <summary>Saved project file: the raw input text plus the settings.</summary>
    public class ProjectData
    {
        public string InputText { get; set; } = "";
        public Settings Settings { get; set; } = new();
    }

    /// <summary>One display row of the result table.</summary>
    public class GroupRow
    {
        public int No { get; set; }
        public int Count { get; set; }
        public string Niche { get; set; }
        public string Box { get; set; }
        public string Hatch { get; set; }
        public string MaxGap { get; set; }
        public string Lap { get; set; }
        public string Composition { get; set; }
    }

    /// <summary>Parses text pasted from the customer document.</summary>
    public static class NicheParser
    {
        // "84 x 89", "87 x105", "- 91х85" (Latin x, Cyrillic х, or the multiplication sign)
        private static readonly Regex SizeRx = new(@"(\d+(?:[.,]\d+)?)\s*[xXхХ×]\s*(\d+(?:[.,]\d+)?)");
        // "(2 під’їзд)" / "(2 entrance)" / "(2 klatka)" / "Entrance 2" switches the current entrance
        private static readonly Regex EntranceRx = new(@"(\d+)\s*(?:під|entr|klatk)|(?:під|entr|klatk)\S*\s*(\d+)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Input sizes are in <paramref name="unit"/>; the result is in millimetres.
        /// Non-empty lines that are neither a size nor an entrance header are added to <paramref name="skipped"/>.
        /// </summary>
        public static List<Niche> Parse(string text, LengthUnit unit, List<string> skipped = null)
        {
            var result = new List<Niche>();
            int entrance = 1;
            foreach (var line in text.Split('\n'))
            {
                var size = SizeRx.Match(line);
                if (size.Success)
                {
                    result.Add(new Niche
                    {
                        WidthMm = ToMm(size.Groups[1].Value, unit),
                        HeightMm = ToMm(size.Groups[2].Value, unit),
                        Entrance = entrance,
                    });
                    continue;
                }
                var e = EntranceRx.Match(line);
                if (e.Success)
                {
                    entrance = int.Parse(e.Groups[1].Success ? e.Groups[1].Value : e.Groups[2].Value);
                    continue;
                }
                if (skipped != null && !string.IsNullOrWhiteSpace(line)) skipped.Add(line.Trim());
            }
            return result;
        }

        private static int ToMm(string value, LengthUnit unit) =>
            (int)Math.Round(double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture) * (unit == LengthUnit.Cm ? 10 : 1));
    }
}
