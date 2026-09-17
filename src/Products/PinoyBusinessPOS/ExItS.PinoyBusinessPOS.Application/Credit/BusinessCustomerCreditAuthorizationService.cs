using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

/// <summary>
/// Shared NEW-Utang authorization against BusinessCustomerCreditPolicy + outstanding ledger
/// + active connected-PO Utang reservations.
/// Call inside an ambient transaction after <see cref="IBusinessCreditEntryRepository.AcquireBusinessCreditLockAsync"/>.
/// Outstanding is net: active credits − settled BusinessRepayments (matches BusinessOutstandingBalanceService).
/// </summary>
public sealed class BusinessCustomerCreditAuthorizationService
{
    private readonly IBusinessCustomerCreditPolicyRepository _policies;
    private readonly IBusinessCreditEntryRepository _businessCredits;
    private readonly IBusinessRepaymentRepository _businessRepayments;
    private readonly IConnectedPurchaseOrderRepository? _connectedOrders;

    public BusinessCustomerCreditAuthorizationService(
        IBusinessCustomerCreditPolicyRepository policies,
        IBusinessCreditEntryRepository businessCredits,
        IBusinessRepaymentRepository businessRepayments,
        IConnectedPurchaseOrderRepository? connectedOrders = null)
    {
        _policies = policies;
        _businessCredits = businessCredits;
        _businessRepayments = businessRepayments;
        _connectedOrders = connectedOrders;
    }

    public sealed record AuthorizationResult(
        BusinessCustomerCreditPolicy Policy,
        decimal Outstanding,
        decimal ReservedByActivePos,
        decimal AvailableCredit,
        decimal RequestedCredit,
        DateOnly DefaultDueDate);

    public async Task<ApplicationResult<AuthorizationResult>> AuthorizeNewCreditAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        decimal requestedCreditAmount,
        DateOnly businessDate,
        CancellationToken cancellationToken = default,
        Guid? excludeConnectedPurchaseOrderId = null)
    {
        await _businessCredits
            .AcquireBusinessCreditLockAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);

        var policy = await _policies
            .GetBySellerAndBuyerAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (policy is null || !policy.PermitsNewUtang)
        {
            return ApplicationResult<AuthorizationResult>.Failure(
                ApplicationErrorCodes.BusinessCustomerCreditNotApproved,
                "Utang is not approved for this business customer.");
        }

        var outstanding = await ComputeNetOutstandingAsync(
                sellerOrganizationId,
                buyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        var reserved = 0m;
        if (_connectedOrders is not null)
        {
            var orders = await _connectedOrders
                .ListBetweenOrganizationsAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            reserved = ConnectedPoUtangCredit.SumActiveReservations(orders, excludeConnectedPurchaseOrderId);
        }

        var available = ConnectedPoUtangCredit.AvailableCredit(
            policy.Status,
            policy.CreditLimit,
            outstanding,
            reserved);

        if (requestedCreditAmount > available)
        {
            return ApplicationResult<AuthorizationResult>.Failure(
                ApplicationErrorCodes.BusinessCustomerCreditLimitExceeded,
                $"Credit limit exceeded. Limit {policy.CreditLimit:0.00}, outstanding {outstanding:0.00}, reserved by active POs {reserved:0.00}, available {available:0.00}, requested {requestedCreditAmount:0.00}.");
        }

        var due = CustomerCreditPolicy.ComputeDefaultDueDate(businessDate, policy.DefaultTermDays);
        return ApplicationResult<AuthorizationResult>.Success(
            new AuthorizationResult(policy, outstanding, reserved, available, requestedCreditAmount, due));
    }

    /// <summary>Read-only credit picture (no lock) for GET / UI.</summary>
    public async Task<(decimal Outstanding, decimal ReservedByActivePos, decimal AvailableCredit)> GetCreditPictureAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CustomerCreditPolicyStatus status,
        decimal creditLimit,
        CancellationToken cancellationToken = default)
    {
        var outstanding = await ComputeNetOutstandingAsync(
                sellerOrganizationId,
                buyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        var reserved = 0m;
        if (_connectedOrders is not null)
        {
            var orders = await _connectedOrders
                .ListBetweenOrganizationsAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            reserved = ConnectedPoUtangCredit.SumActiveReservations(orders);
        }

        var available = ConnectedPoUtangCredit.AvailableCredit(status, creditLimit, outstanding, reserved);
        return (outstanding, reserved, available);
    }

    private async Task<decimal> ComputeNetOutstandingAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken)
    {
        var credits = await _businessCredits
            .SumActiveAmountAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var repayments = await _businessRepayments
            .SumSettledAmountAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        return credits - repayments;
    }
}
