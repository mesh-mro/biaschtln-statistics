# Biaschtln-Statistik

Windows-Desktop-App (**WPF, C#, .NET 9**) zur Auswertung von CSV-Exporten aus dem
Gastro-/Bar-Kassensystem **Biaschtln**. Sie liest mehrere Exporte gleichzeitig ein, filtert
die Daten interaktiv und visualisiert sie in mehreren Diagrammseiten.

## Funktionen

- **Mehrere CSV-Dateien** gleichzeitig laden (Mehrfachauswahl) — die Zeilen werden einfach
  konkateniert (keine Deduplizierung, die Daten sind vorab bereinigt).
- **KPI-Kopf:** Anzahl Bestellungen, Positionen und Gesamtumsatz — live bei Datei- und
  Filteränderung.
- **Filter-Sidebar:** Zeitraum, Bezahlstatus, Stornos ein/aus sowie Kategorien, Artikel,
  Tische, Benutzer und Zahlungsmethoden (Optionen dynamisch aus den Daten).
- **Diagramme (LiveCharts2):**
  - *Umsatz nach Kategorie / Artikel* — Donut je Kategorie + Top-Artikel-Balken (nach Umsatz
    oder Stückzahl umschaltbar).
  - *Zubereitung & Personal* — Ø Zubereitungsdauer je Gericht (nur Positionen mit Dauer) +
    Positionen/Umsatz je Benutzer.
  - *Weitere Auswertungen* — Umsatz über Zeit (Tag/Stunde), Zahlungsmethoden-Verteilung,
    Umsatz je Tisch, Stornoquote.
- **Export:** jede Diagrammseite als **PNG** sowie ihre Aggregat-Tabelle als **CSV**.
- Alle Diagramme reagieren **live** auf Filteränderungen.

## Build & Start

Voraussetzung: **.NET SDK 9+** (oder neuer) unter Windows (WPF-Target
`net9.0-windows10.0.19041.0`).

```powershell
# Bauen
dotnet build

# Starten
dotnet run --project src/Biaschtln.Statistics

# Optional: CSV-Dateien direkt vorladen (auch per "Öffnen mit"/Drag-auf-Exe)
dotnet run --project src/Biaschtln.Statistics -- samples/Export_2026-05-08-23-16-14.csv samples/Export_2026-05-10-12-50-16.csv
```

In der App: **„Dateien öffnen…"** in der Toolbar → eine oder mehrere `samples/*.csv`
auswählen. KPI-Kopf, Filter und Diagramme füllen sich automatisch.

## Tests

xUnit-Tests laufen gegen die drei Beispieldateien in `samples/` (Import, Aggregation,
Filter, Diagramm-ViewModels, Export):

```powershell
dotnet test
```

## CSV-Format (Kurzfassung)

Die Exporte haben ein festes Schema. Wichtige Eckdaten fürs Parsen:

| Eigenschaft | Wert |
|---|---|
| Trennzeichen | Semikolon `;` |
| Encoding | UTF-8 **mit BOM**, Zeilenende CRLF |
| Dezimaltrennzeichen | **Komma** (`4,5`) → Parsen mit `de-DE`-Kultur (nur fürs Parsen) |
| Datum/Zeit | `yyyy-MM-dd HH:mm:ss` |
| Boolean | `WAHR` / `FALSCH` |
| Null | Wert `NULL` **oder** leer |
| Erste Spalte | unbenannter Zeilenindex → wird ignoriert |

Spalten (Auswahl): `Bestell-ID`, `Bestellung` (gruppiert Positionen zu einer Bestellung),
`Status`, `Tisch`, `Artikel`, `Kategorie` (`Alk`/`Anti`/`Essen`), `Preis`,
`Bestellzeitpunkt`, `Benutzer`, `Bezahlt`, `Zahlungsmethode`,
`Zubereitungsdauer in Sekunden` (nur bei Essen). Vollständige Mapping-Tabelle:
siehe [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md) §2.

**Storno:** Eine Position gilt als storniert, wenn ihr `Status` `STORNO`/`CANCELED` enthält.
**Umsatz** = Summe `Preis` **ohne** Stornos (per Filter „Stornos einbeziehen" umstellbar).

## Filter

Alle Kriterien sind UND-verknüpft; eine leere Mengen-Auswahl bedeutet „kein Filter für dieses
Kriterium" (alles erlaubt):

- **Zeitraum** — Von/Bis (inklusive), leer = unbegrenzt.
- **Bezahlstatus** — Alle / nur bezahlt / nur unbezahlt.
- **Stornos einbeziehen** — standardmäßig aus (Stornos werden ausgeschlossen).
- **Kategorien / Artikel / Tische / Benutzer / Zahlungsmethoden** — Mehrfachauswahl aus den in
  den Daten vorkommenden Werten. Die Auswahl bleibt beim Nachladen erhalten.
- **Zurücksetzen** — setzt alle Kriterien auf den Ausgangszustand.

Die *Stornoquote* (Seite „Weitere Auswertungen") wird bewusst **inklusive** stornierter
Positionen berechnet, unabhängig vom Storno-Schalter.

## Projektstruktur

```
Biaschtln.Statistics.slnx
src/Biaschtln.Statistics/
  App.xaml(.cs)          DI-Container (Microsoft.Extensions.DependencyInjection)
  MainWindow.xaml(.cs)   Shell: Toolbar, KPI-Kopf, Filter-Sidebar, Tab-Navigation
  Models/                OrderLine, OrderFilter, StatisticsSummaries
  Services/              CSV-Import, Datenspeicher, Statistik, Filter, Datei-Dialog, Export
  ViewModels/            Main-, Filter- und die Diagramm-ViewModels (MVVM, CommunityToolkit)
  Views/                 CategorySalesView, PreparationStaffView, AnalyticsView, ChartExport
  Converters/            EnumToBooleanConverter
tests/Biaschtln.Statistics.Tests/   xUnit-Tests gegen samples/
samples/                 drei Beispiel-CSV-Exporte (~6.400 Zeilen)
docs/                    IMPLEMENTATION_PLAN.md (maßgebliche Spezifikation), TODO.md
```

## Technologie-Stack

- WPF, .NET 9 (`net9.0-windows10.0.19041.0`), MVVM-Architektur
- [LiveChartsCore.SkiaSharpView.WPF](https://livecharts.dev/) 2.0.0 — Diagramme
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) 8.4.2
- [CsvHelper](https://joshclose.github.io/CsvHelper/) 33.1.0 — CSV-Import/-Export
- Microsoft.Extensions.DependencyInjection — Service-/ViewModel-Registrierung
- xUnit — Tests

> **Hinweis zum Target:** Es muss `net9.0-windows10.0.19041.0` sein (nicht nur
> `net9.0-windows`), sonst baut SkiaSharp 3 / LiveCharts2 nicht.

## Lizenz

MIT — siehe `LICENSE`.
