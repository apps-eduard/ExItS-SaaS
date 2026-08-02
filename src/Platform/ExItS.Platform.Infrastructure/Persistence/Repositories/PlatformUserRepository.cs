using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformUserRepository : IPlatformUserRepository
{
    private readonly PlatformDbContext _db;

    public PlatformUserRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformUser?> GetByIdAsync(PlatformUserId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToDomain(record);
    }

    public async Task<PlatformUser?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToDomain(record);
    }

    public async Task<PlatformUser?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : IdentityAccessEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<PlatformUser> Items, int TotalCount)> ListAsync(
        AccountStatus? status,
        string? search,
        UserDirectoryFilter? directoryFilter,
        string? sortBy,
        bool sortDesc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PlatformUsers.AsNoTracking();
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(u => u.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.NormalizedUsername.Contains(term)
                || u.NormalizedEmail.Contains(term)
                || u.DisplayName.ToLower().Contains(term)
                || (u.FirstName != null && u.FirstName.ToLower().Contains(term))
                || (u.LastName != null && u.LastName.ToLower().Contains(term))
                || (u.StaffNumber != null && u.StaffNumber.ToLower().Contains(term))
                || (u.EmployeeCode != null && u.EmployeeCode.ToLower().Contains(term)));
        }

        var activeProfile = nameof(AccountStatus.Active);
        var platformClass = nameof(AccountClass.Platform);
        var organizationClass = nameof(AccountClass.Organization);
        var personalClass = nameof(AccountClass.Personal);

        if (directoryFilter is UserDirectoryFilter.Unassigned)
        {
            query = query.Where(u => !_db.AccountProfiles.Any(p =>
                p.UserIdentityId == u.Id && p.Status == activeProfile));
        }
        else if (directoryFilter is UserDirectoryFilter.Organization)
        {
            query = query.Where(u => _db.AccountProfiles.Any(p =>
                p.UserIdentityId == u.Id
                && p.Status == activeProfile
                && p.AccountClass == organizationClass));
        }
        else if (directoryFilter is UserDirectoryFilter.PlatformStaff)
        {
            query = query.Where(u => _db.AccountProfiles.Any(p =>
                p.UserIdentityId == u.Id
                && p.Status == activeProfile
                && p.AccountClass == platformClass));
        }
        else if (directoryFilter is UserDirectoryFilter.Personal)
        {
            query = query.Where(u => _db.AccountProfiles.Any(p =>
                p.UserIdentityId == u.Id
                && p.Status == activeProfile
                && p.AccountClass == personalClass));
        }

        query = ApplySort(query, sortBy, sortDesc);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(IdentityAccessEntityMapper.ToDomain).ToList(), total);
    }

    private IQueryable<PlatformUserRecord> ApplySort(
        IQueryable<PlatformUserRecord> query,
        string? sortBy,
        bool sortDesc)
    {
        var sortKey = sortBy?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(sortKey))
        {
            return query.OrderBy(u => u.NormalizedUsername).ThenBy(u => u.Id);
        }

        var activeProfile = nameof(AccountStatus.Active);
        var activeMembership = nameof(MembershipStatus.Active);
        var platformClass = nameof(AccountClass.Platform);
        var organizationClass = nameof(AccountClass.Organization);
        var personalClass = nameof(AccountClass.Personal);

        return sortKey switch
        {
            "displayname" => sortDesc
                ? query.OrderByDescending(u => u.DisplayName).ThenBy(u => u.Id)
                : query.OrderBy(u => u.DisplayName).ThenBy(u => u.Id),
            "username" => sortDesc
                ? query.OrderByDescending(u => u.NormalizedUsername).ThenBy(u => u.Id)
                : query.OrderBy(u => u.NormalizedUsername).ThenBy(u => u.Id),
            "email" => sortDesc
                ? query.OrderByDescending(u => u.NormalizedEmail).ThenBy(u => u.Id)
                : query.OrderBy(u => u.NormalizedEmail).ThenBy(u => u.Id),
            "status" => sortDesc
                ? query.OrderByDescending(u => u.Status).ThenBy(u => u.Id)
                : query.OrderBy(u => u.Status).ThenBy(u => u.Id),
            "updatedutc" => sortDesc
                ? query.OrderByDescending(u => u.UpdatedAtUtc).ThenBy(u => u.Id)
                : query.OrderBy(u => u.UpdatedAtUtc).ThenBy(u => u.Id),
            "accounttype" => ApplyAccountTypeSort(query, activeProfile, platformClass, organizationClass, personalClass, sortDesc),
            "organization" => ApplyOrganizationSort(query, activeMembership, sortDesc),
            _ => query.OrderBy(u => u.NormalizedUsername).ThenBy(u => u.Id)
        };
    }

    private IQueryable<PlatformUserRecord> ApplyAccountTypeSort(
        IQueryable<PlatformUserRecord> query,
        string activeProfile,
        string platformClass,
        string organizationClass,
        string personalClass,
        bool sortDesc)
    {
        // Prefer EXISTS-based ranks — nested ternaries over AccountClass are not reliably translated by EF.
        if (sortDesc)
        {
            return query
                .OrderByDescending(u =>
                    _db.AccountProfiles.Any(p =>
                        p.UserIdentityId == u.Id && p.Status == activeProfile && p.AccountClass == platformClass)
                        ? 0
                        : _db.AccountProfiles.Any(p =>
                            p.UserIdentityId == u.Id && p.Status == activeProfile && p.AccountClass == organizationClass)
                            ? 1
                            : _db.AccountProfiles.Any(p =>
                                p.UserIdentityId == u.Id && p.Status == activeProfile && p.AccountClass == personalClass)
                                ? 2
                                : 99)
                .ThenBy(u => u.Id);
        }

        return query
            .OrderBy(u =>
                _db.AccountProfiles.Any(p =>
                    p.UserIdentityId == u.Id && p.Status == activeProfile && p.AccountClass == platformClass)
                    ? 0
                    : _db.AccountProfiles.Any(p =>
                        p.UserIdentityId == u.Id && p.Status == activeProfile && p.AccountClass == organizationClass)
                        ? 1
                        : _db.AccountProfiles.Any(p =>
                            p.UserIdentityId == u.Id && p.Status == activeProfile && p.AccountClass == personalClass)
                            ? 2
                            : 99)
            .ThenBy(u => u.Id);
    }

    private IQueryable<PlatformUserRecord> ApplyOrganizationSort(
        IQueryable<PlatformUserRecord> query,
        string activeMembership,
        bool sortDesc)
    {
        if (sortDesc)
        {
            return query
                .OrderByDescending(u =>
                    _db.OrganizationMemberships
                        .Where(m => m.UserId == u.Id && m.Status == activeMembership)
                        .Join(
                            _db.Organizations,
                            m => m.OrganizationId,
                            o => o.Id,
                            (_, o) => o.DisplayName)
                        .Any())
                .ThenByDescending(u =>
                    _db.OrganizationMemberships
                        .Where(m => m.UserId == u.Id && m.Status == activeMembership)
                        .Join(
                            _db.Organizations,
                            m => m.OrganizationId,
                            o => o.Id,
                            (_, o) => o.DisplayName)
                        .Min())
                .ThenBy(u => u.Id);
        }

        return query.OrderBy(u =>
                _db.OrganizationMemberships
                    .Where(m => m.UserId == u.Id && m.Status == activeMembership)
                    .Join(
                        _db.Organizations,
                        m => m.OrganizationId,
                        o => o.Id,
                        (_, o) => o.DisplayName)
                    .Min() ?? string.Empty)
            .ThenBy(u => u.Id);
    }

    public async Task<IReadOnlyDictionary<Guid, PlatformUserDirectoryExtras>> GetDirectoryExtrasAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, PlatformUserDirectoryExtras>();
        }

        var ids = userIds.Distinct().ToArray();
        var activeProfile = nameof(AccountStatus.Active);
        var activeMembership = nameof(MembershipStatus.Active);

        var profiles = await _db.AccountProfiles.AsNoTracking()
            .Where(p => ids.Contains(p.UserIdentityId) && p.Status == activeProfile)
            .Select(p => new { p.UserIdentityId, p.AccountClass })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var memberships = await (
                from m in _db.OrganizationMemberships.AsNoTracking()
                join o in _db.Organizations.AsNoTracking() on m.OrganizationId equals o.Id
                where ids.Contains(m.UserId) && m.Status == activeMembership
                orderby o.DisplayName
                select new { m.UserId, o.DisplayName, m.Role })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var classOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(AccountClass.Platform)] = 0,
            [nameof(AccountClass.Organization)] = 1,
            [nameof(AccountClass.Personal)] = 2
        };

        var result = new Dictionary<Guid, PlatformUserDirectoryExtras>(ids.Length);
        foreach (var id in ids)
        {
            var classes = profiles
                .Where(p => p.UserIdentityId == id)
                .Select(p => p.AccountClass)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => classOrder.TryGetValue(c, out var rank) ? rank : 99)
                .ThenBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var organizations = memberships
                .Where(m => m.UserId == id)
                .GroupBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var row = g.First();
                    var role = Enum.TryParse<OrganizationRole>(row.Role, ignoreCase: true, out var parsed)
                        ? parsed
                        : OrganizationRole.OrganizationMember;
                    return new PlatformUserOrganizationDirectoryItem(
                        row.DisplayName,
                        role.ToString(),
                        OrganizationRoleDisplay.ToDisplayLabel(role));
                })
                .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            result[id] = new PlatformUserDirectoryExtras(
                classes,
                organizations.Select(o => o.Name).ToList(),
                organizations);
        }

        return result;
    }

    public Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        _db.PlatformUsers.Add(IdentityAccessEntityMapper.ToRecord(user));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformUser user, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformUsers
            .FirstOrDefaultAsync(u => u.Id == user.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        IdentityAccessEntityMapper.ApplyToRecord(user, record);
    }
}
