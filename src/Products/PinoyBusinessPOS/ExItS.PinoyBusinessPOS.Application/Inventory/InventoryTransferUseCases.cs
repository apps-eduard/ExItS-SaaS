using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class InventoryTransferQueryService
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryTransferDamageCustodyRepository _damageCustodies;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly ICatalogProductRepository _products;

    public InventoryTransferQueryService(
        IInventoryTransferRepository transfers,
        IInventoryTransferDamageCustodyRepository damageCustodies,
        IOrganizationBranchDirectory branches,
        ICatalogProductRepository products)
    {
        _transfers = transfers;
        _damageCustodies = damageCustodies;
        _branches = branches;
        _products = products;
    }

    public async Task<InventoryTransferDto?> GetByIdAsync(
        Guid organizationId,
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var transfer = await _transfers
            .GetByIdAsync(orgId, InventoryTransferId.From(transferId), cancellationToken)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return null;
        }

        var names = await _branches
            .GetNamesAsync(organizationId, [transfer.SourceBranchId.Value, transfer.DestinationBranchId.Value], cancellationToken)
            .ConfigureAwait(false);
        var productIds = transfer.Lines.Select(l => l.ProductId).Distinct().ToList();
        var skuByProduct = (await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(p => p.Id.Value, p => p.Sku);

        var family = await _transfers
            .ListByRootTransferIdAsync(orgId, transfer.FamilyRootId, cancellationToken)
            .ConfigureAwait(false);
        if (family.Count == 0)
        {
            family = [transfer];
        }

        var custodies = await _damageCustodies
            .ListByRootTransferIdAsync(orgId, transfer.FamilyRootId, cancellationToken)
            .ConfigureAwait(false);

        // Authoritative family coverage (same formula as StockRequestDispatchCoverage):
        // Remaining = MAX(0, Target − GoodReceived − OpenInTransit − Waived)
        var goodReceived = family.Where(t => t.Status != InventoryTransferStatus.Cancelled)
            .SelectMany(t => t.Lines)
            .Sum(l => l.ReceivedQty);
        var openInTransit = family
            .Where(t => t.Status is InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived)
            .SelectMany(t => t.Lines)
            .Sum(l => Math.Max(0m, l.OutstandingQty));
        var waived = family.Where(t => t.Status != InventoryTransferStatus.Cancelled)
            .SelectMany(t => t.Lines)
            .Sum(l => l.WaivedQty);
        var target = family.Where(t => t.RootTransferId is null).SelectMany(t => t.Lines).Sum(l => l.SentQty);
        var remaining = Math.Max(0m, target - goodReceived - openInTransit - waived);

        return Map(
            transfer,
            names,
            skuByProduct,
            family.Select(t =>
            {
                var receiptLines = t.Receipts.SelectMany(r => r.Lines);
                return new InventoryTransferFamilyMemberDto(
                    t.Id.Value,
                    t.TransferNumber,
                    InventoryTransferStatuses.ToCode(t.Status),
                    t.ReplacementSequence,
                    t.RootTransferId is null,
                    t.TotalSentQty,
                    t.TotalReceivedQty,
                    t.TotalOutstandingQty,
                    receiptLines.Sum(l => l.QuantityDamaged),
                    receiptLines.Sum(l => l.QuantityMissing),
                    receiptLines.Sum(l => l.QuantityOther));
            }).ToList(),
            custodies.Select(InventoryTransferDamageCustodyMapping.Map).ToList(),
            goodReceived,
            openInTransit,
            remaining,
            waived);
    }

    public async Task<PagedResult<InventoryTransferListItemDto>> ListAsync(
        Guid organizationId,
        InventoryTransferFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _transfers
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var branchIds = items
            .SelectMany(t => new[] { t.SourceBranchId.Value, t.DestinationBranchId.Value })
            .Distinct()
            .ToList();
        var names = await _branches.GetNamesAsync(organizationId, branchIds, cancellationToken).ConfigureAwait(false);
        return new PagedResult<InventoryTransferListItemDto>(
            items.Select(t => MapListItem(t, names)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    internal static InventoryTransferDto Map(
        InventoryTransfer transfer,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlyDictionary<Guid, string?>? skuByProduct = null,
        IReadOnlyList<InventoryTransferFamilyMemberDto>? familyMembers = null,
        IReadOnlyList<InventoryTransferDamageCustodyDto>? damageCustodies = null,
        decimal satisfiedAtDestinationQty = 0,
        decimal openInTransitQty = 0,
        decimal remainingToDispatchQty = 0,
        decimal waivedQty = 0) =>
        new(
            transfer.Id.Value,
            transfer.OrganizationId.Value,
            transfer.StockRequestId?.Value,
            transfer.TransferNumber,
            transfer.SourceBranchId.Value,
            names.GetValueOrDefault(transfer.SourceBranchId.Value),
            transfer.DestinationBranchId.Value,
            names.GetValueOrDefault(transfer.DestinationBranchId.Value),
            InventoryTransferStatuses.ToCode(transfer.Status),
            transfer.Notes,
            transfer.CreatedBy,
            transfer.CreatedAtUtc,
            transfer.UpdatedAtUtc,
            transfer.DispatchedAtUtc,
            transfer.DispatchedBy,
            transfer.ReceivedAtUtc,
            transfer.ReceivedBy,
            transfer.CancelledAtUtc,
            transfer.CancelledBy,
            transfer.ClosedAtUtc,
            transfer.ClosedBy,
            transfer.TotalSentQty,
            transfer.TotalReceivedQty,
            transfer.TotalClosedQty,
            transfer.TotalOutstandingQty,
            transfer.TotalDifferenceQty,
            transfer.Receipts.Count,
            transfer.Receipts.Count > 0 ? transfer.Receipts[^1].ReceivedAtUtc : null,
            transfer.Receipts.Select(r => new InventoryTransferReceiptDto(
                r.Id.Value,
                r.Sequence,
                r.ReceivedAtUtc,
                r.ReceivedBy,
                r.Lines.Select(rl => new InventoryTransferReceiptLineDto(
                    rl.Id.Value,
                    rl.TransferLineId.Value,
                    rl.ProductId.Value,
                    rl.QuantityReceived,
                    rl.QuantityDamaged,
                    rl.QuantityMissing,
                    rl.QuantityOther,
                    rl.OtherReasonCode,
                    rl.OtherReasonNote,
                    rl.MissingDisposition is null
                        ? null
                        : InventoryTransferMissingDispositions.ToCode(rl.MissingDisposition.Value),
                    rl.DamagedFollowUp is null
                        ? null
                        : InventoryTransferDiscrepancyFollowUps.ToCode(rl.DamagedFollowUp.Value),
                    rl.OtherFollowUp is null
                        ? null
                        : InventoryTransferDiscrepancyFollowUps.ToCode(rl.OtherFollowUp.Value),
                    rl.QuantityWaived,
                    rl.Note)).ToList())).ToList(),
            transfer.Lines.Select(l => new InventoryTransferLineDto(
                l.Id.Value,
                l.ProductId.Value,
                l.NameSnapshot,
                l.UnitOfMeasure.ToString(),
                l.LineNumber,
                l.SentQty,
                l.ReceivedQty,
                l.OutstandingQty,
                l.ClosedQty,
                l.WaivedQty,
                l.DifferenceQty,
                l.LineStatus,
                l.DiscrepancyReason is null ? null : InventoryTransferDiscrepancyReasons.ToCode(l.DiscrepancyReason.Value),
                l.DiscrepancyNote,
                l.SourceLotId?.Value,
                l.LotNumber,
                l.ExpirationDate,
                l.UnitCostSnapshot,
                skuByProduct is not null && skuByProduct.TryGetValue(l.ProductId.Value, out var sku)
                    ? sku
                    : null)).ToList(),
            transfer.RootTransferId?.Value,
            transfer.ReplacementSequence,
            transfer.ReplacementReason,
            InventoryTransferDamageHandlingPolicies.ToCode(transfer.DamageHandlingPolicy),
            familyMembers,
            damageCustodies,
            satisfiedAtDestinationQty,
            openInTransitQty,
            remainingToDispatchQty,
            waivedQty);
    private static InventoryTransferListItemDto MapListItem(
        InventoryTransfer transfer,
        IReadOnlyDictionary<Guid, string> names) =>
        new(
            transfer.Id.Value,
            transfer.StockRequestId?.Value,
            transfer.TransferNumber,
            transfer.SourceBranchId.Value,
            names.GetValueOrDefault(transfer.SourceBranchId.Value),
            transfer.DestinationBranchId.Value,
            names.GetValueOrDefault(transfer.DestinationBranchId.Value),
            InventoryTransferStatuses.ToCode(transfer.Status),
            transfer.Lines.Count,
            transfer.TotalSentQty,
            transfer.TotalReceivedQty,
            transfer.TotalDifferenceQty,
            transfer.UpdatedAtUtc,
            transfer.CreatedBy,
            transfer.DispatchedBy,
            transfer.ReceivedBy,
            transfer.CancelledBy,
            transfer.ClosedAtUtc,
            transfer.ClosedBy);
}

public sealed class CreateInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryLotRepository _lots;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly InventoryCostResolver _costs;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateInventoryTransfer(
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IInventoryLotRepository lots,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        InventoryCostResolver? costs = null)
    {
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _lots = lots;
        _branches = branches;
        _costs = costs ?? new InventoryCostResolver(inventory);
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransfer>> ExecuteAsync(
        Guid organizationId,
        CreateInventoryTransferRequest request,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a transfer.");
        }

        var branchGuard = await InventoryTransferAuthorization
            .EnsureSourceBranchAsync(_branches, organizationId, request.SourceBranchId, request.DestinationBranchId, actingBranchId, cancellationToken)
            .ConfigureAwait(false);
        if (branchGuard is not null)
        {
            return branchGuard;
        }

        var orgId = PosOrganizationId.From(organizationId);
        var drafts = await InventoryTransferLineFactory
            .CreateDraftsAsync(_products, _lots, orgId, request.Lines, cancellationToken)
            .ConfigureAwait(false);
        if (!drafts.IsSuccess)
        {
            return ApplicationResult<InventoryTransfer>.Failure(drafts.ErrorCode!, drafts.ErrorMessage!);
        }

        var costByProduct = await _costs
            .ResolveUnitCostsAsync(orgId, drafts.Value!.Select(d => d.ProductId), cancellationToken)
            .ConfigureAwait(false);
        var draftsWithCosts = drafts.Value!
            .Select(d => d with { UnitCostSnapshot = costByProduct.GetValueOrDefault(d.ProductId.Value) })
            .ToList();

        try
        {
            var transfer = InventoryTransfer.CreateDraft(
                orgId,
                PosBranchId.From(request.SourceBranchId),
                PosBranchId.From(request.DestinationBranchId),
                draftsWithCosts,
                actorId,
                _clock.UtcNow,
                request.Notes,
                stockRequestId: request.StockRequestId is Guid requestId && requestId != Guid.Empty
                    ? StockRequestId.From(requestId)
                    : null,
                rootTransferId: request.RootTransferId is Guid rootId && rootId != Guid.Empty
                    ? InventoryTransferId.From(rootId)
                    : null,
                replacementSequence: request.ReplacementSequence,
                replacementReason: request.ReplacementReason,
                damageHandlingPolicy: string.IsNullOrWhiteSpace(request.DamageHandlingPolicy)
                    ? InventoryTransferDamageHandlingPolicy.ReceiverMayDecide
                    : InventoryTransferDamageHandlingPolicies.Parse(request.DamageHandlingPolicy));

            var productIds = transfer.Lines.Select(l => l.ProductId).ToList();
            var accounts = (await _inventory.ListByProductIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false))
                .ToDictionary(a => a.ProductId.Value);
            var balances = (await _balances.ListByProductIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false))
                .ToList();
            var lotIds = transfer.Lines
                .Where(l => l.SourceLotId is not null)
                .Select(l => l.SourceLotId!)
                .Distinct()
                .ToList();
            var lotsById = new Dictionary<Guid, InventoryLot>();
            foreach (var lotId in lotIds)
            {
                var lot = await _lots.GetByIdAsync(orgId, lotId, cancellationToken).ConfigureAwait(false);
                if (lot is not null)
                {
                    lotsById[lot.Id.Value] = lot;
                }
            }

            var branchNames = await _branches
                .GetNamesAsync(organizationId, [request.SourceBranchId], cancellationToken)
                .ConfigureAwait(false);
            var sourceBranchName = branchNames.TryGetValue(request.SourceBranchId, out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "source branch";

            var stockGuard = InventoryTransferStock.ValidateSourceAvailability(
                orgId,
                transfer.SourceBranchId,
                sourceBranchName,
                transfer.Lines.Select(l => new InventoryTransferStockDemand(
                    l.ProductId,
                    l.SentQty,
                    l.NameSnapshot,
                    l.UnitOfMeasure,
                    l.SourceLotId)).ToList(),
                accounts,
                balances,
                lotsById,
                _clock.UtcNow);
            if (stockGuard is not null)
            {
                return stockGuard;
            }

            await _transfers.AddAsync(transfer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Source prepares a draft replacement transfer for family remaining qty
/// (direct transfers without a stock request, or when remaining is still open).
/// Idempotent: returns an existing linked Draft when present.
/// </summary>
public sealed class PrepareInventoryTransferRemaining
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly CreateInventoryTransfer _createTransfer;
    private readonly InventoryTransferQueryService _queries;
    private readonly PrepareStockRequestTransfer _prepareStockRequestTransfer;

    public PrepareInventoryTransferRemaining(
        IInventoryTransferRepository transfers,
        CreateInventoryTransfer createTransfer,
        InventoryTransferQueryService queries,
        PrepareStockRequestTransfer prepareStockRequestTransfer)
    {
        _transfers = transfers;
        _createTransfer = createTransfer;
        _queries = queries;
        _prepareStockRequestTransfer = prepareStockRequestTransfer;
    }

    public async Task<ApplicationResult<InventoryTransferDto>> ExecuteAsync(
        Guid organizationId,
        Guid transferId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var transfer = await _transfers
            .GetByIdAsync(orgId, InventoryTransferId.From(transferId), cancellationToken)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.");
        }

        if (transfer.StockRequestId is StockRequestId stockRequestId)
        {
            return await _prepareStockRequestTransfer
                .ExecuteAsync(organizationId, stockRequestId.Value, actorId, actingBranchId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (actingBranchId != transfer.SourceBranchId.Value)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the source branch can prepare remaining fulfillment.");
        }

        var family = await _transfers
            .ListByRootTransferIdAsync(orgId, transfer.FamilyRootId, cancellationToken)
            .ConfigureAwait(false);
        if (family.Count == 0)
        {
            family = [transfer];
        }

        var existingDraft = family.FirstOrDefault(t => t.Status == InventoryTransferStatus.Draft);
        if (existingDraft is not null)
        {
            var existingDto = await _queries
                .GetByIdAsync(organizationId, existingDraft.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            return existingDto is null
                ? ApplicationResult<InventoryTransferDto>.Failure(
                    ApplicationErrorCodes.InventoryTransferNotFound,
                    "Inventory transfer was not found.")
                : ApplicationResult<InventoryTransferDto>.Success(existingDto);
        }

        var root = family.FirstOrDefault(t => t.RootTransferId is null)
            ?? family.OrderBy(t => t.CreatedAtUtc).First();
        var remainingLines = BuildRemainingFamilyLines(family, root);
        if (remainingLines.Count == 0)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferNoRemainingToFulfill,
                "No remaining quantity is available to prepare. Outstanding quantity is already covered.");
        }

        var nextSequence = family
            .Where(t => t.RootTransferId == root.Id || t.Id == root.Id)
            .Select(t => t.ReplacementSequence ?? 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var createRequest = new CreateInventoryTransferRequest(
            root.SourceBranchId.Value,
            root.DestinationBranchId.Value,
            remainingLines,
            root.Notes,
            StockRequestId: null,
            RootTransferId: root.Id.Value,
            ReplacementSequence: nextSequence,
            ReplacementReason: "Replacement for remaining / discrepancy fulfillment",
            DamageHandlingPolicy: InventoryTransferDamageHandlingPolicies.ToCode(root.DamageHandlingPolicy));

        var created = await _createTransfer
            .ExecuteAsync(organizationId, createRequest, actorId, actingBranchId, cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess)
        {
            return ApplicationResult<InventoryTransferDto>.Failure(created.ErrorCode!, created.ErrorMessage!);
        }

        var dto = await _queries
            .GetByIdAsync(organizationId, created.Value!.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        return dto is null
            ? ApplicationResult<InventoryTransferDto>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.")
            : ApplicationResult<InventoryTransferDto>.Success(dto);
    }

    internal static IReadOnlyList<InventoryTransferLineRequest> BuildRemainingFamilyLines(
        IReadOnlyList<InventoryTransfer> family,
        InventoryTransfer root)
    {
        var active = family.Where(t => t.Status != InventoryTransferStatus.Cancelled).ToList();
        var goodByProduct = active
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.ReceivedQty));
        var openByProduct = family
            .Where(t => t.Status is InventoryTransferStatus.InTransit or InventoryTransferStatus.PartiallyReceived)
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => Math.Max(0m, l.OutstandingQty)));
        var waivedByProduct = active
            .SelectMany(t => t.Lines)
            .GroupBy(l => l.ProductId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.WaivedQty));

        var lines = new List<InventoryTransferLineRequest>();
        foreach (var line in root.Lines)
        {
            var productId = line.ProductId.Value;
            var remaining = Math.Max(
                0m,
                line.SentQty
                - goodByProduct.GetValueOrDefault(productId)
                - openByProduct.GetValueOrDefault(productId)
                - waivedByProduct.GetValueOrDefault(productId));
            if (remaining > 0m)
            {
                lines.Add(new InventoryTransferLineRequest(productId, remaining, line.SourceLotId?.Value));
            }
        }

        return lines;
    }
}

