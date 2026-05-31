using CommunityToolkit.Mvvm.ComponentModel;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>Eine auswählbare Filteroption (z. B. eine Kategorie oder ein Tisch).</summary>
public partial class SelectableOption : ObservableObject
{
    private readonly Action _onSelectionChanged;

    public SelectableOption(string name, Action onSelectionChanged)
    {
        Name = name;
        _onSelectionChanged = onSelectionChanged;
    }

    /// <summary>Anzeigename und Filterwert.</summary>
    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value) => _onSelectionChanged();
}
