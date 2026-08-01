using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record UpdateOrganizationProfileCommand(
    string? DisplayName,
    string? LegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId,
    string? Locale,
    string? CurrencyCode,
    DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed record UpdateOrganizationPlatformCommand(
    string? DisplayName,
    string? Slug,
    string? LegalName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId,
    string? Locale,
    string? CurrencyCode,
    DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed record UpdateOrganizationBrandingCommand(
    string? BrandDisplayName,
    string? LogoUrl,
    string? PrimaryColor,
    string? AccentColor,
    DateTimeOffset? ExpectedUpdatedAtUtc);

public sealed class UpdateOrganizationProfile
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateOrganizationProfile(
        IPlatformOrganizationRepository organizations,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformOrganization>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        UpdateOrganizationProfileCommand command,
        bool requireActiveOrganization,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (requireActiveOrganization && organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Only an active Platform Organization can be updated by organization administrators.");
        }

        if (IsConcurrencyMismatch(organization, command.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The organization was modified by another request. Refresh and try again.");
        }

        try
        {
            var now = _clock.UtcNow;
            if (!string.IsNullOrWhiteSpace(command.DisplayName))
            {
                organization.Rename(command.DisplayName, now);
            }

            organization.UpdateProfile(
                OrganizationProfile.Create(
                    command.LegalName,
                    command.ContactEmail,
                    command.ContactPhone,
                    command.AddressLine1,
                    command.AddressLine2,
                    command.City,
                    command.Region,
                    command.PostalCode,
                    command.CountryCode,
                    command.TimeZoneId,
                    command.Locale,
                    command.CurrencyCode),
                now);

            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformOrganization>.Success(organization);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static bool IsConcurrencyMismatch(PlatformOrganization organization, DateTimeOffset? expected)
    {
        if (expected is null)
        {
            return false;
        }

        // JSON / PostgreSQL round-trips commonly lose sub-millisecond precision.
        var currentMs = organization.UpdatedAtUtc.ToUnixTimeMilliseconds();
        var expectedMs = expected.Value.ToUnixTimeMilliseconds();
        return currentMs != expectedMs;
    }
}

public sealed class UpdateOrganizationPlatformFields
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateOrganizationPlatformFields(
        IPlatformOrganizationRepository organizations,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformOrganization>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        UpdateOrganizationPlatformCommand command,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (UpdateOrganizationProfile.IsConcurrencyMismatch(organization, command.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The organization was modified by another request. Refresh and try again.");
        }

        try
        {
            var now = _clock.UtcNow;
            if (!string.IsNullOrWhiteSpace(command.DisplayName))
            {
                organization.Rename(command.DisplayName, now);
            }

            if (!string.IsNullOrWhiteSpace(command.Slug))
            {
                var normalized = PlatformOrganization.NormalizeSlug(command.Slug);
                if (!string.Equals(normalized, organization.Slug, StringComparison.Ordinal))
                {
                    var existing = await _organizations.GetBySlugAsync(normalized, cancellationToken).ConfigureAwait(false);
                    if (existing is not null && existing.Id != organization.Id)
                    {
                        return ApplicationResult<PlatformOrganization>.Failure(
                            ApplicationErrorCodes.SlugConflict,
                            "A Platform Organization with this slug already exists.");
                    }

                    organization.ChangeSlug(command.Slug, now);
                }
            }

            organization.UpdateProfile(
                OrganizationProfile.Create(
                    command.LegalName,
                    command.ContactEmail,
                    command.ContactPhone,
                    command.AddressLine1,
                    command.AddressLine2,
                    command.City,
                    command.Region,
                    command.PostalCode,
                    command.CountryCode,
                    command.TimeZoneId,
                    command.Locale,
                    command.CurrencyCode),
                now);

            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformOrganization>.Success(organization);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateOrganizationBranding
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdateOrganizationBranding(
        IPlatformOrganizationRepository organizations,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformOrganization>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        UpdateOrganizationBrandingCommand command,
        bool requireActiveOrganization,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (requireActiveOrganization && organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Only an active Platform Organization can update branding.");
        }

        if (UpdateOrganizationProfile.IsConcurrencyMismatch(organization, command.ExpectedUpdatedAtUtc))
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                "The organization was modified by another request. Refresh and try again.");
        }

        try
        {
            organization.UpdateBranding(
                OrganizationBranding.Create(
                    command.BrandDisplayName,
                    command.LogoUrl,
                    command.PrimaryColor,
                    command.AccentColor),
                _clock.UtcNow);

            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformOrganization>.Success(organization);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivatePlatformOrganization
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivatePlatformOrganization(
        IPlatformOrganizationRepository organizations,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformOrganization>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        try
        {
            organization.Reactivate(_clock.UtcNow);
            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformOrganization>.Success(organization);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ClosePlatformOrganization
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClosePlatformOrganization(
        IPlatformOrganizationRepository organizations,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformOrganization>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        try
        {
            organization.Close(_clock.UtcNow);
            await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);
            await _sessions
                .ClearSelectedOrganizationForOrganizationAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            await _accessTokens
                .ClearOrganizationBindingForOrganizationAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformOrganization>.Success(organization);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