public sealed class DispatchInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly InventoryLotStockService _lots;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IInventoryTransferAlertSink _alerts;
    private readonly IStockRequestRepository _stockRequests;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly InventoryCostResolver _costs;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DispatchInventoryTransfer(
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IInventoryLotRepository lotRepository,
        InventoryLotStockService lots,
        IOrganizationBranchDirectory branches,
        IInventoryTransferAlertSink alerts,
        IStockRequestRepository stockRequests,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock,
        InventoryCostResolver? costs = null)
    {
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _lotRepository = lotRepository;
        _lots = lots;
        _branches = branches;
        _alerts = alerts;
        _stockRequests = stockRequests;
        _notifications = notifications;
        _costs = costs ?? new InventoryCostResolver(inventory);
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransfer>> ExecuteAsync(
        Guid organizationId,
        Guid transferId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to dispatch a transfer.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
        var orgId = PosOrganizationId.From(organizationId);
        var transfer = await _transfers
            .GetByIdAsync(orgId, InventoryTransferId.From(transferId), ct)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.");
        }

        if (transfer.Status == InventoryTransferStatus.InTransit)
        {
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }

        var branchGuard = await InventoryTransferAuthorization
            .EnsureSourceActorAsync(_branches, organizationId, transfer.SourceBranchId.Value, transfer.DestinationBranchId.Value, actingBranchId, ct)
            .ConfigureAwait(false);
        if (branchGuard is not null)
        {
            return branchGuard;
        }

        var productIds = transfer.Lines.Select(l => l.ProductId).ToList();
        var accounts = (await _inventory.ListByProductIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
            .ToDictionary(a => a.ProductId.Value);
        var catalog = (await _products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
            .ToDictionary(p => p.Id.Value);
        var balances = (await _balances.ListByProductIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
            .ToList();

        try
        {
            var utcNow = _clock.UtcNow;
            var lotIds = transfer.Lines
                .Where(l => l.SourceLotId is not null)
                .Select(l => l.SourceLotId!)
                .Distinct()
                .ToList();
            var lotsById = new Dictionary<Guid, InventoryLot>();
            foreach (var lotId in lotIds)
            {
                var lot = await _lotRepository.GetByIdAsync(orgId, lotId, ct).ConfigureAwait(false);
                if (lot is not null)
                {
                    lotsById[lot.Id.Value] = lot;
                }
            }

            var branchNames = await _branches
                .GetNamesAsync(organizationId, [transfer.SourceBranchId.Value], ct)
                .ConfigureAwait(false);
            var sourceBranchName = branchNames.TryGetValue(transfer.SourceBranchId.Value, out var name)
                && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : "source branch";

            var stockGuard = InventoryTransferStock.ValidateSourceAvailability(
                orgId,
                transfer.SourceBranchId,
                sourceBranchName,
                transfer.Lines.Select(l => new InventoryTransferStockDemand(
                    l.ProductId,
                    l.SentQty,
                    l.NameSnapshot,
                    l.UnitOfMeasure,
                    l.SourceLotId)).ToList(),
                accounts,
                balances,
                lotsById,
                utcNow);
            if (stockGuard is not null)
            {
                return stockGuard;
            }

            var number = transfer.IsReplacementChild
                ? await AllocateReplacementNumberAsync(orgId, transfer, ct).ConfigureAwait(false)
                : await _transfers
                    .AllocateNextNumberAsync(orgId, InventoryTransferNumbers.BusinessDateOf(utcNow), ct)
                    .ConfigureAwait(false);

            // Dispatch-time acquisition costs are authoritative (draft may have sat).
            var dispatchCosts = await _costs
                .ResolveUnitCostsAsync(orgId, productIds, ct)
                .ConfigureAwait(false);
            transfer.RefreshLineUnitCosts(dispatchCosts);

            foreach (var line in transfer.Lines)
            {
                var account = accounts[line.ProductId.Value];
                if (await _inventory
                        .HasInventoryTransferMovementAsync(orgId, transfer.Id, line.ProductId, StockMovementType.TransferOut, line.SourceLotId, ct)
                        .ConfigureAwait(false))
                {
                    continue;
                }

                var sourceBalance = InventoryTransferStock.EnsureSourceBalance(
                    orgId,
                    transfer.SourceBranchId,
                    line.ProductId,
                    account.OnHandQuantity,
                    balances,
                    utcNow);

                var sellingMode = catalog.TryGetValue(line.ProductId.Value, out var product)
                    ? product.SellingMode
                    : SellingMode.PerItem;
                var movement = StockMovement.TransferOut(
                    orgId,
                    line.ProductId,
                    account.Id,
                    transfer.SourceBranchId,
                    line.SentQty,
                    line.UnitOfMeasure,
                    transfer.Id.Value,
                    number,
                    actorId,
                    utcNow,
                    sellingMode: sellingMode,
                    unitCost: line.UnitCostSnapshot);
                if (line.SourceLotId is not null)
                {
                    var lot = await _lotRepository
                        .GetByIdAsync(orgId, line.SourceLotId, ct)
                        .ConfigureAwait(false);
                    if (lot is null || lot.ProductId != line.ProductId)
                    {
                        return ApplicationResult<InventoryTransfer>.Failure(
                            DomainErrorCodes.InventoryLotMismatch,
                            $"Source lot for '{line.NameSnapshot}' was not found.");
                    }

                    await _lots
                        .ConsumeSpecificAsync(
                            orgId,
                            lot,
                            line.SentQty,
                            actorId,
                            utcNow,
                            StockMovementType.TransferOut,
                            StockMovementSourceType.InventoryTransfer,
                            transfer.Id.Value,
                            movement.Id.Value,
                            ct)
                        .ConfigureAwait(false);
                    movement = movement.WithLot(lot.Id);
                }

                account.ApplyMovementEffect(movement.QuantityEffect);
                account.Touch(utcNow);
                sourceBalance.Apply(movement.QuantityEffect, utcNow);
                await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(sourceBalance, ct).ConfigureAwait(false);
            }

            transfer.Dispatch(number, actorId, utcNow);
            await _transfers.UpdateAsync(transfer, ct).ConfigureAwait(false);

            if (transfer.StockRequestId is StockRequestId stockRequestId)
            {
                var stockRequest = await _stockRequests
                    .GetByIdAsync(orgId, stockRequestId, ct)
                    .ConfigureAwait(false);
                if (stockRequest is not null
                    && stockRequest.Status is StockRequestStatus.Approved or StockRequestStatus.Preparing)
                {
                    stockRequest.MarkDispatched(actorId, utcNow, transfer.Id.Value);
                    await _stockRequests.UpdateAsync(stockRequest, ct).ConfigureAwait(false);
                    await StockRequestNotificationHelper
                        .PublishAsync(
                            _notifications,
                            organizationId,
                            StockRequestNotificationTypes.Dispatched,
                            stockRequest,
                            stockRequest.DestinationLocationId.Value,
                            "Stock request dispatched",
                            $"{stockRequest.RequestNumber ?? stockRequest.Id.Value.ToString("D")} is in transit.",
                            ct)
                        .ConfigureAwait(false);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            await _alerts.PublishAsync(
                    new InventoryTransferAlert(
                        "dispatched",
                        organizationId,
                        transfer.DestinationBranchId.Value,
                        transfer.Id.Value,
                        transfer.TransferNumber!,
                        $"Inventory transfer {transfer.TransferNumber} is on the way.",
                        transfer.StockRequestId?.Value),
                    ct)
                .ConfigureAwait(false);
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<InventoryTransfer>.Failure(code, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.InventoryInsufficientStock
                ? ApplicationErrorCodes.InsufficientStock
                : ex.ErrorCode;
            return ApplicationResult<InventoryTransfer>.Failure(code, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<string> AllocateReplacementNumberAsync(
        PosOrganizationId orgId,
        InventoryTransfer transfer,
        CancellationToken cancellationToken)
    {
        var rootId = transfer.RootTransferId
            ?? throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReplacementSequence,
                "Replacement children require a root transfer id.");
        var sequence = transfer.ReplacementSequence
            ?? throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReplacementSequence,
                "Replacement children require a replacement sequence.");

        var root = await _transfers.GetByIdAsync(orgId, rootId, cancellationToken).ConfigureAwait(false);
        if (root is null || string.IsNullOrWhiteSpace(root.TransferNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferNumber,
                "Root transfer must be dispatched before a replacement child can be numbered.");
        }

        return InventoryTransferNumbers.FormatReplacement(root.TransferNumber, sequence);
    }
}

public sealed class ReceiveInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryTransferDamageCustodyRepository _damageCustodies;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly InventoryLotStockService _lots;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IInventoryTransferAlertSink _alerts;
    private readonly IStockRequestRepository _stockRequests;
    private readonly IOrganizationBusinessNotificationPublisher _notifications;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceiveInventoryTransfer(
        IInventoryTransferRepository transfers,
        IInventoryTransferDamageCustodyRepository damageCustodies,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IInventoryLotRepository lotRepository,
        InventoryLotStockService lots,
        IOrganizationBranchDirectory branches,
        IInventoryTransferAlertSink alerts,
        IStockRequestRepository stockRequests,
        IOrganizationBusinessNotificationPublisher notifications,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _damageCustodies = damageCustodies;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _lotRepository = lotRepository;
        _lots = lots;
        _branches = branches;
        _alerts = alerts;
        _stockRequests = stockRequests;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransfer>> ExecuteAsync(
        Guid organizationId,
        Guid transferId,
        ReceiveInventoryTransferRequest request,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to receive a transfer.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
        var orgId = PosOrganizationId.From(organizationId);
        var transfer = await _transfers
            .GetByIdAsync(orgId, InventoryTransferId.From(transferId), ct)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.");
        }

        if (transfer.Status is InventoryTransferStatus.Received or InventoryTransferStatus.ClosedWithDiscrepancy)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferAlreadyReceived,
                "This transfer has already been completed.");
        }

        if (actingBranchId != transfer.DestinationBranchId.Value)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the destination branch can receive this transfer.");
        }

        var destOk = await _branches
            .ExistsInOrganizationAsync(organizationId, transfer.DestinationBranchId.Value, ct)
            .ConfigureAwait(false);
        if (!destOk)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Destination branch was not found in this organization.");
        }

        var receiveDrafts = new List<InventoryTransferReceiveLineDraft>();
        foreach (var line in request.Lines ?? [])
        {
            InventoryTransferDiscrepancyReason? reason = null;
            if (!string.IsNullOrWhiteSpace(line.DiscrepancyReason))
            {
                if (!InventoryTransferDiscrepancyReasons.TryParse(line.DiscrepancyReason, out var parsed))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDiscrepancyReason,
                        "Discrepancy reason is not recognized.");
                }

                reason = parsed;
            }

            var goodQty = line.GoodQty > 0m ? line.GoodQty : line.ReceivedQty;
            InventoryTransferMissingDisposition? missingDisposition = null;
            if (!string.IsNullOrWhiteSpace(line.MissingDisposition))
            {
                if (!InventoryTransferMissingDispositions.TryParse(line.MissingDisposition, out var parsedMissing))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferMissingDisposition,
                        "Missing disposition is not recognized.");
                }

                missingDisposition = parsedMissing;
            }

            InventoryTransferDiscrepancyFollowUp? damagedFollowUp = null;
            if (!string.IsNullOrWhiteSpace(line.DamagedFollowUp))
            {
                if (!InventoryTransferDiscrepancyFollowUps.TryParse(line.DamagedFollowUp, out var parsedDamaged))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDiscrepancyFollowUp,
                        "Damaged follow-up is not recognized.");
                }

                damagedFollowUp = parsedDamaged;
            }

            InventoryTransferDiscrepancyFollowUp? otherFollowUp = null;
            if (!string.IsNullOrWhiteSpace(line.OtherFollowUp))
            {
                if (!InventoryTransferDiscrepancyFollowUps.TryParse(line.OtherFollowUp, out var parsedOther))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDiscrepancyFollowUp,
                        "Other follow-up is not recognized.");
                }

                otherFollowUp = parsedOther;
            }

            InventoryTransferDamagedCustodyDecision? damagedDecision = null;
            if (!string.IsNullOrWhiteSpace(line.DamagedCustodyDecision))
            {
                if (!InventoryTransferDamagedCustodyDecisions.TryParse(line.DamagedCustodyDecision, out var parsedDecision))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDamagedCustodyDecision,
                        "Damaged custody decision is not recognized.");
                }

                damagedDecision = parsedDecision;
            }

            receiveDrafts.Add(new InventoryTransferReceiveLineDraft(
                CatalogProductId.From(line.ProductId),
                goodQty,
                reason,
                line.DiscrepancyNote,
                LineId: line.LineId is null ? null : InventoryTransferLineId.From(line.LineId.Value),
                DamagedQty: line.DamagedQty,
                MissingQty: line.MissingQty,
                OtherQty: line.OtherQty,
                OtherReasonCode: line.OtherReasonCode,
                OtherReasonNote: line.OtherReasonNote,
                MissingDisposition: missingDisposition,
                DamagedFollowUp: damagedFollowUp,
                OtherFollowUp: otherFollowUp,
                DamagedCustodyDecision: damagedDecision));
        }

        var productIds = transfer.Lines.Select(l => l.ProductId).ToList();
        var accounts = (await _inventory.ListByProductIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
            .ToDictionary(a => a.ProductId.Value);
        var catalog = (await _products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
            .ToDictionary(p => p.Id.Value);
        var balances = (await _balances.ListByProductIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
            .ToList();

        try
        {
            var utcNow = _clock.UtcNow;
            // Receive-now semantics: request lines are quantities for this wave only (not cumulative).
            var receipt = transfer.Receive(receiveDrafts, actorId, utcNow);
            var lineById = transfer.Lines.ToDictionary(l => l.Id);

            foreach (var receiptLine in receipt.Lines)
            {
                if (!lineById.TryGetValue(receiptLine.TransferLineId, out var line))
                {
                    continue;
                }

                var waveQty = receiptLine.QuantityReceived;
                if (waveQty <= 0m)
                {
                    continue;
                }

                if (!catalog.TryGetValue(line.ProductId.Value, out var product))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        ApplicationErrorCodes.InventoryProductNotFound,
                        $"Product '{line.NameSnapshot}' was not found.");
                }

                if (!accounts.TryGetValue(line.ProductId.Value, out var account))
                {
                    account = InventoryAccount.CreateUntracked(orgId, line.ProductId, utcNow);
                    account.Enable(0m, product.UnitOfMeasure, actorId, utcNow, hasOpeningStockAlready: false, product.SellingMode);
                    await _inventory.AddAccountAsync(account, ct).ConfigureAwait(false);
                    accounts[line.ProductId.Value] = account;
                }
                else if (!account.IsTracked)
                {
                    account.Enable(0m, product.UnitOfMeasure, actorId, utcNow, hasOpeningStockAlready: true, product.SellingMode);
                }

                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            receipt.Id.Value,
                            line.ProductId,
                            StockMovementType.TransferIn,
                            line.SourceLotId,
                            ct)
                        .ConfigureAwait(false))
                {
                    continue;
                }

                var movement = StockMovement.TransferIn(
                    orgId,
                    line.ProductId,
                    account.Id,
                    transfer.DestinationBranchId,
                    waveQty,
                    line.UnitOfMeasure,
                    receipt.Id.Value,
                    transfer.TransferNumber!,
                    actorId,
                    utcNow,
                    sellingMode: product.SellingMode,
                    unitCost: line.UnitCostSnapshot);
                DateOnly? expiry = line.ExpirationDate;
                var lotNumber = line.LotNumber;
                if (line.SourceLotId is not null && expiry is null)
                {
                    var sourceLot = await _lotRepository
                        .GetByIdAsync(orgId, line.SourceLotId, ct)
                        .ConfigureAwait(false);
                    expiry = sourceLot?.ExpirationDate;
                    lotNumber ??= sourceLot?.LotNumber;
                }

                if (expiry is DateOnly lotExpiry)
                {
                    var destLot = await _lots
                        .ReceiveAsync(
                            orgId,
                            line.ProductId,
                            lotExpiry,
                            waveQty,
                            actorId,
                            utcNow,
                            StockMovementType.TransferIn,
                            StockMovementSourceType.InventoryTransfer,
                            transfer.DestinationBranchId,
                            lotNumber,
                            receipt.Id.Value,
                            movement.Id.Value,
                            ct)
                        .ConfigureAwait(false);
                    movement = movement.WithLot(destLot.Id);
                }

                account.ApplyMovementEffect(movement.QuantityEffect);
                account.Touch(utcNow);
                var destBalance = InventoryTransferStock.EnsureBalance(
                    orgId,
                    transfer.DestinationBranchId,
                    line.ProductId,
                    balances,
                    utcNow);
                destBalance.Apply(movement.QuantityEffect, utcNow);
                await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(destBalance, ct).ConfigureAwait(false);
            }

            var decisionByLineId = receiveDrafts
                .Where(d => d.LineId is not null)
                .ToDictionary(d => d.LineId!.Value, d => d.DamagedCustodyDecision);
            var decisionByProduct = receiveDrafts
                .GroupBy(d => d.ProductId.Value)
                .ToDictionary(g => g.Key, g => g.Last().DamagedCustodyDecision);

            foreach (var receiptLine in receipt.Lines)
            {
                if (receiptLine.QuantityDamaged <= 0m)
                {
                    continue;
                }

                if (!lineById.TryGetValue(receiptLine.TransferLineId, out var line))
                {
                    continue;
                }

                if (!catalog.TryGetValue(line.ProductId.Value, out var product))
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        ApplicationErrorCodes.InventoryProductNotFound,
                        $"Product '{line.NameSnapshot}' was not found.");
                }

                if (!accounts.TryGetValue(line.ProductId.Value, out var account))
                {
                    account = InventoryAccount.CreateUntracked(orgId, line.ProductId, utcNow);
                    account.Enable(0m, product.UnitOfMeasure, actorId, utcNow, hasOpeningStockAlready: false, product.SellingMode);
                    await _inventory.AddAccountAsync(account, ct).ConfigureAwait(false);
                    accounts[line.ProductId.Value] = account;
                }

                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            receiptLine.Id.Value,
                            line.ProductId,
                            StockMovementType.TransferDamageHold,
                            line.SourceLotId,
                            ct)
                        .ConfigureAwait(false))
                {
                    continue;
                }

                decisionByLineId.TryGetValue(receiptLine.TransferLineId.Value, out var requestedDecision);
                if (requestedDecision is null)
                {
                    decisionByProduct.TryGetValue(line.ProductId.Value, out requestedDecision);
                }

                InventoryTransferDamagedCustodyDecision decision;
                try
                {
                    decision = InventoryTransferDamagedCustodyDecisions.Resolve(
                        transfer.DamageHandlingPolicy,
                        requestedDecision);
                }
                catch (DomainException ex)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
                }

                var followUp = receiptLine.DamagedFollowUp
                    ?? InventoryTransferDiscrepancyFollowUp.RequestReplacement;
                var custody = InventoryTransferDamageCustody.Open(
                    orgId,
                    transfer.Id,
                    transfer.FamilyRootId,
                    receiptLine.Id,
                    line.ProductId,
                    receiptLine.QuantityDamaged,
                    decision,
                    followUp,
                    transfer.DestinationBranchId,
                    actorId,
                    utcNow);

                var holdMovement = StockMovement.TransferDamageCustody(
                    orgId,
                    line.ProductId,
                    account.Id,
                    transfer.DestinationBranchId,
                    StockMovementType.TransferDamageHold,
                    receiptLine.QuantityDamaged,
                    line.UnitOfMeasure,
                    receiptLine.Id.Value,
                    transfer.TransferNumber!,
                    actorId,
                    utcNow,
                    sellingMode: product.SellingMode,
                    decisionDetail: StockMovementPresentation.FormatDamageHoldDecisionDetail(
                        decision,
                        followUp));

                var destBalance = InventoryTransferStock.EnsureBalance(
                    orgId,
                    transfer.DestinationBranchId,
                    line.ProductId,
                    balances,
                    utcNow);
                // Damaged is physical at destination but never sellable.
                // Park in DamagedQuantity (not InspectionHold). Destination does not re-inspect.
                destBalance.Apply(holdMovement.QuantityEffect, utcNow);
                destBalance.IncreaseDamaged(receiptLine.QuantityDamaged, utcNow);

                await _damageCustodies.AddAsync(custody, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(holdMovement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(destBalance, ct).ConfigureAwait(false);
            }

            if (transfer.StockRequestId is StockRequestId stockRequestId)
            {
                var stockRequest = await _stockRequests
                    .GetByIdAsync(orgId, stockRequestId, ct)
                    .ConfigureAwait(false);
                if (stockRequest is not null)
                {
                    var linkedTransfers = await _transfers
                        .ListByStockRequestIdAsync(orgId, stockRequestId, ct)
                        .ConfigureAwait(false);
                    var coverage = StockRequestDispatchCoverage.Compute(stockRequest, linkedTransfers);
                    stockRequest.RecalculateStatusFromFulfillmentCoverage(
                        coverage.ReceivedByProduct,
                        coverage.WaivedByProduct,
                        utcNow);
                    await _stockRequests.UpdateAsync(stockRequest, ct).ConfigureAwait(false);

                    if (stockRequest.Status is StockRequestStatus.Fulfilled or StockRequestStatus.PartiallyFulfilled)
                    {
                        var relatedType = stockRequest.Status == StockRequestStatus.Fulfilled
                            ? StockRequestNotificationTypes.Received
                            : StockRequestNotificationTypes.PartiallyReceived;
                        var title = stockRequest.Status == StockRequestStatus.Fulfilled
                            ? "Stock request received"
                            : "Stock request partially received";
                        var preview = stockRequest.Status == StockRequestStatus.Fulfilled
                            ? $"{stockRequest.RequestNumber ?? stockRequest.Id.Value.ToString("D")} was fully received."
                            : $"{stockRequest.RequestNumber ?? stockRequest.Id.Value.ToString("D")} was partially received.";
                        await _notifications
                            .PublishAsync(
                                organizationId,
                                organizationId,
                                relatedType,
                                stockRequest.Id.Value.ToString("D"),
                                title,
                                preview,
                                ct,
                                stockRequest.RequestedSourceLocationId.Value)
                            .ConfigureAwait(false);
                    }
                }
            }

            await _transfers.UpdateAsync(transfer, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            var message = transfer.Status == InventoryTransferStatus.PartiallyReceived
                ? $"Transfer {transfer.TransferNumber} partially received. {transfer.TotalDifferenceQty} units short."
                : $"Transfer {transfer.TransferNumber} received.";
            await _alerts.PublishAsync(
                    new InventoryTransferAlert(
                        transfer.Status == InventoryTransferStatus.PartiallyReceived ? "partially-received" : "received",
                        organizationId,
                        transfer.SourceBranchId.Value,
                        transfer.Id.Value,
                        transfer.TransferNumber!,
                        message,
                        transfer.StockRequestId?.Value),
                    ct)
                .ConfigureAwait(false);
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CloseRemainderInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IStockRequestRepository _stockRequests;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CloseRemainderInventoryTransfer(
        IInventoryTransferRepository transfers,
        IOrganizationBranchDirectory branches,
        IStockRequestRepository stockRequests,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _branches = branches;
        _stockRequests = stockRequests;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransfer>> ExecuteAsync(
        Guid organizationId,
        Guid transferId,
        CloseRemainderInventoryTransferRequest request,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to close transfer remainder.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var transfer = await _transfers
                    .GetByIdAsync(orgId, InventoryTransferId.From(transferId), ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                if (actingBranchId != transfer.DestinationBranchId.Value)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Only the destination branch can close remaining transfer quantity.");
                }

                var destOk = await _branches
                    .ExistsInOrganizationAsync(organizationId, transfer.DestinationBranchId.Value, ct)
                    .ConfigureAwait(false);
                if (!destOk)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchNotFound,
                        "Destination branch was not found in this organization.");
                }

                InventoryTransferDiscrepancyReason? transferLevelReason = null;
                if (!string.IsNullOrWhiteSpace(request.DiscrepancyReason))
                {
                    if (!InventoryTransferDiscrepancyReasons.TryParse(request.DiscrepancyReason, out var parsed))
                    {
                        return ApplicationResult<InventoryTransfer>.Failure(
                            DomainErrorCodes.InvalidInventoryTransferDiscrepancyReason,
                            "Discrepancy reason is not recognized.");
                    }

                    transferLevelReason = parsed;
                }

                var closeDrafts = new List<InventoryTransferCloseRemainderLineDraft>();
                foreach (var line in request.Lines ?? [])
                {
                    if (!InventoryTransferDiscrepancyReasons.TryParse(line.DiscrepancyReason, out var reason))
                    {
                        return ApplicationResult<InventoryTransfer>.Failure(
                            DomainErrorCodes.InvalidInventoryTransferDiscrepancyReason,
                            "Discrepancy reason is not recognized.");
                    }

                    closeDrafts.Add(new InventoryTransferCloseRemainderLineDraft(
                        reason,
                        line.DiscrepancyNote,
                        LineId: line.LineId is null ? null : InventoryTransferLineId.From(line.LineId.Value),
                        ProductId: line.ProductId is null ? null : CatalogProductId.From(line.ProductId.Value)));
                }

                try
                {
                    var utcNow = _clock.UtcNow;
                    transfer.CloseRemainder(
                        actorId,
                        utcNow,
                        closeDrafts,
                        transferLevelReason,
                        request.DiscrepancyNote);

                    if (transfer.StockRequestId is StockRequestId stockRequestId)
                    {
                        var stockRequest = await _stockRequests
                            .GetByIdAsync(orgId, stockRequestId, ct)
                            .ConfigureAwait(false);
                        if (stockRequest is not null)
                        {
                            var linkedTransfers = await _transfers
                                .ListByStockRequestIdAsync(orgId, stockRequestId, ct)
                                .ConfigureAwait(false);
                            var coverage = StockRequestDispatchCoverage.Compute(stockRequest, linkedTransfers);
                            stockRequest.RecalculateStatusFromFulfillmentCoverage(
                                coverage.ReceivedByProduct,
                                coverage.WaivedByProduct,
                                utcNow);
                            await _stockRequests.UpdateAsync(stockRequest, ct).ConfigureAwait(false);
                        }
                    }

                    await _transfers.UpdateAsync(transfer, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return ApplicationResult<InventoryTransfer>.Success(transfer);
                }
                catch (DomainException ex)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
                }
                catch (PersistenceConflictException ex)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class CancelInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly InventoryLotStockService _lots;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelInventoryTransfer(
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        InventoryLotStockService lots,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _lots = lots;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransfer>> ExecuteAsync(
        Guid organizationId,
        Guid transferId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to cancel a transfer.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
        var orgId = PosOrganizationId.From(organizationId);
        var transfer = await _transfers
            .GetByIdAsync(orgId, InventoryTransferId.From(transferId), ct)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferNotFound,
                "Inventory transfer was not found.");
        }

        if (transfer.Status == InventoryTransferStatus.Cancelled)
        {
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }

        var branchGuard = await InventoryTransferAuthorization
            .EnsureSourceActorAsync(_branches, organizationId, transfer.SourceBranchId.Value, transfer.DestinationBranchId.Value, actingBranchId, ct)
            .ConfigureAwait(false);
        if (branchGuard is not null)
        {
            return branchGuard;
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var wasInTransit = transfer.Status == InventoryTransferStatus.InTransit;
            transfer.Cancel(actorId, utcNow);

            if (wasInTransit)
            {
                var productIds = transfer.Lines.Select(l => l.ProductId).ToList();
                var accounts = (await _inventory.ListByProductIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
                    .ToDictionary(a => a.ProductId.Value);
                var catalog = (await _products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
                    .ToDictionary(p => p.Id.Value);
                var balances = (await _balances.ListByProductIdsAsync(orgId, productIds, ct).ConfigureAwait(false))
                    .ToList();

                foreach (var line in transfer.Lines)
                {
                    if (await _inventory
                            .HasInventoryTransferMovementAsync(orgId, transfer.Id, line.ProductId, StockMovementType.TransferCancelRestore, line.SourceLotId, ct)
                            .ConfigureAwait(false))
                    {
                        continue;
                    }

                    if (!accounts.TryGetValue(line.ProductId.Value, out var account))
                    {
                        continue;
                    }

                    var sellingMode = catalog.TryGetValue(line.ProductId.Value, out var product)
                        ? product.SellingMode
                        : SellingMode.PerItem;
                    var movement = StockMovement.TransferCancelRestore(
                        orgId,
                        line.ProductId,
                        account.Id,
                        transfer.SourceBranchId,
                        line.SentQty,
                        line.UnitOfMeasure,
                        transfer.Id.Value,
                        transfer.TransferNumber!,
                        actorId,
                        utcNow,
                        sellingMode: sellingMode);
                    account.ApplyMovementEffect(movement.QuantityEffect);
                    account.Touch(utcNow);
                    var sourceBalance = InventoryTransferStock.EnsureBalance(
                        orgId,
                        transfer.SourceBranchId,
                        line.ProductId,
                        balances,
                        utcNow);
                    sourceBalance.Apply(movement.QuantityEffect, utcNow);
                    await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                    await _balances.UpsertAsync(sourceBalance, ct).ConfigureAwait(false);
                }

                await _lots
                    .RestoreSourceAsync(
                        orgId,
                        transfer.Id.Value,
                        StockMovementType.TransferOut,
                        StockMovementType.TransferCancelRestore,
                        actorId,
                        utcNow,
                        ct)
                    .ConfigureAwait(false);
            }

            await _transfers.UpdateAsync(transfer, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class InventoryTransferAuthorization
{
    public static Task<ApplicationResult<InventoryTransfer>?> EnsureSourceBranchAsync(
        IOrganizationBranchDirectory branches,
        Guid organizationId,
        Guid sourceBranchId,
        Guid destinationBranchId,
        Guid actingBranchId,
        CancellationToken cancellationToken) =>
        EnsureSourceActorAsync(branches, organizationId, sourceBranchId, destinationBranchId, actingBranchId, cancellationToken);

    public static async Task<ApplicationResult<InventoryTransfer>?> EnsureSourceActorAsync(
        IOrganizationBranchDirectory branches,
        Guid organizationId,
        Guid sourceBranchId,
        Guid destinationBranchId,
        Guid actingBranchId,
        CancellationToken cancellationToken)
    {
        if (actingBranchId != sourceBranchId)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchForbidden,
                "Only the source branch can create, dispatch, or cancel this transfer.");
        }

        if (!await branches.ExistsInOrganizationAsync(organizationId, sourceBranchId, cancellationToken).ConfigureAwait(false)
            || !await branches.ExistsInOrganizationAsync(organizationId, destinationBranchId, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferBranchNotFound,
                "Source and destination branches must belong to the same organization.");
        }

        return null;
    }
}

internal static class InventoryTransferLineFactory
{
    public static async Task<ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>> CreateDraftsAsync(
        ICatalogProductRepository products,
        IInventoryLotRepository lots,
        PosOrganizationId organizationId,
        IReadOnlyList<InventoryTransferLineRequest>? lines,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>.Failure(
                DomainErrorCodes.InventoryTransferRequiresLines,
                "A transfer must contain at least one line.");
        }

        var productIds = lines.Select(l => CatalogProductId.From(l.ProductId)).ToList();
        var catalog = (await products.ListByIdsAsync(organizationId, productIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(p => p.Id.Value);

        var drafts = new List<InventoryTransferLineDraft>(lines.Count);
        foreach (var line in lines)
        {
            if (!catalog.TryGetValue(line.ProductId, out var product))
            {
                return ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>.Failure(
                    ApplicationErrorCodes.InventoryProductNotFound,
                    "Product was not found.");
            }

            if (product.Status != CatalogProductStatus.Active)
            {
                return ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>.Failure(
                    DomainErrorCodes.ProductNotActive,
                    $"Product '{product.Name}' is not active.");
            }

            InventoryLotId? sourceLotId = null;
            string? lotNumber = null;
            DateOnly? expirationDate = null;
            if (product.TracksExpiration)
            {
                if (line.SourceLotId is null)
                {
                    return ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>.Failure(
                        DomainErrorCodes.InventoryLotMismatch,
                        $"A source lot is required to transfer '{product.Name}'.");
                }

                var lot = await lots
                    .GetByIdAsync(organizationId, InventoryLotId.From(line.SourceLotId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (lot is null || lot.ProductId != product.Id)
                {
                    return ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>.Failure(
                        DomainErrorCodes.InventoryLotMismatch,
                        $"Lot does not belong to '{product.Name}'.");
                }

                sourceLotId = lot.Id;
                lotNumber = lot.LotNumber;
                expirationDate = lot.ExpirationDate;
            }

            drafts.Add(new InventoryTransferLineDraft(
                product.Id,
                line.Quantity,
                product.Name,
                product.UnitOfMeasure,
                product.SellingMode,
                sourceLotId,
                lotNumber,
                expirationDate));
        }

        return ApplicationResult<IReadOnlyList<InventoryTransferLineDraft>>.Success(drafts);
    }
}

internal readonly record struct InventoryTransferStockDemand(
    CatalogProductId ProductId,
    decimal SentQty,
    string NameSnapshot,
    UnitOfMeasure UnitOfMeasure,
    InventoryLotId? SourceLotId);

internal static class InventoryTransferStock
{
    public static ApplicationResult<InventoryTransfer>? ValidateSourceAvailability(
        PosOrganizationId organizationId,
        PosBranchId sourceBranchId,
        string sourceBranchName,
        IReadOnlyList<InventoryTransferStockDemand> lines,
        IReadOnlyDictionary<Guid, InventoryAccount> accounts,
        List<InventoryBranchBalance> balances,
        IReadOnlyDictionary<Guid, InventoryLot> lotsById,
        DateTimeOffset utcNow)
    {
        foreach (var productGroup in lines.GroupBy(l => l.ProductId.Value))
        {
            var sample = productGroup.First();
            if (!accounts.TryGetValue(productGroup.Key, out var account) || !account.IsTracked)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    ApplicationErrorCodes.InventoryTransferProductNotTracked,
                    $"Inventory is not tracked for '{sample.NameSnapshot}'.");
            }

            var requested = productGroup.Sum(l => l.SentQty);
            var sourceBalance = EnsureSourceBalance(
                organizationId,
                sourceBranchId,
                sample.ProductId,
                account.OnHandQuantity,
                balances,
                utcNow);
            var available = Math.Min(sourceBalance.OnHandQuantity, account.OnHandQuantity);
            var unit = UnitOfMeasures.ToCode(sample.UnitOfMeasure);
            if (available <= 0m)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    ApplicationErrorCodes.InsufficientStock,
                    $"{sample.NameSnapshot} is out of stock at {sourceBranchName}.");
            }

            if (available < requested)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    ApplicationErrorCodes.InsufficientStock,
                    $"{sample.NameSnapshot} has only {available:0.####} {unit} available at {sourceBranchName}. Requested: {requested:0.####}.");
            }
        }

        foreach (var lotGroup in lines
                     .Where(l => l.SourceLotId is not null)
                     .GroupBy(l => l.SourceLotId!.Value))
        {
            var sample = lotGroup.First();
            var requested = lotGroup.Sum(l => l.SentQty);
            if (!lotsById.TryGetValue(lotGroup.Key, out var lot))
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    DomainErrorCodes.InventoryLotMismatch,
                    $"Lot was not found for '{sample.NameSnapshot}'.");
            }

            if (lot.BranchId is not null && lot.BranchId != sourceBranchId)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    DomainErrorCodes.InventoryLotMismatch,
                    $"Lot for '{sample.NameSnapshot}' does not belong to {sourceBranchName}.");
            }

            if (lot.QuantityOnHand < requested)
            {
                var unit = UnitOfMeasures.ToCode(sample.UnitOfMeasure);
                var lotLabel = string.IsNullOrWhiteSpace(lot.LotNumber) ? "selected lot" : lot.LotNumber;
                if (lot.QuantityOnHand <= 0m)
                {
                    return ApplicationResult<InventoryTransfer>.Failure(
                        ApplicationErrorCodes.InsufficientStock,
                        $"{sample.NameSnapshot} lot '{lotLabel}' is out of stock at {sourceBranchName}.");
                }

                return ApplicationResult<InventoryTransfer>.Failure(
                    ApplicationErrorCodes.InsufficientStock,
                    $"{sample.NameSnapshot} lot '{lotLabel}' has only {lot.QuantityOnHand:0.####} {unit} available at {sourceBranchName}. Requested: {requested:0.####}.");
            }
        }

        return null;
    }

    public static InventoryBranchBalance EnsureSourceBalance(
        PosOrganizationId organizationId,
        PosBranchId sourceBranchId,
        CatalogProductId productId,
        decimal organizationOnHand,
        List<InventoryBranchBalance> balances,
        DateTimeOffset utcNow)
    {
        var existing = balances.FirstOrDefault(b =>
            b.BranchId == sourceBranchId && b.ProductId == productId);
        if (existing is not null)
        {
            return existing;
        }

        var other = balances
            .Where(b => b.ProductId == productId && b.BranchId != sourceBranchId)
            .Sum(b => b.OnHandQuantity);
        var seed = Math.Max(0m, organizationOnHand - other);
        var created = InventoryBranchBalance.Create(organizationId, sourceBranchId, productId, seed, utcNow);
        balances.Add(created);
        return created;
    }

    public static InventoryBranchBalance EnsureBalance(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        List<InventoryBranchBalance> balances,
        DateTimeOffset utcNow)
    {
        var existing = balances.FirstOrDefault(b => b.BranchId == branchId && b.ProductId == productId);
        if (existing is not null)
        {
            return existing;
        }

        var created = InventoryBranchBalance.Create(organizationId, branchId, productId, 0m, utcNow);
        balances.Add(created);
        return created;
    }
}

