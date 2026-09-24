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

public sealed class DispatchInventoryTransferExceptionReturn
{
    private readonly IInventoryTransferExceptionCustodyRepository _custodies;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DispatchInventoryTransferExceptionReturn(
        IInventoryTransferExceptionCustodyRepository custodies,
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _custodies = custodies;
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransferExceptionCustodyDto>> ExecuteAsync(
        Guid organizationId,
        Guid custodyId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var custody = await _custodies
                    .GetByIdAsync(orgId, InventoryTransferExceptionCustodyId.From(custodyId), ct)
                    .ConfigureAwait(false);
                if (custody is null)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferExceptionCustodyId,
                        "Exception custody was not found.");
                }

                var transfer = await _transfers
                    .GetByIdAsync(orgId, custody.TransferId, ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                if (actingBranchId != transfer.DestinationBranchId.Value)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Only the destination branch can dispatch an exception return.");
                }

                var utcNow = _clock.UtcNow;
                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ActualProductId,
                            StockMovementType.TransferExceptionReturnOut,
                            null,
                            ct)
                        .ConfigureAwait(false))
                {
                    if (custody.Status is InventoryTransferExceptionCustodyStatus.AwaitingReturn
                        or InventoryTransferExceptionCustodyStatus.HeldAtDestination)
                    {
                        custody.MarkReturnDispatched(actorId, utcNow, transfer.SourceBranchId);
                        await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    }

                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Success(Map(custody));
                }

                custody.MarkReturnDispatched(actorId, utcNow, transfer.SourceBranchId);

                var account = await EnsureAccountAsync(orgId, custody.ActualProductId, actorId, utcNow, ct)
                    .ConfigureAwait(false);
                var product = await _products.GetByIdAsync(orgId, custody.ActualProductId, ct).ConfigureAwait(false);
                var uom = product?.UnitOfMeasure ?? UnitOfMeasure.Piece;
                var sellingMode = product?.SellingMode ?? SellingMode.PerItem;

                var movement = StockMovement.TransferExceptionCustody(
                    orgId,
                    custody.ActualProductId,
                    account.Id,
                    transfer.DestinationBranchId,
                    StockMovementType.TransferExceptionReturnOut,
                    custody.Quantity,
                    uom,
                    custody.Id.Value,
                    transfer.TransferNumber ?? transfer.Id.Value.ToString("D"),
                    actorId,
                    utcNow,
                    sellingMode: sellingMode);

                var balances = (await _balances
                        .ListByProductIdsAsync(orgId, [custody.ActualProductId], ct)
                        .ConfigureAwait(false))
                    .ToList();
                var dest = InventoryTransferStock.EnsureBalance(
                    orgId,
                    transfer.DestinationBranchId,
                    custody.ActualProductId,
                    balances,
                    utcNow);
                dest.DecreaseInspectionHold(custody.Quantity, utcNow);
                dest.Apply(movement.QuantityEffect, utcNow);

                await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(dest, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<InventoryTransferExceptionCustodyDto>.Success(Map(custody));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<InventoryAccount> EnsureAccountAsync(
        PosOrganizationId orgId,
        CatalogProductId productId,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken ct)
    {
        var account = await _inventory.GetByProductIdAsync(orgId, productId, ct).ConfigureAwait(false);
        if (account is not null)
        {
            return account;
        }

        var product = await _products.GetByIdAsync(orgId, productId, ct).ConfigureAwait(false)
            ?? throw new DomainException(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        account = InventoryAccount.CreateUntracked(orgId, productId, utcNow);
        account.Enable(0m, product.UnitOfMeasure, actorId, utcNow, hasOpeningStockAlready: false, product.SellingMode);
        await _inventory.AddAccountAsync(account, ct).ConfigureAwait(false);
        return account;
    }

    internal static InventoryTransferExceptionCustodyDto Map(InventoryTransferExceptionCustody c) =>
        InventoryTransferExceptionCustodyMapping.Map(c);
}

public sealed class ReceiveInventoryTransferExceptionReturn
{
    private readonly IInventoryTransferExceptionCustodyRepository _custodies;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceiveInventoryTransferExceptionReturn(
        IInventoryTransferExceptionCustodyRepository custodies,
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _custodies = custodies;
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransferExceptionCustodyDto>> ExecuteAsync(
        Guid organizationId,
        Guid custodyId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var custody = await _custodies
                    .GetByIdAsync(orgId, InventoryTransferExceptionCustodyId.From(custodyId), ct)
                    .ConfigureAwait(false);
                if (custody is null)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferExceptionCustodyId,
                        "Exception custody was not found.");
                }

                var transfer = await _transfers
                    .GetByIdAsync(orgId, custody.TransferId, ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                if (actingBranchId != transfer.SourceBranchId.Value)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Only the source branch can receive an exception return.");
                }

                var utcNow = _clock.UtcNow;
                var directSellable =
                    TransferExceptionCustodyPolicy.RestoresDirectlyToSellableOnSourceReceive(custody.ReasonCode);
                var returnInType = directSellable
                    ? StockMovementType.TransferExceptionReturnRestock
                    : StockMovementType.TransferExceptionReturnIn;

                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ActualProductId,
                            returnInType,
                            null,
                            ct)
                        .ConfigureAwait(false))
                {
                    // Heal custody if a prior attempt posted the movement but status lagged.
                    if (custody.Status == InventoryTransferExceptionCustodyStatus.ReturnInTransit)
                    {
                        custody.MarkReturnReceivedAtSource(actorId, utcNow);
                        await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    }

                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Success(
                        InventoryTransferExceptionCustodyMapping.Map(custody));
                }

                custody.MarkReturnReceivedAtSource(actorId, utcNow);

                var account = await _inventory.GetByProductIdAsync(orgId, custody.ActualProductId, ct).ConfigureAwait(false)
                    ?? InventoryAccount.CreateUntracked(orgId, custody.ActualProductId, utcNow);
                var product = await _products.GetByIdAsync(orgId, custody.ActualProductId, ct).ConfigureAwait(false);
                var uom = product?.UnitOfMeasure ?? UnitOfMeasure.Piece;
                var sellingMode = product?.SellingMode ?? SellingMode.PerItem;

                var movement = StockMovement.TransferExceptionCustody(
                    orgId,
                    custody.ActualProductId,
                    account.Id,
                    transfer.SourceBranchId,
                    returnInType,
                    custody.Quantity,
                    uom,
                    custody.Id.Value,
                    transfer.TransferNumber ?? transfer.Id.Value.ToString("D"),
                    actorId,
                    utcNow,
                    sellingMode: sellingMode);

                var balances = (await _balances
                        .ListByProductIdsAsync(orgId, [custody.ActualProductId], ct)
                        .ConfigureAwait(false))
                    .ToList();
                var source = InventoryTransferStock.EnsureBalance(
                    orgId,
                    transfer.SourceBranchId,
                    custody.ActualProductId,
                    balances,
                    utcNow);
                source.Apply(movement.QuantityEffect, utcNow);
                if (!directSellable)
                {
                    source.IncreaseInspectionHold(custody.Quantity, utcNow);
                }

                await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(source, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<InventoryTransferExceptionCustodyDto>.Success(
                    InventoryTransferExceptionCustodyMapping.Map(custody));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class InspectInventoryTransferExceptionCustody
{
    private readonly IInventoryTransferExceptionCustodyRepository _custodies;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public InspectInventoryTransferExceptionCustody(
        IInventoryTransferExceptionCustodyRepository custodies,
        IInventoryTransferRepository transfers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _custodies = custodies;
        _transfers = transfers;
        _inventory = inventory;
        _balances = balances;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransferExceptionCustodyDto>> ExecuteAsync(
        Guid organizationId,
        Guid custodyId,
        InspectInventoryTransferExceptionCustodyRequest request,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var custody = await _custodies
                    .GetByIdAsync(orgId, InventoryTransferExceptionCustodyId.From(custodyId), ct)
                    .ConfigureAwait(false);
                if (custody is null)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferExceptionCustodyId,
                        "Exception custody was not found.");
                }

                var transfer = await _transfers
                    .GetByIdAsync(orgId, custody.TransferId, ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                if (custody.Decision == InventoryTransferExceptionCustodyDecision.KeepAtDestination)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferExceptionCustodyStatus,
                        "Destination-held exception goods are not source-inspected. Only returned exception custody is inspected at source.");
                }

                if (actingBranchId != transfer.SourceBranchId.Value)
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Exception custody must be inspected at the source branch.");
                }

                var utcNow = _clock.UtcNow;
                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ActualProductId,
                            StockMovementType.TransferExceptionRecovery,
                            null,
                            ct)
                        .ConfigureAwait(false)
                    || await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ActualProductId,
                            StockMovementType.TransferExceptionWriteOff,
                            null,
                            ct)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<InventoryTransferExceptionCustodyDto>.Success(
                        InventoryTransferExceptionCustodyMapping.Map(custody));
                }

                if (custody.Decision == InventoryTransferExceptionCustodyDecision.ReturnToSource
                    && custody.Status == InventoryTransferExceptionCustodyStatus.ReceivedAtSource)
                {
                    custody.MarkReadyForSourceInspection(actorId, utcNow);
                }

                custody.Inspect(
                    request.RecoveredSellableQty,
                    request.ConfirmedNonSellableQty,
                    actorId,
                    utcNow);

                var account = await _inventory.GetByProductIdAsync(orgId, custody.ActualProductId, ct).ConfigureAwait(false);
                if (account is null)
                {
                    var catalogProduct = await _products.GetByIdAsync(orgId, custody.ActualProductId, ct).ConfigureAwait(false)
                        ?? throw new DomainException(
                            ApplicationErrorCodes.InventoryProductNotFound,
                            "Product was not found.");
                    account = InventoryAccount.CreateUntracked(orgId, custody.ActualProductId, utcNow);
                    account.Enable(
                        0m,
                        catalogProduct.UnitOfMeasure,
                        actorId,
                        utcNow,
                        hasOpeningStockAlready: false,
                        catalogProduct.SellingMode);
                    await _inventory.AddAccountAsync(account, ct).ConfigureAwait(false);
                }

                var product = await _products.GetByIdAsync(orgId, custody.ActualProductId, ct).ConfigureAwait(false);
                var uom = product?.UnitOfMeasure ?? UnitOfMeasure.Piece;
                var sellingMode = product?.SellingMode ?? SellingMode.PerItem;
                var transferNumber = transfer.TransferNumber ?? transfer.Id.Value.ToString("D");
                var holdBranch = transfer.SourceBranchId;

                var balances = (await _balances
                        .ListByProductIdsAsync(orgId, [custody.ActualProductId], ct)
                        .ConfigureAwait(false))
                    .ToList();
                var balance = InventoryTransferStock.EnsureBalance(
                    orgId,
                    holdBranch,
                    custody.ActualProductId,
                    balances,
                    utcNow);

                if (custody.RecoveredSellableQty > 0m)
                {
                    var recovery = StockMovement.TransferExceptionCustody(
                        orgId,
                        custody.ActualProductId,
                        account.Id,
                        holdBranch,
                        StockMovementType.TransferExceptionRecovery,
                        custody.RecoveredSellableQty,
                        uom,
                        custody.Id.Value,
                        transferNumber,
                        actorId,
                        utcNow,
                        sellingMode: sellingMode);
                    balance.DecreaseInspectionHold(custody.RecoveredSellableQty, utcNow);
                    account.ApplyMovementEffect(recovery.QuantityEffect);
                    account.Touch(utcNow);
                    await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                    await _inventory.AddMovementAsync(recovery, ct).ConfigureAwait(false);
                }

                if (custody.ConfirmedNonSellableQty > 0m)
                {
                    var writeOff = StockMovement.TransferExceptionCustody(
                        orgId,
                        custody.ActualProductId,
                        account.Id,
                        holdBranch,
                        StockMovementType.TransferExceptionWriteOff,
                        custody.ConfirmedNonSellableQty,
                        uom,
                        custody.Id.Value,
                        transferNumber,
                        actorId,
                        utcNow,
                        sellingMode: sellingMode);
                    balance.DecreaseInspectionHold(custody.ConfirmedNonSellableQty, utcNow);
                    balance.IncreaseDamaged(custody.ConfirmedNonSellableQty, utcNow);
                    await _inventory.AddMovementAsync(writeOff, ct).ConfigureAwait(false);
                }

                await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(balance, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<InventoryTransferExceptionCustodyDto>.Success(
                    InventoryTransferExceptionCustodyMapping.Map(custody));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferExceptionCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class InventoryTransferExceptionCustodyMapping
{
    public static InventoryTransferExceptionCustodyDto Map(
        InventoryTransferExceptionCustody c,
        string? expectedProductName = null,
        string? actualProductName = null) =>
        new(
            c.Id.Value,
            c.TransferId.Value,
            c.RootTransferId.Value,
            c.ReceiptLineId.Value,
            c.ExpectedProductId.Value,
            c.ActualProductId.Value,
            c.Quantity,
            c.ReasonCode,
            InventoryTransferExceptionCustodyDecisions.ToCode(c.Decision),
            InventoryTransferDiscrepancyFollowUps.ToCode(c.FollowUpIntent),
            InventoryTransferExceptionCustodyStatuses.ToCode(c.Status),
            c.HeldBranchId.Value,
            c.RecoveredSellableQty,
            c.ConfirmedNonSellableQty,
            c.ReplacementDemandQty,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.ReturnDispatchedAtUtc,
            c.ReturnReceivedAtUtc,
            c.InspectedAtUtc,
            expectedProductName,
            actualProductName);
}
