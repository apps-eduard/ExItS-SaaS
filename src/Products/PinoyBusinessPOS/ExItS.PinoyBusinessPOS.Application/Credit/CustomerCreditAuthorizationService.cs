using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

/// <summary>
/// Shared NEW-Utang authorization against CustomerCreditPolicy + ledger outstanding.
/// Call inside an ambient transaction after <see cref="ICustomerCreditPolicyRepository.AcquireCustomerCreditLockAsync"/>.
/// </summary>
public sealed class CustomerCreditAuthorizationService
{
    private readonly ICustomerCreditPolicyRepository _policies;
    private readonly IOutstandingBalanceService _outstanding;

    public CustomerCreditAuthorizationService(
        ICustomerCreditPolicyRepository policies,
        IOutstandingBalanceService outstanding)
    {
        _policies = policies;
        _outstanding = outstanding;
    }

    public sealed record AuthorizationResult(
        CustomerCreditPolicy Policy,
        decimal Outstanding,
        decimal AvailableCredit,
        decimal RequestedCredit,
        DateOnly DefaultDueDate);

    public async Task<ApplicationResult<AuthorizationResult>> AuthorizeNewCreditAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal requestedCreditAmount,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        await _policies
            .AcquireCustomerCreditLockAsync(organizationId, customerId, cancellationToken)
            .ConfigureAwait(false);

        var policy = await _policies
            .GetByCustomerAsync(organizationId, customerId, cancellationToken)
            .ConfigureAwait(false);

        if (policy is null || !policy.PermitsNewUtang)
        {
            return ApplicationResult<AuthorizationResult>.Failure(
                ApplicationErrorCodes.CustomerCreditNotApproved,
                "Utang is not approved for this customer.");
        }

        var outstanding = await _outstanding
            .GetOutstandingAsync(organizationId, customerId, cancellationToken)
            .ConfigureAwait(false);

        var projected = outstanding + requestedCreditAmount;
        var available = CustomerCreditPolicy.AvailableCredit(
            policy.Status,
            policy.CreditLimit,
            outstanding);

        if (projected > policy.CreditLimit)
        {
            return ApplicationResult<AuthorizationResult>.Failure(
                ApplicationErrorCodes.CustomerCreditLimitExceeded,
                $"Credit limit exceeded. Limit {policy.CreditLimit:0.00}, outstanding {outstanding:0.00}, available {available:0.00}, requested {requestedCreditAmount:0.00}.");
        }

        var due = CustomerCreditPolicy.ComputeDefaultDueDate(businessDate, policy.DefaultTermDays);
        return ApplicationResult<AuthorizationResult>.Success(
            new AuthorizationResult(policy, outstanding, available, requestedCreditAmount, due));
    }
}
