using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

/// <summary>
/// Shared NEW-Utang authorization against BusinessCustomerCreditPolicy + business ledger outstanding.
/// Call inside an ambient transaction after <see cref="IBusinessCreditEntryRepository.AcquireBusinessCreditLockAsync"/>.
/// </summary>
public sealed class BusinessCustomerCreditAuthorizationService
{
    private readonly IBusinessCustomerCreditPolicyRepository _policies;
    private readonly IBusinessCreditEntryRepository _businessCredits;

    public BusinessCustomerCreditAuthorizationService(
        IBusinessCustomerCreditPolicyRepository policies,
        IBusinessCreditEntryRepository businessCredits)
    {
        _policies = policies;
        _businessCredits = businessCredits;
    }

    public sealed record AuthorizationResult(
        BusinessCustomerCreditPolicy Policy,
        decimal Outstanding,
        decimal AvailableCredit,
        decimal RequestedCredit,
        DateOnly DefaultDueDate);

    public async Task<ApplicationResult<AuthorizationResult>> AuthorizeNewCreditAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        decimal requestedCreditAmount,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
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

        var outstanding = await _businessCredits
            .SumActiveAmountAsync(sellerOrganizationId, buyerOrganizationId, cancellationToken)
            .ConfigureAwait(false);

        var projected = outstanding + requestedCreditAmount;
        var available = BusinessCustomerCreditPolicy.AvailableCredit(
            policy.Status,
            policy.CreditLimit,
            outstanding);

        if (projected > policy.CreditLimit)
        {
            return ApplicationResult<AuthorizationResult>.Failure(
                ApplicationErrorCodes.BusinessCustomerCreditLimitExceeded,
                $"Credit limit exceeded. Limit {policy.CreditLimit:0.00}, outstanding {outstanding:0.00}, available {available:0.00}, requested {requestedCreditAmount:0.00}.");
        }

        var due = CustomerCreditPolicy.ComputeDefaultDueDate(businessDate, policy.DefaultTermDays);
        return ApplicationResult<AuthorizationResult>.Success(
            new AuthorizationResult(policy, outstanding, available, requestedCreditAmount, due));
    }
}
