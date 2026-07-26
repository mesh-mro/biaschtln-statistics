using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="IOrderFilterService" />
public sealed class OrderFilterService : IOrderFilterService
{
    public IEnumerable<OrderLine> Apply(IEnumerable<OrderLine> orders, OrderFilter filter) =>
        orders.Where(o => Matches(o, filter));

    private static bool Matches(OrderLine o, OrderFilter f)
    {
        if (!f.IncludeCanceled && o.IsCanceled)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(f.File) &&
            !string.Equals(o.SourceFile, f.File, StringComparison.Ordinal))
        {
            return false;
        }

        if (f.Categories.Count > 0 && !f.Categories.Contains(o.Category))
        {
            return false;
        }

        if (f.Articles.Count > 0 && !f.Articles.Contains(o.Article))
        {
            return false;
        }

        // Abhol-Bestellungen gehören keinem Tisch an → erfüllen keinen Tisch-Filter.
        if (f.Tables.Count > 0 && (IsPickup(o, f.PickupUser) || !f.Tables.Contains(o.Table)))
        {
            return false;
        }

        if (f.Users.Count > 0 && !f.Users.Contains(o.User))
        {
            return false;
        }

        if (f.PaymentMethods.Count > 0 && (o.PaymentMethod is null || !f.PaymentMethods.Contains(o.PaymentMethod)))
        {
            return false;
        }

        return f.Paid switch
        {
            PaidFilter.OnlyPaid => o.IsPaid,
            PaidFilter.OnlyUnpaid => !o.IsPaid,
            _ => true,
        };
    }

    private static bool IsPickup(OrderLine o, string? pickupUser) =>
        !string.IsNullOrEmpty(pickupUser) &&
        string.Equals(o.User, pickupUser, StringComparison.OrdinalIgnoreCase);
}
