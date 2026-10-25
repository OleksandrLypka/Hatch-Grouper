# HatchGrouper

A small Windows (WPF) tool that groups wall-niche sizes for water-meter (or others) inspection hatches
into the smallest possible number of standard box sizes,
so that one box can be manufactured for a whole group of similar niches instead of one box per niche.

**Version:** 0.1 · **Author:** Oleksandr Lypka · **License:** [CC BY-NC 4.0](LICENSE)

## Features

- Paste the niche list straight from the customer's document (Word, Excel, plain text).
- Exact grouping: finds the minimum number of groups that cover all niches, and assigns borderline niches that fit into several groups so that the maximum gap is minimal.
- Per group: box size, hatch size, maximum gap, minimum trim lap and the list of niches (by entrance).
- Adjustable parameters: gap, trim, minimum lap, hatch offset (symmetric on all sides by default; optional separate parameters/tolerance for width and height in **Settings…**).
- Units of measurement (cm / mm) chosen separately for the input list, the parameter fields and the result table.
- Interface languages: English (default), Ukrainian, Polish. Switch in **Settings…**.
- **Formulas…** window: plain-language explanation, all formulas and an example computed from your current numbers.
- **Visualization…** window: interactive 2D map of niches and group bounding boxes with zoom, pan, and group highlighting.
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
| Trim | 60 | how far the trim sticks out beyond the box, per side |
| Min. lap | 25 | minimal trim overlap on the wall (assumption, confirm with the designer) |
| Hatch offset | 60 | the hatch is smaller than the box by this much per side (from one example: box 1160×850 → hatch 1040×730) |

Formulas:

```
max gap per side   = trim - min. lap
spread tolerance   = 2 * (max gap - gap)              # per dimension
box size           = smallest niche in the group - 2 * gap
hatch size         = box size - 2 * hatch offset
gap per side       = gap + spread / 2                 # spread = largest - smallest niche in the group
lap on the wall    = trim - gap per side
```

Grouping: every distinct (width, height) is a point. A group is a `toleranceW × toleranceH` rectangle
(a square by default when tolerance is symmetric on all sides).
The program searches for the minimal number of rectangles that cover all points
(exact set cover, depth-first search with pruning), and then assigns points that fall into
several chosen rectangles so that the maximum gap across groups is minimal (see [`Grouper.cs`](src/Grouper.cs)).

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

The version stays at 0.1 until the author decides otherwise.

- First version: parse, group, save/load project, CSV export.
- Program settings saved between runs; example input; user-friendly error messages.
- Units (cm / mm) for the input list, parameters and result table; interface language switch.
- **Formulas…** window with a plain explanation; Polish language; English is the default language.
- Optimal assignment of borderline niches to minimise the maximum gap; optional separate tolerance for width and height (symmetric on all sides by default).
- **Visualization…** window with 2D grouping map, smooth zoom/pan controls, group highlighting, and full trilingual localization.
