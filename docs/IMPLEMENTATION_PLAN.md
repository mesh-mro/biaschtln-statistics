# Biaschtln-Statistik — Implementierungsplan & Analyse

> **Zweck dieses Dokuments:** Vollständige, eigenständige Spezifikation für die
> Umsetzung der App. Es enthält alle Analyse-Ergebnisse (CSV-Format, Spalten-Mapping,
> Beispieldaten), die getroffenen Technologie-Entscheidungen inkl. konkreter
> Paket-Versionen und Stolpersteine, sowie die in unabhängige Arbeitspakete
> aufgeteilte Roadmap. Eine künftige Coding-Session kann **ohne erneute Analyse**
> direkt mit der Umsetzung beginnen.
>
> Stand: 2026-05-31.

---

## 1. Kontext & Ziel

Es soll eine Desktop-App (**WPF, C#, .NET 9**) gebaut werden, die CSV-Exporte aus dem
Bestellsystem **Biaschtln** (ein Gastro-/Bar-Kassensystem) einliest und auswertet.

**Funktionale Ziele:**
- Mehrere CSV-Dateien gleichzeitig einlesen (Mehrfachauswahl).
- Daten über verschiedene Filter eingrenzen.
- Eingegrenzte Daten in mehreren Diagrammtypen visualisieren.

**Schwerpunkt-Auswertungen (mit dem Auftraggeber abgestimmt):**
1. **Umsatz nach Kategorie / Artikel** — Priorität 1
2. **Zubereitungsdauer & Personal** — Priorität 2
3. Umsatz über Zeit, Zahlung & Tische — optional/sekundär

**Ausgangslage:** Greenfield. Das Repo enthält nur die drei Beispiel-Exporte unter
`samples/`, kein Quellcode (`src/` ist leer). MIT-Lizenz, Git-Repo (Markus Roider).

---

## 2. CSV-Datenformat (verifiziert anhand `samples/`)

Drei Beispieldateien, **identisches Schema**:

| Datei | Größe | Datenzeilen |
|---|---|---|
| `samples/Export_2026-05-08-23-16-14.csv` | 261 KB | 1.926 |
| `samples/Export_2026-05-10-12-50-16.csv` | 348 KB | 2.547 |
| `samples/Export_2026-05-11-13-24-51.csv` | 275 KB | 2.017 |

Gesamt ~6.490 Datenzeilen.

### Format-Eckdaten (WICHTIG fürs Parsen)
- **Trennzeichen:** Semikolon `;`
- **Encoding:** UTF-8 **mit BOM**, Zeilenende **CRLF**
- **Dezimaltrennzeichen:** **Komma** (`4,5` = vier-komma-fünf) → **`de-DE`-Kultur beim Parsen nötig**
- **Datum/Zeit:** `yyyy-MM-dd HH:mm:ss` (z. B. `2026-05-08 18:32:50`)
- **Bool:** `WAHR` / `FALSCH`
- **Null:** Wert `NULL` **oder** leerer String
- **Quoting:** keines beobachtet (Werte enthalten Sonderzeichen wie `#fbc02d`, Umlaute)
- **Erste Spalte:** unbenannter Zeilenindex (0,1,2,…) → **ignorieren**

### Spalten (19) und Property-Mapping

| # | CSV-Header | Property | Typ | Bemerkung |
|---|---|---|---|---|
| 1 | *(leer)* | — | — | Zeilenindex, ignorieren |
| 2 | `Bestell-ID` | `OrderLineId` | `int` | ID der Bestellposition |
| 3 | `Bestellung` | `OrderId` | `int` | gruppiert mehrere Positionen zu einer Bestellung |
| 4 | `Status` | `Status` | `string` | z. B. `ORDER: CREATED - Bestellung erfasst`, `ORDER: CANCELED - Bestellung STORNO` |
| 5 | `Tisch` | `Table` | `string` | z. B. `H10`, `A1`, `E2` |
| 6 | `Artikel` | `Article` | `string` | z. B. `Bier 0,5`, `Schnitzel mit Pommes` |
| 7 | `Artikelfarbe` | `ArticleColor` | `string` | Hex, z. B. `#fbc02d` |
| 8 | `Kategorie` | `Category` | `string` | `Alk`, `Anti`, `Essen` |
| 9 | `Preis` | `Price` | `decimal` | Komma-Dezimal, z. B. `4,5` |
| 10 | `Ust-Satz` | `VatRate` | `decimal` | meist `0` |
| 11 | `Ust-Schlüssel` | `VatKey` | `string?` | oft `NULL` |
| 12 | `Kommentar` | `Comment` | `string?` | oft leer |
| 13 | `Bestellzeitpunkt` | `OrderedAt` | `DateTime` | siehe Datumsformat |
| 14 | `Benutzer` | `User` | `string` | z. B. `Kellner 9`, `Abholstation` |
| 15 | `Bezahlt` | `IsPaid` | `bool` | `WAHR`/`FALSCH` |
| 16 | `Bezahlter Betrag` | `PaidAmount` | `decimal?` | leer wenn nicht bezahlt |
| 17 | `Zahlungsmethode` | `PaymentMethod` | `string?` | `cash`, `card`, `voucher`, `ec`, `other` |
| 18 | `Zahlungsreferenz` | `PaymentReference` | `string?` | meist leer |
| 19 | `Zubereitungsdauer in Sekunden` | `PreparationSeconds` | `int?` | nur bei Essen gesetzt |

**Abgeleitetes Feld:** `IsCanceled` = `Status` enthält `STORNO` oder `CANCELED`.

### Beispielzeilen (Header + Daten, original)
```
;Bestell-ID;Bestellung;Status;Tisch;Artikel;Artikelfarbe;Kategorie;Preis;Ust-Satz;Ust-Schlüssel;Kommentar;Bestellzeitpunkt;Benutzer;Bezahlt;Bezahlter Betrag;Zahlungsmethode;Zahlungsreferenz;Zubereitungsdauer in Sekunden
0;152;63;ORDER: CREATED - Bestellung erfasst;H10;Bier 0,5;#fbc02d;Alk;4,5;0;NULL;;2026-05-08 18:32:50;Kellner 9;WAHR;4,5;cash;;
0;97;29;ORDER: CANCELED - Bestellung STORNO;B5;Bier 0,5;#fbc02d;Alk;4,5;0;NULL;;2026-05-09 18:03:09;Kellner 11;FALSCH;0;;;
1;100;30;ORDER: CREATED - Bestellung erfasst;A1;Schnitzelsemmerl;#f57f17;Essen;4,5;0;NULL;;2026-05-09 18:12:30;Abholstation;WAHR;4,5;cash;;183
```

### Domänen-Semantik
Restaurant/Bar-Kassensystem. Eine Zeile = eine Bestellposition. `OrderId` (`Bestellung`)
fasst mehrere Positionen einer Bestellung an einem Tisch zusammen. Kategorien:
`Alk` (Alkohol), `Anti` (alkoholfrei), `Essen`. Stornos haben `IsPaid=FALSCH` und
`PaidAmount`/`Zahlungsmethode` leer.

### Datenkonvention: Mehrere Dateien
**Keine Deduplizierung implementieren.** Die Daten werden vom Auftraggeber vorab
bereinigt, sodass es **keine zeitlichen Überschneidungen** zwischen Dateien gibt.
Mehrere Dateien werden beim Laden einfach **konkateniert**.

---

## 3. Technologie-Stack (abgestimmt + recherchiert)

| Zweck | Paket / Setting | Version | Hinweis |
|---|---|---|---|
| Runtime/UI | WPF, **TargetFramework `net9.0-windows10.0.19041.0`** | .NET 9 | siehe Stolperstein unten |
| Diagramme | `LiveChartsCore.SkiaSharpView.WPF` | **2.0.0** (stabil) | MVVM-freundlich, animiert |
| MVVM | `CommunityToolkit.Mvvm` | **8.4.2** | `[ObservableProperty]`, `[RelayCommand]` |
| CSV | `CsvHelper` | **33.1.0** | ClassMap + `de-DE` Culture |
| DI | `Microsoft.Extensions.DependencyInjection` | aktuell | Services/ViewModels registrieren |
| Tests | `xUnit` | aktuell | Import/Aggregation/Filter |

### ⚠️ Stolpersteine (unbedingt beachten)
1. **TargetFramework muss `net9.0-windows10.0.19041.0` sein** (nicht nur
   `net9.0-windows`). SkiaSharp 3 (transitiv über LiveCharts2) liefert keinen Build
   für `netx.0-windows` ohne Min-Windows-Version → sonst Build-Fehler.
2. **`de-DE`-Kultur nur fürs CSV-Parsen** verwenden (in `CsvConfiguration`), **nicht**
   global per `Thread.CurrentCulture` setzen — sonst ändern sich UI-Formatierungen.
3. **Umsatzdefinition** früh festlegen: Umsatz = Summe `Price` **ohne** stornierte Zeilen
   (`IsCanceled`). Als sichtbaren Filter/Toggle anbieten, damit Kennzahlen nachvollziehbar bleiben.
4. **LiveCharts2-Series an `ObservableCollection` binden**, damit Filter-Updates ohne
   Chart-Neuaufbau durchschlagen.

---

## 4. Geplante Projektstruktur

```
Biaschtln.Statistics.sln
src/Biaschtln.Statistics/
  Biaschtln.Statistics.csproj      (net9.0-windows10.0.19041.0, UseWPF=true)
  App.xaml(.cs)                    (DI-Container-Setup)
  Models/        OrderLine.cs, OrderFilter.cs
  Services/      CsvOrderImporter.cs, IOrderDataService.cs, StatisticsService.cs, OrderFilterService.cs
  ViewModels/    MainViewModel.cs, FilterViewModel.cs, CategorySalesViewModel.cs, PreparationStaffViewModel.cs
  Views/         MainWindow.xaml, CategorySalesView.xaml, PreparationStaffView.xaml
  Converters/
tests/Biaschtln.Statistics.Tests/  (xUnit; samples als CopyToOutputDirectory)
samples/                           (vorhandene CSV-Exporte)
docs/IMPLEMENTATION_PLAN.md        (dieses Dokument)
```

---

## 5. Arbeitspakete

WP1–WP4 bilden das Fundament. WP5–WP8 hängen nur an WP3/WP4 und sind untereinander
weitgehend unabhängig umsetzbar.

### WP1 — Solution- & Projekt-Setup *(Fundament, zuerst)*
- `Biaschtln.Statistics.sln` im Repo-Root.
- WPF-Projekt `src/Biaschtln.Statistics` mit `<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>`, `<UseWPF>true</UseWPF>`.
- NuGet-Pakete referenzieren (Versionen siehe §3).
- DI-Container in `App.xaml.cs` (Services + ViewModels registrieren, MainWindow daraus auflösen).
- Ordnerstruktur (§4) anlegen.
- **Akzeptanz:** Leeres Fenster startet, `dotnet build` grün.

### WP2 — Datenmodell & CSV-Import *(hängt an WP1)*
- `Models/OrderLine.cs` — Felder gemäß Mapping-Tabelle §2; abgeleitetes `IsCanceled`.
- `Services/CsvOrderImporter.cs` mit CsvHelper `ClassMap<OrderLine>`:
  - `CsvConfiguration`: `Delimiter=";"`, `CultureInfo("de-DE")`, BOM-Erkennung.
  - Bool-Konverter `WAHR/FALSCH`; `NULL`/leer → `null` bei nullable Feldern.
  - Erste (Index-)Spalte ignorieren; Mapping per Header-Name (nicht Position) wo möglich.
  - `LoadFiles(IEnumerable<string> paths)` → konkateniert alle Zeilen, liefert
    `List<OrderLine>` + pro Datei Erfolg/Fehler-Info.
- **Akzeptanz:** Test lädt alle 3 `samples/`-Dateien → erwartete Zeilenanzahl;
  `4,5` → `4.5m`; Datum/Bool korrekt.

### WP3 — Datenspeicher & Aggregations-Service *(hängt an WP2)*
- `Services/IOrderDataService` — hält geladene `OrderLine`s in-memory, meldet Änderungen.
- `Services/StatisticsService.cs` — LINQ-Aggregationen auf der jeweils gefilterten Menge:
  - Umsatz/Anzahl je `Category`; je `Article` (Top-N).
  - Ø/Median/Max `PreparationSeconds` je `Article` (nur Zeilen mit Wert).
  - Anzahl/Umsatz je `User`.
  - Vorrat für WP8: Umsatz je Zeitintervall, je `PaymentMethod`, je `Table`, Stornoquote.
  - Umsatz = Summe `Price` ohne `IsCanceled` (per Filter umstellbar).
- **Akzeptanz:** Tests prüfen Aggregat-Summen gegen Sample-Daten.

### WP4 — Filter-Engine *(hängt an WP3)*
- `Models/OrderFilter.cs` — Zeitraum (von/bis), Kategorien, Artikel, Tische, Benutzer,
  Zahlungsmethoden, Storno ein/aus, bezahlt ein/aus.
- `Services/OrderFilterService` — wendet Filter als Predikat auf `IEnumerable<OrderLine>` an.
- `ViewModels/FilterViewModel.cs` — Optionen dynamisch aus geladenen Daten (distinct
  Kategorien/Artikel/Tische/Benutzer); löst Refresh aus.
- **Akzeptanz:** Filteränderung aktualisiert berechnete Mengen reproduzierbar (Test).

### WP5 — App-Shell, Datei-Laden-UI & Navigation *(hängt an WP1; integriert WP2/WP4)*
- `Views/MainWindow.xaml` — Toolbar (Dateien öffnen via `OpenFileDialog{Multiselect=true}`),
  linke Filter-Sidebar (WP4), Hauptbereich mit Tabs/Navigation für Diagrammseiten.
- `ViewModels/MainViewModel.cs` — `[RelayCommand] OpenFiles`, Status-/Fehleranzeige,
  Liste geladener Dateien, KPI-Kopf (Anzahl Bestellungen, Gesamtumsatz).
- **Akzeptanz:** Mehrere CSVs laden → KPI-Kopf korrekt, Filter befüllt sich.

### WP6 — Diagramme: Umsatz nach Kategorie / Artikel *(Priorität 1; hängt an WP3/WP4/WP5)*
- `Views/CategorySalesView.xaml` + ViewModel:
  - Donut: Umsatzanteil je Kategorie (Alk/Anti/Essen), Farbe aus Kategorie/`ArticleColor`.
  - Balken: Top-N Artikel nach Umsatz **und** nach Stückzahl (umschaltbar).
  - LiveCharts2 `PieSeries`/`ColumnSeries`, gebunden an Aggregat-`ObservableCollection`.
- **Akzeptanz:** Werte = manuelle LINQ-Summe; reagiert live auf Filter.

### WP7 — Diagramme: Zubereitungsdauer & Personal *(Priorität 2; hängt an WP3/WP4/WP5)*
- `Views/PreparationStaffView.xaml` + ViewModel:
  - Balken: Ø Zubereitungsdauer je Gericht (nur `Essen` mit `PreparationSeconds`).
  - Balken: Bestellungen/Umsatz je Benutzer (Kellner/Abholstation).
  - Optional: Min/Ø/Max-Hinweis.
- **Akzeptanz:** Nur Zeilen mit Dauer fließen ein; Personalzahlen plausibel.

### WP8 — (Optional) Weitere Auswertungen *(unabhängig; hängt an WP3/WP4/WP5)*
- Umsatz über Zeit (Linie/Balken je Stunde/Tag), Zahlungsmethoden-Verteilung,
  Umsatz je Tisch, Stornoquote. Jede Seite eigenständig ergänzbar.

### WP9 — (Optional) Export *(hängt an WP6/WP7)*
- Chart als PNG (LiveCharts2/SkiaSharp `SKCanvas`), optional CSV-Export der Aggregate.

### WP10 — Tests & Beispieldaten-Integration *(begleitend; hängt an WP2/WP3/WP4)*
- `tests/Biaschtln.Statistics.Tests` (xUnit). `samples/`-Dateien als
  `CopyToOutputDirectory` einbinden und als Fixtures nutzen (Import-, Aggregations-,
  Filter-Tests). Keine WPF-UI-Tests nötig.

### WP11 — (Optional) Politur & README
- README mit Build-/Startanleitung und Kurzdoku zu CSV-Format und Filtern.

---

## 6. Verifikation (End-to-End)
1. `dotnet build` der Solution → grün (prüft den .NET-9/SkiaSharp-Target-Stolperstein).
2. `dotnet test` → Import/Aggregation/Filter gegen die 3 `samples/`-Dateien grün.
3. App starten, alle 3 `samples/*.csv` per Dialog laden:
   - KPI-Kopf zeigt Gesamtanzahl Bestellungen & Gesamtumsatz.
   - WP6: Kategorie-Donut + Top-Artikel-Balken plausibel (Alk/Anti/Essen).
   - WP7: Zubereitungsdauer je Gericht & Bestellungen je Kellner.
   - Filter setzen (z. B. Kategorie = Essen) → alle Diagramme aktualisieren live.
