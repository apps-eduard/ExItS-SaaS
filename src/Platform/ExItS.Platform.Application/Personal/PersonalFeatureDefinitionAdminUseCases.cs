using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalFeatureDefinitionDto(
    string FeatureCode,
    string DisplayName,
    bool IsActive,
    int? RewardPointsPrice,
    int? DefaultEntitlementDurationDays,
    bool IsRewardRedeemable,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdatePersonalFeatureDefinitionCommand(
    string DisplayName,
    bool IsActive,
    int? RewardPointsPrice,
    int? DefaultEntitlementDurationDays,
    DateTimeOffset? ExpectedUpdatedAtUtc);

/// <summary>
/// Ensures known Personal feature catalog rows exist, then lists all definitions.
/// Seed defaults are development placeholders — Admin configuration owns live economics.
/// </summary>
public sealed class ListPersonalFeatureDefinitions
{
    private readonly IPersonalFeatureDefinitionRepository _definitions;
    private readonly EnsureKnownPersonalFeatureDefinitions _ensureKnown;
    private readonly IPlatformUnitOfWork _unitOfWork;

    public ListPersonalFeatureDefinitions(
        IPersonalFeatureDefinitionRepository definitions,
        EnsureKnownPersonalFeatureDefinitions ensureKnown,
        IPlatformUnitOfWork unitOfWork)
    {
        _definitions = definitions;
        _ensureKnown = ensureKnown;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalFeatureDefinitionDto>>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        await _ensureKnown.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var items = await _definitions.ListAllAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<PersonalFeatureDefinitionDto>>.Success(
            items
                .OrderBy(d => d.FeatureCode.Value, StringComparer.Ordinal)
                .Select(Map)
                .ToList());
    }

    internal static PersonalFeatureDefinitionDto Map(PersonalFeatureDefinition definition) =>
        new(
            definition.FeatureCode.Value,
            definition.DisplayName,
            definition.IsActive,
            definition.RewardPointsPrice,
            definition.DefaultEntitlementDurationDays,
            definition.IsRewardRedeemable,
            definition.CreatedAtUtc,
            definition.UpdatedAtUtc);
}

public sealed class GetPersonalFeatureDefinition
{
    private readonly IPersonalFeatureDefinitionRepository _definitions;
    private readonly EnsureKnownPersonalFeatureDefinitions _ensureKnown;
    private readonly IPlatformUnitOfWork _unitOfWork;

    public GetPersonalFeatureDefinition(
        IPersonalFeatureDefinitionRepository definitions,
        EnsureKnownPersonalFeatureDefinitions ensureKnown,
        IPlatformUnitOfWork unitOfWork)
    {
        _definitions = definitions;
        _ensureKnown = ensureKnown;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult<PersonalFeatureDefinitionDto>> ExecuteAsync(
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
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _ensureKnown.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var definition = await _definitions.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureDefinitionNotFound,
                "Personal feature was not found.");
        }

        return ApplicationResult<PersonalFeatureDefinitionDto>.Success(
            ListPersonalFeatureDefinitions.Map(definition));
    }
}

/// <summary>
/// Platform Admin update of Personal feature commercial configuration.
/// FeatureCode is immutable. Does not rewrite historical entitlements or reward transactions.
/// </summary>
public sealed class UpdatePersonalFeatureDefinition
{
    private readonly IPersonalFeatureDefinitionRepository _definitions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalFeatureDefinition(
        IPersonalFeatureDefinitionRepository definitions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _definitions = definitions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalFeatureDefinitionDto>> ExecuteAsync(
        string featureCode,
        UpdatePersonalFeatureDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        FeatureCode code;
        try
        {
            code = FeatureCode.Create(featureCode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(ex.ErrorCode, ex.Message);
        }

        if (!IsKnownPersonalFeature(code.Value))
        {
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureDefinitionNotFound,
                "Personal feature was not found.");
        }

        var definition = await _definitions.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (definition is null)
        {
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(
                ApplicationErrorCodes.PersonalFeatureDefinitionNotFound,
                "Personal feature was not found.");
        }

        if (RenameProduct.IsConcurrencyMismatch(definition.UpdatedAtUtc, command.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The Personal feature was modified by another request. Refresh and try again.");
        }

        var utcNow = _clock.UtcNow;
        try
        {
            definition.SetDisplayName(command.DisplayName, utcNow);
            definition.SetActive(command.IsActive, utcNow);
            definition.SetRewardPointsPrice(command.RewardPointsPrice, utcNow);
            definition.SetDefaultEntitlementDurationDays(command.DefaultEntitlementDurationDays, utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalFeatureDefinitionDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _definitions.UpdateAsync(definition, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalFeatureDefinitionDto>.Success(
            ListPersonalFeatureDefinitions.Map(definition));
    }

    public static bool IsKnownPersonalFeature(string featureCode) =>
        featureCode is PersonalFeatureCodes.DigitalRecordsExtended or PersonalFeatureCodes.AdFree;
}

/// <summary>Idempotently seeds known Personal feature definitions when missing.</summary>
public sealed class EnsureKnownPersonalFeatureDefinitions
{
    private readonly IPersonalFeatureDefinitionRepository _definitions;
    private readonly IClock _clock;

    public EnsureKnownPersonalFeatureDefinitions(
        IPersonalFeatureDefinitionRepository definitions,
        IClock clock)
    {
        _definitions = definitions;
        _clock = clock;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        await EnsureAsync(
            PersonalFeatureCodes.DigitalRecordsExtendedCode,
            "Digital Records Extended History",
            PersonalFeatureCodes.DigitalRecordsExtendedDefaultRewardPoints,
            utcNow,
            cancellationToken).ConfigureAwait(false);
        await EnsureAsync(
            PersonalFeatureCodes.AdFreeCode,
            "Ad-Free Personal",
            PersonalFeatureCodes.AdFreeDefaultRewardPoints,
            utcNow,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureAsync(
        FeatureCode code,
        string displayName,
        int defaultRewardPrice,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _definitions.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var created = PersonalFeatureDefinition.Create(
            code,
            displayName,
            utcNow,
            isActive: true,
            rewardPointsPrice: defaultRewardPrice,
            defaultEntitlementDurationDays: null);
        await _definitions.AddAsync(created, cancellationToken).ConfigureAwait(false);
    }
}
