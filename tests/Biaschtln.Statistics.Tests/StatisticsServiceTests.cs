using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;

namespace Biaschtln.Statistics.Tests;

public sealed class StatisticsServiceTests
{
    private readonly StatisticsService _stats = new();

    /// <summary>
    /// Deterministischer Mini-Datensatz:
    /// 4x Bier (Alk, 4.5, K1, Bestellung 1), 3x Schnitzel (Essen, 8, K2, Dauer 100/200/300,
    /// Bestellungen 2/2/3), 1x Cola (Anti, 3, K1, Bestellung 3, STORNO).
    /// </summary>
    private static List<OrderLine> Sample()
    {
        var list = new List<OrderLine>();
        for (var i = 0; i < 4; i++)
        {
            list.Add(Line("Bier 0,5", "Alk", 4.5m, "K1", orderId: 1));
        }

        list.Add(Line("Schnitzel", "Essen", 8m, "K2", orderId: 2, prep: 100));
        list.Add(Line("Schnitzel", "Essen", 8m, "K2", orderId: 2, prep: 200));
        list.Add(Line("Schnitzel", "Essen", 8m, "K2", orderId: 3, prep: 300));
        list.Add(Line("Cola 0,33", "Anti", 3m, "K1", orderId: 3, canceled: true));
        return list;
    }

    private static OrderLine Line(string article, string category, decimal price, string user,
        int orderId, int? prep = null, bool canceled = false) =>
        new()
        {
            Article = article,
            Category = category,
            Price = price,
            User = user,
            OrderId = orderId,
            PreparationSeconds = prep,
            Status = canceled ? "ORDER: CANCELED - Bestellung STORNO" : "ORDER: CREATED - Bestellung erfasst",
        };

    [Fact]
    public void Totals_AreComputedOverAllLines()
    {
        var orders = Sample();

        Assert.Equal(45m, _stats.TotalRevenue(orders));
        Assert.Equal(8, _stats.TotalCount(orders));
        Assert.Equal(3, _stats.DistinctOrderCount(orders));
    }

    [Fact]
    public void RevenueByCategory_GroupedAndSortedByRevenueDescending()
    {
        var result = _stats.RevenueByCategory(Sample());

        Assert.Collection(result,
            c => { Assert.Equal("Essen", c.Category); Assert.Equal(24m, c.Revenue); Assert.Equal(3, c.Count); },
            c => { Assert.Equal("Alk", c.Category); Assert.Equal(18m, c.Revenue); Assert.Equal(4, c.Count); },
            c => { Assert.Equal("Anti", c.Category); Assert.Equal(3m, c.Revenue); Assert.Equal(1, c.Count); });
    }

    [Fact]
    public void TopArticles_ByRevenue_RanksByRevenue()
    {
        var result = _stats.TopArticles(Sample(), topN: 2, ArticleRanking.Revenue);

        Assert.Equal(2, result.Count);
        Assert.Equal("Schnitzel", result[0].Article);
        Assert.Equal(24m, result[0].Revenue);
        Assert.Equal("Bier 0,5", result[1].Article);
    }

    [Fact]
    public void TopArticles_ByQuantity_RanksByQuantity()
    {
        var result = _stats.TopArticles(Sample(), topN: 2, ArticleRanking.Quantity);

        Assert.Equal("Bier 0,5", result[0].Article);
        Assert.Equal(4, result[0].Quantity);
        Assert.Equal("Schnitzel", result[1].Article);
    }

    [Fact]
    public void PreparationByArticle_OnlyLinesWithDuration()
    {
        var result = _stats.PreparationByArticle(Sample());

        var schnitzel = Assert.Single(result);
        Assert.Equal("Schnitzel", schnitzel.Article);
        Assert.Equal(200d, schnitzel.AverageSeconds);
        Assert.Equal(200d, schnitzel.MedianSeconds);
        Assert.Equal(300, schnitzel.MaxSeconds);
        Assert.Equal(3, schnitzel.Count);
    }

    [Fact]
    public void RevenueByUser_SortedByCountDescending()
    {
        var result = _stats.RevenueByUser(Sample());

        Assert.Collection(result,
            u => { Assert.Equal("K1", u.User); Assert.Equal(5, u.Count); Assert.Equal(21m, u.Revenue); },
            u => { Assert.Equal("K2", u.User); Assert.Equal(3, u.Count); Assert.Equal(24m, u.Revenue); });
    }

    [Fact]
    public void CancellationRate_IsFractionOfCanceledLines()
    {
        Assert.Equal(0.125d, _stats.CancellationRate(Sample()));
        Assert.Equal(0d, _stats.CancellationRate([]));
    }

    [Fact]
    public void RevenueOverTime_BucketsByHour()
    {
        var orders = new List<OrderLine>
        {
            new() { Price = 5m, OrderedAt = new DateTime(2026, 5, 9, 18, 10, 0) },
            new() { Price = 7m, OrderedAt = new DateTime(2026, 5, 9, 18, 55, 0) },
            new() { Price = 9m, OrderedAt = new DateTime(2026, 5, 9, 19, 5, 0) },
        };

        var result = _stats.RevenueOverTime(orders, TimeBucket.Hour);

        Assert.Collection(result,
            b => { Assert.Equal(new DateTime(2026, 5, 9, 18, 0, 0), b.BucketStart); Assert.Equal(12m, b.Revenue); Assert.Equal(2, b.Count); },
            b => { Assert.Equal(new DateTime(2026, 5, 9, 19, 0, 0), b.BucketStart); Assert.Equal(9m, b.Revenue); Assert.Equal(1, b.Count); });
    }
}
