using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;

namespace Biaschtln.Statistics.Tests;

public sealed class OrderFilterServiceTests
{
    private readonly OrderFilterService _filter = new();

    // o1 Alk/Bier/A1/K1/cash/paid/09.18:00; o2 Essen/Schnitzel/A1/K2/card/paid/09.19:00;
    // o3 Anti/Cola/B5/K1/(null)/unpaid/10.12:00 STORNO; o4 Alk/Radler/B5/K3/voucher/paid/11.10:00.
    private static List<OrderLine> Sample() =>
    [
        Line(1, "Alk", "Bier 0,5", "A1", "K1", "cash", paid: true, new DateTime(2026, 5, 9, 18, 0, 0)),
        Line(2, "Essen", "Schnitzel", "A1", "K2", "card", paid: true, new DateTime(2026, 5, 9, 19, 0, 0)),
        Line(3, "Anti", "Cola 0,33", "B5", "K1", null, paid: false, new DateTime(2026, 5, 10, 12, 0, 0), canceled: true),
        Line(4, "Alk", "Radler 0,5", "B5", "K3", "voucher", paid: true, new DateTime(2026, 5, 11, 10, 0, 0)),
    ];

    private static OrderLine Line(int id, string category, string article, string table, string user,
        string? payment, bool paid, DateTime orderedAt, bool canceled = false, string? sourceFile = null) =>
        new()
        {
            OrderLineId = id,
            Category = category,
            Article = article,
            Table = table,
            User = user,
            PaymentMethod = payment,
            IsPaid = paid,
            OrderedAt = orderedAt,
            SourceFile = sourceFile ?? string.Empty,
            Status = canceled ? "ORDER: CANCELED - Bestellung STORNO" : "ORDER: CREATED - Bestellung erfasst",
        };

    private int[] Ids(OrderFilter filter) =>
        _filter.Apply(Sample(), filter).Select(o => o.OrderLineId).OrderBy(i => i).ToArray();

    [Fact]
    public void EmptyFilter_ExcludesCanceledByDefault()
    {
        Assert.Equal([1, 2, 4], Ids(new OrderFilter()));
    }

    [Fact]
    public void IncludeCanceled_ReturnsAll()
    {
        Assert.Equal([1, 2, 3, 4], Ids(new OrderFilter { IncludeCanceled = true }));
    }

    [Fact]
    public void CategoryFilter_KeepsOnlySelectedCategories()
    {
        var filter = new OrderFilter { Categories = { "Alk" } };
        Assert.Equal([1, 4], Ids(filter));
    }

    [Fact]
    public void ArticleFilter_KeepsOnlySelectedArticles()
    {
        var filter = new OrderFilter { Articles = { "Schnitzel" } };
        Assert.Equal([2], Ids(filter));
    }

    [Fact]
    public void TableFilter_RespectsCanceledToggle()
    {
        Assert.Equal([4], Ids(new OrderFilter { Tables = { "B5" } }));
        Assert.Equal([3, 4], Ids(new OrderFilter { Tables = { "B5" }, IncludeCanceled = true }));
    }

    [Fact]
    public void UserFilter_KeepsOnlySelectedUsers()
    {
        Assert.Equal([1], Ids(new OrderFilter { Users = { "K1" } }));
    }

    [Fact]
    public void PaymentMethodFilter_ExcludesNullMethodOrders()
    {
        var filter = new OrderFilter { PaymentMethods = { "cash", "card" } };
        Assert.Equal([1, 2], Ids(filter));
    }

    [Fact]
    public void FileFilter_MatchesOnlySelectedFile_AndEmptyMeansAll()
    {
        var orders = new List<OrderLine>
        {
            Line(1, "Alk", "Bier 0,5", "A1", "K1", "cash", paid: true, new DateTime(2026, 5, 9, 18, 0, 0), sourceFile: "tag1.csv"),
            Line(2, "Essen", "Schnitzel", "A1", "K2", "card", paid: true, new DateTime(2026, 5, 10, 19, 0, 0), sourceFile: "tag2.csv"),
        };

        int[] Apply(OrderFilter f) =>
            _filter.Apply(orders, f).Select(o => o.OrderLineId).OrderBy(i => i).ToArray();

        // Leerer Datei-Filter = alle Dateien.
        Assert.Equal([1, 2], Apply(new OrderFilter()));

        // Genau eine Datei.
        Assert.Equal([1], Apply(new OrderFilter { File = "tag1.csv" }));
        Assert.Equal([2], Apply(new OrderFilter { File = "tag2.csv" }));
    }

    [Fact]
    public void PaidFilter_OnlyPaidAndOnlyUnpaid()
    {
        Assert.Equal([1, 2, 4], Ids(new OrderFilter { Paid = PaidFilter.OnlyPaid }));
        Assert.Equal([3], Ids(new OrderFilter { Paid = PaidFilter.OnlyUnpaid, IncludeCanceled = true }));
    }

    [Fact]
    public void CombinedCriteria_AreAndedTogether()
    {
        var filter = new OrderFilter { Categories = { "Alk" }, Paid = PaidFilter.OnlyPaid };
        Assert.Equal([1, 4], Ids(filter));
    }

    [Fact]
    public void TableFilter_ExcludesPickupUserOrders()
    {
        // Zwei Positionen am selben Tisch A1: eine von Kellner, eine von Abholstation.
        var orders = new List<OrderLine>
        {
            Line(1, "Essen", "Schnitzel", "A1", "Kellner 1", "cash", paid: true, new DateTime(2026, 5, 9, 18, 0, 0)),
            Line(2, "Essen", "Pommes", "A1", "Abholstation", "cash", paid: true, new DateTime(2026, 5, 9, 18, 30, 0)),
        };

        // Tisch-Filter A1 + Abholstation → nur die Kellner-Position (Abholung zählt keinem Tisch).
        var atTable = new OrderFilter { Tables = { "A1" }, PickupUser = "Abholstation" };
        Assert.Equal([1], _filter.Apply(orders, atTable).Select(o => o.OrderLineId).OrderBy(i => i).ToArray());

        // Ohne Tisch-Filter bleibt die Abholung erhalten (echter Umsatz, nur ohne Tisch).
        var noTable = new OrderFilter { PickupUser = "Abholstation" };
        Assert.Equal([1, 2], _filter.Apply(orders, noTable).Select(o => o.OrderLineId).OrderBy(i => i).ToArray());
    }
}
