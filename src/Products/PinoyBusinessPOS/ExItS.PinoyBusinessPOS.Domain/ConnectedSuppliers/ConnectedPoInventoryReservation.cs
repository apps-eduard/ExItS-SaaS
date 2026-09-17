using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

public enum ConnectedPoReservationType
{
    TemporaryProposal = 0,
    ConfirmedOrder = 1
}

public enum ConnectedPoReservationStatus
{
    Active = 0,
    Released = 1,
    Consumed = 2,
    Expired = 3
}

/// <summary>
/// Aggregate-level inventory hold state on a connected purchase order.
/// </summary>
public enum ConnectedPoInventoryReservationState
{
    None = 0,
    TemporaryProposal = 1,
    Confirmed = 2,
    Released = 3,
    Consumed = 4
}

/// <summary>Testable defaults for temporary proposal holds.</summary>
public static class ConnectedPoInventoryReservationOptions
{
    public static TimeSpan DefaultProposalHoldDuration { get; set; } = TimeSpan.FromHours(24);
}

public sealed class ConnectedPoInventoryReservationId : ConnectedSupplierGuidId<ConnectedPoInventoryReservationId>
{
    private ConnectedPoInventoryReservationId(Guid value) : base(value) { }
    public static ConnectedPoInventoryReservationId New() => new(Guid.NewGuid());
    public static ConnectedPoInventoryReservationId From(Guid value) => new(value);
}

/// <summary>
/// Per-product reservation ledger row for a connected purchase order revision.
/// Does not change on-hand; on-hand decreases only when <see cref="Consume"/> runs
/// in concert with inventory <c>ConsumeReservation</c>.
/// </summary>
public sealed class ConnectedPoInventoryReservation
{
    public ConnectedPoInventoryReservationId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public ConnectedPurchaseOrderId ConnectedPurchaseOrderId { get; }
    public int Revision { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }
    public ConnectedPoReservationType Type { get; private set; }
    public ConnectedPoReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    /// <summary>Optimistic concurrency token (application-managed).</summary>
    public int Version { get; private set; }

    private ConnectedPoInventoryReservation(
        ConnectedPoInventoryReservationId id,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        int revision,
        decimal quantity,
        decimal remainingQuantity,
        ConnectedPoReservationType type,
        ConnectedPoReservationStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? releasedAtUtc,
        int version)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        ConnectedPurchaseOrderId = connectedPurchaseOrderId;
        Revision = revision;
        Quantity = quantity;
        RemainingQuantity = remainingQuantity;
        Type = type;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ReleasedAtUtc = releasedAtUtc;
        Version = version;
    }

    public static ConnectedPoInventoryReservation CreateTemporary(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        int revision,
        decimal quantity,
        DateTimeOffset utcNow,
        DateTimeOffset expiresAtUtc,
        ConnectedPoInventoryReservationId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureUtc(expiresAtUtc);
        EnsurePositiveQuantity(quantity);
        if (expiresAtUtc <= utcNow)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Temporary reservation expiry must be after creation time.");
        }

        if (revision < 1)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Reservation revision must be at least 1.");
        }

        return new(
            id ?? ConnectedPoInventoryReservationId.New(),
            organizationId,
            branchId,
            productId,
            connectedPurchaseOrderId,
            revision,
            quantity,
            quantity,
            ConnectedPoReservationType.TemporaryProposal,
            ConnectedPoReservationStatus.Active,
            utcNow,
            expiresAtUtc,
            releasedAtUtc: null,
            version: 1);
    }

    public static ConnectedPoInventoryReservation CreateConfirmed(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        int revision,
        decimal quantity,
        DateTimeOffset utcNow,
        ConnectedPoInventoryReservationId? id = null)
    {
        EnsureUtc(utcNow);
        EnsurePositiveQuantity(quantity);
        if (revision < 1)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOrder,
                "Reservation revision must be at least 1.");
        }

        return new(
            id ?? ConnectedPoInventoryReservationId.New(),
            organizationId,
            branchId,
            productId,
            connectedPurchaseOrderId,
            revision,
            quantity,
            quantity,
            ConnectedPoReservationType.ConfirmedOrder,
            ConnectedPoReservationStatus.Active,
            utcNow,
            expiresAtUtc: null,
            releasedAtUtc: null,
            version: 1);
    }

    public static ConnectedPoInventoryReservation Rehydrate(
        ConnectedPoInventoryReservationId id,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        int revision,
        decimal quantity,
        decimal remainingQuantity,
        ConnectedPoReservationType type,
        ConnectedPoReservationStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset? releasedAtUtc,
        int version) =>
        new(
            id,
            organizationId,
            branchId,
            productId,
            connectedPurchaseOrderId,
            revision,
            quantity,
            remainingQuantity,
            type,
            status,
            createdAtUtc,
            expiresAtUtc,
            releasedAtUtc,
            version);

    public void Release(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is ConnectedPoReservationStatus.Released or ConnectedPoReservationStatus.Consumed
            or ConnectedPoReservationStatus.Expired)
        {
            return;
        }

        if (Status != ConnectedPoReservationStatus.Active)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only an active reservation can be released.");
        }

        RemainingQuantity = 0m;
        Status = ConnectedPoReservationStatus.Released;
        ReleasedAtUtc = utcNow;
        Version++;
    }

    public void Expire(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is ConnectedPoReservationStatus.Expired or ConnectedPoReservationStatus.Released
            or ConnectedPoReservationStatus.Consumed)
        {
            return;
        }

        if (Status != ConnectedPoReservationStatus.Active
            || Type != ConnectedPoReservationType.TemporaryProposal)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only an active temporary reservation can expire.");
        }

        RemainingQuantity = 0m;
        Status = ConnectedPoReservationStatus.Expired;
        ReleasedAtUtc = utcNow;
        Version++;
    }

    public void Consume(decimal quantity)
    {
        EnsurePositiveQuantity(quantity);
        if (Status != ConnectedPoReservationStatus.Active)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only an active reservation can be consumed.");
        }

        if (quantity > RemainingQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Cannot consume more than the remaining reserved quantity.");
        }

        RemainingQuantity -= quantity;
        if (RemainingQuantity == 0m)
        {
            Status = ConnectedPoReservationStatus.Consumed;
        }

        Version++;
    }

    /// <summary>
    /// Converts an active temporary proposal hold into a confirmed order hold (clears expiry).
    /// </summary>
    public void ConfirmFromTemporary(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == ConnectedPoReservationStatus.Active
            && Type == ConnectedPoReservationType.ConfirmedOrder)
        {
            return;
        }

        if (Status != ConnectedPoReservationStatus.Active
            || Type != ConnectedPoReservationType.TemporaryProposal)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Only an active temporary reservation can be confirmed.");
        }

        Type = ConnectedPoReservationType.ConfirmedOrder;
        ExpiresAtUtc = null;
        Version++;
    }

    /// <summary>
    /// Active hold that still blocks ATP — excludes released/expired/consumed and time-expired
    /// temporary rows even before status persistence cleanup runs.
    /// </summary>
    public bool IsEffectivelyActive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != ConnectedPoReservationStatus.Active || RemainingQuantity <= 0m)
        {
            return false;
        }

        if (ExpiresAtUtc is DateTimeOffset expires && utcNow >= expires)
        {
            return false;
        }

        return true;
    }

    private static void EnsurePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Reservation quantity must be greater than zero.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
