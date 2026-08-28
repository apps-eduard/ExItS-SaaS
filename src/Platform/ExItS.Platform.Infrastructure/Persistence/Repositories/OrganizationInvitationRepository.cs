using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationInvitationRepository : IOrganizationInvitationRepository
{
    private readonly PlatformDbContext _db;

    public OrganizationInvitationRepository(PlatformDbContext db) => _db = db;

    public async Task<OrganizationInvitation?> GetByIdAsync(
        OrganizationInvitationId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationInvitations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<OrganizationInvitation?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(InvitationStatus.Pending);
        var record = await _db.OrganizationInvitations.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.TokenHash == tokenHash && i.Status == pending,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<OrganizationInvitation?> FindPendingByOrganizationAndEmailAsync(
        PlatformOrganizationId organizationId,
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(InvitationStatus.Pending);
        var record = await _db.OrganizationInvitations.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.OrganizationId == organizationId.Value
                     && i.NormalizedEmail == normalizedEmail
                     && i.Status == pending,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<OrganizationInvitation?> FindPendingByOrganizationAndTargetUserAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId targetPersonalUserId,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(InvitationStatus.Pending);
        var record = await _db.OrganizationInvitations.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.OrganizationId == organizationId.Value
                     && i.TargetPersonalUserId == targetPersonalUserId.Value
                     && i.Status == pending,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> ListPendingByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(InvitationStatus.Pending);
        var records = await _db.OrganizationInvitations.AsNoTracking()
            .Where(i => i.NormalizedEmail == normalizedEmail && i.Status == pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> ListPendingByTargetPersonalUserIdAsync(
        PlatformUserId targetPersonalUserId,
        CancellationToken cancellationToken = default)
    {
        var pending = nameof(InvitationStatus.Pending);
        var records = await _db.OrganizationInvitations.AsNoTracking()
            .Where(i => i.TargetPersonalUserId == targetPersonalUserId.Value && i.Status == pending)
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<OrganizationInvitation> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        InvitationStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.OrganizationInvitations.AsNoTracking()
            .Where(i => i.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(i => i.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(ToDomain).ToList(), total);
    }

    public Task AddAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        _db.OrganizationInvitations.Add(ToRecord(invitation));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(OrganizationInvitation invitation, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationInvitations
            .FirstOrDefaultAsync(i => i.Id == invitation.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException($"Organization invitation '{invitation.Id}' was not found for update.");
        }

        Apply(invitation, record);
    }

    private static OrganizationInvitation ToDomain(OrganizationInvitationRecord record) =>
        OrganizationInvitation.Rehydrate(
            OrganizationInvitationId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            record.NormalizedEmail,
            Enum.Parse<OrganizationRole>(record.Role),
            Enum.Parse<InvitationStatus>(record.Status),
            record.TokenHash,
            record.InvitedByUserId is Guid invitedBy ? PlatformUserId.From(invitedBy) : null,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.ExpiresAtUtc,
            record.AcceptedAtUtc,
            record.RevokedAtUtc,
            record.AcceptedByUserId is Guid acceptedBy ? PlatformUserId.From(acceptedBy) : null,
            record.InviteeDisplayName,
            record.FirstName,
            record.LastName,
            record.Branch,
            record.ProductRole,
            record.TargetPersonalUserId is Guid target ? PlatformUserId.From(target) : null,
            record.TargetPublicUserId,
            record.DeclinedAtUtc);

    private static OrganizationInvitationRecord ToRecord(OrganizationInvitation invitation) =>
        new()
        {
            Id = invitation.Id.Value,
            OrganizationId = invitation.OrganizationId.Value,
            NormalizedEmail = invitation.NormalizedEmail,
            Role = invitation.Role.ToString(),
            Status = invitation.Status.ToString(),
            TokenHash = invitation.TokenHash,
            InvitedByUserId = invitation.InvitedByUserId?.Value,
            CreatedAtUtc = invitation.CreatedAtUtc,
            UpdatedAtUtc = invitation.UpdatedAtUtc,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            AcceptedAtUtc = invitation.AcceptedAtUtc,
            RevokedAtUtc = invitation.RevokedAtUtc,
            DeclinedAtUtc = invitation.DeclinedAtUtc,
            AcceptedByUserId = invitation.AcceptedByUserId?.Value,
            InviteeDisplayName = invitation.InviteeDisplayName,
            FirstName = invitation.FirstName,
            LastName = invitation.LastName,
            Branch = invitation.Branch,
            ProductRole = invitation.ProductRole,
            TargetPersonalUserId = invitation.TargetPersonalUserId?.Value,
            TargetPublicUserId = invitation.TargetPublicUserId
        };

    private static void Apply(OrganizationInvitation invitation, OrganizationInvitationRecord record)
    {
        record.NormalizedEmail = invitation.NormalizedEmail;
        record.Role = invitation.Role.ToString();
        record.Status = invitation.Status.ToString();
        record.TokenHash = invitation.TokenHash;
        record.UpdatedAtUtc = invitation.UpdatedAtUtc;
        record.ExpiresAtUtc = invitation.ExpiresAtUtc;
        record.AcceptedAtUtc = invitation.AcceptedAtUtc;
        record.RevokedAtUtc = invitation.RevokedAtUtc;
        record.DeclinedAtUtc = invitation.DeclinedAtUtc;
        record.AcceptedByUserId = invitation.AcceptedByUserId?.Value;
        record.InviteeDisplayName = invitation.InviteeDisplayName;
        record.FirstName = invitation.FirstName;
        record.LastName = invitation.LastName;
        record.Branch = invitation.Branch;
        record.ProductRole = invitation.ProductRole;
        record.TargetPersonalUserId = invitation.TargetPersonalUserId?.Value;
        record.TargetPublicUserId = invitation.TargetPublicUserId;
    }
}
