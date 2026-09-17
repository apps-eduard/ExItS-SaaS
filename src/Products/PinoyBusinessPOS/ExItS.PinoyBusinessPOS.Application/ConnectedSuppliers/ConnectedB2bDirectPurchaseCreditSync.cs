using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Keeps seller BusinessCreditEntry and buyer SupplierPayable aligned for connected B2B Utang from
/// Direct Purchase (buyer-created) and Sell checkout (seller-originated). Neither creates PO reservations.
/// </summary>
public sealed class ConnectedB2bDirectPurchaseCreditSync
{
    /// <summary>
    /// Stable actor used when buyer credit-policy GET heals missing payables without a mutation actor.
    /// </summary>
    public static readonly Guid SystemRepairActorId =
        Guid.Parse("d1a2c3b4-e5f6-4789-a012-3456789abcde");

    private readonly ISupplierRepository _suppliers;
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCreditEntryRepository _businessCredits;
    private readonly ISupplierPayableRepository _payables;

    public ConnectedB2bDirectPurchaseCreditSync(
        ISupplierRepository suppliers,
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCreditEntryRepository businessCredits,
        ISupplierPayableRepository payables)
    {
        _suppliers = suppliers;
        _relationships = relationships;
        _businessCredits = businessCredits;
        _payables = payables;
    }

    public async Task PostFromReceiptAsync(
        DirectPurchaseReceipt receipt,
        decimal? paidNow,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        DateOnly? dueDate = null)
    {
        if (receipt.SupplierId is null)
        {
            return;
        }

        var total = SaleMoney.RoundMoney(receipt.TotalCost);
        if (total <= 0m)
        {
            return;
        }

        var effectivePaid = paidNow is null
            ? total
            : SaleMoney.RoundMoney(paidNow.Value);
        if (effectivePaid < 0m)
        {
            effectivePaid = 0m;
        }

        if (effectivePaid > total)
        {
            effectivePaid = total;
        }

        var obligation = ConnectedPoUtangObligationProjection.ObligationAmount(total, effectivePaid);
        if (obligation <= 0m)
        {
            return;
        }

        var relationship = await ResolveActiveConnectedRelationshipAsync(
                receipt.OrganizationId,
                receipt.SupplierId,
                cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return;
        }

        BusinessCreditEntry? postedOrExisting = null;
        var entries = await _businessCredits
            .ListChronologicalForBuyerAsync(
                relationship.SupplierOrganizationId,
                relationship.BuyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in entries)
        {
            if (entry.Status != CreditEntryStatus.Active)
            {
                continue;
            }

            if (!ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                    entry.Remarks,
                    out var existingReceiptId)
                || existingReceiptId != receipt.Id.Value)
            {
                continue;
            }

            // Idempotent: Active credit already posted for this receipt.
            postedOrExisting = entry;
            break;
        }

        if (postedOrExisting is null)
        {
            postedOrExisting = BusinessCreditEntry.Create(
                relationship.SupplierOrganizationId,
                relationship.BuyerOrganizationId,
                obligation,
                ConnectedPoUtangObligationProjection.BuildDirectPurchaseRemark(
                    receipt.Id.Value,
                    receipt.ReceiptNumber),
                utcNow,
                relationship.Id.Value);
            if (dueDate is not null)
            {
                postedOrExisting.ApplyCurrentDueDate(dueDate);
            }

            await _businessCredits.AddAsync(postedOrExisting, cancellationToken).ConfigureAwait(false);
        }

        await EnsureMatchingPayableAsync(
                receipt.OrganizationId,
                receipt.SupplierId,
                receipt.Id.Value,
                total,
                effectivePaid,
                dueDate ?? postedOrExisting.CurrentDueDate,
                receipt.CreatedByUserId,
                utcNow,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Historical repair: for each Active Direct-Purchase BusinessCreditEntry between the pair,
    /// ensure the buyer has a matching SupplierPayable (idempotent; never duplicates).
    /// </summary>
    public async Task<int> ReconcileMissingPayablesForRelationshipAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid connectionId,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (connectionId == Guid.Empty || actorId == Guid.Empty)
        {
            return 0;
        }

        var relationshipId = ConnectedSupplierRelationshipId.From(connectionId);
        var supplier = await _suppliers
            .FindByConnectedRelationshipIdAsync(buyerOrganizationId, relationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return 0;
        }

        var entries = await _businessCredits
            .ListChronologicalForBuyerAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);

        var created = 0;
        foreach (var entry in entries)
        {
            if (entry.Status != CreditEntryStatus.Active)
            {
                continue;
            }

            if (ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                    entry.Remarks,
                    out var receiptId))
            {
                var existingDpr = await _payables
                    .FindBySourceAsync(
                        buyerOrganizationId,
                        SupplierPayableSourceType.DirectPurchaseReceipt,
                        receiptId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (existingDpr is not null)
                {
                    continue;
                }

                var dprPayable = SupplierPayable.Create(
                    buyerOrganizationId,
                    supplier.Id,
                    SupplierPayableSourceType.DirectPurchaseReceipt,
                    receiptId,
                    entry.Amount,
                    actorId,
                    utcNow,
                    paidNow: 0m,
                    dueDate: entry.CurrentDueDate);
                await _payables.AddAsync(dprPayable, cancellationToken).ConfigureAwait(false);
                created++;
                continue;
            }

            // Seller-originated connected Sell Utang (SourceSaleId) — including historical remarks
            // without the sale: prefix.
            if (entry.SourceSaleId is null)
            {
                continue;
            }

            var saleId = entry.SourceSaleId.Value;
            var existingSale = await _payables
                .FindBySourceAsync(
                    buyerOrganizationId,
                    SupplierPayableSourceType.Sale,
                    saleId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingSale is not null)
            {
                continue;
            }

            var salePayable = SupplierPayable.Create(
                buyerOrganizationId,
                supplier.Id,
                SupplierPayableSourceType.Sale,
                saleId,
                entry.Amount,
                actorId,
                utcNow,
                paidNow: 0m,
                dueDate: entry.CurrentDueDate);
            await _payables.AddAsync(salePayable, cancellationToken).ConfigureAwait(false);
            created++;
        }

