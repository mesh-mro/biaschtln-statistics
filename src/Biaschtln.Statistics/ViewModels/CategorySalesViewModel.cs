using System.Collections.ObjectModel;
using System.Globalization;
using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// ViewModel für "Umsatz nach Kategorie / Artikel" (WP6): ein Donut mit dem Umsatzanteil
/// je Kategorie und ein Balkendiagramm der Top-N-Artikel, umschaltbar zwischen Umsatz und
/// Stückzahl. Reagiert live auf Datei- und Filteränderungen.
/// </summary>
public sealed partial class CategorySalesViewModel : FilteredChartViewModel
{
    private const int TopN = 10;
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private readonly IStatisticsService _statistics;
    private readonly Axis _articleXAxis;
    private readonly Axis _articleYAxis;

    public CategorySalesViewModel(
        IOrderDataService data,
        IOrderFilterService filterService,
        IStatisticsService statistics,
        FilterViewModel filter,
        ICsvExporter csvExporter)
        : base(data, filterService, filter, csvExporter)
    {
        _statistics = statistics;

        _articleXAxis = new Axis
        {
            Labels = [],
            LabelsRotation = 25,
            LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            TextSize = 12,
            SeparatorsPaint = null,
        };
        _articleYAxis = new Axis
        {
            MinLimit = 0,
            Labeler = value => value.ToString("N0", German),
            LabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(ChartPalette.Grid) { StrokeThickness = 1 },
        };

        ArticleXAxes = [_articleXAxis];
        ArticleYAxes = [_articleYAxis];

        Refresh();
    }

    /// <summary>Donut-Serien: je Kategorie ein Segment.</summary>
    public ObservableCollection<ISeries> CategorySeries { get; } = [];

    /// <summary>Balken-Serie: Top-N-Artikel nach aktuellem Ranking.</summary>
    public ObservableCollection<ISeries> ArticleSeries { get; } = [];

    public IReadOnlyList<ICartesianAxis> ArticleXAxes { get; }

    public IReadOnlyList<ICartesianAxis> ArticleYAxes { get; }

    /// <summary>Umschaltung Top-Artikel nach Umsatz oder Stückzahl.</summary>
    [ObservableProperty]
    private ArticleRanking _articleRanking = ArticleRanking.Revenue;

    /// <summary>False, wenn die gefilterte Menge leer ist (steuert die Leer-Anzeige).</summary>
    [ObservableProperty]
    private bool _hasData;

    partial void OnArticleRankingChanged(ArticleRanking value) => Refresh();

    /// <summary>Exportiert die aktuelle Top-Artikel-Tabelle als CSV.</summary>
    [RelayCommand]
    private void ExportCsv()
    {
        var rows = _statistics.TopArticles(FilteredOrders(), TopN, ArticleRanking);
        var suffix = ArticleRanking == ArticleRanking.Revenue ? "umsatz" : "stueckzahl";
        CsvExporter.ExportCsv(rows, $"top-artikel-{suffix}");
    }

    protected override void Refresh()
    {
        var orders = FilteredOrders();
        HasData = orders.Count > 0;

        BuildCategoryDonut(orders);
        BuildArticleBars(orders);
    }

    private void BuildCategoryDonut(IReadOnlyList<OrderLine> orders)
    {
        CategorySeries.Clear();
        foreach (var category in _statistics.RevenueByCategory(orders))
        {
            var summary = category;
            CategorySeries.Add(new PieSeries<decimal>
            {
                Values = [summary.Revenue],
                Name = summary.Category,
                InnerRadius = 72,
                Fill = new SolidColorPaint(ChartPalette.Category(summary.Category)),
                Stroke = null,
                DataLabelsPaint = new SolidColorPaint(ChartPalette.Ink),
                DataLabelsSize = 13,
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = _ => $"{summary.Category} · {summary.Revenue.ToString("C0", German)}",
                ToolTipLabelFormatter = _ =>
                    $"{summary.Category}: {summary.Revenue.ToString("C2", German)} ({summary.Count} Pos.)",
            });
        }
    }

    private void BuildArticleBars(IReadOnlyList<OrderLine> orders)
    {
        var byRevenue = ArticleRanking == ArticleRanking.Revenue;
        var top = _statistics.TopArticles(orders, TopN, ArticleRanking);

        var values = top
            .Select(a => byRevenue ? (double)a.Revenue : a.Quantity)
            .ToArray();

        ArticleSeries.Clear();
        ArticleSeries.Add(new ColumnSeries<double>
        {
            Values = values,
            Name = byRevenue ? "Umsatz" : "Stückzahl",
            Fill = new SolidColorPaint(ChartPalette.SeriesBlue),
            Stroke = null,
            Rx = 4,
            Ry = 4,
            DataLabelsPaint = new SolidColorPaint(ChartPalette.Muted),
            DataLabelsSize = 11,
            DataLabelsPosition = DataLabelsPosition.Top,
            DataLabelsFormatter = point => byRevenue
                ? ((decimal)point.Coordinate.PrimaryValue).ToString("C0", German)
                : point.Coordinate.PrimaryValue.ToString("N0", German),
            YToolTipLabelFormatter = point => byRevenue
                ? ((decimal)point.Coordinate.PrimaryValue).ToString("C2", German)
                : $"{point.Coordinate.PrimaryValue.ToString("N0", German)} Stk.",
        });

        _articleXAxis.Labels = top.Select(a => a.Article).ToList();
        _articleYAxis.Labeler = byRevenue
            ? (value => value.ToString("C0", German))
            : (value => value.ToString("N0", German));
    }
}
