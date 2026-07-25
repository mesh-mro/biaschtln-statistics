using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// Basis für Diagramm-ViewModels, die auf der gefilterten Bestellmenge arbeiten. Kümmert
/// sich um das Abonnieren von Daten- und Filteränderungen; die abgeleitete Klasse baut in
/// <see cref="Refresh"/> ihre Diagrammdaten neu auf. Die abgeleitete Klasse muss
/// <see cref="Refresh"/> am Ende ihres Konstruktors einmalig selbst aufrufen (nicht die
/// Basis, um keinen virtuellen Aufruf vor voller Initialisierung auszulösen).
/// </summary>
public abstract class FilteredChartViewModel : ObservableObject
{
    private readonly IOrderDataService _data;
    private readonly IOrderFilterService _filterService;
    private readonly FilterViewModel _filter;

    protected FilteredChartViewModel(
        IOrderDataService data,
        IOrderFilterService filterService,
        FilterViewModel filter,
        ICsvExporter csvExporter)
    {
        _data = data;
        _filterService = filterService;
        _filter = filter;
        CsvExporter = csvExporter;
        _data.OrdersChanged += (_, _) => Refresh();
        _filter.FilterChanged += (_, _) => Refresh();
    }

    /// <summary>CSV-Export für die Aggregat-Tabellen der abgeleiteten Seite.</summary>
    protected ICsvExporter CsvExporter { get; }

    /// <summary>Alle geladenen Positionen, ungefiltert (z. B. für stabile Farbzuordnungen).</summary>
    protected IReadOnlyList<OrderLine> AllOrders => _data.Orders;

    /// <summary>
    /// Die aktuell gemäß Filter-Sidebar gefilterten Positionen. Über <paramref name="adjust"/>
    /// lässt sich der Filter vor dem Anwenden anpassen (z. B. Stornos einbeziehen für die
    /// Stornoquote), ohne den Sidebar-Zustand zu verändern.
    /// </summary>
    protected IReadOnlyList<OrderLine> FilteredOrders(Action<OrderFilter>? adjust = null)
    {
        var filter = _filter.BuildFilter();
        adjust?.Invoke(filter);
        return _filterService.Apply(_data.Orders, filter).ToList();
    }

    /// <summary>Baut die Diagrammdaten aus der aktuellen gefilterten Menge neu auf.</summary>
    protected abstract void Refresh();
}
