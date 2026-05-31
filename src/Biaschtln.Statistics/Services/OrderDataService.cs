using Biaschtln.Statistics.Models;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="IOrderDataService" />
public sealed class OrderDataService : IOrderDataService
{
    private readonly ICsvOrderImporter _importer;
    private IReadOnlyList<OrderLine> _orders = [];

    public OrderDataService(ICsvOrderImporter importer)
    {
        _importer = importer;
    }

    public IReadOnlyList<OrderLine> Orders => _orders;

    public event EventHandler? OrdersChanged;

    public ImportResult LoadFiles(IEnumerable<string> paths)
    {
        var result = _importer.LoadFiles(paths);
        _orders = result.Orders;
        OrdersChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    public void Clear()
    {
        if (_orders.Count == 0)
        {
            return;
        }

        _orders = [];
        OrdersChanged?.Invoke(this, EventArgs.Empty);
    }
}
