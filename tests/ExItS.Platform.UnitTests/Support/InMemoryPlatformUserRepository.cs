using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformUserRepository : IPlatformUserRepository
{
    private readonly Dictionary<Guid, PlatformUser> _byId = new();
    private readonly Dictionary<string, Guid> _emailIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> _usernameIndex = new(StringComparer.Ordinal);

    public int AddCount { get; private set; }
    public int UpdateCount { get; private set; }

    public Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var user);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<PlatformUser>> ListByIdsAsync(
        IReadOnlyCollection<PlatformUserId> ids,
        CancellationToken cancellationToken = default)
    {
        var items = ids
            .Select(id => id.Value)
            .Distinct()
            .Select(id => _byId.TryGetValue(id, out var user) ? user : null)
            .Where(u => u is not null)
            .Cast<PlatformUser>()
            .ToList();
        return Task.FromResult<IReadOnlyList<PlatformUser>>(items);
    }

    public Task<PlatformUser?> GetByPublicUserIdAsync(string publicUserId, CancellationToken cancellationToken = default)
    {
        if (!PublicUserIdRules.TryNormalize(publicUserId, out var normalized))
        {
            return Task.FromResult<PlatformUser?>(null);
        }

        var match = _byId.Values.FirstOrDefault(u =>
            string.Equals(u.PublicUserId, normalized, StringComparison.Ordinal));
        return Task.FromResult(match);
    }

    public Task<PlatformUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        if (_emailIndex.TryGetValue(normalizedEmail, out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<PlatformUser?>(user);
        }

        return Task.FromResult<PlatformUser?>(null);
    }

    public Task<PlatformUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        if (_usernameIndex.TryGetValue(normalizedUsername, out var id) && _byId.TryGetValue(id, out var user))
        {
            return Task.FromResult<PlatformUser?>(user);
        }

        return Task.FromResult<PlatformUser?>(null);
    }

    public Task<PlatformUser?> FindActiveStaffByHomeOrgAndContactEmailAsync(
        PlatformOrganizationId homeOrganizationId,
        string normalizedContactEmail,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(u =>
            u.Status == AccountStatus.Active
            && u.HomeOrganizationId == homeOrganizationId
            && string.Equals(u.NormalizedContactEmail, normalizedContactEmail, StringComparison.Ordinal));
        return Task.FromResult(match);
    }

    public Task<PlatformUser?> FindActiveStaffByHomeOrgAndLinkedPersonalUserIdAsync(
        PlatformOrganizationId homeOrganizationId,
        PlatformUserId linkedPersonalUserId,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(u =>
            u.Status == AccountStatus.Active
            && u.HomeOrganizationId == homeOrganizationId
            && u.LinkedPersonalUserId == linkedPersonalUserId);
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<PlatformUser>> ListByNormalizedContactEmailAsync(
        string normalizedContactEmail,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlatformUser> matches = _byId.Values
            .Where(u => string.Equals(u.NormalizedContactEmail, normalizedContactEmail, StringComparison.Ordinal))
            .OrderBy(u => u.NormalizedUsername, StringComparer.Ordinal)
            .ThenBy(u => u.Id.Value)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<(IReadOnlyList<PlatformUser> Items, int TotalCount)> ListAsync(
        AccountStatus? status,
        string? search,
        UserDirectoryFilter? directoryFilter,
        string? sortBy,
        bool sortDesc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _byId.Values.AsEnumerable();
        if (status is not null)
        {
            query = query.Where(u => u.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.NormalizedUsername.Contains(term, StringComparison.Ordinal)
                || u.NormalizedEmail.Contains(term, StringComparison.Ordinal)
                || u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (u.FirstName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (u.LastName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (u.StaffNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (u.EmployeeCode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _ = directoryFilter;
        _ = sortBy;
        _ = sortDesc;

        var ordered = query.OrderBy(u => u.NormalizedUsername, StringComparer.Ordinal).ToList();
        return Task.FromResult<(IReadOnlyList<PlatformUser>, int)>((ordered.Skip(skip).Take(take).ToList(), ordered.Count));
    }

    public Task<IReadOnlyDictionary<Guid, PlatformUserDirectoryExtras>> GetDirectoryExtrasAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = userIds
            .Distinct()
            .ToDictionary(
                id => id,
                _ => new PlatformUserDirectoryExtras([], []));
        return Task.FromResult<IReadOnlyDictionary<Guid, PlatformUserDirectoryExtras>>(result);
    }

    public Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        _emailIndex[user.NormalizedEmail] = user.Id.Value;
        _usernameIndex[user.NormalizedUsername] = user.Id.Value;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _byId[user.Id.Value] = user;
        _emailIndex[user.NormalizedEmail] = user.Id.Value;
        _usernameIndex[user.NormalizedUsername] = user.Id.Value;
        UpdateCount++;
        return Task.CompletedTask;
    }
}
