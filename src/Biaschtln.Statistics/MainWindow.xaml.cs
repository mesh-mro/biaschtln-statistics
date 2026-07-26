using System.Windows;
using Biaschtln.Statistics.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Biaschtln.Statistics;

/// <summary>
/// Interaction logic for MainWindow.xaml. Erhaelt sein ViewModel per DI.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnShowAbout(object sender, RoutedEventArgs e)
    {
        var dialog = App.Current.Services.GetRequiredService<AboutWindow>();
        dialog.Owner = this;
        dialog.ShowDialog();
    }

    private void OnExit(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
