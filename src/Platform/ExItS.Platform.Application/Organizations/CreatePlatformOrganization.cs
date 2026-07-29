using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed class CreatePlatformOrganization
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IClock _clock;

    public CreatePlatformOrganization(IPlatformOrganizationRepository organizations, IClock clock)
    {
        _organizations = organizations;
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

            await _organizations.AddAsync(organization, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformOrganization>.Success(organization);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformOrganization>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
