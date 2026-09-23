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

public sealed class DispatchInventoryTransferDamageReturn
{
    private readonly IInventoryTransferDamageCustodyRepository _custodies;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DispatchInventoryTransferDamageReturn(
        IInventoryTransferDamageCustodyRepository custodies,
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

    public async Task<ApplicationResult<InventoryTransferDamageCustodyDto>> ExecuteAsync(
        Guid organizationId,
        Guid custodyId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var custody = await _custodies
                    .GetByIdAsync(orgId, InventoryTransferDamageCustodyId.From(custodyId), ct)
                    .ConfigureAwait(false);
                if (custody is null)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDamageCustodyId,
                        "Damage custody was not found.");
                }

                var transfer = await _transfers
                    .GetByIdAsync(orgId, custody.TransferId, ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                if (actingBranchId != transfer.DestinationBranchId.Value)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Only the destination branch can dispatch a damage return.");
                }

                var utcNow = _clock.UtcNow;
                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ProductId,
                            StockMovementType.TransferDamageReturnOut,
                            null,
                            ct)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Success(Map(custody));
                }

                custody.MarkReturnDispatched(actorId, utcNow, transfer.SourceBranchId);

                var account = await EnsureAccountAsync(orgId, custody.ProductId, actorId, utcNow, ct)
                    .ConfigureAwait(false);
                var product = await _products.GetByIdAsync(orgId, custody.ProductId, ct).ConfigureAwait(false);
                var uom = product?.UnitOfMeasure ?? UnitOfMeasure.Piece;
                var sellingMode = product?.SellingMode ?? SellingMode.PerItem;

                var movement = StockMovement.TransferDamageCustody(
                    orgId,
                    custody.ProductId,
                    account.Id,
                    transfer.DestinationBranchId,
                    StockMovementType.TransferDamageReturnOut,
                    custody.Quantity,
                    uom,
                    custody.Id.Value,
                    transfer.TransferNumber ?? transfer.Id.Value.ToString("D"),
                    actorId,
                    utcNow,
                    sellingMode: sellingMode);

                var balances = (await _balances
                        .ListByProductIdsAsync(orgId, [custody.ProductId], ct)
                        .ConfigureAwait(false))
                    .ToList();
                var dest = InventoryTransferStock.EnsureBalance(
                    orgId,
                    transfer.DestinationBranchId,
                    custody.ProductId,
                    balances,
                    utcNow);
                // Damaged was parked in DamagedQuantity at destination; return clears that bucket.
                dest.DecreaseDamaged(custody.Quantity, utcNow);
                dest.Apply(movement.QuantityEffect, utcNow);

                await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(dest, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<InventoryTransferDamageCustodyDto>.Success(Map(custody));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(ex.ErrorCode, ex.Message);
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

    internal static InventoryTransferDamageCustodyDto Map(InventoryTransferDamageCustody c) =>
        InventoryTransferDamageCustodyMapping.Map(c);
}

public sealed class ReceiveInventoryTransferDamageReturn
{
    private readonly IInventoryTransferDamageCustodyRepository _custodies;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceiveInventoryTransferDamageReturn(
        IInventoryTransferDamageCustodyRepository custodies,
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

    public async Task<ApplicationResult<InventoryTransferDamageCustodyDto>> ExecuteAsync(
        Guid organizationId,
        Guid custodyId,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var custody = await _custodies
                    .GetByIdAsync(orgId, InventoryTransferDamageCustodyId.From(custodyId), ct)
                    .ConfigureAwait(false);
                if (custody is null)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDamageCustodyId,
                        "Damage custody was not found.");
                }

                var transfer = await _transfers
                    .GetByIdAsync(orgId, custody.TransferId, ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                if (actingBranchId != transfer.SourceBranchId.Value)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Only the source branch can receive a damage return.");
                }

