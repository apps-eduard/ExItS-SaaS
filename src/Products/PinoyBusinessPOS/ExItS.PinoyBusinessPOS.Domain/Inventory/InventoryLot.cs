using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Quantity of stock for one product at one location with one expiration date.
/// Expiration belongs to this lot, not to <see cref="Catalog.CatalogProduct"/>.
/// </summary>
public sealed class InventoryLot
{
    public const int LotNumberMaxLength = 64;
    public const int DefaultWarningDays = 7;
    public const int MinWarningDays = 1;
    public const int MaxWarningDays = 365;
    public const string ExpiredWriteOffReason = "Expired";

    public InventoryLotId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public PosBranchId? BranchId { get; }
    public string? LotNumber { get; }
    public string NormalizedLotNumber { get; }
    public DateOnly ExpirationDate { get; }
    public decimal QuantityOnHand { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private InventoryLot(
        InventoryLotId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        string? lotNumber,
        string normalizedLotNumber,
        DateOnly expirationDate,
        decimal quantityOnHand,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductId = productId;
        BranchId = branchId;
        LotNumber = lotNumber;
        NormalizedLotNumber = normalizedLotNumber;
        ExpirationDate = expirationDate;
        QuantityOnHand = quantityOnHand;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static InventoryLot Create(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        DateOnly expirationDate,
        decimal quantityOnHand,
        DateTimeOffset utcNow,
        PosBranchId? branchId = null,
        string? lotNumber = null,
        InventoryLotId? id = null)
    {
        EnsureUtc(utcNow);
        if (quantityOnHand < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryQuantity,
                "Lot quantity cannot be negative.");
        }

        var (display, normalized) = NormalizeLotNumber(lotNumber);
        return new InventoryLot(
            id ?? InventoryLotId.New(),
            organizationId,
            productId,
            branchId,
            display,
            normalized,
            expirationDate,
            quantityOnHand,
            utcNow,
            utcNow);
    }

    public static InventoryLot Rehydrate(
        InventoryLotId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        string? lotNumber,
        string normalizedLotNumber,
        DateOnly expirationDate,
        decimal quantityOnHand,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            productId,
            branchId,
            lotNumber,
            normalizedLotNumber,
            expirationDate,
            quantityOnHand,
            createdAtUtc,
            updatedAtUtc);

    public void Apply(decimal signedQuantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (signedQuantity == 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryQuantity,
                "Lot quantity effect cannot be zero.");
        }

        var next = QuantityOnHand + signedQuantity;
        if (next < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient lot quantity for this movement.");
        }

        QuantityOnHand = next;
        UpdatedAtUtc = utcNow;
    }

    public bool IsExpired(DateOnly today) => ExpirationDate < today;

    public bool IsSellable(DateOnly today) => QuantityOnHand > 0m && !IsExpired(today);

    public bool IsNearExpiry(DateOnly today, int warningDays)
    {
        if (IsExpired(today) || QuantityOnHand <= 0m)
        {
            return false;
        }

        var window = today.AddDays(NormalizeWarningDays(warningDays));
        return ExpirationDate <= window;
    }

    public InventoryLotExpiryStatus ExpiryStatus(DateOnly today, int warningDays)
    {
        if (IsExpired(today))
        {
            return InventoryLotExpiryStatus.Expired;
        }

        if (ExpirationDate == today)
        {
            return InventoryLotExpiryStatus.ExpiresToday;
        }

        if (IsNearExpiry(today, warningDays))
        {
            return InventoryLotExpiryStatus.NearExpiry;
        }

        return InventoryLotExpiryStatus.Ok;
    }

    public static (string? Display, string Normalized) NormalizeLotNumber(string? lotNumber)
    {
        if (string.IsNullOrWhiteSpace(lotNumber))
        {
            return (null, string.Empty);
        }

        var trimmed = lotNumber.Trim();
        if (trimmed.Length > LotNumberMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryLotNumber,
                $"Lot number must be at most {LotNumberMaxLength} characters.");
        }

        return (trimmed, trimmed.ToUpperInvariant());
    }

    public static int NormalizeWarningDays(int? warningDays)
    {
        if (warningDays is null)
        {
            return DefaultWarningDays;
        }

        if (warningDays.Value < MinWarningDays || warningDays.Value > MaxWarningDays)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpirationWarningDays,
                $"Near-expiry warning days must be between {MinWarningDays} and {MaxWarningDays}.");
        }

        return warningDays.Value;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return DateOnly.FromDateTime(utcNow.UtcDateTime);
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}

public enum InventoryLotExpiryStatus
{
    Ok = 0,
    NearExpiry = 1,
    ExpiresToday = 2,
    Expired = 3
}

public static class InventoryLotExpiryStatuses
{
    public static string ToCode(InventoryLotExpiryStatus status) => status.ToString();
}
