# HatchGrouper

A small Windows (WPF) tool that groups wall-niche sizes for water-meter (or others) inspection hatches
into the smallest possible number of standard box sizes,
so that one box can be manufactured for a whole group of similar niches instead of one box per niche.

**Version:** 0.3-beta · **Author:** Oleksandr Lypka · **License:** [CC BY-NC 4.0](LICENSE)

## Features

- Paste the niche list straight from the customer's document (Word, Excel, plain text).
- Exact grouping: finds the minimum number of groups that cover all niches, and assigns borderline niches that fit into several groups so that the maximum gap is minimal.
- Per group: box size, size with trim, hatch size, maximum gap, minimum trim lap, drawing code / note column, and the list of niches with source line numbers (by entrance).
- Editable **Code / Note** column in the table for drawing designations, saved in project files and exported to CSV.
- Adjustable parameters: gap, trim, minimum lap, hatch offset, hatch opening clearance, mounting ears (symmetric on all sides by default; optional separate parameters/tolerance for width and height in **Settings…**).
- Units of measurement (cm / mm) chosen separately for the input list, the parameter fields and the result table.
- Interface languages: English (default), Ukrainian, Polish. Switch in **Settings…**.
- **Formulas & CAD…** window: tabbed window with formulas, plain-language calculation explanation, and a step-by-step visual CAD guide with illustrations showing where to measure your 3D model in SolidWorks/Inventor.
- **Visualization…** window: interactive 2D map of niches and group bounding boxes with zoom, pan, and group highlighting.
- **Extreme cases…** window: visual 2D cross-section schematic and recommendation analysis of critical cases (smallest and largest niches) with wall clearances and trim laps so you don't need to test every single niche.
- **Niche specification tab (1.1, 1.2...)**: natural-order list of every niche with its matching box size, trim, hatch, and group, with one-click clipboard copying (`(1.1) Niche... - Box...`) and CSV export.
- Settings are saved automatically between runs.
- Save / open a whole project (`*.hatch.json`) and export the result to CSV (opens correctly in Excel).
- Readable error messages instead of crashes.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or Visual Studio 2022 with the ".NET desktop development" workload)

## Build and run

```
dotnet build src
dotnet run --project src
```

Or open `src/HatchGrouper.csproj` in Visual Studio and press F5.

## Releases

```
.\publish.ps1                      # interactive: asks to choose Self-Contained or Framework-Dependent
.\publish.ps1 -SelfContained       # single self-contained .exe, works without .NET installed
.\publish.ps1 -FrameworkDependent  # small build, needs the .NET 8 Desktop Runtime
```

The script puts the build and a `.zip` (with `LICENSE` and `README.md`) into the `releases` folder,
named `HatchGrouper-v<version>-win-x64`. The version comes from `<Version>` in `src/HatchGrouper.csproj`.
The contents of `releases` are not committed to git: upload the `.zip` to GitHub → Releases instead.

## Usage

1. Paste the list of niches into the left box (an example is shown on first start).
2. Check the parameters at the top and the units in **Settings…**.
3. Press **Group**.
4. Read the table, then **Save…** the project or **Export CSV…**.

### Input format

One niche per line, `width x height`, in the unit chosen for the input list (cm by default):

```
(1 entrance)
1)  84 x 89
2)  86 x 89
(2 entrance)
1)  100 x 105
```

- A header line switches the current entrance: `(2 під’їзд)`, `(2 entrance)`, `(2 klatka)` or `Entrance 2`.
- The separator can be Latin `x`, Cyrillic `х` or `×`; decimal comma or dot are both accepted.
- Numbering (`1)`, `-`, tabs) and blank lines are ignored.
- Other lines are skipped and counted in a warning under the table.

## How it works

All sizes are millimetres internally.

| Parameter | Default | Meaning |
|---|---|---|
| Gap | 5 | gap between box and niche, per side |
| Trim | 60 | nominal trim flange width beyond the nominal box body |
| Min. lap | 25 | minimal trim overlap on the wall (assumption, confirm with the designer) |
| Hatch smaller than trim (W / H) | 131.6 / 131.2 | how much smaller the hatch width / height is than the outer trim size |
| Ears | 1 | flat mounting tabs/ears on top, bottom and sides (+1 mm per side; 0 allowed) |
| Trim to box bend offset (W / H) | 116.2 / 117.8 | difference between outer trim and box body in CAD sheet metal bend profile |
| Box Depth | 80 | standard box depth callout for drawings |

Formulas:

```
max gap per side       = trim - min. lap
spread tolerance       = 2 * (max gap - (gap + ears))     # per dimension
nominal box size       = smallest niche in the group - 2 * (gap + ears)
size with trim         = nominal box size + 2 * trim
actual CAD box body    = (trim width - 116.2) × (trim height - 117.8)
drawing callout        = rounded(actual box W) × rounded(actual box H) × depth
hatch size (W × H)     = (trim width - 131.6) × (trim height - 131.2)
gap to box body        = (gap + ears) + spread / 2        # spread = largest - smallest niche in the group
gap to ear             = gap to box body - ears
lap on the wall        = trim - gap to box body
```

Group: every distinct (width, height) is a point. A group is a `toleranceW × toleranceH` rectangle
(a square by default when tolerance is symmetric on all sides).
The program searches for the minimal number of rectangles that cover all points
(exact set cover, depth-first search with pruning), and then assigns points that fall into
several chosen rectangles so that the maximum gap across groups is minimal (see [`Grouper.cs`](src/Grouper.cs)).

## CAD & Sheet Metal Profile Customization

> [!NOTE]
> Different manufacturers and sheet metal workshops use different sheet metal thicknesses, bend deductions (K-factors), and construction profiles.

