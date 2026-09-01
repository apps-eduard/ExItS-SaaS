using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class DirectPurchaseReceiptQueryService
{
    private readonly IDirectPurchaseReceiptRepository _receipts;

    public DirectPurchaseReceiptQueryService(IDirectPurchaseReceiptRepository receipts) =>
        _receipts = receipts;

    public async Task<DirectPurchaseReceiptDto?> GetByIdAsync(
        Guid organizationId,
        Guid receiptId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _receipts
            .GetByIdAsync(
                PosOrganizationId.From(organizationId),
                DirectPurchaseReceiptId.From(receiptId),
                cancellationToken)
            .ConfigureAwait(false);
        return receipt is null ? null : DirectPurchaseReceiptMapper.Map(receipt);
    }

    public async Task<PagedResult<DirectPurchaseReceiptListItemDto>> ListAsync(
        Guid organizationId,
        DirectPurchaseReceiptFilter filter,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _receipts
            .ListAsync(PosOrganizationId.From(organizationId), filter, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<DirectPurchaseReceiptListItemDto>(
            items.Select(DirectPurchaseReceiptMapper.MapListItem).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateDirectPurchaseReceipt
{
    private readonly IDirectPurchaseReceiptRepository _receipts;
    private readonly ICatalogProductRepository _products;
    private readonly ISupplierRepository _suppliers;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly CreateSupplierPayableFromReceipt _createPayable;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public CreateDirectPurchaseReceipt(
        IDirectPurchaseReceiptRepository receipts,
        ICatalogProductRepository products,
        ISupplierRepository suppliers,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        BranchInventoryMutationService branchMutations,
        IPosUnitOfWork unitOfWork,
        CreateSupplierPayableFromReceipt createPayable,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _receipts = receipts;
        _products = products;
        _suppliers = suppliers;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _branchMutations = branchMutations;
        _unitOfWork = unitOfWork;
        _createPayable = createPayable;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<DirectPurchaseReceiptDto>> ExecuteAsync(
        Guid organizationId,
        CreateDirectPurchaseReceiptRequest request,
        Guid actorId,
        Guid receivingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to create a direct purchase receipt.");
        }

        if (receivingBranchId == Guid.Empty)
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A receiving branch is required for direct purchase receipts.");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                DomainErrorCodes.DirectPurchaseRequiresLines,
                "At least one direct purchase line is required.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var receivingBranch = PosBranchId.From(receivingBranchId);
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        if (idempotencyKey is not null)
                        {
                            var existing = await _receipts
                                .FindByIdempotencyKeyAsync(orgId, idempotencyKey, ct)
                                .ConfigureAwait(false);
                            if (existing is not null)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Success(
                                    DirectPurchaseReceiptMapper.Map(existing));
                            }
                        }

                        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
                        var products = await _products
                            .ListByIdsAsync(orgId, productIds.Select(CatalogProductId.From).ToList(), ct)
                            .ConfigureAwait(false);
                        var productsById = products.ToDictionary(p => p.Id.Value);

                        foreach (var productId in productIds)
                        {
                            if (!productsById.TryGetValue(productId, out var product))
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    ApplicationErrorCodes.PurchaseProductNotFound,
                                    "One or more products were not found in this organization.");
                            }

                            if (product.Status != CatalogProductStatus.Active)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    ApplicationErrorCodes.PurchaseProductNotActive,
                                    "Only active catalog products can be received via direct purchase.");
                            }

                            if (!product.CanBePurchased)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    ApplicationErrorCodes.DirectPurchaseProductNotPurchasable,
                                    "Product is not marked as purchasable.");
                            }
                        }

                        string? sourceName = request.SourceName;
                        SupplierId? supplierId = null;
                        if (request.SupplierId is Guid supplierGuid)
                        {
                            var supplier = await _suppliers
                                .GetByIdAsync(orgId, SupplierId.From(supplierGuid), ct)
                                .ConfigureAwait(false);
                            if (supplier is null)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    ApplicationErrorCodes.SupplierNotFound,
                                    "Supplier was not found in this organization.");
                            }

                            if (supplier.Status != SupplierStatus.Active)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    ApplicationErrorCodes.PurchaseSupplierNotActive,
                                    "Only active suppliers can be used on direct purchase receipts.");
                            }

                            supplierId = supplier.Id;
                            sourceName = string.IsNullOrWhiteSpace(sourceName) ? supplier.Name : sourceName;
                        }

                        var catalogProductIds = productIds.Select(CatalogProductId.From).ToList();
                        var accounts = await _inventory
                            .ListByProductIdsAsync(orgId, catalogProductIds, ct)
                            .ConfigureAwait(false);
                        var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);

                        foreach (var productId in productIds)
                        {
                            if (!accountsByProduct.TryGetValue(productId, out var account) || !account.IsTracked)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    DomainErrorCodes.InventoryNotTracked,
                                    "Inventory must be tracked for all products on a direct purchase receipt.");
                            }
                        }

                        var drafts = new List<DirectPurchaseReceiptLineDraft>(request.Lines.Count);
                        foreach (var line in request.Lines)
                        {
                            var product = productsById[line.ProductId];
                            if (product.TracksExpiration && line.ExpiryDate is null)
                            {
                                return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                    DomainErrorCodes.InventoryExpirationRequired,
                                    "Expiration date is required when receiving expiration-tracked stock.");
                            }

                            drafts.Add(new DirectPurchaseReceiptLineDraft(
                                product.Id,
                                product.Name,
                                product.Sku,
                                product.UnitOfMeasure,
                                line.Quantity,
                                line.UnitCost,
                                product.SellingMode,
                                line.ExpiryDate,
                                line.LotNumber));
                        }

                        var utcNow = _clock.UtcNow;
                        var businessDate = DirectPurchaseReceiptNumbers.BusinessDateOf(utcNow);
                        var receiptNumber = await _receipts
                            .AllocateNextNumberAsync(orgId, businessDate, ct)
                            .ConfigureAwait(false);

                        var receipt = DirectPurchaseReceipt.Create(
                            orgId,
                            receiptNumber,
                            request.PurchaseDate,
                            drafts,
                            actorId,
                            utcNow,
                            supplierId,
                            sourceName,
                            request.ReferenceNumber,
                            request.Notes,
                            idempotencyKey,
                            receivingBranch);

                        var primaryBranchId = await _branches
                            .GetPrimaryBranchIdAsync(orgId.Value, ct)
                            .ConfigureAwait(false);

                        foreach (var line in receipt.Lines)
                        {
                            var account = accountsByProduct[line.ProductId.Value];
                            if (await _inventory
                                    .HasDirectPurchaseReceiptAsync(orgId, receipt.Id, line.ProductId, ct)
                                    .ConfigureAwait(false))
                            {
                                continue;
                            }

                            var product = productsById[line.ProductId.Value];
                            var orgOnHandBefore = account.OnHandQuantity;
                            var movement = StockMovement.DirectPurchaseReceipt(
                                orgId,
                                line.ProductId,
                                account.Id,
                                line.Quantity,
                                line.UnitOfMeasureSnapshot,
                                receipt.Id.Value,
                                actorId,
                                utcNow,
                                sellingMode: product.SellingMode,
                                unitCost: line.UnitCost)
                                .WithBranch(receivingBranch.Value);

                            if (product.TracksExpiration)
                            {
                                var receivedLot = await _lots
                                    .ReceiveAsync(
                                        orgId,
                                        line.ProductId,
                                        line.ExpiryDate!.Value,
                                        line.Quantity,
                                        actorId,
                                        utcNow,
                                        movement.MovementType,
                                        StockMovementSourceType.DirectPurchase,
                                        receivingBranch,
                                        line.LotNumber,
                                        sourceId: receipt.Id.Value,
                                        stockMovementId: movement.Id.Value,
                                        cancellationToken: ct)
                                    .ConfigureAwait(false);
                                movement = movement.WithLot(receivedLot.Id);
                            }

                            line.AttachInventoryMovement(movement.Id);
                            account.ApplyMovementEffect(movement.QuantityEffect);
                            account.Touch(utcNow);
                            await _branchMutations
                                .ApplyBranchDeltaAsync(
                                    _branchBalances,
                                    orgId,
                                    receivingBranch,
                                    primaryBranchId,
                                    line.ProductId,
                                    orgOnHandBefore,
                                    movement.QuantityEffect,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                            await _inventory.UpdateAccountAsync(account, ct).ConfigureAwait(false);
                            await _inventory.AddMovementAsync(movement, ct).ConfigureAwait(false);
                        }

                        await _receipts.AddAsync(receipt, ct).ConfigureAwait(false);

                        var payableResult = await _createPayable
                            .TryCreateFromDirectPurchaseAsync(
                                receipt,
                                request.PaidNow,
                                request.DueDate,
                                request.PaymentMethodAtReceipt,
                                actorId,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
                        if (!payableResult.IsSuccess)
                        {
                            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                payableResult.ErrorCode!,
                                payableResult.ErrorMessage!);
                        }

                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                        return ApplicationResult<DirectPurchaseReceiptDto>.Success(
                            DirectPurchaseReceiptMapper.Map(receipt));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (Exception ex) when (IsNumberConflict(ex))
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                ApplicationErrorCodes.DirectPurchaseReceiptNumberConflict,
                "Direct purchase receipt number conflict. Retry the request.");
        }
    }

    private static bool IsNumberConflict(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("ux_direct_purchase_receipts_org_receipt_number", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ux_direct_purchase_receipts_org_idempotency_key", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class VoidDirectPurchaseReceipt
{
    private readonly IDirectPurchaseReceiptRepository _receipts;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _branchBalances;
    private readonly InventoryLotStockService _lots;
    private readonly BranchInventoryMutationService _branchMutations;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly CreateSupplierPayableFromReceipt _createPayable;
    private readonly IClock _clock;
    private readonly IOrganizationBranchDirectory? _branches;

    public VoidDirectPurchaseReceipt(
        IDirectPurchaseReceiptRepository receipts,
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository branchBalances,
        InventoryLotStockService lots,
        BranchInventoryMutationService branchMutations,
        IPosUnitOfWork unitOfWork,
        CreateSupplierPayableFromReceipt createPayable,
        IClock clock,
        IOrganizationBranchDirectory? branches = null)
    {
        _receipts = receipts;
        _products = products;
        _inventory = inventory;
        _branchBalances = branchBalances;
        _lots = lots;
        _branchMutations = branchMutations;
        _unitOfWork = unitOfWork;
        _createPayable = createPayable;
        _clock = clock;
        _branches = branches;
    }

    public async Task<ApplicationResult<DirectPurchaseReceiptDto>> ExecuteAsync(
        Guid organizationId,
        Guid receiptId,
        VoidDirectPurchaseReceiptRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to void a direct purchase receipt.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                DomainErrorCodes.InvalidDirectPurchaseVoidReason,
                "A void reason is required.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var id = DirectPurchaseReceiptId.From(receiptId);

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        var receipt = await _receipts.GetByIdAsync(orgId, id, ct).ConfigureAwait(false);
                        if (receipt is null)
                        {
                            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                ApplicationErrorCodes.DirectPurchaseReceiptNotFound,
                                "Direct purchase receipt was not found.");
                        }

                        if (receipt.Status == DirectPurchaseReceiptStatus.Voided)
                        {
                            return ApplicationResult<DirectPurchaseReceiptDto>.Success(
                                DirectPurchaseReceiptMapper.Map(receipt));
                        }

                        var productIds = receipt.Lines.Select(l => l.ProductId).Distinct().ToList();
                        var products = await _products.ListByIdsAsync(orgId, productIds, ct).ConfigureAwait(false);
                        var productsById = products.ToDictionary(p => p.Id.Value);
                        var utcNow = _clock.UtcNow;
                        var voidReason = request.Reason.Trim();
                        if (_branches is null)
                        {
                            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                ApplicationErrorCodes.InventoryBranchRequired,
                                "Branch directory is unavailable.");
                        }

                        var branchResolved = await BranchInventoryMutationService
                            .ResolvePhysicalBranchAsync(
                                receipt.ReceivingBranchId?.Value,
                                _branches,
                                organizationId,
                                ct)
                            .ConfigureAwait(false);
                        if (!branchResolved.IsSuccess)
                        {
                            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                branchResolved.ErrorCode!,
                                branchResolved.ErrorMessage!);
                        }

                        var receivingBranch = branchResolved.Value!;
                        var primaryBranchId = await _branches
                            .GetPrimaryBranchIdAsync(orgId.Value, ct)
                            .ConfigureAwait(false);

                        await _createPayable
                            .EnsureVoidOrBlockForReceiptReversalAsync(
                                orgId,
                                SupplierPayableSourceType.DirectPurchaseReceipt,
                                receipt.Id.Value,
                                voidReason,
                                actorId,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);

                        ApplicationResult<DirectPurchaseReceiptDto>? failure = null;
                        await _inventory
                            .ExecuteWithProductReservationLocksAsync(
                                orgId,
                                productIds,
                                async (accounts, lockCt) =>
                                {
                                    var accountsByProduct = accounts.ToDictionary(a => a.ProductId.Value);
                                    var anyLotTracked = receipt.Lines.Any(l =>
                                        productsById.TryGetValue(l.ProductId.Value, out var p)
                                        && p.TracksExpiration
                                        && accountsByProduct.TryGetValue(l.ProductId.Value, out var a)
                                        && a.IsTracked);

                                    if (anyLotTracked)
                                    {
                                        try
                                        {
                                            await _lots
                                                .ReverseReceiveSourceAsync(
                                                    orgId,
                                                    receipt.Id.Value,
                                                    StockMovementType.DirectPurchaseReceipt,
                                                    StockMovementType.DirectPurchaseReceiptReversal,
                                                    actorId,
                                                    utcNow,
                                                    lockCt)
                                                .ConfigureAwait(false);
                                        }
                                        catch (DomainException ex)
                                        {
                                            failure = ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                                DomainErrorCodes.DirectPurchaseReceiptVoidInsufficient,
                                                string.IsNullOrWhiteSpace(ex.Message)
                                                    ? "Cannot void direct purchase: attributable stock has already been consumed."
                                                    : ex.Message);
                                            return;
                                        }
                                    }

                                    foreach (var lineGroup in receipt.Lines.GroupBy(l => l.ProductId.Value))
                                    {
                                        var productId = CatalogProductId.From(lineGroup.Key);
                                        if (!accountsByProduct.TryGetValue(lineGroup.Key, out var account)
                                            || !account.IsTracked)
                                        {
                                            continue;
                                        }

                                        if (!productsById.TryGetValue(lineGroup.Key, out var product))
                                        {
                                            failure = ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                                ApplicationErrorCodes.SaleProductNotFound,
                                                "One or more products on the receipt were not found.");
                                            return;
                                        }

                                        var totalQty = lineGroup.Sum(l => l.Quantity);
                                        if (!product.TracksExpiration && account.OnHandQuantity < totalQty)
                                        {
                                            failure = ApplicationResult<DirectPurchaseReceiptDto>.Failure(
                                                DomainErrorCodes.DirectPurchaseReceiptVoidInsufficient,
                                                "Cannot void direct purchase: attributable stock has already been consumed.");
                                            return;
                                        }

                                        if (await _inventory
                                                .HasDirectPurchaseReceiptReversalAsync(
                                                    orgId,
                                                    receipt.Id,
                                                    productId,
                                                    lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        if (!await _inventory
                                                .HasDirectPurchaseReceiptAsync(orgId, receipt.Id, productId, lockCt)
                                                .ConfigureAwait(false))
                                        {
                                            continue;
                                        }

                                        // Preserve original receipt unit cost (first line in group when uniform).
                                        var unitCost = lineGroup.First().UnitCost;
                                        var orgOnHandBefore = account.OnHandQuantity;
                                        var reversal = StockMovement.DirectPurchaseReceiptReversal(
                                            orgId,
                                            productId,
                                            account.Id,
                                            totalQty,
                                            product.UnitOfMeasure,
                                            receipt.Id.Value,
                                            actorId,
                                            utcNow,
                                            reason: voidReason,
                                            sellingMode: product.SellingMode,
                                            branchId: receivingBranch.Value,
                                            unitCost: unitCost);

                                        account.ApplyMovementEffect(reversal.QuantityEffect);
                                        account.Touch(utcNow);
                                        await _branchMutations
                                            .ApplyBranchDeltaAsync(
                                                _branchBalances,
                                                orgId,
                                                receivingBranch,
                                                primaryBranchId,
                                                productId,
                                                orgOnHandBefore,
                                                reversal.QuantityEffect,
                                                utcNow,
                                                lockCt)
                                            .ConfigureAwait(false);
                                        await _inventory.UpdateAccountAsync(account, lockCt).ConfigureAwait(false);
                                        await _inventory.AddMovementAsync(reversal, lockCt).ConfigureAwait(false);
                                    }

                                    if (failure is not null)
                                    {
                                        return;
                                    }

                                    receipt.Void(utcNow, actorId, voidReason);
                                    await _receipts.UpdateAsync(receipt, lockCt).ConfigureAwait(false);
                                    await _unitOfWork.SaveChangesAsync(lockCt).ConfigureAwait(false);
                                    failure = ApplicationResult<DirectPurchaseReceiptDto>.Success(
                                        DirectPurchaseReceiptMapper.Map(receipt));
                                },
                                ct)
                            .ConfigureAwait(false);

                        if (failure is not null)
                        {
                            return failure;
                        }

                        var reloaded = await _receipts.GetByIdAsync(orgId, id, ct).ConfigureAwait(false) ?? receipt;
                        return ApplicationResult<DirectPurchaseReceiptDto>.Success(
                            DirectPurchaseReceiptMapper.Map(reloaded));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<DirectPurchaseReceiptDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
