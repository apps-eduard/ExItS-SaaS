using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
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
    private readonly IOrganizationBranchDirectory _branches;

    public InventoryTransferQueryService(
        IInventoryTransferRepository transfers,
        IOrganizationBranchDirectory branches)
    {
        _transfers = transfers;
        _branches = branches;
    }

    public async Task<InventoryTransferDto?> GetByIdAsync(
        Guid organizationId,
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _transfers
            .GetByIdAsync(PosOrganizationId.From(organizationId), InventoryTransferId.From(transferId), cancellationToken)
            .ConfigureAwait(false);
        if (transfer is null)
        {
            return null;
        }

        var names = await _branches
            .GetNamesAsync(organizationId, [transfer.SourceBranchId.Value, transfer.DestinationBranchId.Value], cancellationToken)
            .ConfigureAwait(false);
        return Map(transfer, names);
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
        IReadOnlyDictionary<Guid, string> names) =>
        new(
            transfer.Id.Value,
            transfer.OrganizationId.Value,
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
            transfer.TotalSentQty,
            transfer.TotalReceivedQty,
            transfer.TotalDifferenceQty,
            transfer.Lines.Select(l => new InventoryTransferLineDto(
                l.Id.Value,
                l.ProductId.Value,
                l.NameSnapshot,
                l.UnitOfMeasure.ToString(),
                l.LineNumber,
                l.SentQty,
                l.ReceivedQty,
                l.DifferenceQty,
                l.LineStatus,
                l.DiscrepancyReason is null ? null : InventoryTransferDiscrepancyReasons.ToCode(l.DiscrepancyReason.Value),
                l.DiscrepancyNote,
                l.SourceLotId?.Value,
                l.LotNumber,
                l.ExpirationDate)).ToList());

    private static InventoryTransferListItemDto MapListItem(
        InventoryTransfer transfer,
        IReadOnlyDictionary<Guid, string> names) =>
        new(
            transfer.Id.Value,
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
            transfer.UpdatedAtUtc);
}

public sealed class CreateInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryLotRepository _lots;
    private readonly IOrganizationBranchDirectory _branches;
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

        try
        {
            var transfer = InventoryTransfer.CreateDraft(
                orgId,
                PosBranchId.From(request.SourceBranchId),
                PosBranchId.From(request.DestinationBranchId),
                drafts.Value!,
                actorId,
                _clock.UtcNow,
                request.Notes);

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
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _lotRepository = lotRepository;
        _lots = lots;
        _branches = branches;
        _alerts = alerts;
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

            var number = await _transfers
                .AllocateNextNumberAsync(orgId, InventoryTransferNumbers.BusinessDateOf(utcNow), ct)
                .ConfigureAwait(false);

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
                    sellingMode: sellingMode);
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
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
            await _alerts.PublishAsync(
                    new InventoryTransferAlert(
                        "dispatched",
                        organizationId,
                        transfer.DestinationBranchId.Value,
                        transfer.Id.Value,
                        transfer.TransferNumber!,
                        $"Inventory transfer {transfer.TransferNumber} is on the way."),
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
}

public sealed class ReceiveInventoryTransfer
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryLotRepository _lotRepository;
    private readonly InventoryLotStockService _lots;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IInventoryTransferAlertSink _alerts;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceiveInventoryTransfer(
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IInventoryLotRepository lotRepository,
        InventoryLotStockService lots,
        IOrganizationBranchDirectory branches,
        IInventoryTransferAlertSink alerts,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _lotRepository = lotRepository;
        _lots = lots;
        _branches = branches;
        _alerts = alerts;
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

        if (transfer.Status is InventoryTransferStatus.Received or InventoryTransferStatus.PartiallyReceived)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.InventoryTransferAlreadyReceived,
                "This transfer has already been received.");
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

            receiveDrafts.Add(new InventoryTransferReceiveLineDraft(
                CatalogProductId.From(line.ProductId),
                line.ReceivedQty,
                reason,
                line.DiscrepancyNote,
                LineId: line.LineId is null ? null : InventoryTransferLineId.From(line.LineId.Value)));
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
            transfer.Receive(receiveDrafts, actorId, utcNow);

            foreach (var line in transfer.Lines)
            {
                if (line.ReceivedQty <= 0m)
                {
                    continue;
                }

                if (await _inventory
                        .HasInventoryTransferMovementAsync(orgId, transfer.Id, line.ProductId, StockMovementType.TransferIn, line.SourceLotId, ct)
                        .ConfigureAwait(false))
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

                var movement = StockMovement.TransferIn(
                    orgId,
                    line.ProductId,
                    account.Id,
                    transfer.DestinationBranchId,
                    line.ReceivedQty,
                    line.UnitOfMeasure,
                    transfer.Id.Value,
                    transfer.TransferNumber!,
                    actorId,
                    utcNow,
                    sellingMode: product.SellingMode);
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
                            line.ReceivedQty,
                            actorId,
                            utcNow,
                            StockMovementType.TransferIn,
                            StockMovementSourceType.InventoryTransfer,
                            transfer.DestinationBranchId,
                            lotNumber,
                            transfer.Id.Value,
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
                        message),
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
