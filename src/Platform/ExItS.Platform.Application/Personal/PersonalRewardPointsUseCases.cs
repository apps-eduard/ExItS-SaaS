using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalRewardBalanceDto(Guid PersonalUserId, int AvailablePoints);

public sealed record PersonalRewardTransactionDto(
    Guid Id,
    Guid PersonalUserId,
    string TransactionType,
    int Points,
    int SignedDelta,
    int BalanceAfter,
    string Source,
    string? Reason,
    string? ReferenceId,
    DateTimeOffset CreatedAtUtc);

public sealed record PersonalRewardActivityPageDto(
    Guid PersonalUserId,
    IReadOnlyList<PersonalRewardTransactionDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);

public sealed record RedeemPersonalFeatureResultDto(
    Guid PersonalUserId,
    string FeatureCode,
    bool AlreadyActive,
    int? PointsDebited,
    int AvailablePoints,
    PersonalFeatureEntitlementDto? Entitlement);

/// <summary>
/// Trusted award path for Admin/application code. Not exposed as a Personal self-award API.
/// </summary>
public sealed class AwardPersonalRewardPoints
{
    private readonly IPersonalRewardBalanceRepository _balances;
    private readonly IPersonalRewardTransactionRepository _transactions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AwardPersonalRewardPoints(
        IPersonalRewardBalanceRepository balances,
        IPersonalRewardTransactionRepository transactions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _balances = balances;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalRewardTransactionDto>> ExecuteAsync(
        Guid personalUserId,
        int points,
        string source,
        string? reason = null,
        string? referenceId = null,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (personalUserId == Guid.Empty)
        {
            return ApplicationResult<PersonalRewardTransactionDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Personal user was not found.");
        }

        var userId = PlatformUserId.From(personalUserId);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _transactions
                .FindByIdempotencyKeyAsync(userId, idempotencyKey.Trim(), cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<PersonalRewardTransactionDto>.Success(Map(existing));
            }
        }

        var utcNow = _clock.UtcNow;
        var balance = await _balances.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var isNew = balance is null;
        balance ??= PersonalRewardBalance.Create(userId, utcNow);
        var expectedVersion = balance.Version;

        PersonalRewardTransaction tx;
        try
        {
            tx = balance.Credit(points, source, utcNow, reason, referenceId, idempotencyKey);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalRewardTransactionDto>.Failure(
                MapDomain(ex),
                ex.Message);
        }

        if (isNew)
        {
            await _balances.AddAsync(balance, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _balances.UpdateAsync(balance, expectedVersion, cancellationToken).ConfigureAwait(false);
        }

        await _transactions.AddAsync(tx, cancellationToken).ConfigureAwait(false);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException)
        {
            return ApplicationResult<PersonalRewardTransactionDto>.Failure(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "Personal reward balance was modified concurrently. Retry the award.");
        }

        return ApplicationResult<PersonalRewardTransactionDto>.Success(Map(tx));
    }

    private static string MapDomain(DomainException ex) =>
        ex.ErrorCode switch
        {
            DomainErrorCodes.InsufficientPersonalRewardPoints =>
                ApplicationErrorCodes.InsufficientPersonalRewardPoints,
            _ => ApplicationErrorCodes.DomainViolation
        };

    private static PersonalRewardTransactionDto Map(PersonalRewardTransaction tx) =>
        new(
            tx.Id,
            tx.PersonalUserId.Value,
            tx.TransactionType.ToString(),
            tx.Points,
            tx.SignedDelta,
            tx.BalanceAfter,
            tx.Source,
            tx.Reason,
            tx.ReferenceId,
            tx.CreatedAtUtc);
}

public sealed class GetPersonalRewardPointsBalance
{
    private readonly IPersonalRewardBalanceRepository _balances;

    public GetPersonalRewardPointsBalance(IPersonalRewardBalanceRepository balances) =>
        _balances = balances;

    public async Task<ApplicationResult<PersonalRewardBalanceDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var balance = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalRewardBalanceDto>.Success(
            new PersonalRewardBalanceDto(personalUserId.Value, balance?.AvailablePoints ?? 0));
    }
}

public sealed class ListPersonalRewardPointsActivity
{
    private readonly IPersonalRewardTransactionRepository _transactions;

    public ListPersonalRewardPointsActivity(IPersonalRewardTransactionRepository transactions) =>
        _transactions = transactions;

    public async Task<ApplicationResult<PersonalRewardActivityPageDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        var (items, total) = await _transactions
            .ListByUserDescendingAsync(personalUserId, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PersonalRewardActivityPageDto>.Success(
            new PersonalRewardActivityPageDto(
                personalUserId.Value,
                items.Select(Map).ToList(),
                pageNumber,
                take,
                total,
                skip + items.Count < total));
    }

    private static PersonalRewardTransactionDto Map(PersonalRewardTransaction tx) =>
        new(
            tx.Id,
            tx.PersonalUserId.Value,
            tx.TransactionType.ToString(),
            tx.Points,
            tx.SignedDelta,
            tx.BalanceAfter,
            tx.Source,
            tx.Reason,
            tx.ReferenceId,
            tx.CreatedAtUtc);
}

