# CLAUDE.md — Biaschtln-Statistik

WPF-Desktop-App (C#, .NET 9), die CSV-Exporte aus dem Gastro-Kassensystem **Biaschtln**
einliest, filtert und in Diagrammen visualisiert.

## Zuerst lesen

- **[docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md)** — maßgebliche Spezifikation:
  CSV-Format & Spalten-Mapping, Technologie-Stack mit konkreten Paket-Versionen,
  Stolpersteine, Projektstruktur und die Arbeitspakete WP1–WP11. **Vor jeder Umsetzung
  konsultieren.** (Bewusst nur referenziert, nicht importiert — zu groß für den Auto-Kontext.)

## Aktueller Stand / TODO

@docs/TODO.md

## Wichtigste Eckpunkte (Kurzfassung — Details im Plan)

- **Stack:** WPF, .NET 9, LiveChartsCore.SkiaSharpView.WPF 2.0.0, CommunityToolkit.Mvvm 8.4.2,
  CsvHelper 33.1.0, Microsoft.Extensions.DependencyInjection. MVVM-Architektur.
- **⚠️ TargetFramework muss `net9.0-windows10.0.19041.0` sein** (nicht nur `net9.0-windows`),
  sonst baut SkiaSharp 3 / LiveCharts2 nicht.
- **CSV:** Trennzeichen `;`, UTF-8 mit BOM, **Komma-Dezimal** (`4,5`) → Parsen mit `de-DE`-Kultur
  (nur fürs Parsen, nicht global setzen). Datum `yyyy-MM-dd HH:mm:ss`, Bool `WAHR/FALSCH`.
- **Mehrere Dateien** werden einfach konkateniert — **keine Deduplizierung** (Daten sind vorab
  bereinigt, keine Zeitüberschneidungen).
- **Umsatz** = Summe `Price` ohne Stornos (`IsCanceled`), per Filter umstellbar.
- Beispieldaten liegen in `samples/` (3 CSV-Exporte, ~6.500 Zeilen).

## Konventionen

- Erledigte TODO-Punkte in `docs/TODO.md` mit `[x]` abhaken, wenn ein Arbeitspaket fertig
  und dessen Akzeptanzkriterium erfüllt ist.
- Reihenfolge-Empfehlung: WP1 → WP2 → WP3 → WP4 → WP5 → WP6/WP7. WP8–WP11 optional.

## Build & Test

- `dotnet build` — baut die Solution.
- `dotnet test` — führt die Tests (xUnit) gegen die `samples/`-Dateien aus.
