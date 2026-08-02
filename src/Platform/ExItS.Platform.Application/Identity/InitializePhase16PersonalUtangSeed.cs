using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Development/Testing/LocalValidation-only Personal Utang sample seed (idempotent). Never runs in Production.
/// </summary>
public sealed class InitializePhase16PersonalUtangSeed
{
    private readonly IHostEnvironment _environment;
    private readonly IPlatformUserRepository _users;
    private readonly CreatePersonalContact _createContact;
    private readonly CreatePersonalDebtRelationship _createRelationship;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly ILogger<InitializePhase16PersonalUtangSeed> _logger;

    public InitializePhase16PersonalUtangSeed(
        IHostEnvironment environment,
        IPlatformUserRepository users,
        CreatePersonalContact createContact,
        CreatePersonalDebtRelationship createRelationship,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        ILogger<InitializePhase16PersonalUtangSeed> logger)
    {
        _environment = environment;
        _users = users;
        _createContact = createContact;
        _createRelationship = createRelationship;
        _contacts = contacts;
        _relationships = relationships;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_environment.IsProduction())
        {
            throw new InvalidOperationException("Phase 16 Personal Utang seed must never run in Production.");
        }

        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing")
            && !_environment.IsEnvironment("Staging"))
        {
            return;
        }

        var user1 = await _users.GetByNormalizedUsernameAsync("personal.user1", cancellationToken)
            .ConfigureAwait(false);
        var user2 = await _users.GetByNormalizedUsernameAsync("personal.user2", cancellationToken)
            .ConfigureAwait(false);
        if (user1 is null || user2 is null)
        {
            return;
        }

        var existing = await _relationships.ListForUserAsync(user1.Id, cancellationToken).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            _logger.LogDebug("Phase 16 Personal Utang seed already applied for personal.user1.");
            return;
        }

        _logger.LogInformation("Phase 16 Personal Utang sample seed beginning.");

        var coworker = await EnsureContactAsync(
            user1.Id,
            "Seed Coworker Ana",
            phone: "+639171234567",
            email: "ana.coworker@example.test",
            cancellationToken).ConfigureAwait(false);

        await _createRelationship.ExecuteAsync(
            user1.Id,
            new CreatePersonalDebtRelationshipRequest(
                CreditorUserIdentityId: user1.Id.Value,
                CreditorContactId: null,
                DebtorUserIdentityId: null,
                DebtorContactId: coworker.Id,
                CurrencyCode: "PHP",
                DueDateUtc: null,
                InitialLoanAmount: 500m,
                InitialLoanNotes: "Seed lunch advance"),
            cancellationToken).ConfigureAwait(false);

        await _createRelationship.ExecuteAsync(
            user1.Id,
            new CreatePersonalDebtRelationshipRequest(
                CreditorUserIdentityId: user2.Id.Value,
                CreditorContactId: null,
                DebtorUserIdentityId: user1.Id.Value,
                DebtorContactId: null,
                CurrencyCode: "PHP",
                DueDateUtc: null,
                InitialLoanAmount: 200m,
                InitialLoanNotes: "Seed borrowed from personal.user2"),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Phase 16 Personal Utang sample seed finished.");
    }

    private async Task<PersonalContactDto> EnsureContactAsync(
        PlatformUserId ownerUserIdentityId,
        string displayName,
        string? phone,
        string? email,
        CancellationToken cancellationToken)
    {
        var contacts = await _contacts.ListByOwnerAsync(ownerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var existing = contacts.FirstOrDefault(c =>
            string.Equals(c.DisplayName, displayName, StringComparison.Ordinal));
        if (existing is not null)
        {
            return CreatePersonalContact.ToDto(existing);
        }

        var created = await _createContact.ExecuteAsync(
            ownerUserIdentityId,
            new CreatePersonalContactRequest(displayName, phone, email),
            cancellationToken).ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            throw new InvalidOperationException(created.ErrorMessage ?? "Unable to create seed personal contact.");
        }

        return created.Value;
    }
}