                var utcNow = _clock.UtcNow;
                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ProductId,
                            StockMovementType.TransferDamageReturnIn,
                            null,
                            ct)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Success(
                        InventoryTransferDamageCustodyMapping.Map(custody));
                }

                custody.MarkReturnReceivedAtSource(actorId, utcNow);

                var account = await _inventory.GetByProductIdAsync(orgId, custody.ProductId, ct).ConfigureAwait(false)
                    ?? InventoryAccount.CreateUntracked(orgId, custody.ProductId, utcNow);
                var product = await _products.GetByIdAsync(orgId, custody.ProductId, ct).ConfigureAwait(false);
                var uom = product?.UnitOfMeasure ?? UnitOfMeasure.Piece;
                var sellingMode = product?.SellingMode ?? SellingMode.PerItem;

                var movement = StockMovement.TransferDamageCustody(
                    orgId,
                    custody.ProductId,
                    account.Id,
                    transfer.SourceBranchId,
                    StockMovementType.TransferDamageReturnIn,
                    custody.Quantity,
                    uom,
                    custody.Id.Value,
                    transfer.TransferNumber ?? transfer.Id.Value.ToString("D"),
                    actorId,
                    utcNow,
                    sellingMode: sellingMode);

                var balances = (await _balances
                        .ListByProductIdsAsync(orgId, [custody.ProductId], ct)
                        .ConfigureAwait(false))
                    .ToList();
                var source = InventoryTransferStock.EnsureBalance(
                    orgId,
                    transfer.SourceBranchId,
                    custody.ProductId,
                    balances,
                    utcNow);
                source.Apply(movement.QuantityEffect, utcNow);
                source.IncreaseInspectionHold(custody.Quantity, utcNow);

                await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(source, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<InventoryTransferDamageCustodyDto>.Success(
                    InventoryTransferDamageCustodyMapping.Map(custody));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class InspectInventoryTransferDamageCustody
{
    private readonly IInventoryTransferDamageCustodyRepository _custodies;
    private readonly IInventoryTransferRepository _transfers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public InspectInventoryTransferDamageCustody(
        IInventoryTransferDamageCustodyRepository custodies,
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

    public async Task<ApplicationResult<InventoryTransferDamageCustodyDto>> ExecuteAsync(
        Guid organizationId,
        Guid custodyId,
        InspectInventoryTransferDamageCustodyRequest request,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            return await _unitOfWork.ExecuteInSerializableTransactionAsync(async ct =>
            {
                var orgId = PosOrganizationId.From(organizationId);
                var custody = await _custodies
                    .GetByIdAsync(orgId, InventoryTransferDamageCustodyId.From(custodyId), ct)
                    .ConfigureAwait(false);
                if (custody is null)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDamageCustodyId,
                        "Damage custody was not found.");
                }

                var transfer = await _transfers
                    .GetByIdAsync(orgId, custody.TransferId, ct)
                    .ConfigureAwait(false);
                if (transfer is null)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferNotFound,
                        "Inventory transfer was not found.");
                }

                var expectedBranch = custody.Decision == InventoryTransferDamagedCustodyDecision.KeepAtDestination
                    ? transfer.DestinationBranchId.Value
                    : transfer.SourceBranchId.Value;
                if (custody.Decision == InventoryTransferDamagedCustodyDecision.KeepAtDestination)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        DomainErrorCodes.InvalidInventoryTransferDamageCustodyStatus,
                        "Destination-received damaged goods are already classified as damaged. Only source inspects returned damage.");
                }

                if (actingBranchId != expectedBranch)
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                        ApplicationErrorCodes.InventoryTransferBranchForbidden,
                        "Damage custody must be inspected at the holding branch.");
                }

                var utcNow = _clock.UtcNow;
                if (await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ProductId,
                            StockMovementType.TransferDamageRecovery,
                            null,
                            ct)
                        .ConfigureAwait(false)
                    || await _inventory
                        .HasInventoryTransferSourceMovementAsync(
                            orgId,
                            custody.Id.Value,
                            custody.ProductId,
                            StockMovementType.TransferDamageWriteOff,
                            null,
                            ct)
                        .ConfigureAwait(false))
                {
                    return ApplicationResult<InventoryTransferDamageCustodyDto>.Success(
                        InventoryTransferDamageCustodyMapping.Map(custody));
                }

                InventoryTransferDiscrepancyFollowUp? followUpOverride = null;
                if (!string.IsNullOrWhiteSpace(request.FollowUpOverride))
                {
                    if (!InventoryTransferDiscrepancyFollowUps.TryParse(request.FollowUpOverride, out var parsed))
                    {
                        return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(
                            DomainErrorCodes.InvalidInventoryTransferDiscrepancyFollowUp,
                            "Follow-up override is not recognized.");
                    }

                    followUpOverride = parsed;
                }

                if (custody.Decision == InventoryTransferDamagedCustodyDecision.ReturnToSource
                    && custody.Status == InventoryTransferDamageCustodyStatus.ReceivedAtSource)
                {
                    custody.MarkReadyForSourceInspection(actorId, utcNow);
                }

                custody.Inspect(
                    request.RecoveredSellableQty,
                    request.ConfirmedDamagedQty,
                    actorId,
                    utcNow,
                    followUpOverride);

                var account = await _inventory.GetByProductIdAsync(orgId, custody.ProductId, ct).ConfigureAwait(false);
                if (account is null)
                {
                    var catalogProduct = await _products.GetByIdAsync(orgId, custody.ProductId, ct).ConfigureAwait(false)
                        ?? throw new DomainException(
                            ApplicationErrorCodes.InventoryProductNotFound,
                            "Product was not found.");
                    account = InventoryAccount.CreateUntracked(orgId, custody.ProductId, utcNow);
                    account.Enable(
                        0m,
                        catalogProduct.UnitOfMeasure,
                        actorId,
                        utcNow,
                        hasOpeningStockAlready: false,
                        catalogProduct.SellingMode);
                    await _inventory.AddAccountAsync(account, ct).ConfigureAwait(false);
                }

                var product = await _products.GetByIdAsync(orgId, custody.ProductId, ct).ConfigureAwait(false);
                var uom = product?.UnitOfMeasure ?? UnitOfMeasure.Piece;
                var sellingMode = product?.SellingMode ?? SellingMode.PerItem;
                var transferNumber = transfer.TransferNumber ?? transfer.Id.Value.ToString("D");
                var holdBranch = PosBranchId.From(expectedBranch);

                var balances = (await _balances
                        .ListByProductIdsAsync(orgId, [custody.ProductId], ct)
                        .ConfigureAwait(false))
                    .ToList();
                var balance = InventoryTransferStock.EnsureBalance(
                    orgId,
                    holdBranch,
                    custody.ProductId,
                    balances,
                    utcNow);

                if (custody.RecoveredSellableQty > 0m)
                {
                    var recovery = StockMovement.TransferDamageCustody(
                        orgId,
                        custody.ProductId,
                        account.Id,
                        holdBranch,
                        StockMovementType.TransferDamageRecovery,
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

                if (custody.ConfirmedDamagedQty > 0m)
                {
                    var writeOff = StockMovement.TransferDamageCustody(
                        orgId,
                        custody.ProductId,
                        account.Id,
                        holdBranch,
                        StockMovementType.TransferDamageWriteOff,
                        custody.ConfirmedDamagedQty,
                        uom,
                        custody.Id.Value,
                        transferNumber,
                        actorId,
                        utcNow,
                        sellingMode: sellingMode);
                    balance.ConfirmDamagedFromHold(custody.ConfirmedDamagedQty, utcNow);
                    // Write-off is branch bucket only — org sellable was never increased for damaged hold.
                    await _inventory.AddMovementAsync(writeOff, ct).ConfigureAwait(false);
                }

                await _custodies.UpdateAsync(custody, ct).ConfigureAwait(false);
                await _balances.UpsertAsync(balance, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                return ApplicationResult<InventoryTransferDamageCustodyDto>.Success(
                    InventoryTransferDamageCustodyMapping.Map(custody));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransferDamageCustodyDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

internal static class InventoryTransferDamageCustodyMapping
{
    public static InventoryTransferDamageCustodyDto Map(InventoryTransferDamageCustody c) =>
        new(
            c.Id.Value,
            c.TransferId.Value,
            c.RootTransferId.Value,
            c.ReceiptLineId.Value,
            c.ProductId.Value,
            c.Quantity,
            InventoryTransferDamagedCustodyDecisions.ToCode(c.Decision),
            InventoryTransferDiscrepancyFollowUps.ToCode(c.FollowUpIntent),
            InventoryTransferDamageCustodyStatuses.ToCode(c.Status),
            c.HeldBranchId.Value,
            c.RecoveredSellableQty,
            c.ConfirmedDamagedQty,
            c.WaivedQty,
            c.DestinationRecoveredSellableQty,
            c.ReplacementDemandQty,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.ReturnDispatchedAtUtc,
            c.ReturnReceivedAtUtc,
            c.InspectedAtUtc);
}
