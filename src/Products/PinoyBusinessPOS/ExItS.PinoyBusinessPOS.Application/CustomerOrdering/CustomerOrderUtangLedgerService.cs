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
/// Posts exactly one Business Utang charge when a Personal Utang customer order completes.
/// Reuses the canonical Product-Based Utang sale + credit entry path without duplicate stock movement.
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
        if (order.PaymentMethod != CustomerOrderPaymentMethod.Utang
            || order.CustomerParty.PartyType != CustomerPartyType.Personal
            || order.Status != CustomerOrderStatus.Completed)
        {
            return;
        }

        if (order.PlatformBusinessCustomerId is not Guid platformBusinessCustomerId
            || platformBusinessCustomerId == Guid.Empty)
        {
            throw new DomainException(
                ApplicationErrorCodes.CustomerOrderLinkedCustomerRequired,
                "A linked business customer is required to post Utang for this order.");
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

        var posCustomer = await _customers
            .FindByPlatformBusinessCustomerIdAsync(orgId, platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (posCustomer is null)
        {
            throw new DomainException(
                ApplicationErrorCodes.LinkedCustomerNotFound,
                "Linked POS customer was not found for this order.");
        }

        var businessDate = SaleNumbers.BusinessDateOf(utcNow);
        var saleNumber = await _sales
            .ReserveNextSaleNumberAsync(orgId, businessDate, cancellationToken)
            .ConfigureAwait(false);

        var lineDrafts = CustomerOrderUtangSettlementLines.FromOrder(order);
        lineDrafts = await _costResolver
            .EnrichDraftsWithCostsAsync(orgId, lineDrafts, cancellationToken)
            .ConfigureAwait(false);
        var creditEntryId = CustomerOrderUtangSettlementIds.CreditEntryIdForOrder(order.Id);
        var buyerParty = SaleBuyerParty.ExternalCustomer(order.CustomerParty.DisplayNameSnapshot);

        var sale = Sale.RecordCustomerOrderUtangSettlement(
            orgId,
            saleNumber,
            order,
            order.Total,
            lineDrafts,
            actorId,
            utcNow,
            posCustomer.Id,
            creditEntryId,
            buyerParty,
            PosBranchId.From(order.FulfillmentBranchId),
            settlementSaleId);

        var credit = CreditEntry.Create(
            orgId,
            posCustomer.Id,
            order.Total,
            ProductBasedUtangRemarks.ForCustomerOrderNumber(order.OrderNumber),
            utcNow,
            creditEntryId,
            sale.Id);

        try
        {
            await _sales.AddAsync(sale, cancellationToken).ConfigureAwait(false);
            await _credits.AddAsync(credit, cancellationToken).ConfigureAwait(false);
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
}