By default, HatchGrouper is configured for an **integrated single-piece bent sheet metal hatch** (where the internal box body and the outer trim flange are formed from a single unfolded sheet):
- Outer trim dimensions are driven by the nominal flange width (e.g. 60 mm nominal trim: `TrimW = NominalBoxW + 2 * TrimMm`).
- Internal box dimensions are calculated via CAD bend difference parameters:
  - `TrimDiffWMm = 116.2 mm`
  - `TrimDiffHMm = 117.8 mm`
  - Result: `ActualBoxW = TrimW - 116.2 mm`, `ActualBoxH = TrimH - 117.8 mm`

If your 3D CAD model or manufacturing technology uses a different profile (for example, a welded frame, a separate bolt-on trim profile, or different bending deductions for thicker/thinner sheet metal), these parameters can be customized in **Settings…** or in the source code to match your specific CAD model and workshop tooling.

For a step-by-step visual tutorial with SolidWorks screenshots, see the **[CAD Measurement Guide (docs/CAD_GUIDE.md)](docs/CAD_GUIDE.md)** or open the **Formulas & CAD…** window directly in the application.

## Project structure

| File | Purpose |
|---|---|
| `src/Models.cs` | data classes, settings, units, input parser |
| `src/Grouper.cs` | grouping algorithm (no UI code, easy to unit-test) |
| `src/Localization.cs` | all UI texts in Ukrainian / English / Polish |
| `src/MainWindow.*` | main window, project save/load, CSV export |
| `src/SettingsWindow.*` | settings dialog (units, language) |
| `src/HelpWindow.*` | formulas and explanation window |
| `src/VisualizerWindow.*` | interactive 2D niche grouping map (zoom, pan, legend) |
| `src/ExtremeCasesWindow.*` | 2D cross-section diagram and recommendation analysis of extreme cases |
| `src/SettingsStore.cs` | saves the settings to `%AppData%\HatchGrouper\settings.json` |

## Adding or fixing a translation

Every text is one line in `src/Localization.cs`: `["key"] = (Ukrainian, English, Polish)`.
To add a language, add a value to `AppLanguage`, one more text to every row of the table (and to `Loc.T`) and an item to the language list in `SettingsWindow`.

## Roadmap

- Export the result to .docx (Open XML SDK).
- Unit tests for `Grouper` (expected result for the sample list: 10 groups at 60 mm tolerance).

## License

Copyright (c) Oleksandr Lypka.

This project is licensed under the
[Creative Commons Attribution-NonCommercial 4.0 International](https://creativecommons.org/licenses/by-nc/4.0/) license
(full text in [LICENSE](LICENSE)).

In plain words:

- ✅ You may use, copy, fork, modify and share the project and your modified versions.
- ✅ You must give credit to **Oleksandr Lypka as the author of the original** and indicate if you made changes.
- ❌ You may not use it, or anything based on it, for commercial purposes: selling it, or building a new commercial product or service on top of it.

### Additional permission: use at work

Using HatchGrouper as a tool in your daily work, including at a commercial company
(for example to prepare quotations or production sizes), is explicitly allowed and is
**not** considered commercial use under this license. The results you get from the program are yours.
A company may also modify the code to fit its own needs and use that modified version internally;
the credit to the original author (see above) must be kept.
The NonCommercial restriction is about the program itself: selling it, or creating and distributing
a new commercial product based on its code.

For anything beyond that, contact the author to agree on separate terms.

## Changelog

### v0.3-beta
- Support for sheet metal integrated box & trim profile where trim and box body form a single bent part: exact CAD bend difference parameters (`TrimDiffWMm = 116.2`, `TrimDiffHMm = 117.8`).
- Added Box Depth parameter (`DepthMm`, default 80 mm).
- Added dedicated **Drawing size** column (`BoxDrawing`, e.g. `852 × 768 × 80`) alongside exact calculated dimensions.
- Configurable box size rounding in Settings for drawing specifications (To nearest, Always up / Ceiling, Always down / Floor).
- Single-cell and row clipboard copying via right-click ContextMenu and `Ctrl+C`.
- **Formulas & CAD Guide** window with a visual step-by-step tutorial (with 3D CAD screenshots) explaining where and how to measure trim, box body, bend deltas, and hatch opening.
- New offline & online CAD documentation ([`docs/CAD_GUIDE.md`](docs/CAD_GUIDE.md)).

### v0.2-beta
- Ordered niche-by-niche specification tab (`1.1, 1.2...`) in the main window with one-click clipboard copying (`(1.1) Ніша - Розмір короба`) and dedicated CSV export.
- Full decimal millimeter support (0.1 mm precision) for niche dimensions in input text as well as box sizes.
- Mounting ears parameter (`EarMm`, default 1 mm per side) with clearance check.
- Exact hatch formula linked directly to outer trim dimensions: HatchW = TrimW - HatchDiffW, HatchH = TrimH - HatchDiffH.
- Editable **Code / Note** column in the main table with CSV export and project file persistence.
- **Size with trim** column added to the result table.
- Source niche identifiers (`[entrance.niche]`) displayed in the compact composition column and in extreme cases.
- **Extreme cases & gap analysis** window (`ExtremeCasesWindow`) with 2D cross-section diagram, critical niches check (narrowest, lowest, widest, tallest, envelope), and dynamic clearance recommendations.

### v0.1
- First version: parse, group, save/load project, CSV export.
- Program settings saved between runs; example input; user-friendly error messages.
- Units (cm / mm) for the input list, parameters and result table; interface language switch.
- **Formulas…** window with a plain explanation; Polish language; English is the default language.
- Optimal assignment of borderline niches to minimise the maximum gap; optional separate tolerance for width and height.
- **Visualization…** window with 2D grouping map, smooth zoom/pan controls, group highlighting, and full trilingual localization.
