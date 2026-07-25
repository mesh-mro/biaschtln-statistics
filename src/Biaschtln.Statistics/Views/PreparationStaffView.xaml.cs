using System.Windows;
using System.Windows.Controls;

namespace Biaschtln.Statistics.Views;

/// <summary>
/// Ansicht "Zubereitung &amp; Personal" (WP7). Erhält ihr ViewModel über den DataContext
/// (im MainWindow an <c>PreparationStaff</c> gebunden).
/// </summary>
public partial class PreparationStaffView : UserControl
{
    public PreparationStaffView()
    {
        InitializeComponent();
    }

    private void OnExportPng(object sender, RoutedEventArgs e) =>
        ChartExport.SaveElementAsPng(ExportRoot, "zubereitung-personal");
}
