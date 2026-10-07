using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class StaffPasswordResetRequestRepository : IStaffPasswordResetRequestRepository
{
    private static readonly string[] OpenStatuses =
    [
        nameof(StaffPasswordResetRequestStatus.Pending),
        nameof(StaffPasswordResetRequestStatus.Approved)
    ];

    private readonly PlatformDbContext _db;

    public StaffPasswordResetRequestRepository(PlatformDbContext db) => _db = db;

    public async Task<StaffPasswordResetRequest?> GetByIdAsync(
        StaffPasswordResetRequestId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.StaffPasswordResetRequests.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<StaffPasswordResetRequest?> FindOpenByStaffUserAsync(
        PlatformUserId staffUserId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.StaffPasswordResetRequests.AsNoTracking()
            .Where(row => row.StaffUserId == staffUserId.Value && OpenStatuses.Contains(row.Status))
            .OrderByDescending(row => row.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<StaffPasswordResetRequest>> ListOpenByStaffUsersAsync(
        IReadOnlyCollection<PlatformUserId> staffUserIds,
        CancellationToken cancellationToken = default)
    {
        if (staffUserIds.Count == 0)
        {
            return [];
        }

        var ids = staffUserIds.Select(id => id.Value).ToArray();
        var records = await _db.StaffPasswordResetRequests.AsNoTracking()
            .Where(row => ids.Contains(row.StaffUserId) && OpenStatuses.Contains(row.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<StaffPasswordResetRequest>> ListOpenByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.StaffPasswordResetRequests.AsNoTracking()
            .Where(row => row.OrganizationId == organizationId.Value && OpenStatuses.Contains(row.Status))
            .OrderBy(row => row.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public Task AddAsync(StaffPasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        _db.StaffPasswordResetRequests.Add(ToRecord(request));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(StaffPasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _db.StaffPasswordResetRequests
            .FirstOrDefaultAsync(row => row.Id == request.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException(
                $"Staff password reset request '{request.Id}' was not found for update.");
        }

        record.Status = request.Status.ToString();
        record.ExpiresAtUtc = request.ExpiresAtUtc;
        record.DecidedAtUtc = request.DecidedAtUtc;
        record.DecidedByUserId = request.DecidedByUserId?.Value;
        record.CompletedAtUtc = request.CompletedAtUtc;
        record.UpdatedAtUtc = request.UpdatedAtUtc;
    }

    private static StaffPasswordResetRequest ToDomain(StaffPasswordResetRequestRecord record) =>
        StaffPasswordResetRequest.Rehydrate(
            StaffPasswordResetRequestId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            PlatformUserId.From(record.StaffUserId),
            OrganizationMembershipId.From(record.MembershipId),
            PlatformUserId.From(record.RequestedByUserId),
            Enum.Parse<StaffPasswordResetRequestStatus>(record.Status),
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.DecidedAtUtc,
            record.DecidedByUserId is null ? null : PlatformUserId.From(record.DecidedByUserId.Value),
            record.CompletedAtUtc,
            record.UpdatedAtUtc);

    private static StaffPasswordResetRequestRecord ToRecord(StaffPasswordResetRequest request) =>
        new()
        {
            Id = request.Id.Value,
            OrganizationId = request.OrganizationId.Value,
            StaffUserId = request.StaffUserId.Value,
            MembershipId = request.MembershipId.Value,
            RequestedByUserId = request.RequestedByUserId.Value,
            Status = request.Status.ToString(),
            CreatedAtUtc = request.CreatedAtUtc,
            ExpiresAtUtc = request.ExpiresAtUtc,
            DecidedAtUtc = request.DecidedAtUtc,
            DecidedByUserId = request.DecidedByUserId?.Value,
            CompletedAtUtc = request.CompletedAtUtc,
            UpdatedAtUtc = request.UpdatedAtUtc
        };
}
