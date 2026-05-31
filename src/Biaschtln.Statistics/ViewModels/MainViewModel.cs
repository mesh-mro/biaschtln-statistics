using CommunityToolkit.Mvvm.ComponentModel;

namespace Biaschtln.Statistics.ViewModels;

/// <summary>
/// Wurzel-ViewModel des Hauptfensters. Wird in WP5 um Datei-Laden, Filter-Sidebar
/// und Navigation erweitert; dient in WP1 als DI-/MVVM-Geruest.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Biaschtln-Statistik";
}
