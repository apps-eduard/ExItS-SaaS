using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

public interface IStaffLoginNameAllocator
{
    Task<string> AllocateAsync(
        string contactEmail,
        string publicOrganizationId,
        CancellationToken cancellationToken = default);
}
