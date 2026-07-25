using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="IStatisticsService" />
public sealed class StatisticsService : IStatisticsService
{
    public decimal TotalRevenue(IEnumerable<OrderLine> orders) => orders.Sum(o => o.Price);

    public int TotalCount(IEnumerable<OrderLine> orders) => orders.Count();

    public int DistinctOrderCount(IEnumerable<OrderLine> orders) =>
        orders.Select(o => o.OrderId).Distinct().Count();

    public IReadOnlyList<CategorySummary> RevenueByCategory(IEnumerable<OrderLine> orders) =>
        orders
            .GroupBy(o => o.Category)
            .Select(g => new CategorySummary(g.Key, g.Sum(o => o.Price), g.Count()))
            .OrderByDescending(s => s.Revenue)
            .ToList();

    public IReadOnlyList<ArticleSummary> TopArticles(IEnumerable<OrderLine> orders, int topN, ArticleRanking ranking)
    {
        var summaries = orders
            .GroupBy(o => o.Article)
            .Select(g => new ArticleSummary(g.Key, g.Sum(o => o.Price), g.Count()));

        summaries = ranking switch
        {
            ArticleRanking.Quantity => summaries.OrderByDescending(s => s.Quantity).ThenByDescending(s => s.Revenue),
            _ => summaries.OrderByDescending(s => s.Revenue).ThenByDescending(s => s.Quantity),
        };

        return summaries.Take(topN).ToList();
    }

    public IReadOnlyList<PreparationSummary> PreparationByArticle(IEnumerable<OrderLine> orders) =>
        orders
            .Where(o => o.PreparationSeconds.HasValue)
            .GroupBy(o => o.Article)
            .Select(g =>
            {
                var values = g.Select(o => o.PreparationSeconds!.Value).OrderBy(v => v).ToList();
                return new PreparationSummary(
                    g.Key,
                    AverageSeconds: values.Average(),
                    MedianSeconds: Median(values),
                    MaxSeconds: values[^1],
                    Count: values.Count);
            })
            .OrderByDescending(s => s.AverageSeconds)
            .ToList();

    public IReadOnlyList<UserSummary> RevenueByUser(IEnumerable<OrderLine> orders) =>
        orders
            .GroupBy(o => o.User)
            .Select(g => new UserSummary(g.Key, g.Sum(o => o.Price), g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

    public IReadOnlyList<PaymentMethodSummary> RevenueByPaymentMethod(IEnumerable<OrderLine> orders) =>
        orders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.PaymentMethod) ? "(ohne)" : o.PaymentMethod)
            .Select(g => new PaymentMethodSummary(g.Key, g.Sum(o => o.Price), g.Count()))
            .OrderByDescending(s => s.Revenue)
            .ToList();

    public IReadOnlyList<TableSummary> RevenueByTable(IEnumerable<OrderLine> orders) =>
        orders
            .GroupBy(o => o.Table)
            .Select(g => new TableSummary(g.Key, g.Sum(o => o.Price), g.Count()))
            .OrderByDescending(s => s.Revenue)
            .ToList();

    public IReadOnlyList<TimeBucketSummary> RevenueOverTime(IEnumerable<OrderLine> orders, TimeBucket bucket) =>
        orders
            .GroupBy(o => Truncate(o.OrderedAt, bucket))
            .Select(g => new TimeBucketSummary(g.Key, g.Sum(o => o.Price), g.Count()))
            .OrderBy(s => s.BucketStart)
            .ToList();

    public double CancellationRate(IEnumerable<OrderLine> orders)
    {
        var total = 0;
        var canceled = 0;
        foreach (var o in orders)
        {
            total++;
            if (o.IsCanceled)
            {
                canceled++;
            }
        }

        return total == 0 ? 0d : (double)canceled / total;
    }

    private static DateTime Truncate(DateTime value, TimeBucket bucket) => bucket switch
    {
        TimeBucket.Minute => new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind),
        TimeBucket.QuarterHour => new DateTime(
            value.Year, value.Month, value.Day, value.Hour, value.Minute - (value.Minute % 15), 0, value.Kind),
        _ => new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0, value.Kind),
    };

    /// <summary>Median einer aufsteigend sortierten, nicht leeren Werteliste.</summary>
    private static double Median(IReadOnlyList<int> sortedValues)
    {
        var n = sortedValues.Count;
        var mid = n / 2;
        return n % 2 == 1
            ? sortedValues[mid]
            : (sortedValues[mid - 1] + sortedValues[mid]) / 2d;
    }
}
