using System.Windows;
using Biaschtln.Statistics.ViewModels;

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
}
