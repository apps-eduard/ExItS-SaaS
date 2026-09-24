using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record PosInventoryReservationItemDto(
    Guid ReservationId,
    string SourceType,
    Guid ConnectedPurchaseOrderId,
    Guid? BuyerPurchaseOrderId,
    string? ReferenceNumber,
    string? CounterpartyName,
    decimal ReservedQuantity,
    string ReservationType,
    string Status,
    DateTimeOffset? ExpiresAtUtc,
    Guid BranchId,
    string? BranchName,
    DateTimeOffset CreatedAtUtc,
    Guid? InventoryTransferId = null);

public sealed record PosInventoryReservationsDto(
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    IReadOnlyList<PosInventoryReservationItemDto> Reservations,
    decimal InTransitOutboundQuantity = 0m,
    decimal InTransitInboundQuantity = 0m);

/// <summary>
/// Authoritative product reservation visibility: stock breakdown + active CPO holds
/// (time-expired temporary rows excluded even before ledger cleanup).
/// </summary>
public sealed class InventoryProductReservationsQuery
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly BranchInventoryContextResolver _branchResolver;
    private readonly BranchInventoryReadService _branchRead;
    private readonly IConnectedPoInventoryReservationRepository _reservations;
    private readonly IConnectedPurchaseOrderRepository _orders;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IOrganizationBranchDirectory? _branches;
    private readonly IPosCommercialAccessAccessor _access;
    private readonly TimeProvider _clock;

    public InventoryProductReservationsQuery(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        BranchInventoryContextResolver branchResolver,
        BranchInventoryReadService branchRead,
        IConnectedPoInventoryReservationRepository reservations,
        IConnectedPurchaseOrderRepository orders,
        IConnectedSupplierRelationshipRepository relationships,
        IInventoryTransferRepository transfers,
        IPosCommercialAccessAccessor access,
        IOrganizationBranchDirectory? branches = null,
        TimeProvider? clock = null)
    {
        _inventory = inventory;
        _products = products;
        _branchResolver = branchResolver;
        _branchRead = branchRead;
        _reservations = reservations;
        _orders = orders;
        _relationships = relationships;
        _transfers = transfers;
        _access = access;
        _branches = branches;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PosInventoryReservationsDto>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        BranchInventoryContext branchContext,
        CancellationToken cancellationToken = default)
    {
        var gate = CommercialAccessGuard.Require(_access, UtangCapability.ViewInventory);
        if (!gate.IsSuccess)
        {
            return ApplicationResult<PosInventoryReservationsDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || !_branchResolver.CanViewProductInManagement(product, branchContext))
        {
            return ApplicationResult<PosInventoryReservationsDto>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        BranchInventoryProductRead? branchRead = null;
        if (account is not null)
        {
            branchRead = await _branchRead
                .ResolveSingleAsync(branchContext, account, cancellationToken)
                .ConfigureAwait(false);
        }

        var onHand = branchRead?.BranchOnHand ?? account?.OnHandQuantity ?? 0m;
        var reserved = branchRead?.BranchReserved ?? account?.ReservedQuantity ?? 0m;
        var available = branchRead?.BranchAvailable
            ?? account?.AvailableQuantity
            ?? Math.Max(0m, onHand - reserved);
        if (available < 0m)
        {
            available = 0m;
        }

        var utcNow = _clock.GetUtcNow();
        var branchId = PosBranchId.From(branchContext.BranchId);

        var ledger = await _reservations
            .ListByProductBranchAsync(orgId, catalogProductId, branchId, cancellationToken)
            .ConfigureAwait(false);

        var active = ledger.Where(r => r.IsEffectivelyActive(utcNow)).ToList();
        // Prefer ledger-effective reserved when rows exist (covers expiry-before-cleanup).
        if (ledger.Count > 0)
        {
            reserved = active.Sum(r => r.RemainingQuantity);
            available = Math.Max(0m, onHand - reserved);
        }

        var orderIds = active.Select(r => r.ConnectedPurchaseOrderId).Distinct().ToList();
        var orders = new Dictionary<Guid, ConnectedPurchaseOrder>();
        foreach (var orderId in orderIds)
        {
            var order = await _orders.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
            if (order is not null)
            {
                orders[orderId.Value] = order;
            }
        }

        var relationshipIds = orders.Values.Select(o => o.RelationshipId).Distinct().ToList();
        var relationships = new Dictionary<Guid, ConnectedSupplierRelationship>();
        foreach (var relationshipId in relationshipIds)
        {
            var rel = await _relationships.GetAsync(relationshipId, cancellationToken).ConfigureAwait(false);
            if (rel is not null)
            {
                relationships[relationshipId.Value] = rel;
            }
        }

        string? branchName = null;
        if (_branches is not null)
        {
            var names = await _branches
                .GetNamesAsync(organizationId, [branchId.Value], cancellationToken)
                .ConfigureAwait(false);
            names.TryGetValue(branchId.Value, out branchName);
        }

        var items = active
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r =>
            {
                orders.TryGetValue(r.ConnectedPurchaseOrderId.Value, out var order);
                relationships.TryGetValue(order?.RelationshipId.Value ?? Guid.Empty, out var rel);
                var typeLabel = r.Type == ConnectedPoReservationType.TemporaryProposal
                    ? "TemporaryProposal"
                    : "ConfirmedOrder";
                var statusLabel = r.Type == ConnectedPoReservationType.TemporaryProposal
                    ? "Temporary"
                    : "Confirmed";
                return new PosInventoryReservationItemDto(
                    r.Id.Value,
                    "ConnectedPurchaseOrder",
                    r.ConnectedPurchaseOrderId.Value,
                    order?.BuyerPurchaseOrderId.Value,
                    order?.BuyerPoNumber,
                    rel?.BuyerDisplayNameSnapshot,
                    r.RemainingQuantity,
                    typeLabel,
                    statusLabel,
                    r.Type == ConnectedPoReservationType.TemporaryProposal ? r.ExpiresAtUtc : null,
                    r.BranchId.Value,
                    branchName,
                    r.CreatedAtUtc);
            })
            .ToList();

        var transferCommitments = await _transfers
            .ListOpenCommitmentsForBranchAsync(
                orgId,
                branchId,
                [catalogProductId],
                cancellationToken)
            .ConfigureAwait(false);

        var peerIds = transferCommitments.Select(c => c.PeerBranchId).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> peerNames = new Dictionary<Guid, string>();
        if (_branches is not null && peerIds.Count > 0)
        {
            peerNames = await _branches
                .GetNamesAsync(organizationId, peerIds, cancellationToken)
                .ConfigureAwait(false);
        }

        decimal inTransitOutbound = 0m;
        decimal inTransitInbound = 0m;
        (inTransitOutbound, inTransitInbound) = InventoryTransferCommitmentTotals.Sum(transferCommitments);
        foreach (var commitment in transferCommitments.OrderByDescending(c => c.CreatedAtUtc))
        {
            peerNames.TryGetValue(commitment.PeerBranchId, out var peerName);
            var isOutbound = string.Equals(commitment.Direction, "Outbound", StringComparison.Ordinal);

            items.Add(new PosInventoryReservationItemDto(
                commitment.TransferId,
                "InventoryTransfer",
                Guid.Empty,
                null,
                commitment.TransferNumber,
                peerName,
                commitment.OutstandingQuantity,
                isOutbound ? "TransferOutbound" : "TransferInbound",
                "InTransit",
                null,
                branchId.Value,
                branchName,
                commitment.CreatedAtUtc,
                commitment.TransferId));
        }

        // Reserved stays true reservation quantity only (ledger / branch reserved).
        // In-transit transfer commitments are surface separately and must not be
        // double-deducted from Available — on-hand already reflects dispatch.
        return ApplicationResult<PosInventoryReservationsDto>.Success(
            new PosInventoryReservationsDto(
                productId,
                product.Name,
                UnitOfMeasures.ToCode(product.UnitOfMeasure),
                onHand,
                reserved,
                available,
                items,
                inTransitOutbound,
                inTransitInbound));
    }
}

/// <summary>Sums open transfer commitments without treating them as reservations.</summary>
public static class InventoryTransferCommitmentTotals
{
    public static (decimal Outbound, decimal Inbound) Sum(
        IEnumerable<InventoryTransferOpenCommitment> commitments)
    {
        decimal outbound = 0m;
        decimal inbound = 0m;
        foreach (var commitment in commitments)
        {
            if (string.Equals(commitment.Direction, "Outbound", StringComparison.Ordinal))
            {
                outbound += commitment.OutstandingQuantity;
            }
            else
            {
                inbound += commitment.OutstandingQuantity;
            }
        }

        return (outbound, inbound);
    }
}
