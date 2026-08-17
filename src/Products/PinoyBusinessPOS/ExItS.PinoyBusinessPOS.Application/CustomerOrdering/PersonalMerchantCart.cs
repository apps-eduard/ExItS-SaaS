namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed record PersonalMerchantCartLine(
    Guid ProductId,
    string Name,
    string? Sku,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal Quantity)
{
    public decimal LineTotal => Math.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);
}

/// <summary>
/// In-memory Personal storefront cart for one linked merchant at a time.
/// Cleared after successful place or when leaving/switching merchants.
/// </summary>
public sealed class PersonalMerchantCart
{
    private readonly Dictionary<Guid, PersonalMerchantCartLine> _lines = new();
    private Guid? _sellerOrganizationId;
    private string? _organizationDisplayName;

    public Guid? SellerOrganizationId => _sellerOrganizationId;
    public string? OrganizationDisplayName => _organizationDisplayName;
    public IReadOnlyList<PersonalMerchantCartLine> Lines =>
        _lines.Values.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase).ToList();
    public int LineCount => _lines.Count;
    public decimal ItemCount => _lines.Values.Sum(l => l.Quantity);
    public decimal MerchandiseSubtotal =>
        Math.Round(_lines.Values.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);

    public event Action? Changed;

    public void EnsureMerchant(Guid sellerOrganizationId, string? organizationDisplayName)
    {
        if (_sellerOrganizationId == sellerOrganizationId)
        {
            if (!string.IsNullOrWhiteSpace(organizationDisplayName))
            {
                _organizationDisplayName = organizationDisplayName.Trim();
            }

            return;
        }

        _lines.Clear();
        _sellerOrganizationId = sellerOrganizationId;
        _organizationDisplayName = string.IsNullOrWhiteSpace(organizationDisplayName)
            ? null
            : organizationDisplayName.Trim();
        Changed?.Invoke();
    }

    public decimal GetQuantity(Guid productId) =>
        _lines.TryGetValue(productId, out var line) ? line.Quantity : 0m;

    public bool CanIncrement(CustomerStorefrontProductDto product) =>
        CustomerStorefrontAvailability.CanIncrement(product, GetQuantity(product.ProductId));

    public void Increment(CustomerStorefrontProductDto product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (!CustomerStorefrontAvailability.CanIncrement(product, GetQuantity(product.ProductId)))
        {
            return;
        }

        if (_lines.TryGetValue(product.ProductId, out var existing))
        {
            _lines[product.ProductId] = existing with { Quantity = existing.Quantity + 1m };
        }
        else
        {
            _lines[product.ProductId] = new PersonalMerchantCartLine(
                product.ProductId,
                product.Name,
                product.Sku,
                product.UnitOfMeasure,
                product.UnitPrice,
                1m);
        }

        Changed?.Invoke();
    }

    public void Decrement(Guid productId)
    {
        if (!_lines.TryGetValue(productId, out var existing))
        {
            return;
        }

        var next = existing.Quantity - 1m;
        if (next <= 0m)
        {
            _lines.Remove(productId);
        }
        else
        {
            _lines[productId] = existing with { Quantity = next };
        }

        Changed?.Invoke();
    }

    public void Remove(Guid productId)
    {
        if (_lines.Remove(productId))
        {
            Changed?.Invoke();
        }
    }

    public void Clear()
    {
        if (_lines.Count == 0 && _sellerOrganizationId is null)
        {
            return;
        }

        _lines.Clear();
        _sellerOrganizationId = null;
        _organizationDisplayName = null;
        Changed?.Invoke();
    }

    public void ClearLinesOnly()
    {
        if (_lines.Count == 0)
        {
            return;
        }

        _lines.Clear();
        Changed?.Invoke();
    }
}
