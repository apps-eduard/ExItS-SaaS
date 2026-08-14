using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// One cart line held in memory. Amounts are a display preview for online checkout (server still
/// prices from the live catalog). Offline cash sync embeds these values as immutable snapshots.
/// When a selling unit is selected, <see cref="Quantity"/> is base inventory quantity and
/// <see cref="EnteredQuantity"/> is the cashier-entered pack/custom quantity.
/// </summary>
public sealed record SaleCartItem(
    Guid ProductId,
    string Name,
    string? Sku,
    string? Barcode,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal Quantity,
    string SellingMode = "PerItem",
    Guid? SellingUnitId = null,
    string? SellingUnitName = null,
    decimal? EnteredQuantity = null,
    decimal MultiplierToBase = 1m)
{
    public decimal LineTotal =>
        PosSaleOptions.RoundMoney(UnitPrice * (EnteredQuantity ?? Quantity));
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
        var index = _items.FindIndex(i => i.ProductId == productId && i.SellingUnitId is null);
        return index >= 0 ? _items[index].Quantity : 0m;
    }

    public decimal GetEnteredQuantity(Guid productId, Guid? sellingUnitId)
    {
        var index = _items.FindIndex(i => i.ProductId == productId && i.SellingUnitId == sellingUnitId);
        if (index < 0)
        {
            return 0m;
        }

        return _items[index].EnteredQuantity ?? _items[index].Quantity;
    }

    /// <summary>
    /// Adds a product, folding a repeat scan into the existing line by summing the quantity. The
    /// snapshot fields are refreshed from the product so a price edited between scans is reflected in
    /// the preview.
    /// </summary>
    public void Add(
        PosCatalogProductDto product,
        decimal quantity = 1m,
        PosCatalogProductUnitDto? sellingUnit = null)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (quantity <= 0m)
        {
            return;
        }

        var sellingMode = string.IsNullOrWhiteSpace(product.SellingMode) ? "PerItem" : product.SellingMode;
        if (sellingUnit is not null)
        {
            var multiplier = sellingUnit.MultiplierToBase <= 0m ? 1m : sellingUnit.MultiplierToBase;
            var entered = quantity;
            var baseQty = PosSaleOptions.RoundMoney(entered * multiplier); // keep preview precision
            // Prefer 3dp style quantity math without SaleMoney dependency here:
            baseQty = decimal.Round(entered * multiplier, 3, MidpointRounding.AwayFromZero);
            var unitPrice = sellingUnit.SellingPrice ?? product.SellingPrice;
            var index = _items.FindIndex(i =>
                i.ProductId == product.ProductId && i.SellingUnitId == sellingUnit.UnitId);
            var existingEntered = index >= 0
                ? ( _items[index].EnteredQuantity ?? _items[index].Quantity)
                : 0m;
            var nextEntered = existingEntered + entered;
            var nextBase = decimal.Round(nextEntered * multiplier, 3, MidpointRounding.AwayFromZero);
            var item = new SaleCartItem(
                product.ProductId,
                product.Name,
                product.Sku,
                product.Barcode,
                product.UnitOfMeasure,
                unitPrice,
                nextBase,
                sellingMode,
                sellingUnit.UnitId,
                sellingUnit.DisplayName,
                nextEntered,
                multiplier);
            if (index >= 0)
            {
                _items[index] = item;
            }
            else
            {
                _items.Add(item);
            }

            Changed?.Invoke();
            return;
        }

        var simpleIndex = _items.FindIndex(i => i.ProductId == product.ProductId && i.SellingUnitId is null);
        var existingQuantity = simpleIndex >= 0 ? _items[simpleIndex].Quantity : 0m;
        var simpleItem = new SaleCartItem(
            product.ProductId,
            product.Name,
            product.Sku,
            product.Barcode,
            product.UnitOfMeasure,
            product.SellingPrice,
            existingQuantity + quantity,
            sellingMode);

        if (simpleIndex >= 0)
        {
            _items[simpleIndex] = simpleItem;
        }
        else
        {
            _items.Add(simpleItem);
        }

        Changed?.Invoke();
    }

    public void SetQuantity(Guid productId, decimal quantity)
    {
        var index = _items.FindIndex(i => i.ProductId == productId && i.SellingUnitId is null);
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

    public void Remove(Guid productId, Guid? sellingUnitId)
    {
        if (_items.RemoveAll(i => i.ProductId == productId && i.SellingUnitId == sellingUnitId) > 0)
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
        _items.Count > 0
        && _items.All(i => PosSaleOptions.IsValidQuantity(
            i.EnteredQuantity ?? i.Quantity,
            i.UnitOfMeasure,
            i.SellingMode));

    /// <summary>
    /// Online checkout: ProductId + Quantity (+ optional selling unit).
    /// Offline cash sync: include immutable line snapshots (payload_version 2).
    /// </summary>
    public List<CheckoutSaleLineRequest> ToCheckoutLines(bool includePriceSnapshots = false) =>
        includePriceSnapshots
            ? _items.Select(i => new CheckoutSaleLineRequest(
                i.ProductId,
                i.Quantity,
                UnitPriceSnapshot: i.UnitPrice,
                UnitOfMeasure: i.UnitOfMeasure,
                SellingMode: i.SellingMode,
                LineTotal: i.LineTotal,
                NameSnapshot: i.Name,
                SkuSnapshot: i.Sku,
                BarcodeSnapshot: i.Barcode,
                SellingUnitId: i.SellingUnitId,
                EnteredQuantity: i.EnteredQuantity)).ToList()
            : _items.Select(i => new CheckoutSaleLineRequest(
                i.ProductId,
                i.Quantity,
                SellingUnitId: i.SellingUnitId,
                EnteredQuantity: i.EnteredQuantity)).ToList();

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
