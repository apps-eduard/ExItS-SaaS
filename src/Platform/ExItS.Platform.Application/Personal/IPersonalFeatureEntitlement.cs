using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public interface IPersonalFeatureDefinitionRepository
{
    Task<PersonalFeatureDefinition?> GetByCodeAsync(
        FeatureCode featureCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalFeatureDefinition>> ListAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalFeatureDefinition definition, CancellationToken cancellationToken = default);
}

public interface IPersonalFeatureEntitlementRepository
{
    Task<PersonalFeatureEntitlement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalFeatureEntitlement>> ListByUserAndFeatureAsync(
        PlatformUserId personalUserId,
        FeatureCode featureCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalFeatureEntitlement entitlement, CancellationToken cancellationToken = default);
}

/// <summary>Resolves Personal-scoped feature entitlements (never Organization subscriptions).</summary>
public interface IPersonalFeatureEntitlementService
{
    Task<bool> HasActiveEntitlementAsync(
        PlatformUserId personalUserId,
        string featureCode,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}
