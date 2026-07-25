using System.Windows;
using System.Windows.Controls;

namespace Biaschtln.Statistics.Views;

/// <summary>
/// Ansicht "Weitere Auswertungen" (WP8). Erhält ihr ViewModel über den DataContext
/// (im MainWindow an <c>Analytics</c> gebunden).
/// </summary>
public partial class AnalyticsView : UserControl
{
    public AnalyticsView()
    {
        InitializeComponent();
    }

    private void OnExportPng(object sender, RoutedEventArgs e) =>
        ChartExport.SaveElementAsPng(ExportRoot, "weitere-auswertungen");
}