public sealed class NoOpInventoryTransferAlertSink : IInventoryTransferAlertSink
{
    public Task PublishAsync(InventoryTransferAlert alert, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

/// <summary>
/// Publishes non-stock-request transfer alerts into the organization inbox.
/// Stock-request-linked transfers skip here; those use <see cref="StockRequestNotificationTypes"/> instead.
/// </summary>
public sealed class OrganizationBusinessInventoryTransferAlertSink : IInventoryTransferAlertSink
{
    private readonly IOrganizationBusinessNotificationPublisher _notifications;

    public OrganizationBusinessInventoryTransferAlertSink(IOrganizationBusinessNotificationPublisher notifications) =>
        _notifications = notifications;

    public async Task PublishAsync(InventoryTransferAlert alert, CancellationToken cancellationToken = default)
    {
        if (alert.StockRequestId is not null)
        {
            return;
        }

        var relatedType = alert.Kind switch
        {
            "dispatched" => InventoryTransferNotificationTypes.Dispatched,
            "partially-received" => InventoryTransferNotificationTypes.PartiallyReceived,
            _ => InventoryTransferNotificationTypes.Received
        };
        var title = alert.Kind switch
        {
            "dispatched" => "Inventory transfer dispatched",
            "partially-received" => "Inventory transfer partially received",
            _ => "Inventory transfer received"
        };

        await _notifications
            .PublishAsync(
                alert.OrganizationId,
                alert.OrganizationId,
                relatedType,
                alert.TransferId.ToString("D"),
                title,
                alert.Message,
                cancellationToken,
                alert.TargetBranchId)
            .ConfigureAwait(false);
    }
}
