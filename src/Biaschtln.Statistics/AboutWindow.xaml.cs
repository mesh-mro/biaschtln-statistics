using System.Windows;
using Biaschtln.Statistics.ViewModels;

namespace Biaschtln.Statistics;

/// <summary>
/// Interaction logic for AboutWindow.xaml. Erhält sein ViewModel per DI.
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow(AboutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnOk(object sender, RoutedEventArgs e) => Close();
}
