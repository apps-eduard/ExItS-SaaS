using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed class CreatePlatformOrganization
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPublicOrganizationIdGenerator _publicOrganizationIds;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePlatformOrganization(
        IPlatformOrganizationRepository organizations,
        IPublicOrganizationIdGenerator publicOrganizationIds,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _publicOrganizationIds = publicOrganizationIds;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformOrganization>> ExecuteAsync(
        string displayName,
        string slug,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = PlatformOrganization.Create(displayName, slug, _clock.UtcNow);
            var existing = await _organizations.GetBySlugAsync(organization.Slug, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<PlatformOrganization>.Failure(
                    ApplicationErrorCodes.SlugConflict,
                    "A Platform Organization with this slug already exists.");
            }

            var publicOrgId = await _publicOrganizationIds.GenerateUniqueAsync(cancellationToken)
                .ConfigureAwait(false);
            organization.AssignPublicOrganizationId(publicOrgId, _clock.UtcNow);

            await _organizations.AddAsync(organization, cancellationToken).ConfigureAwait(false);
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

public sealed class SuspendPlatformOrganization
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendPlatformOrganization(
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
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<PlatformOrganization>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        try
        {
            organization.Suspend(_clock.UtcNow);
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
    }
}
