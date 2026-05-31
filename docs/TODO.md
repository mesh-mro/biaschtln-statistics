# Biaschtln-Statistik — Umsetzungs-TODO

> Checkliste zur Planung und Verfolgung der Implementierung. Details zu jedem Paket
> siehe [`IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md). Erledigte Punkte mit `[x]`
> abhaken. Reihenfolge-Empfehlung: WP1 → WP2 → WP3 → WP4 → WP5 → WP6/WP7. WP8–WP11 optional.

Legende: ☐ offen · ✅ erledigt · 🔑 Fundament (blockiert andere) · ⭐ Priorität

---

## WP1 — Solution- & Projekt-Setup 🔑
- [ ] `Biaschtln.Statistics.sln` im Repo-Root anlegen
- [ ] WPF-Projekt `src/Biaschtln.Statistics` (`TargetFramework=net9.0-windows10.0.19041.0`, `UseWPF=true`)
- [ ] NuGet-Pakete hinzufügen: LiveChartsCore.SkiaSharpView.WPF 2.0.0, CommunityToolkit.Mvvm 8.4.2, CsvHelper 33.1.0, Microsoft.Extensions.DependencyInjection
- [ ] DI-Container in `App.xaml.cs` aufsetzen (Services + ViewModels registrieren, MainWindow auflösen)
- [ ] Ordnerstruktur anlegen (`Models/`, `Services/`, `ViewModels/`, `Views/`, `Converters/`)
- [ ] **Akzeptanz:** Leeres Fenster startet, `dotnet build` grün

## WP2 — Datenmodell & CSV-Import
- [ ] `Models/OrderLine.cs` mit allen 18 Feldern + abgeleitetem `IsCanceled`
- [ ] `CsvOrderImporter` + `ClassMap<OrderLine>` (Delimiter `;`, Culture `de-DE`, BOM)
- [ ] Bool-Konverter `WAHR/FALSCH`; `NULL`/leer → `null` bei nullable Feldern; Index-Spalte ignorieren
- [ ] `LoadFiles(paths)` — konkateniert Zeilen, liefert Liste + pro-Datei-Status
- [ ] **Akzeptanz:** Test lädt alle 3 `samples/`-Dateien (Zeilenanzahl, `4,5`→`4.5m`, Datum/Bool)

## WP3 — Datenspeicher & Aggregations-Service
- [ ] `IOrderDataService` (In-Memory-Store, meldet Änderungen)
- [ ] `StatisticsService`: Umsatz/Anzahl je Kategorie & je Artikel (Top-N)
- [ ] `StatisticsService`: Ø/Median/Max Zubereitungsdauer je Artikel
- [ ] `StatisticsService`: Anzahl/Umsatz je Benutzer
- [ ] Umsatzdefinition: Summe `Price` ohne Stornos (per Filter umstellbar)
- [ ] **Akzeptanz:** Tests prüfen Aggregat-Summen gegen Sample-Daten

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

## WP10 — Tests & Beispieldaten-Integration
- [ ] Testprojekt `tests/Biaschtln.Statistics.Tests` (xUnit)
- [ ] `samples/`-Dateien als `CopyToOutputDirectory` einbinden
- [ ] Import-, Aggregations-, Filter-Tests

## WP11 — (Optional) Politur & README
- [ ] README mit Build-/Startanleitung
- [ ] Kurzdoku CSV-Format & Filter

---

## End-to-End-Verifikation (Abschluss)
- [ ] `dotnet build` grün (prüft .NET-9/SkiaSharp-Target)
- [ ] `dotnet test` grün gegen `samples/`
- [ ] App startet, lädt alle 3 `samples/*.csv`, KPI/Diagramme/Filter funktionieren live
