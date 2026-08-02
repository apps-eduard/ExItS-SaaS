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
                || u.DisplayName.ToLower().Contains(term));
        }

        if (directoryFilter is UserDirectoryFilter.Unassigned)
        {
            var removed = nameof(MembershipStatus.Removed);
            query = query.Where(u => !_db.OrganizationMemberships.Any(m =>
                m.UserId == u.Id && m.Status != removed));
        }
        else if (directoryFilter is UserDirectoryFilter.Organization)
        {
            var removed = nameof(MembershipStatus.Removed);
            query = query.Where(u => _db.OrganizationMemberships.Any(m =>
                m.UserId == u.Id && m.Status != removed));
        }
        else if (directoryFilter is UserDirectoryFilter.PlatformStaff)
        {
            var active = nameof(PlatformRoleAssignmentStatus.Active);
            query = query.Where(u =>
                _db.PlatformRoleAssignments.Any(a => a.PlatformUserId == u.Id && a.Status == active)
                || _db.PlatformCustomRoleAssignments.Any(a => a.PlatformUserId == u.Id && a.Status == active));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(u => u.NormalizedUsername)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(IdentityAccessEntityMapper.ToDomain).ToList(), total);
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
                select new { m.UserId, o.DisplayName })
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

            var orgs = memberships
                .Where(m => m.UserId == id)
                .Select(m => m.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            result[id] = new PlatformUserDirectoryExtras(classes, orgs);
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
