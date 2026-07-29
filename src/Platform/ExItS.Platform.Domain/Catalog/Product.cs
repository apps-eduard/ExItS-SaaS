using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>
/// Platform product catalog entry. Not operational settings, UI config, or product-local roles.
/// </summary>
public sealed class Product
{
    public ProductId Id { get; }
    public ProductCode Code { get; }
    public string DisplayName { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Product(
        ProductId id,
        ProductCode code,
        string displayName,
        ProductStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        Code = code;
        DisplayName = displayName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Product Create(ProductCode code, string displayName, DateTimeOffset utcNow, ProductId? id = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        DomainTime.EnsureUtc(utcNow);
        return new Product(
            id ?? ProductId.New(),
            code,
            DomainTime.NormalizeDisplayName(displayName),
            ProductStatus.Active,
            utcNow,
            utcNow);
    }

    internal static Product Rehydrate(
        ProductId id,
        ProductCode code,
        string displayName,
        ProductStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, code, displayName, status, createdAtUtc, updatedAtUtc);

    public void Rename(string displayName, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureNotRetired();
        DisplayName = DomainTime.NormalizeDisplayName(displayName);
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTimeOffset utcNow) => TransitionTo(ProductStatus.Active, utcNow);

    public void Deactivate(DateTimeOffset utcNow) => TransitionTo(ProductStatus.Inactive, utcNow);

    public void Retire(DateTimeOffset utcNow) => TransitionTo(ProductStatus.Retired, utcNow);

    private void TransitionTo(ProductStatus target, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            ProductStatus.Active => target is ProductStatus.Inactive or ProductStatus.Retired,
            ProductStatus.Inactive => target is ProductStatus.Active or ProductStatus.Retired,
            ProductStatus.Retired => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductStatusTransition,
                $"Cannot transition Product from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureNotRetired()
    {
        if (Status == ProductStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductStatusTransition,
                "A retired Product cannot be updated.");
        }
    }
}
