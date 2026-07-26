using System.Collections.ObjectModel;
using Biaschtln.Statistics.Models;
using Biaschtln.Statistics.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// Stellt die Filter-Sidebar dar. Baut die Auswahllisten (Kategorien, Artikel, Tische,
/// Benutzer, Zahlungsmethoden) dynamisch aus den geladenen Daten auf und meldet über
/// <see cref="FilterChanged"/>, wenn sich der Filter geändert hat.
/// </summary>
public partial class FilterViewModel : ObservableObject
{
    private readonly IOrderDataService _data;
    private readonly IPickupSettings _pickup;
    private bool _suppressChangeEvents;

    public FilterViewModel(IOrderDataService data, IPickupSettings pickup)
    {
        _data = data;
        _pickup = pickup;
        _data.OrdersChanged += (_, _) => RebuildOptions();
        RebuildOptions();
    }

    /// <summary>Sentinel-Eintrag der Datei-Auswahl: zeigt alle geladenen Dateien.</summary>
    public const string AllFiles = "(Alle Dateien)";

    public ObservableCollection<SelectableOption> Categories { get; } = [];
    public ObservableCollection<SelectableOption> Articles { get; } = [];
    public ObservableCollection<SelectableOption> Tables { get; } = [];
    public ObservableCollection<SelectableOption> Users { get; } = [];
    public ObservableCollection<SelectableOption> PaymentMethods { get; } = [];

    /// <summary>Auswahlliste der Dateien (Sentinel "Alle Dateien" + je geladene Datei).</summary>
    public ObservableCollection<string> Files { get; } = [AllFiles];

    /// <summary>Benutzernamen (für die Abholstation-Auswahl).</summary>
    public ObservableCollection<string> UserNames { get; } = [];

    /// <summary>
    /// Der als Abholstation geltende Benutzer (dessen Bestellungen keinem Tisch zählen).
    /// Schreibt in die geteilten <see cref="IPickupSettings"/>.
    /// </summary>
    public string PickupUser
    {
        get => _pickup.PickupUser;
        set
        {
            if (value is not null && value != _pickup.PickupUser)
            {
                _pickup.PickupUser = value;
                OnPropertyChanged();
                // Abholstation beeinflusst den Tisch-Filter → alle Ansichten neu berechnen.
                RaiseFilterChanged();
            }
        }
    }

    /// <summary>Ausgewählte Datei; <see cref="AllFiles"/> = alle Dateien anzeigen.</summary>
    [ObservableProperty]
    private string? _selectedFile = AllFiles;

    [ObservableProperty]
    private bool _includeCanceled;

    [ObservableProperty]
    private PaidFilter _paid = PaidFilter.All;

    /// <summary>Wird ausgelöst, wenn sich der Filter (Auswahl oder Eigenschaft) geändert hat.</summary>
    public event EventHandler? FilterChanged;

    /// <summary>Erstellt einen <see cref="OrderFilter"/> aus dem aktuellen Zustand.</summary>
    public OrderFilter BuildFilter()
    {
        var filter = new OrderFilter
        {
            File = SelectedFile == AllFiles ? null : SelectedFile,
            IncludeCanceled = IncludeCanceled,
            Paid = Paid,
            PickupUser = _pickup.PickupUser,
        };

        AddSelected(Categories, filter.Categories);
        AddSelected(Articles, filter.Articles);
        AddSelected(Tables, filter.Tables);
        AddSelected(Users, filter.Users);
        AddSelected(PaymentMethods, filter.PaymentMethods);
        return filter;
    }

    /// <summary>Setzt alle Filterkriterien auf den Ausgangszustand zurück.</summary>
    [RelayCommand]
    public void Reset()
    {
        _suppressChangeEvents = true;
        try
        {
            SelectedFile = AllFiles;
            IncludeCanceled = false;
            Paid = PaidFilter.All;
            foreach (var option in AllOptions())
            {
                option.IsSelected = false;
            }
        }
        finally
        {
            _suppressChangeEvents = false;
        }

        RaiseFilterChanged();
    }

    private void RebuildOptions()
    {
        _suppressChangeEvents = true;
        try
        {
            Sync(Categories, _data.Orders.Select(o => o.Category));
            Sync(Articles, _data.Orders.Select(o => o.Article));
            Sync(Tables, _data.Orders.Select(o => o.Table));
            Sync(Users, _data.Orders.Select(o => o.User));
            Sync(PaymentMethods, _data.Orders
                .Select(o => o.PaymentMethod)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Cast<string>());
            SyncNames(UserNames, _data.Orders.Select(o => o.User));
            SyncFiles();
        }
        finally
        {
            _suppressChangeEvents = false;
        }

        // Damit die ComboBox den Abholstation-Benutzer nach dem (Neu-)Laden wieder auswählt.
        OnPropertyChanged(nameof(PickupUser));
        RaiseFilterChanged();
    }

    /// <summary>
    /// Aktualisiert eine Optionsliste auf die in den Daten vorkommenden, eindeutigen,
    /// alphabetisch sortierten Werte und erhält die bisherige Auswahl wo möglich.
    /// </summary>
    private void Sync(ObservableCollection<SelectableOption> target, IEnumerable<string> values)
    {
        var selected = target.Where(o => o.IsSelected).Select(o => o.Name).ToHashSet();
        var distinct = values.Distinct().OrderBy(v => v, StringComparer.CurrentCulture).ToList();

        target.Clear();
        foreach (var value in distinct)
        {
            target.Add(new SelectableOption(value, RaiseFilterChanged)
            {
                IsSelected = selected.Contains(value),
            });
        }
    }

    /// <summary>
    /// Aktualisiert die Datei-Auswahl auf die in den Daten vorkommenden Quelldateien
    /// (Sentinel "Alle Dateien" bleibt erster Eintrag) und erhält die bisherige Auswahl,
    /// falls die Datei noch existiert — sonst zurück auf "Alle Dateien".
    /// </summary>
    private void SyncFiles()
    {
        var previous = SelectedFile;

        var distinct = _data.Orders
            .Select(o => o.SourceFile)
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct()
            .OrderBy(f => f, StringComparer.CurrentCulture)
            .ToList();

        Files.Clear();
        Files.Add(AllFiles);
        foreach (var file in distinct)
        {
            Files.Add(file);
        }

        SelectedFile = previous is not null && Files.Contains(previous) ? previous : AllFiles;
    }

    /// <summary>Aktualisiert eine String-Liste auf die eindeutigen, sortierten Werte.</summary>
    private static void SyncNames(ObservableCollection<string> target, IEnumerable<string> values)
    {
        var distinct = values.Distinct().OrderBy(v => v, StringComparer.CurrentCulture).ToList();
        target.Clear();
        foreach (var value in distinct)
        {
            target.Add(value);
        }
    }

    private IEnumerable<SelectableOption> AllOptions() =>
        Categories.Concat(Articles).Concat(Tables).Concat(Users).Concat(PaymentMethods);

    private static void AddSelected(IEnumerable<SelectableOption> options, ISet<string> target)
    {
        foreach (var option in options.Where(o => o.IsSelected))
        {
            target.Add(option.Name);
        }
    }

    partial void OnSelectedFileChanged(string? value) => RaiseFilterChanged();

    partial void OnIncludeCanceledChanged(bool value) => RaiseFilterChanged();

    partial void OnPaidChanged(PaidFilter value) => RaiseFilterChanged();

    private void RaiseFilterChanged()
    {
        if (!_suppressChangeEvents)
        {
            FilterChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
