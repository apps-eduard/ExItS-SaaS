using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryOrganizationInvitationRepository : IOrganizationInvitationRepository
{
    private readonly Dictionary<Guid, OrganizationInvitation> _byId = new();

    public Task<OrganizationInvitation?> GetByIdAsync(
        OrganizationInvitationId id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.TryGetValue(id.Value, out var invitation) ? invitation : null);

    public Task<OrganizationInvitation?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(i =>
            i.Status == InvitationStatus.Pending
            && string.Equals(i.TokenHash, tokenHash, StringComparison.Ordinal)));

    public Task<OrganizationInvitation?> FindPendingByOrganizationAndEmailAsync(
        PlatformOrganizationId organizationId,
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(i =>
            i.OrganizationId == organizationId
            && i.Status == InvitationStatus.Pending
            && string.Equals(i.NormalizedEmail, normalizedEmail, StringComparison.Ordinal)));

    public Task<IReadOnlyList<OrganizationInvitation>> ListPendingByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationInvitation>>(
            _byId.Values
                .Where(i =>
                    i.Status == InvitationStatus.Pending
                    && string.Equals(i.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
                .OrderByDescending(i => i.CreatedAtUtc)
                .ToList());

    public Task<(IReadOnlyList<OrganizationInvitation> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        InvitationStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.Where(i => i.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        var list = query.OrderByDescending(i => i.CreatedAtUtc).ToList();
        return Task.FromResult<(IReadOnlyList<OrganizationInvitation>, int)>((
            list.Skip(skip).Take(take).ToList(),
            list.Count));
    }

    public Task AddAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        _byId[invitation.Id.Value] = invitation;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        _byId[invitation.Id.Value] = invitation;
        return Task.CompletedTask;
    }
}
