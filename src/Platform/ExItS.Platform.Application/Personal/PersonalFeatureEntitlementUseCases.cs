using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed class PersonalFeatureEntitlementService(
    IPersonalFeatureDefinitionRepository definitions,
    IPersonalFeatureEntitlementRepository entitlements) : IPersonalFeatureEntitlementService
{
    public async Task<bool> HasActiveEntitlementAsync(
        PlatformUserId personalUserId,
        string featureCode,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);
        if (asOfUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "asOfUtc must be UTC.");
        }

        FeatureCode code;
        try
        {
            code = FeatureCode.Create(featureCode);
        }
        catch (DomainException)
        {
            return false;
        }

        var definition = await definitions
            .GetByCodeAsync(code, cancellationToken)
            .ConfigureAwait(false);
        if (definition is null || !definition.IsActive)
        {
            return false;
        }

        var grants = await entitlements
            .ListByUserAndFeatureAsync(personalUserId, code, cancellationToken)
            .ConfigureAwait(false);

        return grants.Any(g => g.IsActiveAt(asOfUtc));
    }
}

public sealed record PersonalFeatureEntitlementDto(
    Guid Id,
    Guid PersonalUserId,
    string FeatureCode,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    string Status,
    string GrantSource,
    DateTimeOffset CreatedAtUtc,
    bool IsActiveAtQueryTime);

public sealed record PersonalFeatureActiveDto(
    Guid PersonalUserId,
    string FeatureCode,
    bool IsActive);

/// <summary>
/// Server-controlled grant. Not callable by Personal clients for self-grant.
/// Idempotent when an overlapping Active grant already covers the requested window.
/// </summary>
public sealed class GrantPersonalFeature
{
    private readonly IPersonalFeatureDefinitionRepository _definitions;
    private readonly IPersonalFeatureEntitlementRepository _entitlements;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GrantPersonalFeature(
        IPersonalFeatureDefinitionRepository definitions,
        IPersonalFeatureEntitlementRepository entitlements,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _definitions = definitions;
        _entitlements = entitlements;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalFeatureEntitlementDto>> ExecuteAsync(
        Guid personalUserId,
        string featureCode,
        PersonalFeatureGrantSource grantSource,
        DateTimeOffset? startsAtUtc = null,
        DateTimeOffset? endsAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (personalUserId == Guid.Empty)
        {
            return ApplicationResult<PersonalFeatureEntitlementDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Personal user was not found.");
        }

        FeatureCode code;
        try
        {
            code = FeatureCode.Create(featureCode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalFeatureEntitlementDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        var utcNow = _clock.UtcNow;
        var starts = startsAtUtc ?? utcNow;
        var definition = await _definitions.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            definition = PersonalFeatureDefinition.Create(
                code,
                DisplayNameFor(code),
                utcNow,
                isActive: true,
                rewardPointsPrice: DefaultRewardPriceFor(code));
            await _definitions.AddAsync(definition, cancellationToken).ConfigureAwait(false);
        }
        else if (!definition.IsActive)
        {
            return ApplicationResult<PersonalFeatureEntitlementDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureDefinitionInactive,
                "Personal feature definition is inactive.");
        }

        var userId = PlatformUserId.From(personalUserId);
        var existing = await _entitlements
            .ListByUserAndFeatureAsync(userId, code, cancellationToken)
            .ConfigureAwait(false);

        var activeOverlap = existing.FirstOrDefault(g =>
            g.Status == PersonalFeatureEntitlementStatus.Active
            && g.IsActiveAt(starts)
            && (endsAtUtc is null
                || g.EndsAtUtc is null
                || g.EndsAtUtc >= endsAtUtc));

        if (activeOverlap is not null
            && (endsAtUtc is null || activeOverlap.EndsAtUtc is null || activeOverlap.EndsAtUtc >= endsAtUtc)
            && activeOverlap.StartsAtUtc <= starts)
        {
            // Idempotent: existing grant already covers the requested window.
            return ApplicationResult<PersonalFeatureEntitlementDto>.Success(Map(activeOverlap, utcNow));
        }

        PersonalFeatureEntitlement grant;
        try
        {
            grant = PersonalFeatureEntitlement.Grant(
                userId,
                code,
                grantSource,
                starts,
                endsAtUtc,
                utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalFeatureEntitlementDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        await _entitlements.AddAsync(grant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalFeatureEntitlementDto>.Success(Map(grant, utcNow));
    }

    private static string DisplayNameFor(FeatureCode code) =>
        code.Value switch
        {
            PersonalFeatureCodes.DigitalRecordsExtended => "Digital Records Extended History",
            PersonalFeatureCodes.AdFree => "Ad-Free Personal",
            _ => code.Value
        };

    private static int? DefaultRewardPriceFor(FeatureCode code) =>
        code.Value switch
        {
            PersonalFeatureCodes.DigitalRecordsExtended =>
                PersonalFeatureCodes.DigitalRecordsExtendedDefaultRewardPoints,
            PersonalFeatureCodes.AdFree =>
                PersonalFeatureCodes.AdFreeDefaultRewardPoints,
            _ => null
        };

    private static PersonalFeatureEntitlementDto Map(PersonalFeatureEntitlement grant, DateTimeOffset asOfUtc) =>
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

public sealed class RevokePersonalFeature
{
    private readonly IPersonalFeatureEntitlementRepository _entitlements;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokePersonalFeature(
        IPersonalFeatureEntitlementRepository entitlements,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _entitlements = entitlements;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalFeatureEntitlementDto>> ExecuteAsync(
        Guid entitlementId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var grant = await _entitlements.GetByIdAsync(entitlementId, cancellationToken).ConfigureAwait(false);
        if (grant is null)
        {
            return ApplicationResult<PersonalFeatureEntitlementDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureEntitlementNotFound,
                "Personal feature entitlement was not found.");
        }

        var utcNow = _clock.UtcNow;
        grant.Revoke(reason, utcNow);
        await _entitlements.UpdateAsync(grant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PersonalFeatureEntitlementDto>.Success(
            new PersonalFeatureEntitlementDto(
                grant.Id,
                grant.PersonalUserId.Value,
                grant.FeatureCode.Value,
                grant.StartsAtUtc,
                grant.EndsAtUtc,
                grant.Status.ToString(),
                grant.GrantSource.ToString(),
                grant.CreatedAtUtc,
                grant.IsActiveAt(utcNow)));
    }
}

/// <summary>Personal-session check used by POS history APIs via HTTP adapter.</summary>
public sealed class GetPersonalFeatureActiveStatus
{
    private readonly IPersonalFeatureEntitlementService _entitlements;
    private readonly IClock _clock;

    public GetPersonalFeatureActiveStatus(IPersonalFeatureEntitlementService entitlements, IClock clock)
    {
        _entitlements = entitlements;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalFeatureActiveDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        FeatureCode code;
        try
        {
            code = FeatureCode.Create(featureCode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalFeatureActiveDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        var active = await _entitlements
            .HasActiveEntitlementAsync(personalUserId, code.Value, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PersonalFeatureActiveDto>.Success(
            new PersonalFeatureActiveDto(personalUserId.Value, code.Value, active));
    }
}
