using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public sealed record BusinessCustomerCreditPolicyReadDto(
    Guid ConnectionId,
    Guid SellerOrganizationId,
    Guid BuyerOrganizationId,
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

public sealed record BusinessCustomerCreditPolicyChangeDto(
    Guid ChangeId,
    Guid SellerOrganizationId,
    Guid BuyerOrganizationId,
    Guid BusinessCustomerCreditPolicyId,
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

public sealed record UpsertBusinessCustomerCreditPolicyRequest(
    decimal CreditLimit,
    int DefaultTermDays,
    string? Reason = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record ApproveBusinessCustomerCreditPolicyRequest(
    string Reason,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record DisableBusinessCustomerCreditPolicyRequest(
    string Reason,
    DateTimeOffset ExpectedUpdatedAtUtc);

internal static class BusinessCustomerCreditPolicyRelationshipGuard
{
    /// <summary>
    /// Resolves relationship for the seller org. Fail closed if missing / wrong seller.
    /// Does not require Active (Disconnected retains policy for GET/history/disable).
    /// </summary>
    public static async Task<ApplicationResult<ConnectedSupplierRelationship>> ResolveForSellerAsync(
        IConnectedSupplierRelationshipRepository relationships,
        Guid sellerOrganizationId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var relationship = await relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        var seller = PosOrganizationId.From(sellerOrganizationId);
        if (relationship is null || relationship.SupplierOrganizationId != seller)
        {
            return ApplicationResult<ConnectedSupplierRelationship>.Failure(
                ConnectedSupplierErrorCodes.NotFound,
                "Business customer relationship was not found.");
        }

        return ApplicationResult<ConnectedSupplierRelationship>.Success(relationship);
    }

    public static ApplicationResult<T> RequireActiveForMutation<T>(ConnectedSupplierRelationship relationship)
    {
        if (relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ApplicationResult<T>.Failure(
                ConnectedSupplierErrorCodes.RelationshipInactive,
                "Credit policy can only be configured or approved for an active business customer relationship.");
        }

        return ApplicationResult<T>.Success(default!);
    }
}

public sealed class GetBusinessCustomerCreditPolicy
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCustomerCreditPolicyRepository _policies;

    public GetBusinessCustomerCreditPolicy(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCustomerCreditPolicyRepository policies)
    {
        _relationships = relationships;
        _policies = policies;
    }

    public async Task<ApplicationResult<BusinessCustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        var policy = await _policies
            .GetBySellerAndBuyerAsync(
                relationship.SupplierOrganizationId,
                relationship.BuyerOrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (policy is null)
        {
            // Missing policy is a successful NotConfigured read — never BusinessCustomerCreditPolicyNotFound on GET.
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Success(
                new BusinessCustomerCreditPolicyReadDto(
                    connectionId,
                    relationship.SupplierOrganizationId.Value,
                    relationship.BuyerOrganizationId.Value,
                    nameof(CustomerCreditPolicyStatus.NotConfigured),
                    CreditLimit: null,
                    DefaultTermDays: null,
                    OutstandingAmount: 0m,
                    AvailableCredit: 0m,
                    ConfiguredByUserId: null,
                    ConfiguredAtUtc: null,
                    ApprovedByUserId: null,
                    ApprovedAtUtc: null,
                    UpdatedByUserId: null,
                    UpdatedAtUtc: null,
                    ExpectedUpdatedAtUtc: null));
        }

        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Success(Map(policy, connectionId));
    }

    internal static BusinessCustomerCreditPolicyReadDto Map(
        BusinessCustomerCreditPolicy policy,
        Guid connectionId) =>
        new(
            connectionId,
            policy.SellerOrganizationId.Value,
            policy.BuyerOrganizationId.Value,
            policy.Status.ToString(),
            policy.CreditLimit,
            policy.DefaultTermDays,
            OutstandingAmount: 0m,
            BusinessCustomerCreditPolicy.AvailableCredit(policy.Status, policy.CreditLimit, outstanding: 0m),
            policy.ConfiguredByUserId,
            policy.ConfiguredAtUtc,
            policy.ApprovedByUserId,
            policy.ApprovedAtUtc,
            policy.UpdatedByUserId,
            policy.UpdatedAtUtc,
            policy.UpdatedAtUtc);
}

public sealed class UpsertBusinessCustomerCreditPolicy
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCustomerCreditPolicyRepository _policies;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpsertBusinessCustomerCreditPolicy(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCustomerCreditPolicyRepository policies,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _policies = policies;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        decimal creditLimit,
        int defaultTermDays,
        Guid actorUserId,
        string? reason = null,
        DateTimeOffset? expectedUpdatedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        var activeGate = BusinessCustomerCreditPolicyRelationshipGuard
            .RequireActiveForMutation<BusinessCustomerCreditPolicyReadDto>(relationship);
        if (!activeGate.IsSuccess)
        {
            return activeGate;
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    await _policies
                        .AcquireBusinessCustomerCreditLockAsync(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            ct)
                        .ConfigureAwait(false);

                    var existing = await _policies
                        .GetBySellerAndBuyerAsync(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            ct)
                        .ConfigureAwait(false);

                    if (existing is null)
                    {
                        if (expectedUpdatedAtUtc is not null)
                        {
                            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                                ApplicationErrorCodes.BusinessCustomerCreditPolicyConcurrencyConflict,
                                "The business customer credit policy was changed concurrently. Reload and try again.");
                        }

                        var (policy, change) = BusinessCustomerCreditPolicy.Configure(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            connectionId,
                            creditLimit,
                            defaultTermDays,
                            actorUserId,
                            reason,
                            _clock.UtcNow);
                        await _policies.AddAsync(policy, ct).ConfigureAwait(false);
                        await _policies.AddChangeAsync(change, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Success(
                            GetBusinessCustomerCreditPolicy.Map(policy, connectionId));
                    }

                    if (IsStale(expectedUpdatedAtUtc, existing.UpdatedAtUtc))
                    {
                        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.BusinessCustomerCreditPolicyConcurrencyConflict,
                            "The business customer credit policy was changed concurrently. Reload and try again.");
                    }

                    var updateChange = existing.UpdateTerms(
                        creditLimit,
                        defaultTermDays,
                        actorUserId,
                        reason ?? string.Empty,
                        _clock.UtcNow,
                        connectionId);
                    await _policies.UpdateAsync(existing, ct).ConfigureAwait(false);
                    await _policies.AddChangeAsync(updateChange, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Success(
                        GetBusinessCustomerCreditPolicy.Map(existing, connectionId));
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
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

public sealed class ApproveBusinessCustomerCreditPolicy
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCustomerCreditPolicyRepository _policies;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ApproveBusinessCustomerCreditPolicy(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCustomerCreditPolicyRepository policies,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _policies = policies;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        Guid actorUserId,
        string reason,
        DateTimeOffset expectedUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        var activeGate = BusinessCustomerCreditPolicyRelationshipGuard
            .RequireActiveForMutation<BusinessCustomerCreditPolicyReadDto>(relationship);
        if (!activeGate.IsSuccess)
        {
            return activeGate;
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    await _policies
                        .AcquireBusinessCustomerCreditLockAsync(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            ct)
                        .ConfigureAwait(false);

                    var policy = await _policies
                        .GetBySellerAndBuyerAsync(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            ct)
                        .ConfigureAwait(false);
                    if (policy is null)
                    {
                        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.BusinessCustomerCreditPolicyNotFound,
                            "Business customer credit policy was not found.");
                    }

                    if (expectedUpdatedAtUtc.ToUniversalTime().UtcTicks
                        != policy.UpdatedAtUtc.ToUniversalTime().UtcTicks)
                    {
                        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.BusinessCustomerCreditPolicyConcurrencyConflict,
                            "The business customer credit policy was changed concurrently. Reload and try again.");
                    }

                    var change = policy.Approve(actorUserId, reason, _clock.UtcNow);
                    await _policies.UpdateAsync(policy, ct).ConfigureAwait(false);
                    await _policies.AddChangeAsync(change, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Success(
                        GetBusinessCustomerCreditPolicy.Map(policy, connectionId));
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DisableBusinessCustomerCreditPolicy
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCustomerCreditPolicyRepository _policies;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DisableBusinessCustomerCreditPolicy(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCustomerCreditPolicyRepository policies,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _policies = policies;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<BusinessCustomerCreditPolicyReadDto>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        Guid actorUserId,
        string reason,
        DateTimeOffset expectedUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        // Disable allowed on Active or Disconnected when a policy exists (relationship must be known).
        if (relationship.Status is not (ConnectedSupplierRelationshipStatus.Active
            or ConnectedSupplierRelationshipStatus.Disconnected))
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                ConnectedSupplierErrorCodes.RelationshipInactive,
                "Credit policy can only be disabled for an active or disconnected business customer relationship.");
        }

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(async ct =>
                {
                    await _policies
                        .AcquireBusinessCustomerCreditLockAsync(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            ct)
                        .ConfigureAwait(false);

                    var policy = await _policies
                        .GetBySellerAndBuyerAsync(
                            relationship.SupplierOrganizationId,
                            relationship.BuyerOrganizationId,
                            ct)
                        .ConfigureAwait(false);
                    if (policy is null)
                    {
                        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.BusinessCustomerCreditPolicyNotFound,
                            "Business customer credit policy was not found.");
                    }

                    if (expectedUpdatedAtUtc.ToUniversalTime().UtcTicks
                        != policy.UpdatedAtUtc.ToUniversalTime().UtcTicks)
                    {
                        return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(
                            ApplicationErrorCodes.BusinessCustomerCreditPolicyConcurrencyConflict,
                            "The business customer credit policy was changed concurrently. Reload and try again.");
                    }

                    var change = policy.Disable(actorUserId, reason, _clock.UtcNow);
                    await _policies.UpdateAsync(policy, ct).ConfigureAwait(false);
                    await _policies.AddChangeAsync(change, ct).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                    return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Success(
                        GetBusinessCustomerCreditPolicy.Map(policy, connectionId));
                }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<BusinessCustomerCreditPolicyReadDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListBusinessCustomerCreditPolicyHistory
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IBusinessCustomerCreditPolicyRepository _policies;

    public ListBusinessCustomerCreditPolicyHistory(
        IConnectedSupplierRelationshipRepository relationships,
        IBusinessCustomerCreditPolicyRepository policies)
    {
        _relationships = relationships;
        _policies = policies;
    }

    public async Task<ApplicationResult<PagedResult<BusinessCustomerCreditPolicyChangeDto>>> ExecuteAsync(
        Guid sellerOrganizationId,
        Guid connectionId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var resolved = await BusinessCustomerCreditPolicyRelationshipGuard
            .ResolveForSellerAsync(_relationships, sellerOrganizationId, connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<PagedResult<BusinessCustomerCreditPolicyChangeDto>>.Failure(
                resolved.ErrorCode!,
                resolved.ErrorMessage!);
        }

        var relationship = resolved.Value!;
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var (items, total) = await _policies
            .ListChangesAsync(
                relationship.SupplierOrganizationId,
                relationship.BuyerOrganizationId,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PagedResult<BusinessCustomerCreditPolicyChangeDto>>.Success(
            new PagedResult<BusinessCustomerCreditPolicyChangeDto>(
                items.Select(Map).ToList(),
                total,
                Math.Max(page ?? 1, 1),
                take));
    }

    private static BusinessCustomerCreditPolicyChangeDto Map(BusinessCustomerCreditPolicyChange change) =>
        new(
            change.Id.Value,
            change.SellerOrganizationId.Value,
            change.BuyerOrganizationId.Value,
            change.BusinessCustomerCreditPolicyId.Value,
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
