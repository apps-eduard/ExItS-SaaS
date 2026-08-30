using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Posts exactly one settlement sale when a customer order completes, preserving authoritative COGS snapshots.
/// Personal Utang orders also post one Business Utang credit entry. Inventory was already consumed on completion.
/// </summary>
public interface ICustomerOrderUtangLedgerService
{
    Task PostOnCompleteIfNeededAsync(
        CustomerOrder order,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerOrderUtangLedgerService : ICustomerOrderUtangLedgerService
{
    private readonly ISaleRepository _sales;
    private readonly ICreditEntryRepository _credits;
    private readonly IPOSCustomerRepository _customers;
    private readonly InventoryCostResolver _costResolver;

    public CustomerOrderUtangLedgerService(
        ISaleRepository sales,
        ICreditEntryRepository credits,
        IPOSCustomerRepository customers,
        InventoryCostResolver costResolver)
    {
        _sales = sales;
        _credits = credits;
        _customers = customers;
        _costResolver = costResolver;
    }

    public async Task PostOnCompleteIfNeededAsync(
        CustomerOrder order,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        if (order.Status != CustomerOrderStatus.Completed)
        {
            return;
        }

        var orgId = order.SellerOrganizationId;
        var settlementSaleId = CustomerOrderUtangSettlementIds.SaleIdForOrder(order.Id);
        var existingSale = await _sales
            .GetByIdAsync(orgId, settlementSaleId, cancellationToken)
            .ConfigureAwait(false);
        if (existingSale is not null)
        {
            return;
        }

        POSCustomerId? posCustomerId = null;
        if (order.PlatformBusinessCustomerId is Guid platformBusinessCustomerId
            && platformBusinessCustomerId != Guid.Empty)
        {
            var posCustomer = await _customers
                .FindByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (posCustomer is not null)
            {
                posCustomerId = posCustomer.Id;
            }
        }

        CreditEntryId? creditEntryId = null;
        if (order.PaymentMethod == CustomerOrderPaymentMethod.Utang)
        {
            if (order.CustomerParty.PartyType != CustomerPartyType.Personal)
            {
                return;
            }

            if (posCustomerId is null)
            {
                throw new DomainException(
                    ApplicationErrorCodes.CustomerOrderLinkedCustomerRequired,
                    "A linked business customer is required to post Utang for this order.");
            }

            creditEntryId = CustomerOrderUtangSettlementIds.CreditEntryIdForOrder(order.Id);
        }

        var businessDate = SaleNumbers.BusinessDateOf(utcNow);
        var saleNumber = await _sales
            .ReserveNextSaleNumberAsync(orgId, businessDate, cancellationToken)
            .ConfigureAwait(false);

        var lineDrafts = await EnrichOrderLineDraftsWithCostsAsync(orgId, order, cancellationToken)
            .ConfigureAwait(false);
        var paymentMethod = CustomerOrderPaymentMethods.ToSalePaymentMethod(order.PaymentMethod);
        var buyerParty = SaleBuyerParty.ExternalCustomer(order.CustomerParty.DisplayNameSnapshot);

        var sale = Sale.RecordCustomerOrderSettlement(
            orgId,
            saleNumber,
            order,
            order.Total,
            lineDrafts,
            actorId,
            utcNow,
            paymentMethod,
            posCustomerId,
            creditEntryId,
            buyerParty,
            PosBranchId.From(order.FulfillmentBranchId),
            settlementSaleId);

        CreditEntry? credit = null;
        if (creditEntryId is not null && posCustomerId is not null)
        {
            credit = CreditEntry.Create(
                orgId,
                posCustomerId,
                order.Total,
                ProductBasedUtangRemarks.ForCustomerOrderNumber(order.OrderNumber),
                utcNow,
                creditEntryId,
                sale.Id);
        }

        try
        {
            await _sales.AddAsync(sale, cancellationToken).ConfigureAwait(false);
            if (credit is not null)
            {
                await _credits.AddAsync(credit, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (PersistenceConflictException)
        {
            var raced = await _sales
                .GetByIdAsync(orgId, settlementSaleId, cancellationToken)
                .ConfigureAwait(false);
            if (raced is not null)
            {
                return;
            }

            throw;
        }
    }

    private async Task<IReadOnlyList<SaleLineDraft>> EnrichOrderLineDraftsWithCostsAsync(
        PosOrganizationId organizationId,
        CustomerOrder order,
        CancellationToken cancellationToken)
    {
        var lineDrafts = CustomerOrderUtangSettlementLines.FromOrder(order);
        var inventoryDrafts = lineDrafts
            .Where(CustomerOrderUtangSettlementLines.IsInventoryCostLine)
            .ToList();
        if (inventoryDrafts.Count == 0)
        {
            return lineDrafts;
        }

        var enrichedInventory = await _costResolver
            .EnrichDraftsWithCostsAsync(organizationId, inventoryDrafts, cancellationToken)
            .ConfigureAwait(false);

        var enrichedIndex = 0;
        var result = new SaleLineDraft[lineDrafts.Count];
        for (var i = 0; i < lineDrafts.Count; i++)
        {
            if (CustomerOrderUtangSettlementLines.IsInventoryCostLine(lineDrafts[i]))
            {
                result[i] = enrichedInventory[enrichedIndex++];
            }
            else
            {
                result[i] = lineDrafts[i];
            }
        }

        return result;
    }
}
