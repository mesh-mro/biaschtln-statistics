using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <summary>Wendet einen <see cref="OrderFilter"/> auf eine Menge von Bestellpositionen an.</summary>
public interface IOrderFilterService
{
    /// <summary>Gibt die Positionen zurück, die alle Kriterien des Filters erfüllen.</summary>
    IEnumerable<OrderLine> Apply(IEnumerable<OrderLine> orders, OrderFilter filter);
}
