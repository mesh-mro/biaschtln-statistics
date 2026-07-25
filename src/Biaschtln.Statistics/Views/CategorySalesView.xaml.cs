using System.Windows;
using System.Windows.Controls;

namespace Biaschtln.Statistics.Views;

/// <summary>
/// Ansicht "Umsatz nach Kategorie / Artikel" (WP6). Erhält ihr ViewModel über den
/// DataContext (im MainWindow an <c>CategorySales</c> gebunden).
/// </summary>
public partial class CategorySalesView : UserControl
{
    public CategorySalesView()
    {
        InitializeComponent();
    }

    private void OnExportPng(object sender, RoutedEventArgs e) =>
        ChartExport.SaveElementAsPng(ExportRoot, "umsatz-kategorie-artikel");
}