        return created;
    }

    /// <summary>
    /// After seller connected B2B Sell Utang posts a BusinessCreditEntry, ensure the buyer has
    /// a matching SupplierPayable (idempotent).
    /// </summary>
    public async Task EnsurePayableForSaleAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid connectionId,
        Guid saleId,
        decimal amount,
        DateOnly? dueDate,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (connectionId == Guid.Empty
            || saleId == Guid.Empty
            || actorId == Guid.Empty
            || amount <= 0m)
        {
            return;
        }

        var supplier = await _suppliers
            .FindByConnectedRelationshipIdAsync(
                buyerOrganizationId,
                ConnectedSupplierRelationshipId.From(connectionId),
                cancellationToken)
            .ConfigureAwait(false);
        if (supplier is null)
        {
            return;
        }

        var existing = await _payables
            .FindBySourceAsync(
                buyerOrganizationId,
                SupplierPayableSourceType.Sale,
                saleId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var payable = SupplierPayable.Create(
            buyerOrganizationId,
            supplier.Id,
            SupplierPayableSourceType.Sale,
            saleId,
            SaleMoney.RoundMoney(amount),
            actorId,
            utcNow,
            paidNow: 0m,
            dueDate);
        await _payables.AddAsync(payable, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Voids the buyer Sale-sourced payable for a voided connected B2B Sell (blocks if payments exist).
    /// </summary>
    public async Task ReversePayableForSaleAsync(
        PosOrganizationId buyerOrganizationId,
        Guid saleId,
        string voidReason,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (saleId == Guid.Empty || actorId == Guid.Empty)
        {
            return;
        }

        var payable = await _payables
            .FindBySourceAsync(
                buyerOrganizationId,
                SupplierPayableSourceType.Sale,
                saleId,
                cancellationToken)
            .ConfigureAwait(false);
        if (payable is null || payable.Status == SupplierPayableStatus.Voided)
        {
            return;
        }

        payable.Void(voidReason, actorId, utcNow);
        await _payables.UpdateAsync(payable, cancellationToken).ConfigureAwait(false);
    }

    public async Task ReverseForReceiptAsync(
        DirectPurchaseReceipt receipt,
        string voidReason,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default,
        Guid? actorId = null)
    {
        if (receipt.SupplierId is null)
        {
            return;
        }

        var relationship = await ResolveActiveConnectedRelationshipAsync(
                receipt.OrganizationId,
                receipt.SupplierId,
                cancellationToken,
                requireActive: false)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return;
        }

        // Void buyer payable first so posted payments block before seller credit is reversed.
        var payable = await _payables
            .FindBySourceAsync(
                receipt.OrganizationId,
                SupplierPayableSourceType.DirectPurchaseReceipt,
                receipt.Id.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (payable is not null && payable.Status != SupplierPayableStatus.Voided)
        {
            var voidActor = actorId is Guid id && id != Guid.Empty
                ? id
                : receipt.CreatedByUserId;
            payable.Void(voidReason, voidActor, utcNow);
            await _payables.UpdateAsync(payable, cancellationToken).ConfigureAwait(false);
        }

        var entries = await _businessCredits
            .ListChronologicalForBuyerAsync(
                relationship.SupplierOrganizationId,
                relationship.BuyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in entries)
        {
            if (entry.Status != CreditEntryStatus.Active)
            {
                continue;
            }

            if (!ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                    entry.Remarks,
                    out var receiptId)
                || receiptId != receipt.Id.Value)
            {
                continue;
            }

            entry.Reverse(voidReason, utcNow);
            await _businessCredits.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EnsureMatchingPayableAsync(
        PosOrganizationId buyerOrganizationId,
        SupplierId supplierId,
        Guid receiptId,
        decimal originalAmount,
        decimal paidNow,
        DateOnly? dueDate,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _payables
            .FindBySourceAsync(
                buyerOrganizationId,
                SupplierPayableSourceType.DirectPurchaseReceipt,
                receiptId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var payable = SupplierPayable.Create(
            buyerOrganizationId,
            supplierId,
            SupplierPayableSourceType.DirectPurchaseReceipt,
            receiptId,
            originalAmount,
            actorId,
            utcNow,
            paidNow,
            dueDate);
        await _payables.AddAsync(payable, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ConnectedSupplierRelationship?> ResolveActiveConnectedRelationshipAsync(
        PosOrganizationId buyerOrganizationId,
        SupplierId supplierId,
        CancellationToken cancellationToken,
        bool requireActive = true)
    {
        var supplier = await _suppliers
            .GetByIdAsync(buyerOrganizationId, supplierId, cancellationToken)
            .ConfigureAwait(false);
        if (supplier?.ConnectionType != SupplierConnectionType.ConnectedOrganization
            || supplier.ConnectedRelationshipId is null)
        {
            return null;
        }

        var relationship = await _relationships
            .GetAsync(supplier.ConnectedRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null || relationship.BuyerOrganizationId != buyerOrganizationId)
        {
            return null;
        }

        if (requireActive && relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return null;
        }

        return relationship;
    }
}
