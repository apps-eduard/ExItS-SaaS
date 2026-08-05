using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// One cart line held in memory. Amounts are a display preview only — the server recomputes every
/// price and total from the live catalog at checkout.
/// </summary>
public sealed record SaleCartItem(
    Guid ProductId,
    string Name,
    string? Sku,
    string? Barcode,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal Quantity)
{
    public decimal LineTotal => PosSaleOptions.RoundMoney(UnitPrice * Quantity);
}

/// <summary>
/// In-memory checkout cart for the current signed-in session. The cart is never persisted: it is not
/// written to the local SQLite store, never queued for offline sync, and is dropped on sign-out or
/// organization switch so one organization's cart can never leak into another.
/// </summary>
public sealed class SaleCartService : IDisposable
{
    private readonly ICurrentUserContext _currentUser;
    private readonly List<SaleCartItem> _items = [];
    private Guid? _organizationId;
    private bool _disposed;

    public SaleCartService(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
        _organizationId = currentUser.Session?.OrganizationId;
        _currentUser.Changed += OnSessionChangedAsync;
    }

    public event Action? Changed;

    public IReadOnlyList<SaleCartItem> Items => _items;

    public bool IsEmpty => _items.Count == 0;

    public int LineCount => _items.Count;

    public decimal Subtotal => PosSaleOptions.RoundMoney(_items.Sum(i => i.LineTotal));

    public decimal GetQuantity(Guid productId)
    {
        var index = _items.FindIndex(i => i.ProductId == productId);
        return index >= 0 ? _items[index].Quantity : 0m;
    }

    /// <summary>
    /// Adds a product, folding a repeat scan into the existing line by summing the quantity. The
    /// snapshot fields are refreshed from the product so a price edited between scans is reflected in
    /// the preview.
    /// </summary>
    public void Add(PosCatalogProductDto product, decimal quantity = 1m)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (quantity <= 0m)
        {
            return;
        }

        var index = _items.FindIndex(i => i.ProductId == product.ProductId);
        var existingQuantity = index >= 0 ? _items[index].Quantity : 0m;
        var item = new SaleCartItem(
            product.ProductId,
            product.Name,
            product.Sku,
            product.Barcode,
            product.UnitOfMeasure,
            product.SellingPrice,
            existingQuantity + quantity);

        if (index >= 0)
        {
            _items[index] = item;
        }
        else
        {
            _items.Add(item);
        }

        Changed?.Invoke();
    }

    public void SetQuantity(Guid productId, decimal quantity)
    {
        var index = _items.FindIndex(i => i.ProductId == productId);
        if (index < 0)
        {
            return;
        }

        if (quantity <= 0m)
        {
            _items.RemoveAt(index);
        }
        else
        {
            _items[index] = _items[index] with { Quantity = quantity };
        }

        Changed?.Invoke();
    }

    public void Remove(Guid productId)
    {
        if (_items.RemoveAll(i => i.ProductId == productId) > 0)
        {
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        Changed?.Invoke();
    }

    /// <summary>True when every line quantity satisfies the unit-of-measure precision rule.</summary>
    public bool IsCheckoutReady =>
        _items.Count > 0 && _items.All(i => PosSaleOptions.IsValidQuantity(i.Quantity, i.UnitOfMeasure));

    public List<CheckoutSaleLineRequest> ToCheckoutLines() =>
        _items.Select(i => new CheckoutSaleLineRequest(i.ProductId, i.Quantity)).ToList();

    private Task OnSessionChangedAsync()
    {
        var organizationId = _currentUser.Session?.OrganizationId;
        if (organizationId != _organizationId)
        {
            _organizationId = organizationId;
            Clear();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _currentUser.Changed -= OnSessionChangedAsync;
    }
}
