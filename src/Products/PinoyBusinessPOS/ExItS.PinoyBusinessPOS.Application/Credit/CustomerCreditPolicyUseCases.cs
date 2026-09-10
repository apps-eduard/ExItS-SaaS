using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public sealed record CustomerCreditPolicyReadDto(
    Guid CustomerId,
    string Status,
    decimal? CreditLimit,
    int? DefaultTermDays,
    decimal OutstandingAmount,
    decimal AvailableCredit,
    Guid? ConfiguredByUserId,
    DateTimeOffset? ConfiguredAtUtc,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAtUtc,
    Guid? UpdatedByUserId,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed record CustomerCreditPolicyChangeDto(
    Guid ChangeId,
    Guid CustomerId,
    Guid CustomerCreditPolicyId,
    string Action,
    string? PreviousStatus,
    string NewStatus,
    decimal? PreviousCreditLimit,
    decimal? NewCreditLimit,
    int? PreviousTermDays,
    int? NewTermDays,
    Guid ActorUserId,
    string Reason,
    DateTimeOffset ChangedAtUtc);

public sealed record UpsertCustomerCreditPolicyRequest(
    decimal CreditLimit,
    int DefaultTermDays,
    string? Reason = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record ApproveCustomerCreditPolicyRequest(
    string Reason,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record DisableCustomerCreditPolicyRequest(
    string Reason,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed class GetCustomerCreditPolicy
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICustomerCreditPolicyRepository _policies;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly PartyBranchAccessService _branchAccess;
    private readonly IPartyBranchAccessActorAccessor _actorAccessor;

    public GetCustomerCreditPolicy(
        IPOSCustomerRepository customers,
        ICustomerCreditPolicyRepository policies,
        IOutstandingBalanceService outstanding,
        PartyBranchAccessService branchAccess,
        IPartyBranchAccessActorAccessor actorAccessor)
    {
        _customers = customers;
        _policies = policies;
        _outstanding = outstanding;
        _branchAccess = branchAccess;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<CustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        // Fail closed for cross-branch: inaccessible customers look like NotFound (match detail page).
        if (!await _branchAccess.EnsureCanViewCustomerOrNotFoundAsync(
                organizationId,
                customerId,
                _actorAccessor.GetActor(),
                cancellationToken)
            .ConfigureAwait(false))
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        var outstanding = await _outstanding
            .GetOutstandingAsync(orgId, custId, cancellationToken)
            .ConfigureAwait(false);

        var policy = await _policies
            .GetByCustomerAsync(orgId, custId, cancellationToken)
            .ConfigureAwait(false);

        if (policy is null)
        {
            // Missing policy is a successful NotConfigured read — never CustomerCreditPolicyNotFound on GET.
            return ApplicationResult<CustomerCreditPolicyReadDto>.Success(
                new CustomerCreditPolicyReadDto(
                    customerId,
                    nameof(CustomerCreditPolicyStatus.NotConfigured),
                    CreditLimit: null,
                    DefaultTermDays: null,
                    outstanding,
                    AvailableCredit: 0m,
                    ConfiguredByUserId: null,
                    ConfiguredAtUtc: null,
                    ApprovedByUserId: null,
                    ApprovedAtUtc: null,
                    UpdatedByUserId: null,
                    UpdatedAtUtc: null,
                    ExpectedUpdatedAtUtc: null));
        }

        return ApplicationResult<CustomerCreditPolicyReadDto>.Success(Map(policy, outstanding));
    }

    internal static CustomerCreditPolicyReadDto Map(CustomerCreditPolicy policy, decimal outstanding) =>
        new(
            policy.CustomerId.Value,
            policy.Status.ToString(),
            policy.CreditLimit,
            policy.DefaultTermDays,
            outstanding,
            CustomerCreditPolicy.AvailableCredit(policy.Status, policy.CreditLimit, outstanding),
            policy.ConfiguredByUserId,
            policy.ConfiguredAtUtc,
            policy.ApprovedByUserId,
            policy.ApprovedAtUtc,
            policy.UpdatedByUserId,
            policy.UpdatedAtUtc,
            policy.UpdatedAtUtc);
}

public sealed class UpsertCustomerCreditPolicy
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICustomerCreditPolicyRepository _policies;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpsertCustomerCreditPolicy(
        IPOSCustomerRepository customers,
        ICustomerCreditPolicyRepository policies,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _policies = policies;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        decimal creditLimit,
        int defaultTermDays,
        Guid actorUserId,
        string? reason = null,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                DomainErrorCodes.CustomerNotActive,
                "Credit policy can only be configured for an active customer.");
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    await _policies
                        .AcquireCustomerCreditLockAsync(orgId, custId, ct)
                        .ConfigureAwait(false);

                    var existing = await _policies
                        .GetByCustomerAsync(orgId, custId, ct)
                        .ConfigureAwait(false);

                    if (existing is null)
                    {
                        if (expectedUpdatedAtUtc is not null)
                        {
                            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                                ApplicationErrorCodes.CustomerCreditPolicyConcurrencyConflict,
                                "The customer credit policy was changed concurrently. Reload and try again.");
                        }

                        var (policy, change) = CustomerCreditPolicy.Configure(
                            orgId,
                            custId,
                            creditLimit,
                            defaultTermDays,
                            actorUserId,
                            reason,
                            _clock.UtcNow);
                        await _policies.AddAsync(policy, ct).ConfigureAwait(false);
                        await _policies.AddChangeAsync(change, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                        var outstanding = await _outstanding
                            .GetOutstandingAsync(orgId, custId, ct)
                            .ConfigureAwait(false);
                        return ApplicationResult<CustomerCreditPolicyReadDto>.Success(
                            GetCustomerCreditPolicy.Map(policy, outstanding));
                    }

                    if (IsStale(expectedUpdatedAtUtc, existing.UpdatedAtUtc))
                    {
                        return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.CustomerCreditPolicyConcurrencyConflict,
                            "The customer credit policy was changed concurrently. Reload and try again.");
                    }

                    var updateChange = existing.UpdateTerms(
                        creditLimit,
                        defaultTermDays,
                        actorUserId,
                        reason ?? string.Empty,
                        _clock.UtcNow);
                    await _policies.UpdateAsync(existing, ct).ConfigureAwait(false);
                    await _policies.AddChangeAsync(updateChange, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    var outstandingAfter = await _outstanding
                        .GetOutstandingAsync(orgId, custId, ct)
                        .ConfigureAwait(false);
                    return ApplicationResult<CustomerCreditPolicyReadDto>.Success(
                        GetCustomerCreditPolicy.Map(existing, outstandingAfter));
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private static bool IsStale(DateTimeOffset? expectedUpdatedAtUtc, DateTimeOffset actualUpdatedAtUtc)
    {
        if (expectedUpdatedAtUtc is null)
        {
            return false;
        }

        return expectedUpdatedAtUtc.Value.ToUniversalTime().UtcTicks
            != actualUpdatedAtUtc.ToUniversalTime().UtcTicks;
    }
}

public sealed class ApproveCustomerCreditPolicy
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICustomerCreditPolicyRepository _policies;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ApproveCustomerCreditPolicy(
        IPOSCustomerRepository customers,
        ICustomerCreditPolicyRepository policies,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _policies = policies;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        Guid actorUserId,
        string reason,
        DateTimeOffset expectedUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    await _policies
                        .AcquireCustomerCreditLockAsync(orgId, custId, ct)
                        .ConfigureAwait(false);

                    var policy = await _policies
                        .GetByCustomerAsync(orgId, custId, ct)
                        .ConfigureAwait(false);
                    if (policy is null)
                    {
                        return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.CustomerCreditPolicyNotFound,
                            "Customer credit policy was not found.");
                    }

                    if (expectedUpdatedAtUtc.ToUniversalTime().UtcTicks
                        != policy.UpdatedAtUtc.ToUniversalTime().UtcTicks)
                    {
                        return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.CustomerCreditPolicyConcurrencyConflict,
                            "The customer credit policy was changed concurrently. Reload and try again.");
                    }

                    var change = policy.Approve(actorUserId, reason, _clock.UtcNow);
                    await _policies.UpdateAsync(policy, ct).ConfigureAwait(false);
                    await _policies.AddChangeAsync(change, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    var outstanding = await _outstanding
                        .GetOutstandingAsync(orgId, custId, ct)
                        .ConfigureAwait(false);
                    return ApplicationResult<CustomerCreditPolicyReadDto>.Success(
                        GetCustomerCreditPolicy.Map(policy, outstanding));
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DisableCustomerCreditPolicy
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICustomerCreditPolicyRepository _policies;
    private readonly IOutstandingBalanceService _outstanding;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DisableCustomerCreditPolicy(
        IPOSCustomerRepository customers,
        ICustomerCreditPolicyRepository policies,
        IOutstandingBalanceService outstanding,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _policies = policies;
        _outstanding = outstanding;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        Guid actorUserId,
        string reason,
        DateTimeOffset expectedUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    await _policies
                        .AcquireCustomerCreditLockAsync(orgId, custId, ct)
                        .ConfigureAwait(false);

                    var policy = await _policies
                        .GetByCustomerAsync(orgId, custId, ct)
                        .ConfigureAwait(false);
                    if (policy is null)
                    {
                        return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.CustomerCreditPolicyNotFound,
                            "Customer credit policy was not found.");
                    }

                    if (expectedUpdatedAtUtc.ToUniversalTime().UtcTicks
                        != policy.UpdatedAtUtc.ToUniversalTime().UtcTicks)
                    {
                        return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.CustomerCreditPolicyConcurrencyConflict,
                            "The customer credit policy was changed concurrently. Reload and try again.");
                    }

                    var change = policy.Disable(actorUserId, reason, _clock.UtcNow);
                    await _policies.UpdateAsync(policy, ct).ConfigureAwait(false);
                    await _policies.AddChangeAsync(change, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    var outstanding = await _outstanding
                        .GetOutstandingAsync(orgId, custId, ct)
                        .ConfigureAwait(false);
                    return ApplicationResult<CustomerCreditPolicyReadDto>.Success(
                        GetCustomerCreditPolicy.Map(policy, outstanding));
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<CustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListCustomerCreditPolicyHistory
{
    private readonly IPOSCustomerRepository _customers;
    private readonly ICustomerCreditPolicyRepository _policies;

    public ListCustomerCreditPolicyHistory(
        IPOSCustomerRepository customers,
        ICustomerCreditPolicyRepository policies)
    {
        _customers = customers;
        _policies = policies;
    }

    public async Task<ApplicationResult<PagedResult<CustomerCreditPolicyChangeDto>>> ExecuteAsync(
        Guid organizationId,
        Guid customerId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var custId = POSCustomerId.From(customerId);
        var customer = await _customers.GetByIdAsync(orgId, custId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return ApplicationResult<PagedResult<CustomerCreditPolicyChangeDto>>.Failure(
                ApplicationErrorCodes.CustomerNotFound,
                "Customer was not found.");
        }

        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _policies
            .ListChangesAsync(orgId, custId, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PagedResult<CustomerCreditPolicyChangeDto>>.Success(
            new PagedResult<CustomerCreditPolicyChangeDto>(
                items.Select(Map).ToList(),
                total,
                Math.Max(page ?? 1, 1),
                take));
    }

    private static CustomerCreditPolicyChangeDto Map(CustomerCreditPolicyChange change) =>
        new(
            change.Id.Value,
            change.CustomerId.Value,
            change.CustomerCreditPolicyId.Value,
            change.Action.ToString(),
            change.PreviousStatus?.ToString(),
            change.NewStatus.ToString(),
            change.PreviousCreditLimit,
            change.NewCreditLimit,
            change.PreviousTermDays,
            change.NewTermDays,
            change.ActorUserId,
            change.Reason,
            change.ChangedAtUtc);
}
