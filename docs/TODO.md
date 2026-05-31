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

## WP4 — Filter-Engine
- [ ] `Models/OrderFilter.cs` (Zeitraum, Kategorien, Artikel, Tische, Benutzer, Zahlungsmethoden, Storno, bezahlt)
- [ ] `OrderFilterService` (Predikat auf `IEnumerable<OrderLine>`)
- [ ] `FilterViewModel` — Optionen dynamisch aus Daten (distinct), löst Refresh aus
- [ ] **Akzeptanz:** Filteränderung aktualisiert Mengen reproduzierbar (Test)

## WP5 — App-Shell, Datei-Laden-UI & Navigation
- [ ] `MainWindow.xaml` — Toolbar (OpenFileDialog Multiselect), Filter-Sidebar, Tab-Navigation
- [ ] `MainViewModel` — `OpenFiles`-Command, Status/Fehler, Liste geladener Dateien
- [ ] KPI-Kopf (Anzahl Bestellungen, Gesamtumsatz)
- [ ] **Akzeptanz:** Mehrere CSVs laden → KPI korrekt, Filter befüllt

## WP6 — Diagramme: Umsatz nach Kategorie / Artikel ⭐
- [ ] `CategorySalesView` + ViewModel
- [ ] Donut: Umsatzanteil je Kategorie (Alk/Anti/Essen)
- [ ] Balken: Top-N Artikel nach Umsatz **und** Stückzahl (umschaltbar)
- [ ] Bindung an `ObservableCollection`, reagiert live auf Filter
- [ ] **Akzeptanz:** Werte = manuelle LINQ-Summe

## WP7 — Diagramme: Zubereitungsdauer & Personal ⭐
- [ ] `PreparationStaffView` + ViewModel
- [ ] Balken: Ø Zubereitungsdauer je Gericht (nur Essen mit Dauer)
- [ ] Balken: Bestellungen/Umsatz je Benutzer
- [ ] **Akzeptanz:** Nur Zeilen mit Dauer fließen ein; Personalzahlen plausibel

## WP8 — (Optional) Weitere Auswertungen
- [ ] Umsatz über Zeit (je Stunde/Tag)
- [ ] Zahlungsmethoden-Verteilung
- [ ] Umsatz je Tisch
- [ ] Stornoquote

## WP9 — (Optional) Export
- [ ] Chart als PNG (SkiaSharp)
- [ ] CSV-Export der Aggregat-Tabellen

## WP10 — Tests & Beispieldaten-Integration *(läuft mit)*
- [x] Testprojekt `tests/Biaschtln.Statistics.Tests` (xUnit, Target `net9.0-windows10.0.19041.0`)
- [x] `samples/*.csv` via `<None Link>` + `CopyToOutputDirectory` eingebunden
- [x] Import-Tests (WP2)
- [x] Aggregations-Tests (WP3) — `StatisticsServiceTests`, `OrderDataServiceTests`
- [ ] Filter-Tests (WP4)

## WP11 — (Optional) Politur & README
- [ ] README mit Build-/Startanleitung
- [ ] Kurzdoku CSV-Format & Filter

---

## End-to-End-Verifikation (Abschluss)
- [ ] `dotnet build` grün (prüft .NET-9/SkiaSharp-Target)
- [ ] `dotnet test` grün gegen `samples/`
- [ ] App startet, lädt alle 3 `samples/*.csv`, KPI/Diagramme/Filter funktionieren live