/// <summary>
/// Atomically debits reward points and grants PersonalFeatureEntitlement with GrantSource=RewardPoints.
/// Idempotent when the feature is already active (no second debit).
/// </summary>
public sealed class RedeemPersonalFeatureWithRewardPoints
{
    private readonly IPersonalFeatureDefinitionRepository _definitions;
    private readonly IPersonalFeatureEntitlementRepository _entitlements;
    private readonly IPersonalFeatureEntitlementService _entitlementService;
    private readonly IPersonalRewardBalanceRepository _balances;
    private readonly IPersonalRewardTransactionRepository _transactions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RedeemPersonalFeatureWithRewardPoints(
        IPersonalFeatureDefinitionRepository definitions,
        IPersonalFeatureEntitlementRepository entitlements,
        IPersonalFeatureEntitlementService entitlementService,
        IPersonalRewardBalanceRepository balances,
        IPersonalRewardTransactionRepository transactions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _definitions = definitions;
        _entitlements = entitlements;
        _entitlementService = entitlementService;
        _balances = balances;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<RedeemPersonalFeatureResultDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);

        FeatureCode code;
        try
        {
            code = FeatureCode.Create(featureCode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        var utcNow = _clock.UtcNow;
        var alreadyActive = await _entitlementService
            .HasActiveEntitlementAsync(personalUserId, code.Value, utcNow, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyActive)
        {
            var balance = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
            var existing = (await _entitlements
                .ListByUserAndFeatureAsync(personalUserId, code, cancellationToken)
                .ConfigureAwait(false))
                .First(g => g.IsActiveAt(utcNow));

            return ApplicationResult<RedeemPersonalFeatureResultDto>.Success(
                new RedeemPersonalFeatureResultDto(
                    personalUserId.Value,
                    code.Value,
                    AlreadyActive: true,
                    PointsDebited: null,
                    balance?.AvailablePoints ?? 0,
                    MapEntitlement(existing, utcNow)));
        }

        var definition = await _definitions.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            // Seed redeemable digital-records definition for development/test paths.
            if (code.Value == PersonalFeatureCodes.DigitalRecordsExtended)
            {
                definition = PersonalFeatureDefinition.Create(
                    code,
                    "Digital Records Extended History",
                    utcNow,
                    isActive: true,
                    rewardPointsPrice: PersonalFeatureCodes.DigitalRecordsExtendedDefaultRewardPoints);
                await _definitions.AddAsync(definition, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                    ApplicationErrorCodes.PersonalFeatureDefinitionNotFound,
                    "Personal feature was not found.");
            }
        }

        if (!definition.IsActive)
        {
            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureDefinitionInactive,
                "Personal feature definition is inactive.");
        }

        if (!definition.IsRewardRedeemable)
        {
            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureNotRewardRedeemable,
                "Personal feature is not redeemable with reward points.");
        }

        var price = definition.RewardPointsPrice!.Value;
        var rewardBalance = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        var isNewBalance = rewardBalance is null;
        rewardBalance ??= PersonalRewardBalance.Create(personalUserId, utcNow);
        var expectedVersion = rewardBalance.Version;

        PersonalRewardTransaction debitTx;
        try
        {
            debitTx = rewardBalance.Debit(
                price,
                PersonalRewardSources.FeatureRedemption,
                utcNow,
                reason: $"Redeem {code.Value}",
                referenceId: code.Value);
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.InsufficientPersonalRewardPoints)
        {
            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.InsufficientPersonalRewardPoints,
                ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        PersonalFeatureEntitlement grant;
        try
        {
            grant = PersonalFeatureEntitlement.Grant(
                personalUserId,
                code,
                PersonalFeatureGrantSource.RewardPoints,
                startsAtUtc: utcNow,
                endsAtUtc: null,
                utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        if (isNewBalance)
        {
            await _balances.AddAsync(rewardBalance, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _balances.UpdateAsync(rewardBalance, expectedVersion, cancellationToken).ConfigureAwait(false);
        }

        await _transactions.AddAsync(debitTx, cancellationToken).ConfigureAwait(false);
        await _entitlements.AddAsync(grant, cancellationToken).ConfigureAwait(false);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException)
        {
            // Concurrent redemption: if the other request already granted, treat as already active.
            var nowActive = await _entitlementService
                .HasActiveEntitlementAsync(personalUserId, code.Value, _clock.UtcNow, cancellationToken)
                .ConfigureAwait(false);
            if (nowActive)
            {
                var bal = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
                var existing = (await _entitlements
                    .ListByUserAndFeatureAsync(personalUserId, code, cancellationToken)
                    .ConfigureAwait(false))
                    .First(g => g.IsActiveAt(_clock.UtcNow));
                return ApplicationResult<RedeemPersonalFeatureResultDto>.Success(
                    new RedeemPersonalFeatureResultDto(
                        personalUserId.Value,
                        code.Value,
                        AlreadyActive: true,
                        PointsDebited: null,
                        bal?.AvailablePoints ?? 0,
                        MapEntitlement(existing, _clock.UtcNow)));
            }

            return ApplicationResult<RedeemPersonalFeatureResultDto>.Failure(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "Personal reward balance was modified concurrently. Retry the redemption.");
        }

        return ApplicationResult<RedeemPersonalFeatureResultDto>.Success(
            new RedeemPersonalFeatureResultDto(
                personalUserId.Value,
                code.Value,
                AlreadyActive: false,
                PointsDebited: price,
                rewardBalance.AvailablePoints,
                MapEntitlement(grant, utcNow)));
    }

    private static PersonalFeatureEntitlementDto MapEntitlement(
        PersonalFeatureEntitlement grant,
        DateTimeOffset asOfUtc) =>
        new(
            grant.Id,
            grant.PersonalUserId.Value,
            grant.FeatureCode.Value,
            grant.StartsAtUtc,
            grant.EndsAtUtc,
            grant.Status.ToString(),
            grant.GrantSource.ToString(),
            grant.CreatedAtUtc,
            grant.IsActiveAt(asOfUtc));
}
