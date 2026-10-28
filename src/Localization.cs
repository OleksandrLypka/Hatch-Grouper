using System.Globalization;
using System.Text.Json.Serialization;

namespace HatchGrouper
{
    /// <summary>Interface language. The order must match the order of the texts in <see cref="Loc"/> and in the settings combo box.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AppLanguage { Uk, En, Pl }

    /// <summary>
    /// Tiny localisation helper: every user-visible text lives in this table as (Ukrainian, English, Polish).
    /// Use Loc.T("key", args...) - args are inserted with string.Format.
    /// </summary>
    public static class Loc
    {
        public static AppLanguage Current { get; set; } = AppLanguage.En;

        public static string T(string key, params object[] args)
        {
            if (!Map.TryGetValue(key, out var t)) return key; // a missing key is visible, not fatal
            var text = Current switch { AppLanguage.Uk => t.Uk, AppLanguage.Pl => t.Pl, _ => t.En };
            return args.Length == 0 ? text : string.Format(CultureInfo.InvariantCulture, text, args);
        }

        private static readonly Dictionary<string, (string Uk, string En, string Pl)> Map = new()
        {
            // units
            ["unit.cm"] = ("см", "cm", "cm"),
            ["unit.mm"] = ("мм", "mm", "mm"),

            // window, labels, buttons
            ["title"] = ("Групування ніш для ревізійних люків", "Grouping of niches for inspection hatches", "Grupowanie nisz na włazy rewizyjne"),
            ["input.label"] = ("Ніші, {0} (Ш x В). Рядок «(2 під’їзд)» перемикає під’їзд. Можна вставляти список з Word.",
                               "Niches, {0} (W x H). A line like \"(2 entrance)\" switches the entrance. You can paste a list from Word.",
                               "Nisze, {0} (Szer. x Wys.). Linia typu \"(2 klatka)\" przełącza klatkę schodową. Można wkleić listę z Worda."),
            ["p.gap"] = ("Зазор, {0}:", "Gap, {0}:", "Luz, {0}:"),
            ["p.trim"] = ("Лиштва, {0}:", "Trim, {0}:", "Listwa, {0}:"),
            ["p.trim.diff"] = ("Лиштва − короб (Ш/В), {0}:", "Trim − box (W/H), {0}:", "Listwa − skrz. (Sz/W), {0}:"),
            ["p.lap"] = ("Мін. напуск, {0}:", "Min. lap, {0}:", "Min. zakład, {0}:"),
            ["p.hatch"] = ("Лиштва − люк (Ш/В), {0}:", "Trim − hatch (W/H), {0}:", "Listwa − właz (Sz/W), {0}:"),
            ["p.depth"] = ("Глибина, {0}:", "Depth, {0}:", "Głębokość, {0}:"),
            ["p.ear"] = ("Вушка, {0}/бік:", "Ears, {0}/side:", "Uszka, {0}/stronę:"),
            ["p.width.prefix"] = ("Ш (боки) —", "W (sides) —", "Szer. (boki) —"),
            ["p.height.prefix"] = ("В (верх/низ) —", "H (top/bottom) —", "Wys. (góra/dół) —"),
            ["name.gap"] = ("Зазор", "Gap", "Luz"),
            ["name.trim"] = ("Лиштва", "Trim", "Listwa"),
            ["name.trim.h"] = ("Лиштва (В)", "Trim (H)", "Listwa (Wys.)"),
            ["name.trim.w"] = ("Лиштва − короб (Ш)", "Trim − box (W)", "Listwa − skrzynka (Sz)"),
            ["name.trim.h.diff"] = ("Лиштва − короб (В)", "Trim − box (H)", "Listwa − skrzynka (Wys.)"),
            ["name.lap"] = ("Мін. напуск", "Min. lap", "Min. zakład"),
            ["name.hatch.w"] = ("Люк менший за лиштву (Ш)", "Hatch smaller than trim (W)", "Właz mniejszy od listwy (Szer.)"),
            ["name.hatch.h"] = ("Люк менший за лиштву (В)", "Hatch smaller than trim (H)", "Właz mniejszy od listwy (Wys.)"),
            ["name.depth"] = ("Глибина короба", "Box depth", "Głębokość skrzynki"),
            ["name.ear"] = ("Плоскі вушка короба", "Box mounting tabs/ears", "Płaskie uszka skrzynki"),
            ["name.gap.h"] = ("Зазор (В)", "Gap (H)", "Luz (Wys.)"),
            ["name.lap.h"] = ("Мін. напуск (В)", "Min. lap (H)", "Min. zakład (Wys.)"),
            ["btn.group"] = ("Групувати", "Group", "Grupuj"),
            ["btn.open"] = ("Відкрити…", "Open…", "Otwórz…"),
            ["btn.save"] = ("Зберегти…", "Save…", "Zapisz…"),
            ["btn.export"] = ("Експорт CSV…", "Export CSV…", "Eksport CSV…"),
            ["btn.settings"] = ("Налаштування…", "Settings…", "Ustawienia…"),
            ["btn.help"] = ("Формули та CAD…", "Formulas & CAD…", "Wzory i CAD…"),
            ["btn.visualize"] = ("Візуалізація…", "Visualization…", "Wizualizacja…"),
            ["btn.extremes"] = ("Крайні випадки…", "Extreme cases…", "Przypadki skrajne…"),
            ["btn.ok"] = ("OK", "OK", "OK"),
            ["btn.cancel"] = ("Скасувати", "Cancel", "Anuluj"),
            ["btn.close"] = ("Закрити", "Close", "Zamknij"),

            // result table
            ["col.no"] = ("№", "No.", "Nr"),
            ["col.count"] = ("шт", "pcs", "szt."),
            ["col.niche"] = ("Ніша, {0}", "Niche, {0}", "Nisza, {0}"),
            ["col.box"] = ("Короб (точний), {0}", "Box (exact), {0}", "Skrzynka (dokł.), {0}"),
            ["col.box.drawing"] = ("Креслення (Ш×В×Г, мм)", "Drawing (W×H×D, mm)", "Rysunek (Sz×W×G, mm)"),
            ["col.trim"] = ("З лиштвою, {0}", "With trim, {0}", "Z listwą, {0}"),
            ["col.hatch"] = ("Люк, {0}", "Hatch, {0}", "Właz, {0}"),
            ["col.maxgap"] = ("Зазор, {0}", "Max gap, {0}", "Maks. luz, {0}"),
            ["col.lap"] = ("Напуск, {0}", "Min. lap, {0}", "Min. zakład, {0}"),
            ["col.note"] = ("Шифр", "Code", "Kod"),
            ["col.comp"] = ("Склад (ніші, {0})", "Composition (niches, {0})", "Skład (nisze, {0})"),
            ["tab.groups"] = ("Зведені групи", "Group summary", "Podsumowanie grup"),
            ["tab.niches"] = ("По нішах (1.1, 1.2...)", "By niches (1.1, 1.2...)", "Wg nisz (1.1, 1.2...)"),
            ["btn.niches"] = ("По нішах…", "By niches…", "Wg nisz…"),
            ["btn.copy.niches"] = ("📋 Копіювати список (1.1)", "📋 Copy list (1.1)", "📋 Kopiuj listę (1.1)"),
            ["btn.export.niches"] = ("Експорт списку ніш (CSV)", "Export niche list (CSV)", "Eksportuj listę nisz (CSV)"),
            ["btn.labels"] = ("🏷️ Етикетки для монтажу (Word)…", "🏷️ Installation Labels (Word)…", "🏷️ Etykiety montażowe (Word)…"),
            ["chk.group.by.box"] = ("Однакові короби разом", "Group identical boxes", "Jednakowe skrzynki razem"),
            ["col.spec.id"] = ("№", "No.", "Nr"),
            ["col.spec.niche"] = ("Ніша, {0}", "Niche, {0}", "Nisza, {0}"),
            ["col.spec.box"] = ("Короб (точний), {0}", "Box (exact), {0}", "Skrzynka (dokł.), {0}"),
            ["col.spec.box.drawing"] = ("Креслення (Ш×В×Г, мм)", "Drawing (W×H×D, mm)", "Rysunek (Sz×W×G, mm)"),
            ["col.spec.group"] = ("Група", "Group", "Grupa"),
            ["niche.word"] = ("Ніша", "Niche", "Nisza"),
            ["menu.copy.cell"] = ("📋 Копіювати комірку", "📋 Copy cell", "📋 Kopiuj komórkę"),
            ["menu.copy.row"] = ("Копіювати рядок", "Copy row", "Kopiuj wiersz"),
            ["status.cell.copied"] = ("Скопійовано: «{0}»", "Copied: \"{0}\"", "Skopiowano: \"{0}\""),
            ["status.row.copied"] = ("Рядок скопійовано в буфер обміну.", "Row copied to clipboard.", "Skopiowano wiersz do schowka."),
            ["status.niches.copied"] = ("Список ніш скопійовано в буфер обміну ({0} шт).", "Niche list copied to clipboard ({0} pcs).", "Skopiowano listę nisz do schowka ({0} szt.)."),
            ["status.niches.count"] = ("Ніш у списку: {0} шт.", "Niches in list: {0} pcs.", "Nisz na liście: {0} szt."),
            ["csv.niches.header"] = ("№;Ніша {0};Короб точний {0};Креслення (Ш×В×Г, мм);З лиштвою {0};Люк {0};Група;Шифр / Примітка",
                                     "No.;Niche {0};Box exact {0};Drawing (W×H×D, mm);With trim {0};Hatch {0};Group;Code / Note",
                                     "Nr;Nisza {0};Skrzynka dokł. {0};Rysunek (Sz×W×G, mm);Z listwą {0};Właz {0};Grupa;Kod / Uwaga"),
            ["csv.header"] = ("№;шт;Ніша {0};Короб точний {0};Креслення (Ш×В×Г, мм);З лиштвою {0};Люк {0};Макс. зазор {0};Мін. напуск {0};Шифр / Примітка;Склад",
                              "No.;pcs;Niche {0};Box exact {0};Drawing (W×H×D, mm);With trim {0};Hatch {0};Max gap {0};Min. lap {0};Code / Note;Composition",
                              "Nr;szt.;Nisza {0};Skrzynka dokł. {0};Rysunek (Sz×W×G, mm);Z listwą {0};Właz {0};Maks. luz {0};Min. zakład {0};Kod / Uwaga;Skład"),
            ["comp.entrance"] = ("{0} під’їзд: ", "Entrance {0}: ", "Klatka {0}: "),
            ["comp.lines"] = ("[{0}]", "[{0}]", "[{0}]"),
            ["comp.pcs"] = (" ({0} шт)", " ({0} pcs)", " ({0} szt.)"),
            ["example.entrance"] = ("({0} під’їзд)", "({0} entrance)", "({0} klatka)"),

            // settings dialog
            ["set.title"] = ("Налаштування", "Settings", "Ustawienia"),
            ["set.units"] = ("Одиниці виміру", "Units of measurement", "Jednostki miary"),
            ["set.input"] = ("Розміри ніш у списку:", "Niche sizes in the list:", "Wymiary nisz na liście:"),
            ["set.params"] = ("Параметри (зазор, лиштва, напуск, люк, зазор люка):", "Parameters (gap, trim, lap, hatch offset, clearance):",
                              "Parametry (luz, listwa, zakład, odsunięcie włazu, luz włazu):"),
            ["set.display"] = ("Розміри в таблиці результату:", "Sizes in the result table:", "Wymiary w tabeli wyników:"),
            ["set.tolerance"] = ("Допуск розкиду:", "Spread tolerance:", "Tolerancja rozrzutu:"),
            ["set.tol.sym"] = ("Симетричний (з усіх сторін)", "Symmetric (all sides)", "Symetryczna (ze wszystkich stron)"),
            ["set.tol.sep"] = ("Окремий для ширини і висоти", "Separate for width and height", "Osobna dla szerokości i wysokości"),
            ["set.rounding"] = ("Округлення для креслення:", "Drawing rounding:", "Zaokrąglenie dla rysunku:"),
            ["set.round.nearest"] = ("До найближчого (851.8 → 852, 767.2 → 767)", "To nearest (851.8 → 852, 767.2 → 767)", "Do najbliższego (851.8 → 852, 767.2 → 767)"),
            ["set.round.up"] = ("Завжди вгору (851.8 → 852, 767.2 → 768)", "Always up (851.8 → 852, 767.2 → 768)", "Zawsze w górę (851.8 → 852, 767.2 → 768)"),
            ["set.round.down"] = ("Завжди вниз (851.8 → 851, 767.2 → 767)", "Always down (851.8 → 851, 767.2 → 767)", "Zawsze w dół (851.8 → 851, 767.2 → 767)"),
            ["set.language"] = ("Мова / Language / Język:", "Мова / Language / Język:", "Мова / Language / Język:"),
            ["set.note"] = ("Налаштування зберігаються автоматично.", "Settings are saved automatically.", "Ustawienia są zapisywane automatycznie."),
            ["set.version"] = ("Версія {0}", "Version {0}", "Wersja {0}"),
            ["set.author"] = ("Автор: {0}", "Author: {0}", "Autor: {0}"),

            // status line
            ["status.result"] = ("Ніш: {0}, груп: {1}. Допуск розкиду: {2} {3} (макс. зазор {4} {3}).",
                                 "Niches: {0}, groups: {1}. Spread tolerance: {2} {3} (max gap {4} {3}).",
                                 "Nisz: {0}, grup: {1}. Tolerancja rozrzutu: {2} {3} (maks. luz {4} {3})."),
            ["status.skipped"] = ("\nУвага: не розпізнано рядків: {0} (наприклад: «{1}»). Вони пропущені.",
                                  "\nWarning: {0} line(s) not recognised (for example: \"{1}\"). They were skipped.",
                                  "\nUwaga: nierozpoznane linie: {0} (np. \"{1}\"). Zostały pominięte."),
            ["status.unusual"] = ("\nУвага: розміри ніш виглядають незвично. Перевірте одиниці введення (зараз: {0}) у «Налаштування…».",
                                  "\nWarning: niche sizes look unusual. Check the input unit (currently: {0}) in \"Settings…\".",
                                  "\nUwaga: wymiary nisz wyglądają nietypowo. Sprawdź jednostkę wprowadzania (teraz: {0}) w \"Ustawienia…\"."),
            ["status.saved"] = ("Збережено: {0}", "Saved: {0}", "Zapisano: {0}"),
            ["status.exported"] = ("Експортовано: {0}", "Exported: {0}", "Wyeksportowano: {0}"),
            ["status.pressgroup"] = ("Спочатку натисніть «Групувати».", "Press \"Group\" first.", "Najpierw naciśnij \"Grupuj\"."),
            ["status.unitchanged"] = ("Одиниці введення змінено. Перевірте список і натисніть «Групувати».",
                                      "The input unit was changed. Check the list and press \"Group\".",
                                      "Zmieniono jednostkę wprowadzania. Sprawdź listę i naciśnij \"Grupuj\"."),

            // errors
            ["err.title"] = ("Помилка", "Error", "Błąd"),
            ["err.prefix"] = ("Помилка: ", "Error: ", "Błąd: "),
            ["err.group"] = ("Не вдалося згрупувати ніші.", "Could not group the niches.", "Nie udało się pogrupować nisz."),
            ["err.save"] = ("Не вдалося зберегти проєкт.", "Could not save the project.", "Nie udało się zapisać projektu."),
            ["err.open"] = ("Не вдалося відкрити проєкт.", "Could not open the project.", "Nie udało się otworzyć projektu."),
            ["err.export"] = ("Не вдалося експортувати CSV.", "Could not export the CSV file.", "Nie udało się wyeksportować pliku CSV."),
            ["err.noniches"] = ("Не знайдено жодної ніші.\nВставте список розмірів, по одній ніші в рядку, наприклад: 84 x 89 (ширина x висота, {0}).",
                                "No niches found.\nPaste a list of sizes, one niche per line, for example: 84 x 89 (width x height, {0}).",
                                "Nie znaleziono żadnej niszy.\nWklej listę wymiarów, jedna nisza w wierszu, na przykład: 84 x 89 (szerokość x wysokość, {0})."),
            ["err.empty"] = ("Список ніш порожній.", "The niche list is empty.", "Lista nisz jest pusta."),
            ["err.emptyfile"] = ("Файл порожній або не є проєктом цієї програми.", "The file is empty or is not a project of this program.",
                                 "Plik jest pusty lub nie jest projektem tego programu."),
            ["err.field.mm"] = ("Поле «{0}» має містити невід’ємне число в мм (з точністю до 0,1 мм, наприклад 6,5 або 10).",
                                "Field \"{0}\" must contain a non-negative number in mm (precision 0.1 mm, e.g. 6.5 or 10).",
                                "Pole \"{0}\" musi zawierać nieujemną liczbę w mm (z dokładnością do 0,1 mm, np. 6,5 lub 10)."),
            ["err.field.cm"] = ("Поле «{0}» має містити невід’ємне число в см (з точністю до 0,01 см, наприклад 0,65).",
                                "Field \"{0}\" must contain a non-negative number in cm (precision 0.01 cm, e.g. 0.65).",
                                "Pole \"{0}\" musi zawierać nieujemną liczbę w cm (z dokładnością do 0,01 cm, np. 0,65)."),
            ["err.field.empty"] = ("Зараз поле порожнє.", "The field is empty now.", "Pole jest teraz puste."),
            ["err.field.now"] = ("Зараз там: «{0}».", "It now contains: \"{0}\".", "Teraz jest tam: \"{0}\"."),
            ["err.tolerance"] = ("Не можна згрупувати: допуск від’ємний (лиштва {0} − мін. напуск {1} = {2} мм, а зазор {3} мм більший).\nЗбільште лиштву або зменшіть мін. напуск чи зазор.",
                                 "Cannot group: the tolerance is negative (trim {0} − min. lap {1} = {2} mm, but the gap is {3} mm, which is larger).\nIncrease the trim or decrease the min. lap or the gap.",
                                 "Nie można pogrupować: tolerancja jest ujemna (listwa {0} − min. zakład {1} = {2} mm, a luz {3} mm jest większy).\nZwiększ listwę albo zmniejsz min. zakład lub luz."),
            ["err.toosmall"] = ("Ніша {0} x {1} {2} надто мала: після віднімання зазору ({3} мм) і відступу люка ({4} мм) розмір люка стає нульовим або від’ємним.\nПеревірте розміри в списку та одиниці введення (зараз: {2}) у «Налаштування…».",
                                "The niche {0} x {1} {2} is too small: after subtracting the gap ({3} mm) and the hatch offset ({4} mm) the hatch size is zero or negative.\nCheck the sizes in the list and the input unit (currently: {2}) in \"Settings…\".",
                                "Nisza {0} x {1} {2} jest za mała: po odjęciu luzu ({3} mm) i odsunięcia włazu ({4} mm) rozmiar włazu jest zerowy lub ujemny.\nSprawdź wymiary na liście i jednostkę wprowadzania (teraz: {2}) w \"Ustawienia…\"."),
            ["err.json"] = ("Файл пошкоджений або не є проєктом цієї програми.", "The file is damaged or is not a project of this program.",
                            "Plik jest uszkodzony lub nie jest projektem tego programu."),
            ["err.access"] = ("Немає доступу до файлу або папки. Оберіть інше місце.", "No access to the file or folder. Choose another location.",
                              "Brak dostępu do pliku lub folderu. Wybierz inne miejsce."),
            ["err.io"] = ("Файл недоступний. Можливо, він відкритий в іншій програмі.\n({0})", "The file is not accessible. It may be open in another program.\n({0})",
                          "Plik jest niedostępny. Być może jest otwarty w innym programie.\n({0})"),
            ["err.unexpected"] = ("Неочікувана помилка: {0}", "Unexpected error: {0}", "Nieoczekiwany błąd: {0}"),
            ["err.unhandled"] = ("Сталася неочікувана помилка. Програма продовжить роботу.\n\n{0}", "An unexpected error occurred. The program will keep running.\n\n{0}",
                                 "Wystąpił nieoczekiwany błąd. Program będzie działał dalej.\n\n{0}"),

            // file dialogs
            ["filter.project"] = ("Проєкт (*.hatch.json)|*.hatch.json", "Project (*.hatch.json)|*.hatch.json", "Projekt (*.hatch.json)|*.hatch.json"),
            ["filter.csv"] = ("CSV (*.csv)|*.csv", "CSV (*.csv)|*.csv", "CSV (*.csv)|*.csv"),
            ["filter.docx"] = ("Документ Word (*.docx)|*.docx", "Word Document (*.docx)|*.docx", "Dokument Word (*.docx)|*.docx"),

            // labels & stickers window
            ["labels.win.title"] = ("Етикетки для монтажу у під'їздах", "Installation Labels for Hatches", "Etykiety montażowe włazów"),
            ["labels.btn.docx"] = ("📄 Експорт у Word (.docx)", "📄 Export to Word (.docx)", "📄 Eksportuj do Word (.docx)"),
            ["labels.btn.copy"] = ("📋 Копіювати текст", "📋 Copy text", "📋 Kopiuj tekst"),
            ["labels.sort.label"] = ("Порядок:", "Sort order:", "Kolejność:"),
            ["labels.sort.entrance"] = ("За порядком у під'їзді (1.1, 1.2...)", "By entrance & floor (1.1, 1.2...)", "Wg klatki i piętra (1.1, 1.2...)"),
            ["labels.sort.box"] = ("Згрупувати за коробами (однакові разом)", "Group by box size (identical boxes together)", "Grupuj wg skrzynek (jednakowe razem)"),
            ["labels.filter.entrance"] = ("Під'їзд:", "Entrance:", "Klatka:"),
            ["labels.filter.all"] = ("Всі під'їзди", "All entrances", "Wszystkie klatki"),
            ["labels.entrance.item"] = ("Під'їзд {0}", "Entrance {0}", "Klatka {0}"),
            ["labels.total.count"] = ("Етикеток: {0} шт.", "Labels: {0} pcs", "Etykiet: {0} szt."),
            ["labels.bottom.hint"] = ("Формат 1.2 = Під'їзд: 1, Поверх: 2. Включає креслярський розмір короба, нішу та шифр.",
                                      "Format 1.2 = Entrance: 1, Floor: 2. Includes drawing box size, niche, and code.",
                                      "Format 1.2 = Klatka: 1, Piętro: 2. Zawiera wymiar skrzynki, niszę i kod."),
            ["labels.field.entrance"] = ("Під'їзд", "Entrance", "Klatka"),
            ["labels.field.floor"] = ("Поверх", "Floor", "Piętro"),
            ["labels.field.box"] = ("Короб", "Box", "Skrzynka"),
            ["labels.field.exact"] = ("факт", "exact", "dokł."),
            ["labels.field.niche"] = ("Ніша в стіні", "Wall niche", "Nisza w ścianie"),
            ["labels.field.hatch"] = ("Дверцята люка", "Hatch door", "Drzwiczki włazu"),
            ["labels.field.trim"] = ("З лиштвою", "With trim", "Z listwą"),
            ["labels.field.note"] = ("Шифр", "Code", "Kod"),
            ["labels.docx.title"] = ("HatchGrouper — Етикетки для монтажу ревізійних люків",
                                     "HatchGrouper — Inspection Hatch Installation Labels",
                                     "HatchGrouper — Etykiety montażowe włazów rewizyjnych"),
            ["labels.export.success"] = ("Етикетки успішно збережено у файл:\n{0}",
                                         "Labels successfully saved to file:\n{0}",
                                         "Etykiety pomyślnie zapisano do pliku:\n{0}"),
            ["labels.open.prompt"] = ("Відкрити створений файл у Word?", "Open the created file in Word?", "Otworzyć utworzony plik w programie Word?"),
            ["labels.copied.msg"] = ("Скопійовано {0} етикеток у буфер обміну.", "Copied {0} labels to clipboard.", "Skopiowano {0} etykiet do schowka."),

            // visualizer window
            ["vis.title"] = ("Карта розташування ніш за групами", "Niche grouping map", "Mapa grupowania nisz"),
            ["vis.width"] = ("Ширина ({0})", "Width ({0})", "Szerokość ({0})"),
            ["vis.height"] = ("Висота ({0})", "Height ({0})", "Wysokość ({0})"),
            ["vis.legend"] = ("Групи ніш", "Niche groups", "Grupy nisz"),
            ["vis.group.title"] = ("Група {0}", "Group {0}", "Grupa {0}"),
            ["vis.box.label"] = ("Ящик: {0}", "Box: {0}", "Skrzynka: {0}"),
            ["vis.hatch.label"] = ("Люк: {0}", "Hatch: {0}", "Właz: {0}"),
            ["vis.spread.label"] = ("Розкид: {0}", "Spread: {0}", "Rozrzut: {0}"),
            ["vis.niches.label"] = ("Ніші: {0}", "Niches: {0}", "Nisze: {0}"),
            ["vis.showall"] = ("Показати всі групи", "Show all groups", "Pokaż wszystkie grupy"),
            ["vis.zoom.reset"] = ("Скинути вигляд", "Reset view", "Zresetuj widok"),
            ["vis.hint"] = ("Коліщатко миші — масштаб • Затисніть ліву кнопку — переміщення",
                            "Mouse wheel to zoom • Drag to pan",
                            "Kółko myszy — zoom • Przeciągnij myszą — przesuwanie"),
            ["vis.point.tooltip"] = ("Ніша: {0} × {1} {2}\nКількість: {3} шт\nПід'їзд(и): {4}\nГрупа №{5}",
                                     "Niche: {0} × {1} {2}\nCount: {3} pcs\nEntrance(s): {4}\nGroup #{5}",
                                     "Nisza: {0} × {1} {2}\nIlość: {3} szt.\nKlatka(-i): {4}\nGrupa #{5}"),
            ["vis.empty"] = ("Спочатку натисніть «Групувати», щоб побудувати карту груп.",
                             "Please click \"Group\" first to build the groups map.",
                             "Najpierw naciśnij \"Grupuj\", aby zbudować mapę grup."),

            // extreme cases window
            ["ext.title"] = ("Аналіз зазорів та крайні випадки", "Gap analysis and extreme cases", "Analiza luzów i przypadki skrajne"),
            ["ext.group.label"] = ("Група:", "Group:", "Grupa:"),
            ["ext.case.label"] = ("Ніша для перевірки:", "Niche to check:", "Nisza do sprawdzenia:"),
            ["ext.case.smallest"] = ("⭐ Найменша ніша {0} (вхід короба)", "⭐ Smallest niche {0} (box fit)", "⭐ Najmniejsza nisza {0} (wejście skrzynki)"),
            ["ext.case.narrowest"] = ("⭐ Найвужча ніша {0} (вхід по ширині)", "⭐ Narrowest niche {0} (box fit by width)", "⭐ Najwęższa nisza {0} (wejście skrzynki na szerokość)"),
            ["ext.case.lowest"] = ("⭐ Найнижча ніша {0} (вхід по висоті)", "⭐ Lowest niche {0} (box fit by height)", "⭐ Najniższa nisza {0} (wejście skrzynki na wysokość)"),
            ["ext.case.largest"] = ("⭐ Найбільша ніша {0} (напуск лиштви)", "⭐ Largest niche {0} (trim lap)", "⭐ Największa nisza {0} (zakład listwy)"),
            ["ext.case.widest"] = ("⭐ Найширша ніша {0} (напуск по ширині)", "⭐ Widest niche {0} (trim lap by width)", "⭐ Najszersza nisza {0} (zakład listwy na szerokość)"),
            ["ext.case.tallest"] = ("⭐ Найвища ніша {0} (напуск по висоті)", "⭐ Tallest niche {0} (trim lap by height)", "⭐ Najwyższa nisza {0} (zakład listwy na wysokość)"),
            ["ext.case.envelope.min"] = ("📐 Зведений мінімум {0} (Min Ш × Min В)", "📐 Envelope minimum {0} (Min W × Min H)", "📐 Zbiorcze minimum {0} (Min Sz × Min W)"),
            ["ext.case.envelope.max"] = ("📐 Зведений максимум {0} (Max Ш × Max В)", "📐 Envelope maximum {0} (Max W × Max H)", "📐 Zbiorcze maksimum {0} (Max Sz × Max W)"),
            ["ext.case.other"] = ("Ніша {0}", "Niche {0}", "Nisza {0}"),
            ["ext.h.dims"] = ("Розраховані розміри ({0})", "Calculated dimensions ({0})", "Obliczone wymiary ({0})"),
            ["ext.h.gaps"] = ("Зазори та напуски ({0})", "Gaps and laps ({0})", "Luzy i zakłady ({0})"),
            ["ext.h.summary"] = ("Рекомендації та висновок", "Recommendations & summary", "Rekomendacje i podsumowanie"),
            ["ext.lbl.niche"] = ("Ніша в стіні:", "Wall niche:", "Nisza w ścianie:"),
            ["ext.lbl.box"] = ("Короб:", "Box body:", "Skrzynka:"),
            ["ext.lbl.boxears"] = ("Короб з вушками (+{0} {1}/бік):", "Box with ears (+{0} {1}/side):", "Skrzynka z uszkami (+{0} {1}/stronę):"),
            ["ext.lbl.trim"] = ("Розмір з лиштвою:", "Size with trim:", "Rozmiar z listwą:"),
            ["ext.lbl.hatch"] = ("Люк (дверцята):", "Hatch (door):", "Właz (drzwiczki):"),
            ["ext.lbl.gapear"] = ("Зазор до вушка:", "Gap to ear:", "Luz do uszka:"),
            ["ext.lbl.gapbody"] = ("Зазор до стінки короба:", "Gap to box body:", "Luz do ścianki skrzynki:"),
            ["ext.lbl.lap"] = ("Напуск лиштви на стіну:", "Trim lap on wall:", "Zakład listwy na ścianę:"),
            ["ext.lbl.hatchclr"] = ("Люк менший за лиштву:", "Hatch smaller than trim:", "Właz mniejszy od listwy:"),
            ["ext.rec.smallest"] = ("Найменша ніша групи (критичний випадок на вхід короба):\n• Чистий зазор до вушок становить {0} {1} (до стінки короба: {2} {1}).\n• Короб гарантовано вільно входить у нішу без заклинювання.",
                                    "Smallest niche of the group (critical for box fit):\n• Clear gap to ears is {0} {1} (to box body: {2} {1}).\n• The box is guaranteed to fit into the niche without jamming.",
                                    "Najmniejsza nisza w grupie (krytyczna dla wejścia skrzynki):\n• Czysty luz do uszek wynosi {0} {1} (do ścianki skrzynki: {2} {1}).\n• Skrzynka gwarantowanie mieści się w niszy bez klinowania."),
            ["ext.rec.narrowest"] = ("Найвужча ніша групи (критичний випадок на вхід короба по ширині):\n• Чистий зазор до бічних вушок становить {0} {1} (до стінки короба: {2} {1}).\n• Короб гарантовано вільно входить по ширині.",
                                     "Narrowest niche of the group (critical for horizontal box fit):\n• Clear gap to side ears is {0} {1} (to box body: {2} {1}).\n• The box is guaranteed to fit horizontally.",
                                     "Najwęższa nisza w grupie (krytyczna dla wejścia skrzynki na szerokość):\n• Czysty luz do bocznych uszek wynosi {0} {1} (do ścianki skrzynki: {2} {1}).\n• Skrzynka gwarantowanie mieści się na szerokość."),
            ["ext.rec.lowest"] = ("Найнижча ніша групи (критичний випадок на вхід короба по висоті):\n• Чистий зазор до верхнього/нижнього вушка становить {0} {1} (до стінки короба: {2} {1}).\n• Короб гарантовано вільно входить по висоті.",
                                  "Lowest niche of the group (critical for vertical box fit):\n• Clear gap to top/bottom ears is {0} {1} (to box body: {2} {1}).\n• The box is guaranteed to fit vertically.",
                                  "Najniższa nisza w grupie (krytyczna dla wejścia skrzynki na wysokość):\n• Czysty luz do górnego/dolnego uszka wynosi {0} {1} (do ścianki skrzynki: {2} {1}).\n• Skrzynka gwarantowanie mieści się na wysokość."),
            ["ext.rec.largest"] = ("Найбільша ніша групи (критичний випадок на перекриття щілини):\n• Напуск лиштви на стіну становить {0} {1} (норма: від {2} {1}).\n• Декоративна лиштва надійно перекриває монтажний зазор.",
                                   "Largest niche of the group (critical for trim lap):\n• Trim lap on the wall is {0} {1} (required: min {2} {1}).\n• The decorative trim reliably covers the mounting gap.",
                                   "Największa nisza w grupie (krytyczna dla zakładu listwy):\n• Zakład listwy na ścianę wynosi {0} {1} (norma: od {2} {1}).\n• Ozdobna listwa niezawodnie zakrywa szczelinę montażową."),
            ["ext.rec.widest"] = ("Найширша ніша групи (критичний випадок на напуск лиштви по ширині):\n• Напуск бічної лиштви на стіну становить {0} {1} (норма: від {2} {1}).\n• Декоративна лиштва надійно перекриває монтажний зазор по ширині.",
                                  "Widest niche of the group (critical for width trim lap):\n• Side trim lap on the wall is {0} {1} (required: min {2} {1}).\n• The decorative trim reliably covers the horizontal mounting gap.",
                                  "Najszersza nisza w grupie (krytyczna dla zakładu listwy na szerokość):\n• Zakład bocznej listwy na ścianę wynosi {0} {1} (norma: od {2} {1}).\n• Ozdobna listwa niezawodnie zakrywa szczelinę na szerokość."),
            ["ext.rec.tallest"] = ("Найвища ніша групи (критичний випадок на напуск лиштви по висоті):\n• Напуск верхньої/нижньої лиштви на стіну становить {0} {1} (норма: від {2} {1}).\n• Декоративна лиштва надійно перекриває монтажний зазор по висоті.",
                                   "Tallest niche of the group (critical for height trim lap):\n• Top/bottom trim lap on the wall is {0} {1} (required: min {2} {1}).\n• The decorative trim reliably covers the vertical mounting gap.",
                                   "Najwyższa nisza w grupie (krytyczna dla zakładu listwy na wysokość):\n• Zakład górnej/dolnej listwy na ścianę wynosi {0} {1} (norma: od {2} {1}).\n• Ozdobna listwa niezawodnie zakrywa szczelinę na wysokość."),
            ["ext.rec.conclusion"] = ("💡 Висновок: критичні випадки (найвужча, найнижча, найширша та найвища ніші) повністю відповідають вимогам. Усі інші ніші цієї групи гарантовано підходять — тестувати кожну окремо не потрібно.",
                                      "💡 Conclusion: critical cases (narrowest, lowest, widest, and tallest niches) fully meet all requirements. All other niches in this group are guaranteed to fit — testing each one is unnecessary.",
                                      "💡 Wniosek: przypadki krytyczne (najwęższa, najniższa, najszersza i najwyższa nisza) w pełni spełniają wymagania. Wszystkie pozostałe nisze w tej grupie na pewno pasują — nie trzeba testować każdej z osobna."),
            ["ext.legend.wall"] = ("Стіна ніші", "Niche wall", "Ściana niszy"),
            ["ext.legend.trim"] = ("Лиштва", "Trim", "Listwa"),
            ["ext.legend.box"] = ("Короб", "Box body", "Skrzynka"),
            ["ext.legend.ears"] = ("Вушка (+{0} {1})", "Tabs (+{0} {1})", "Uszka (+{0} {1})"),
            ["ext.legend.hatch"] = ("Люк", "Hatch", "Właz"),
            ["ext.empty"] = ("Спочатку натисніть «Групувати», щоб виконати аналіз груп.",
                             "Please click \"Group\" first to perform groups analysis.",
                             "Najpierw naciśnij \"Grupuj\", aby wykonać analizę grup."),

            // help window: formulas and plain explanation
            ["help.title"] = ("Формули та CAD гід", "Formulas & CAD Guide", "Wzory i przewodnik CAD"),
            ["help.tab.formulas"] = ("Формули та розрахунок", "Formulas & Calculation", "Wzory i obliczenia"),
            ["help.tab.cad"] = ("CAD гід: як міряти модель", "CAD Guide: Measuring 3D Model", "Przewodnik CAD: jak mierzyć model"),
            ["img.preview.title"] = ("Перегляд зображення", "Image Preview", "Podgląd obrazu"),
            ["img.click.enlarge"] = ("🔍 Клацніть для збільшення", "🔍 Click to enlarge", "🔍 Kliknij, aby powiększyć"),
            ["img.zoom.in"] = ("Збільшити", "Zoom In", "Powiększ"),
            ["img.zoom.out"] = ("Зменшити", "Zoom Out", "Pomniejsz"),
            ["img.zoom.fit"] = ("За розміром вікна", "Fit to window", "Dopasuj do okna"),
            ["img.zoom.reset"] = ("1:1 (оригінал)", "1:1 (original)", "1:1 (oryginał)"),
            ["img.hint"] = ("Коліщатко миші — масштаб • Затисніть ліву кнопку — переміщення • Двічі клацніть — скинути масштаб • Esc — закрити",
                            "Mouse wheel to zoom • Drag to pan • Double-click to toggle fit • Esc to close",
                            "Kółko myszy — zoom • Przeciągnij — przesuwanie • Dwuklik — dopasuj • Esc — zamknij"),

            // CAD tutorial strings
            ["help.cad.intro.title"] = ("Звідки беруться зміщення для 3D моделі", "Where CAD Model Offsets Come From", "Skąd się biorą przesunięcia dla modelu CAD"),
            ["help.cad.intro.body"] = (
                "У програмі закладено геометрію суцільногнутого виробу, де короб і лиштва утворені з одного розгорнутого листа металу шляхом згинання. " +
                "Через товщину металу, K-фактор і деформацію на лініях згину фактичний внутрішній розмір короба відрізняється від крайнього габариту лиштви на фіксовані зміщення. " +
                "Виконайте 5 простих вимірів у вашій 3D моделі (SolidWorks, Inventor тощо), щоб підставити точні параметри під вашу технологію:",
                "HatchGrouper is configured for an integrated single-piece bent sheet metal hatch where the box body and outer trim flange are bent from a single flat sheet. " +
                "Due to sheet thickness, K-factors, and bend deformation, the actual inner box body differs from the outer trim flange by fixed offsets. " +
                "Perform 5 simple measurements in your 3D CAD model (SolidWorks, Inventor, etc.) to configure the exact parameters for your workshop:",
                "Program bazuje na geometrii konstrukcji giętej z jednego arkusza, gdzie skrzynka i listwa tworzą jeden element po gięciu. " +
                "Ze względu na grubość blachy, współczynnik K i odkształcenia na liniach gięcia rzeczywisty wymiar skrzynki różni się od listwy o stałe przesunięcia. " +
                "Wykonaj 5 prostych pomiarów w swoim modelu 3D (SolidWorks, Inventor itp.), aby dopasować parametry do swojej technologii:"),

            ["help.cad.s1.title"] = ("Крок 1. Зовнішній габарит відігнутої лиштви (W_trim × H_trim)",
                                     "Step 1. Outer Trim Flange Dimensions (W_trim × H_trim)",
                                     "Krok 1. Zewnętrzny wymiar listwy (W_trim × H_trim)"),
            ["help.cad.s1.body"] = (
                "Виміряйте крайній габарит лицьової лиштви (фланця). Наприклад, для базової моделі:\n" +
                "• Ширина лиштви = 968.0 мм\n" +
                "• Висота лиштви = 885.0 мм\n" +
                "Це розмір, який перекриває нішу з боку приміщення разом із декоративним напуском на стіну.",
                "Measure the outermost overall dimensions of the front bent trim flange. For example, in the baseline model:\n" +
                "• Trim width = 968.0 mm\n" +
                "• Trim height = 885.0 mm\n" +
                "This is the front dimension that covers the wall niche with decorative wall overlap.",
                "Zmierz zewnętrzny gabaryt przedniej listwy (kołnierza). Na przykład w modelu bazowym:\n" +
                "• Szerokość listwy = 968.0 mm\n" +
                "• Wysokość listwy = 885.0 mm\n" +
                "Jest to wymiar zewnętrzny, który zakrywa niszę od strony pomieszczenia z zakładem na ścianę."),
            ["help.cad.s1.cap1"] = ("Ширина лиштви: 968 мм (зовнішній край)", "Trim width: 968 mm (outer flange edge)", "Szerokość listwy: 968 mm (krawędź zewnętrzna)"),
            ["help.cad.s1.cap2"] = ("Висота лиштви: 885 мм (зовнішній край)", "Trim height: 885 mm (outer flange edge)", "Wysokość listwy: 885 mm (krawędź zewnętrzna)"),

            ["help.cad.s2.title"] = ("Крок 2. Тіло короба після згину листа (W_box × H_box)",
                                     "Step 2. Internal Box Body After Bending (W_box × H_box)",
                                     "Krok 2. Korpus skrzynki po gięciu blachy (W_box × H_box)"),
            ["help.cad.s2.body"] = (
                "За допомогою інструмента Measure (Виміряти) у вашій CAD-системі виберіть дві протилежні паралельні стінки короба. " +
                "У результаті згинання листового металу розмір має десяткові частки:\n" +
                "• Фактична ширина короба = 851.8 мм\n" +
                "• Фактична висота короба = 767.2 мм",
                "Using the Measure tool in your CAD software, select two opposing parallel faces of the box body. " +
                "Due to sheet metal bend radii and deductions, the result has decimal millimetres:\n" +
                "• Actual box width = 851.8 mm\n" +
                "• Actual box height = 767.2 mm",
                "Za pomocą narzędzia Zmierz (Measure) w programie CAD wybierz dwie przeciwległe równoległe ścianki skrzynki. " +
                "W wyniku gięcia wymiary mają wartości dziesiętne:\n" +
                "• Rzeczywista szerokość skrzynki = 851.8 mm\n" +
                "• Rzeczywista wysokość skrzynki = 767.2 mm"),
            ["help.cad.s2.cap1"] = ("Ширина короба: 851.8 мм (SolidWorks Measure)", "Box width: 851.8 mm (SolidWorks Measure)", "Szerokość skrzynki: 851.8 mm (SolidWorks Measure)"),
            ["help.cad.s2.cap2"] = ("Висота короба: 767.2 мм (SolidWorks Measure)", "Box height: 767.2 mm (SolidWorks Measure)", "Wysokość skrzynki: 767.2 mm (SolidWorks Measure)"),

            ["help.cad.s3.title"] = ("Крок 3. Розрахунок дельти «Лиштва − короб» (TrimDiff)",
                                     "Step 3. Calculate \"Trim − Box\" Bend Offsets (TrimDiff)",
                                     "Krok 3. Obliczenie różnicy \"Listwa − skrzynka\" (TrimDiff)"),
            ["help.cad.s3.body"] = (
                "Відніміть розмір стінок короба від повного габариту лиштви:\n" +
                "• По ширині (TrimDiffW): 968.0 − 851.8 = 116.2 мм\n" +
                "• По висоті (TrimDiffH): 885.0 − 767.2 = 117.8 мм\n\n" +
                "Введіть ці значення у полі «Лиштва − короб (Ш/В): 116,2 / 117,8» (або у вікні «Налаштування…»). " +
                "Програма автоматично відніматиме їх від габариту лиштви, забезпечуючи точний розрахунок тіла короба під вашу CAD-збірку.",
                "Subtract the box body size from the outer trim dimensions:\n" +
                "• Width offset (TrimDiffW): 968.0 − 851.8 = 116.2 mm\n" +
                "• Height offset (TrimDiffH): 885.0 − 767.2 = 117.8 mm\n\n" +
                "Enter these values into the \"Trim − box (W/H): 116.2 / 117.8\" field (or in \"Settings…\"). " +
                "The program automatically subtracts them from the trim size, ensuring the box body matches your CAD model.",
                "Odejmij wymiary skrzynki od wymiarów zewnętrznych listwy:\n" +
                "• Szerokość (TrimDiffW): 968.0 − 851.8 = 116.2 mm\n" +
                "• Wysokość (TrimDiffH): 885.0 − 767.2 = 117.8 mm\n\n" +
                "Wpisz te wartości w polu \"Listwa − skrz. (Sz/W): 116,2 / 117,8\" (lub w \"Ustawienia…\"). " +
                "Program automatycznie odejmie je od wymiaru listwy, dopasowując skrzynkę do Twojego modelu CAD."),
            ["help.cad.s3.formula"] = ("TrimDiffW = TrimW − BoxW = 968.0 − 851.8 = 116.2 мм\nTrimDiffH = TrimH − BoxH = 885.0 − 767.2 = 117.8 мм",
                                       "TrimDiffW = TrimW − BoxW = 968.0 − 851.8 = 116.2 mm\nTrimDiffH = TrimH − BoxH = 885.0 − 767.2 = 117.8 mm",
                                       "TrimDiffW = TrimW − BoxW = 968.0 − 851.8 = 116.2 mm\nTrimDiffH = TrimH − BoxH = 885.0 − 767.2 = 117.8 mm"),

            ["help.cad.s4.title"] = ("Крок 4. Світловий отвір та розмір дверцят люка (HatchDiff)",
                                     "Step 4. Clear Opening and Hatch Door Size (HatchDiff)",
                                     "Krok 4. Prześwit i wymiary drzwiczek włazu (HatchDiff)"),
            ["help.cad.s4.body"] = (
                "Виміряйте внутрішній світловий отвір або дверцята люка (на прикладі: 836.4 × 753.8 мм). " +
                "Різниця від зовнішньої лиштви становить:\n" +
                "• По ширині (HatchDiffW): 968.0 − 836.4 = 131.6 мм\n" +
                "• По висоті (HatchDiffH): 885.0 − 753.8 = 131.2 мм\n\n" +
                "Ці значення вказуються в полі «Лиштва − люк (Ш/В): 131,6 / 131,2» і визначають розмір люка для виготовлення.",
                "Measure the clear opening or hatch door in your CAD model (in this example: 836.4 × 753.8 mm). " +
                "The offset from the outer trim is:\n" +
                "• Width offset (HatchDiffW): 968.0 − 836.4 = 131.6 mm\n" +
                "• Height offset (HatchDiffH): 885.0 − 753.8 = 131.2 mm\n\n" +
                "These values are specified in the \"Trim − hatch (W/H): 131.6 / 131.2\" field to compute the manufactured hatch door size.",
                "Zmierz prześwit lub drzwiczki włazu w modelu CAD (na przykładzie: 836.4 × 753.8 mm). " +
                "Różnica względem listwy wynosi:\n" +
                "• Szerokość (HatchDiffW): 968.0 − 836.4 = 131.6 mm\n" +
                "• Wysokość (HatchDiffH): 885.0 − 753.8 = 131.2 mm\n\n" +
                "Wartości te wpisuje się w polu \"Listwa − właz (Sz/W): 131,6 / 131,2\" i określają one wymiar drzwiczek."),
            ["help.cad.s4.cap1"] = ("Світловий отвір люка: 836.4 × 753.8 мм", "Hatch door opening: 836.4 × 753.8 mm", "Prześwit włazu: 836.4 × 753.8 mm"),

            ["help.cad.s5.title"] = ("Крок 5. Монтажні вушка та глибина короба",
                                     "Step 5. Mounting Tabs and Box Depth",
                                     "Krok 5. Uszka montażowe i głębokość skrzynki"),
            ["help.cad.s5.body"] = (
                "• Вушка: плоскі монтажні планки на стінках короба виступають на 1 мм на кожен бік (поле «Вушка, мм/бік: 1»). " +
                "Програма враховує їх при перевірці вільного проходу в найвужчу нішу.\n" +
                "• Глибина: стандартна глибина короба в стіну (за замовчуванням 80 мм). Вона використовується для колонки " +
                "«Креслення (Ш×В×Г, мм)» (наприклад: 852 × 768 × 80 мм) разом із налаштованим округленням.",
                "• Mounting tabs (ears): flat tabs stick out beyond the box body by 1 mm on each side (\"Ears, mm/side: 1\"). " +
                "The program includes them when verifying that the box freely fits into the smallest niche.\n" +
                "• Depth: standard box depth into the wall (default 80 mm). Used for the " +
                "\"Drawing (W×H×D, mm)\" callout column (e.g. 852 × 768 × 80 mm) according to the configured rounding mode.",
                "• Uszka montażowe: płaskie uszka wystają poza ściankę skrzynki o 1 mm z każdej strony (\"Uszka, mm/stronę: 1\"). " +
                "Program uwzględnia je przy sprawdzaniu swobodnego wejścia w najmniejszą niszę.\n" +
                "• Głębokość: standardowa głębokość skrzynki w ścianę (domyślnie 80 mm). Jest używana w kolumnie " +
                "\"Rysunek (Sz×W×G, mm)\" (np. 852 × 768 × 80 mm) zgodnie z wybranym zaokrągleniem."),

            ["help.h.what"] = ("Що робить програма", "What the program does", "Co robi program"),
            ["help.what"] = (
                "У кожній ніші в стіні буде ящик із ревізійним люком. Робити окремий ящик під кожну нішу дорого, бо розміри ніш трохи відрізняються. " +
                "Програма об’єднує ніші зі схожими розмірами в якомога меншу кількість груп, щоб для кожної групи виготовити один стандартний ящик.",
                "A box with an inspection hatch goes into every wall niche. Making a separate box for every niche is expensive because niche sizes differ slightly. " +
                "The program joins niches of similar size into the smallest possible number of groups, so that one standard box can be made for each group.",
                "W każdej niszy w ścianie zostanie zamontowana skrzynka z włazem rewizyjnym. Robienie osobnej skrzynki do każdej niszy jest drogie, bo wymiary nisz nieco się różnią. " +
                "Program łączy nisze o podobnych wymiarach w jak najmniejszą liczbę grup, aby dla każdej grupy wykonać jedną standardową skrzynkę."),

            ["help.h.params"] = ("Параметри простими словами", "Parameters in plain words", "Parametry prostymi słowami"),
            ["help.params"] = (
                "• Зазор — вільний простір між коробом і стінками ніші з кожного боку (щоб короб увійшов у нішу).\n" +
                "• Вушка — плоскі монтажні вушка зверху, знизу і по бокам короба (за замовчуванням 1 мм на сторону, можна 0).\n" +
                "• Лиштва — на скільки декоративна накладка виступає за межі короба з кожного боку.\n" +
                "• Мін. напуск — мінімум, на скільки лиштва має заходити на стіну навколо ніші, щоб закрити щілину.\n" +
                "• Люк менший за лиштву (Ш / В) — на скільки ширина та висота люка менші за зовнішній габарит лиштви (за замовчуванням 131,6 та 131,2 мм, сюди вже входять зазори на відкривання).",
                "• Gap — free space between the box and the niche walls on each side (so that the box fits into the niche).\n" +
                "• Ears — flat mounting tabs on top, bottom and sides of the box (default 1 mm per side, 0 allowed).\n" +
                "• Trim — how far the decorative trim sticks out beyond the box on each side.\n" +
                "• Min. lap — the minimum the trim must overlap the wall around the niche to hide the gap.\n" +
                "• Hatch smaller than trim (W / H) — how much smaller the hatch width and height are than the outer trim dimensions (default 131.6 and 131.2 mm, including opening clearance).",
                "• Luz — wolna przestrzeń między skrzynką a ściankami niszy z każdej strony (aby skrzynka weszła do niszy).\n" +
                "• Uszka — płaskie uszka montażowe na górze, dole i bokach skrzynki (domyślnie 1 mm na stronę, dopuszczalne 0).\n" +
                "• Listwa — o ile ozdobna listwa wystaje poza skrzynkę z każdej strony.\n" +
                "• Min. zakład — minimum, o jakie listwa musi nachodzić na ścianę wokół niszy, aby zakryć szczelinę.\n" +
                "• Właz mniejszy od listwy (Sz / W) — o ile szerokość i wysokość włazu są mniejsze od zewnętrznego wymiaru z listwą (domyślnie 131,6 i 131,2 mm, wraz z luzem na otwieranie)."),

            ["help.h.how"] = ("Як групуються ніші", "How niches are grouped", "Jak grupowane są nisze"),
            ["help.how"] = (
                "Усі розміри рахуються в міліметрах. Одна група — це ніші, у яких і ширина, і висота відрізняються не більше ніж на «допуск розкиду» " +
                "(за замовчуванням він симетричний з усіх сторін, а в «Налаштування…» можна увімкнути окремий допуск для ширини і висоти). " +
                "Ящик робиться під найменшу нішу групи, тож у більші ніші він входить із трохи більшою щілиною, яку ховає лиштва. " +
                "Програма точним перебором шукає найменшу кількість таких груп, що охоплюють усі ніші, а ніші на межі кількох груп відносить туди, де максимальний зазор буде найменшим.",
                "All sizes are calculated in millimetres. One group is a set of niches whose width and height both differ by no more than the \"spread tolerance\" " +
                "(symmetric on all sides by default; separate tolerance for width and height can be enabled in \"Settings…\"). " +
                "The box is made for the smallest niche of the group, so in larger niches it fits with a slightly bigger gap, which the trim hides. " +
                "The program uses an exact search to find the smallest number of such groups that cover all niches, and assigns borderline niches that fit into several groups so that the maximum gap is minimal.",
                "Wszystkie wymiary są liczone w milimetrach. Jedna grupa to nisze, w których zarówno szerokość, jak i wysokość różnią się nie więcej niż o \"tolerancję rozrzutu\" " +
                "(domyślnie symetryczną ze wszystkich stron; w \"Ustawienia…\" można włączyć osobną tolerancję dla szerokości i wysokości). " +
                "Skrzynka jest robiona na najmniejszą niszę w grupie, więc w większych niszach pasuje z nieco większą szczeliną, którą zakrywa listwa. " +
                "Program dokładnym przeszukiwaniem znajduje najmniejszą liczbę takich grup obejmujących wszystkie nisze, a nisze pasujące do kilku grup przypisuje tak, aby maksymalny luz był jak najmniejszy."),

            ["help.h.formulas"] = ("Формули (усі розміри в мм)", "Formulas (all sizes in mm)", "Wzory (wszystkie wymiary w mm)"),
            ["help.formulas"] = (
                "макс. зазор з одного боку  = лиштва − мін. напуск\n" +
                "допуск розкиду (Ш і В)     = 2 × (макс. зазор − (зазор + вушка))\n" +
                "розмір короба              = найменша ніша групи − 2 × (зазор + вушка)\n" +
                "розмір з лиштвою           = розмір короба + 2 × лиштва\n" +
                "розмір люка (Ш × В)        = (ширина з лиштвою − відступ Ш) × (висота з лиштвою − відступ В)\n" +
                "зазор з одного боку        = (зазор + вушка) + (найбільша ніша − найменша ніша) / 2\n" +
                "напуск лиштви на стіну     = лиштва − зазор з одного боку",
                "max gap per side           = trim − min. lap\n" +
                "spread tolerance (W and H) = 2 × (max gap − (gap + ears))\n" +
                "box size                   = smallest niche of the group − 2 × (gap + ears)\n" +
                "size with trim             = box size + 2 × trim\n" +
                "hatch size (W × H)         = (trim width − diff W) × (trim height − diff H)\n" +
                "gap per side               = (gap + ears) + (largest niche − smallest niche) / 2\n" +
                "trim lap on the wall       = trim − gap per side",
                "maks. luz z jednej strony  = listwa − min. zakład\n" +
                "tolerancja rozrzutu (Sz,W) = 2 × (maks. luz − (luz + uszka))\n" +
                "rozmiar skrzynki           = najmniejsza nisza w grupie − 2 × (luz + uszka)\n" +
                "rozmiar z listwą           = rozmiar skrzynki + 2 × listwa\n" +
                "rozmiar włazu (Sz × W)     = (szer. z listwą − różnica Sz) × (wys. z listwą − różnica W)\n" +
                "luz z jednej strony        = (luz + uszka) + (największa nisza − najmniejsza nisza) / 2\n" +
                "zakład listwy na ścianie   = listwa − luz z jednej strony"),

            ["help.h.example"] = ("Приклад з вашими поточними числами (мм)", "Example with your current numbers (mm)", "Przykład z Twoimi aktualnymi liczbami (mm)"),
            ["help.example"] = (
                "Зазор {0}, вушка {11}, лиштва {1}, мін. напуск {2}, люк менший за лиштву на {3} / {10}.\n" +
                "макс. зазор = {1} − {2} = {4}\n" +
                "допуск розкиду = 2 × ({4} − ({0}+{11})) = {5}\n" +
                "Нехай найменша ніша групи 840 × 890:\n" +
                "короб = (840 − 2×({0}+{11})) × (890 − 2×({0}+{11})) = {6} × {7}\n" +
                "з лиштвою = ({6} + 2×{1}) × ({7} + 2×{1}) = {12} × {13}\n" +
                "люк = ({12} − {3}) × ({13} − {10}) = {8} × {9}",
                "Gap {0}, ears {11}, trim {1}, min. lap {2}, hatch smaller than trim by {3} / {10}.\n" +
                "max gap = {1} − {2} = {4}\n" +
                "spread tolerance = 2 × ({4} − ({0}+{11})) = {5}\n" +
                "Let the smallest niche of a group be 840 × 890:\n" +
                "box = (840 − 2×({0}+{11})) × (890 − 2×({0}+{11})) = {6} × {7}\n" +
                "with trim = ({6} + 2×{1}) × ({7} + 2×{1}) = {12} × {13}\n" +
                "hatch = ({12} − {3}) × ({13} − {10}) = {8} × {9}",
                "Luz {0}, uszka {11}, listwa {1}, min. zakład {2}, właz mniejszy od listwy o {3} / {10}.\n" +
                "maks. luz = {1} − {2} = {4}\n" +
                "tolerancja rozrzutu = 2 × ({4} − ({0}+{11})) = {5}\n" +
                "Niech najmniejsza nisza w grupie ma 840 × 890:\n" +
                "skrzynka = (840 − 2×({0}+{11})) × (890 − 2×({0}+{11})) = {6} × {7}\n" +
                "z listwą = ({6} + 2×{1}) × ({7} + 2×{1}) = {12} × {13}\n" +
                "właz = ({12} − {3}) × ({13} − {10}) = {8} × {9}"),
            ["help.example.negative"] = (
                "Увага: з такими числами допуск від’ємний, групування неможливе. Збільште лиштву або зменшіть мін. напуск чи зазор.",
                "Warning: with these numbers the tolerance is negative, so grouping is impossible. Increase the trim or decrease the min. lap or the gap.",
                "Uwaga: przy tych liczbach tolerancja jest ujemna, więc grupowanie jest niemożliwe. Zwiększ listwę albo zmniejsz min. zakład lub luz."),

            ["help.h.table"] = ("Як читати таблицю", "How to read the table", "Jak czytać tabelę"),
            ["help.table"] = (
                "• шт — скільки ніш у групі (усі вони отримають однаковий ящик).\n" +
                "• Ніша — найменша і найбільша ніша групи.\n" +
                "• Ящик і Люк — розміри, які треба виготовити для групи.\n" +
                "• Макс. зазор — найбільша щілина з одного боку серед ніш групи.\n" +
                "• Мін. напуск — найменше перекриття стіни лиштвою в групі (чим більше, тим краще).\n" +
                "• Склад — які саме ніші з яких під’їздів увійшли до групи.",
                "• pcs — how many niches are in the group (all of them get the same box).\n" +
                "• Niche — the smallest and the largest niche of the group.\n" +
                "• Box and Hatch — the sizes to be manufactured for the group.\n" +
                "• Max gap — the largest gap per side among the niches of the group.\n" +
                "• Min. lap — the smallest overlap of the wall by the trim in the group (the bigger, the better).\n" +
                "• Composition — which niches from which entrances belong to the group.",
                "• szt. — ile nisz jest w grupie (wszystkie dostaną taką samą skrzynkę).\n" +
                "• Nisza — najmniejsza i największa nisza w grupie.\n" +
                "• Skrzynka i Właz — wymiary do wykonania dla grupy.\n" +
                "• Maks. luz — największa szczelina z jednej strony wśród nisz w grupie.\n" +
                "• Min. zakład — najmniejsze nachodzenie listwy na ścianę w grupie (im większe, tym lepiej).\n" +
                "• Skład — które nisze z których klatek należą do grupy."),
        };
    }
}
