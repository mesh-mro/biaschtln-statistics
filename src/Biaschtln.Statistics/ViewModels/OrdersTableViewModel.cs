using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// ViewModel der Datentabelle: zeigt jede einzelne (gefilterte) Bestellposition mit den
/// wesentlichen Feldern und bietet CSV-Export der angezeigten Liste. Reagiert live auf
/// Datei- und Filteränderungen.
/// </summary>
public sealed partial class OrdersTableViewModel : FilteredChartViewModel
{
    public OrdersTableViewModel(
        IOrderDataService data,
        IOrderFilterService filterService,
        FilterViewModel filter,
        ICsvExporter csvExporter)
        : base(data, filterService, filter, csvExporter)
    {
        Refresh();
    }

    /// <summary>Die aktuell gefilterten Positionen als Tabellenzeilen.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowCount))]
    [NotifyPropertyChangedFor(nameof(HasData))]
    private IReadOnlyList<PositionRow> _rows = [];

    /// <summary>Anzahl der angezeigten Positionen.</summary>
    public int RowCount => Rows.Count;

    /// <summary>False, wenn die gefilterte Menge leer ist (steuert die Leer-Anzeige).</summary>
    public bool HasData => Rows.Count > 0;

    /// <summary>Exportiert die aktuell angezeigten Positionen als CSV.</summary>
    [RelayCommand]
    private void ExportCsv() => CsvExporter.ExportCsv(Rows, "positionen");

    protected override void Refresh() =>
        Rows = FilteredOrders().Select(PositionRow.From).ToList();
}
