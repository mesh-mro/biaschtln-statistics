# Biaschtln-Statistik — Umsetzungs-TODO

> Checkliste zur Planung und Verfolgung der Implementierung. Details zu jedem Paket
> siehe [`IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md). Erledigte Punkte mit `[x]`
> abhaken. Reihenfolge-Empfehlung: WP1 → WP2 → WP3 → WP4 → WP5 → WP6/WP7. WP8–WP11 optional.

Legende: ☐ offen · ✅ erledigt · 🔑 Fundament (blockiert andere) · ⭐ Priorität

---

## WP1 — Solution- & Projekt-Setup 🔑 ✅
- [x] `Biaschtln.Statistics.slnx` im Repo-Root anlegen *(SDK 10 nutzt das neue `.slnx`-Format)*
- [x] WPF-Projekt `src/Biaschtln.Statistics` (`TargetFramework=net9.0-windows10.0.19041.0`, `UseWPF=true`)
- [x] NuGet-Pakete hinzufügen: LiveChartsCore.SkiaSharpView.WPF 2.0.0, CommunityToolkit.Mvvm 8.4.2, CsvHelper 33.1.0, Microsoft.Extensions.DependencyInjection 9.0.0
- [x] DI-Container in `App.xaml.cs` aufsetzen (Services + ViewModels registrieren, MainWindow auflösen)
- [x] `ViewModels/MainViewModel.cs` als MVVM-Geruest; weitere Ordner (`Models/`, `Services/`, `Views/`, `Converters/`) folgen mit Inhalt in WP2+
- [x] **Akzeptanz:** App startet (Fenstertitel aus VM-Binding), `dotnet build` grün, 0 Warnungen

## WP2 — Datenmodell & CSV-Import ✅
- [x] `Models/OrderLine.cs` mit allen 18 Feldern + abgeleitetem `IsCanceled`
- [x] `CsvOrderImporter` + `OrderLineMap : ClassMap<OrderLine>` (Delimiter `;`, Culture `de-DE`, BOM)
- [x] `GermanBooleanConverter` (`WAHR/FALSCH`); `NULL`/leer → `null` (NullValues); Index-Spalte ignoriert; Datum per Format `yyyy-MM-dd HH:mm:ss` (Invariant)
- [x] `LoadFiles(paths)` → `ImportResult` (konkatenierte Liste + Pro-Datei-`FileLoadResult`); in DI registriert
- [x] **Akzeptanz:** 6 Tests grün — 3 Dateien laden (1926/2475/2017 = 6418 Zeilen), `4,5`→`4.5m`, Datum/Bool/Nullables/Storno, fehlende Datei gemeldet

## WP3 — Datenspeicher & Aggregations-Service ✅
- [x] `IOrderDataService` + `OrderDataService` (In-Memory-Store, `OrdersChanged`-Event, `LoadFiles`/`Clear`); in DI
- [x] `IStatisticsService` + `StatisticsService`: Umsatz/Anzahl je Kategorie & Top-N Artikel (Umsatz/Stückzahl)
- [x] Ø/Median/Max Zubereitungsdauer je Artikel (nur Positionen mit Dauer)
- [x] Anzahl/Umsatz je Benutzer; plus WP8-Vorrat: je Zahlungsmethode, je Tisch, Umsatz über Zeit, Stornoquote
- [x] Service ist zustandslos und aggregiert die übergebene (gefilterte) Menge — Storno-Ausgrenzung ist Filter-Sache (WP4)
- [x] **Akzeptanz:** 10 neue Tests grün (deterministischer Mini-Datensatz + Store gegen 6418 Sample-Zeilen)

## WP4 — Filter-Engine ✅
- [x] `Models/OrderFilter.cs` + `PaidFilter`-Enum (Zeitraum, Kategorien, Artikel, Tische, Benutzer, Zahlungsmethoden, Storno, bezahlt; leere Menge = alle)
- [x] `IOrderFilterService` + `OrderFilterService` (AND-verknüpftes Predikat; Storno standardmäßig ausgeschlossen)
- [x] `FilterViewModel` + `SelectableOption` — Optionen dynamisch aus Daten (distinct, sortiert, Auswahl bleibt bei Reload), `FilterChanged`-Event, `BuildFilter()`/`Reset()`; in DI
- [x] **Akzeptanz:** 15 neue Tests grün (jedes Kriterium + Kombination, VM-Optionsaufbau/Event/Reset)

## WP5 — App-Shell, Datei-Laden-UI & Navigation ✅
- [x] `MainWindow.xaml` — Toolbar (OpenFileDialog Multiselect), Filter-Sidebar, Tab-Navigation
- [x] `MainViewModel` — `OpenFiles`-Command, Status/Fehler, Liste geladener Dateien
- [x] KPI-Kopf (Anzahl Bestellungen, Gesamtumsatz)
- [x] `IFileDialogService`/`FileDialogService` — testbare Abstraktion des OpenFileDialog; in DI
- [x] **Akzeptanz:** Mehrere CSVs laden → KPI korrekt, Filter befüllt (KPIs live bei Datei-/Filteränderung)

## WP6 — Diagramme: Umsatz nach Kategorie / Artikel ⭐ ✅
- [x] `CategorySalesView` + ViewModel (`FilteredChartViewModel`-Basis, geteilte `ChartPalette`)
- [x] Donut: Umsatzanteil je Kategorie (Alk/Anti/Essen) — CVD-geprüfte Farben, Farbe folgt Kategorie
- [x] Balken: Top-N Artikel nach Umsatz **und** Stückzahl (umschaltbar via `EnumToBooleanConverter`)
- [x] Bindung an `ObservableCollection<ISeries>`, reagiert live auf Filter (+ CSV-Vorladen per Startargument)
- [x] **Akzeptanz:** 4 neue Tests grün (Donut = manuelle LINQ-Summe ohne Storno, Ranking-Toggle, Live-Filter, Leerzustand); visuell verifiziert gegen `samples/` (36.353 € Umsatz, 1043 Bestellungen)

## WP7 — Diagramme: Zubereitungsdauer & Personal ⭐ ✅
- [x] `PreparationStaffView` + ViewModel (`FilteredChartViewModel`-Basis)
- [x] Balken: Ø Zubereitungsdauer je Gericht (nur Essen mit Dauer), Y-Achse als m:ss, Tooltip Ø/Median/Max/n
- [x] Balken: Positionen/Umsatz je Benutzer (umschaltbar via `StaffMetric`/`EnumToBooleanConverter`)
- [x] **Akzeptanz:** 5 neue Tests grün (nur Zeilen mit Dauer, Ø = manuelle LINQ, Metrik-Umschalter/Reorder, Live-Filter, Leerzustand); visuell verifiziert (Hendl mit Pommes Ø 2:53, Kellner 3 = 796 Positionen)

## WP8 — (Optional) Weitere Auswertungen ✅
- [x] Umsatz über Zeit (Linie, je Stunde/Tag umschaltbar) — `AnalyticsView` + `AnalyticsViewModel`
- [x] Zahlungsmethoden-Verteilung (Donut, stabile Farbzuordnung übers Methoden-Universum)
- [x] Umsatz je Tisch (Balken, Top 15)
- [x] Stornoquote (Kachel, bewusst inkl. Stornos berechnet — Basisklasse mit Filter-Override)
- [x] **Akzeptanz:** 4 neue Tests grün (Zahlung/Tisch/Zeit-Summen, Stornoquote inkl. Stornos); visuell verifiziert (Stornoquote 0,6 % = 38/6.418)

## WP9 — (Optional) Export ✅
- [x] Chart als PNG (`ChartExport` via `RenderTargetBitmap` — View-seitig, ViewModels bleiben WPF-frei); Button je Seite
- [x] CSV-Export der Aggregat-Tabellen (`ICsvExporter`/`CsvExporter`, Semikolon + de-DE + UTF-8-BOM); Button je Seite (WP6 Top-Artikel, WP7 Zubereitungsdauer, WP8 Umsatz über Zeit)
- [x] `IFileDialogService.SaveFile` ergänzt
- [x] **Akzeptanz:** 4 neue Tests grün (CSV-Render/-Schreiben/-Abbruch, VM-Command); PNG+CSV end-to-end verifiziert (gültige PNG-Signatur, CSV-Kopf `Article;Revenue;Quantity`)

## WP10 — Tests & Beispieldaten-Integration *(läuft mit)*
- [x] Testprojekt `tests/Biaschtln.Statistics.Tests` (xUnit, Target `net9.0-windows10.0.19041.0`)
- [x] `samples/*.csv` via `<None Link>` + `CopyToOutputDirectory` eingebunden
- [x] Import-Tests (WP2)
- [x] Aggregations-Tests (WP3) — `StatisticsServiceTests`, `OrderDataServiceTests`
- [x] Filter-Tests (WP4) — `OrderFilterServiceTests`, `FilterViewModelTests`

## WP11 — (Optional) Politur & README ✅
- [x] README mit Build-/Startanleitung (inkl. CSV-Vorladen per Startargument)
- [x] Kurzdoku CSV-Format & Filter (plus Projektstruktur, Stack, Export)

---

## End-to-End-Verifikation (Abschluss)
- [x] `dotnet build` grün (prüft .NET-9/SkiaSharp-Target) — 0 Warnungen
- [x] `dotnet test` grün gegen `samples/` — 40 Tests
- [x] App startet, lädt alle 3 `samples/*.csv`, KPI/Diagramme/Filter funktionieren live (visuell verifiziert; CSV-Vorladen per Startargument möglich)
