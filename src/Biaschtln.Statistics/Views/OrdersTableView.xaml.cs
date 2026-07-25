using System.Windows.Controls;

namespace Biaschtln.Statistics.Views;

/// <summary>
/// Ansicht der Datentabelle (jede gefilterte Position). Erhält ihr ViewModel über den
/// DataContext (im MainWindow an <c>OrdersTable</c> gebunden).
/// </summary>
public partial class OrdersTableView : UserControl
{
    public OrdersTableView()
    {
        InitializeComponent();
    }
}
