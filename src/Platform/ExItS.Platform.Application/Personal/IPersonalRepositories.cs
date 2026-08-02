using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public interface IPersonalAccountSettingsRepository
{
    Task<PersonalAccountSettings?> GetByUserAsync(PlatformUserId userIdentityId, CancellationToken cancellationToken = default);

    Task AddAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalAccountSettings settings, CancellationToken cancellationToken = default);
}

public interface IPersonalContactRepository
{
    Task<PersonalContact?> GetByIdAsync(PersonalContactId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalContact>> ListByOwnerAsync(
        PlatformUserId ownerUserIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalContact contact, CancellationToken cancellationToken = default);
}

public interface IPersonalDebtRelationshipRepository
{
    Task<PersonalDebtRelationship?> GetByIdAsync(PersonalDebtRelationshipId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalDebtRelationship>> ListForUserAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalDebtRelationship relationship, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalDebtRelationship relationship, CancellationToken cancellationToken = default);
}

public interface IPersonalUtangEntryRepository
{
    Task<IReadOnlyList<PersonalUtangEntry>> ListByRelationshipAsync(
        PersonalDebtRelationshipId relationshipId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalUtangEntry entry, CancellationToken cancellationToken = default);
}
