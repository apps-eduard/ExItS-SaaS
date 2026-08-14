using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OwnershipTransferUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Request_and_accept_removes_former_owner_and_creates_sole_new_owner()
    {
        var h = await Harness.CreateAsync();
        var request = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.Recipient.PublicUserId!);
        Assert.True(request.IsSuccess);

        var accept = await h.Accept.ExecuteAsync(
            OrganizationOwnershipTransferId.From(request.Value!.Id),
            h.Recipient.Id);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);

        Assert.Equal(1, await h.Memberships.CountActiveGoverningAdminsAsync(h.OrgA.Id));
        var newOwner = await h.Memberships.FindActiveByUserAndOrganizationAsync(h.Recipient.Id, h.OrgA.Id);
        Assert.NotNull(newOwner);
        Assert.Equal(OrganizationRole.OrganizationOwner, newOwner!.Role);

        var former = await h.Memberships.FindActiveByUserAndOrganizationAsync(h.OwnerA.Id, h.OrgA.Id);
        Assert.Null(former);
        var formerAny = await h.Memberships.GetByIdAsync(
            (await h.Memberships.ListByUserAsync(h.OwnerA.Id, null, 0, 10)).Items
                .Single(m => m.OrganizationId == h.OrgA.Id).Id);
        Assert.Equal(MembershipStatus.Removed, formerAny!.Status);

        // Org B ownership preserved.
        Assert.Equal(1, await h.Memberships.CountActiveGoverningAdminsAsync(h.OrgB.Id));
        var ownerB = await h.Memberships.FindActiveByUserAndOrganizationAsync(h.OwnerA.Id, h.OrgB.Id);
        Assert.NotNull(ownerB);
        Assert.Equal(OrganizationRole.OrganizationOwner, ownerB!.Role);

        Assert.Equal(h.OrgAPublicId, h.OrgA.PublicOrganizationId);
        Assert.Equal("Transfer Store A", h.OrgA.DisplayName);
    }

    [Fact]
    public async Task Accept_promotes_existing_member_without_duplicate()
    {
        var h = await Harness.CreateAsync();
        var existing = OrganizationMembership.Create(
            h.OrgA.Id,
            h.Recipient.Id,
            OrganizationRole.OrganizationMember,
            T0);
        // Personal can hold Member in tests; production staff seats use org-scoped identities.
        await h.Memberships.AddAsync(existing);

        var request = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.Recipient.PublicUserId!);
        Assert.True(request.IsSuccess);
        var accept = await h.Accept.ExecuteAsync(
            OrganizationOwnershipTransferId.From(request.Value!.Id),
            h.Recipient.Id);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);

        var (items, _) = await h.Memberships.ListByUserAsync(h.Recipient.Id, null, 0, 20);
        var orgAMemberships = items.Where(m => m.OrganizationId == h.OrgA.Id).ToList();
        Assert.Single(orgAMemberships);
        Assert.Equal(OrganizationRole.OrganizationOwner, orgAMemberships[0].Role);
        Assert.Equal(MembershipStatus.Active, orgAMemberships[0].Status);
    }

    [Fact]
    public async Task Decline_and_cancel_and_admin_cannot_initiate()
    {
        var h = await Harness.CreateAsync();
        var request = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.Recipient.PublicUserId!);
        Assert.True(request.IsSuccess);

        var decline = await h.Decline.ExecuteAsync(
            OrganizationOwnershipTransferId.From(request.Value!.Id),
            h.Recipient.Id);
        Assert.True(decline.IsSuccess);
        Assert.Equal("Declined", decline.Value!.Status);

        var again = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.Recipient.PublicUserId!);
        Assert.True(again.IsSuccess);
        var cancel = await h.Cancel.ExecuteAsync(
            OrganizationOwnershipTransferId.From(again.Value!.Id),
            h.OwnerA.Id);
        Assert.True(cancel.IsSuccess);

        var adminDenied = await h.Request.ExecuteAsync(h.OrgA.Id, h.Admin.Id, h.Recipient.PublicUserId!);
        Assert.False(adminDenied.IsSuccess);
        Assert.Equal(DomainErrorCodes.AuthorizationDenied, adminDenied.ErrorCode);
    }

    [Fact]
    public async Task Wrong_recipient_cannot_accept_and_self_transfer_rejected()
    {
        var h = await Harness.CreateAsync();
        var self = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.OwnerA.PublicUserId!);
        Assert.False(self.IsSuccess);
        Assert.Equal(DomainErrorCodes.OwnershipTransferSelfDenied, self.ErrorCode);

        var request = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.Recipient.PublicUserId!);
        Assert.True(request.IsSuccess);
        var wrong = await h.Accept.ExecuteAsync(
            OrganizationOwnershipTransferId.From(request.Value!.Id),
            h.Admin.Id);
        Assert.False(wrong.IsSuccess);
        Assert.Equal(DomainErrorCodes.OwnershipTransferActorMismatch, wrong.ErrorCode);
    }

    [Fact]
    public async Task Expired_pending_is_cleared_on_get()
    {
        var h = await Harness.CreateAsync();
        var request = await h.Request.ExecuteAsync(h.OrgA.Id, h.OwnerA.Id, h.Recipient.PublicUserId!);
        Assert.True(request.IsSuccess);

        h.Clock.UtcNow = T0.AddDays(8);
        var pending = await h.GetPending.ExecuteAsync(h.OrgA.Id);
        Assert.True(pending.IsSuccess);
        Assert.Null(pending.Value);
    }

    private sealed class Harness
    {
        public required InMemoryPlatformUserRepository Users { get; init; }
        public required InMemoryPlatformOrganizationRepository Organizations { get; init; }
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required InMemoryOrganizationOwnershipTransferRepository Transfers { get; init; }
        public required FixedClock Clock { get; init; }
        public required PlatformUser OwnerA { get; init; }
        public required PlatformUser Recipient { get; init; }
        public required PlatformUser Admin { get; init; }
        public required PlatformOrganization OrgA { get; init; }
        public required PlatformOrganization OrgB { get; init; }
        public required string? OrgAPublicId { get; init; }
        public required RequestOwnershipTransfer Request { get; init; }
        public required AcceptOwnershipTransfer Accept { get; init; }
        public required DeclineOwnershipTransfer Decline { get; init; }
        public required CancelOwnershipTransfer Cancel { get; init; }
        public required GetPendingOwnershipTransferForOrg GetPending { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var users = new InMemoryPlatformUserRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var transfers = new InMemoryOrganizationOwnershipTransferRepository();
            var assignments = new InMemoryProductAccessAssignmentRepository();
            var sessions = new InMemoryPlatformAuthSessionRepository();
            var tokens = new InMemoryPlatformAccessTokenRepository();
            var profiles = new InMemoryAccountProfileRepository();
            var roles = new InMemoryPlatformRoleAssignmentRepository();
            var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);
            var audit = new NoOpAuditWriter();

            var orgA = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
                .ExecuteAsync("Transfer Store A", "transfer-store-a")).Value!;

            var orgB = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
                .ExecuteAsync("Transfer Store B", "transfer-store-b")).Value!;

            var ownerA = PlatformUser.Create("ownera", "Owner A", "ownera@example.com", T0);
            ownerA.AssignPublicUserId("EX-1000-0001", T0);
            var recipient = PlatformUser.Create("recipient", "Recipient User", "recipient@example.com", T0);
            recipient.AssignPublicUserId("EX-2000-0002", T0);
            var admin = PlatformUser.Create("adminuser", "Admin User", "admin@example.com", T0);
            admin.AssignPublicUserId("EX-3000-0003", T0);
            await users.AddAsync(ownerA);
            await users.AddAsync(recipient);
            await users.AddAsync(admin);

            await memberships.AddAsync(
                OrganizationMembership.Create(orgA.Id, ownerA.Id, OrganizationRole.OrganizationOwner, T0));
            await memberships.AddAsync(
                OrganizationMembership.Create(orgB.Id, ownerA.Id, OrganizationRole.OrganizationOwner, T0));
            await memberships.AddAsync(
                OrganizationMembership.Create(orgA.Id, admin.Id, OrganizationRole.OrganizationAdministrator, T0));

            var resolve = new ResolveOwnershipTransferTarget(users);
            var request = new RequestOwnershipTransfer(
                orgs, users, memberships, transfers, uow, clock, audit, resolve);
            var accept = new AcceptOwnershipTransfer(
                transfers, memberships, orgs, users, assignments, sessions, tokens, ensure, uow, clock, audit);
            var decline = new DeclineOwnershipTransfer(transfers, orgs, users, uow, clock, audit);
            var cancel = new CancelOwnershipTransfer(transfers, memberships, orgs, users, uow, clock, audit);
            var getPending = new GetPendingOwnershipTransferForOrg(transfers, orgs, users, uow, clock);

            return new Harness
            {
                Users = users,
                Organizations = orgs,
                Memberships = memberships,
                Transfers = transfers,
                Clock = clock,
                OwnerA = ownerA,
                Recipient = recipient,
                Admin = admin,
                OrgA = orgA,
                OrgB = orgB,
                OrgAPublicId = orgA.PublicOrganizationId,
                Request = request,
                Accept = accept,
                Decline = decline,
                Cancel = cancel,
                GetPending = getPending
            };
        }
    }
}

internal sealed class InMemoryOrganizationOwnershipTransferRepository : IOrganizationOwnershipTransferRepository
{
    private readonly Dictionary<Guid, OrganizationOwnershipTransfer> _byId = new();

    public Task<OrganizationOwnershipTransfer?> GetByIdAsync(
        OrganizationOwnershipTransferId id,
        CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id.Value, out var transfer);
        return Task.FromResult(transfer);
    }

    public Task<OrganizationOwnershipTransfer?> FindPendingByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var match = _byId.Values.FirstOrDefault(t =>
            t.OrganizationId == organizationId
            && t.Status == OrganizationOwnershipTransferStatus.Pending);
        return Task.FromResult(match);
    }

    public Task<IReadOnlyList<OrganizationOwnershipTransfer>> ListPendingByRecipientAsync(
        PlatformUserId toUserId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrganizationOwnershipTransfer> items = _byId.Values
            .Where(t => t.ToUserId == toUserId && t.Status == OrganizationOwnershipTransferStatus.Pending)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToList();
        return Task.FromResult(items);
    }

    public Task AddAsync(OrganizationOwnershipTransfer transfer, CancellationToken cancellationToken = default)
    {
        if (_byId.Values.Any(t =>
                t.OrganizationId == transfer.OrganizationId
                && t.Status == OrganizationOwnershipTransferStatus.Pending))
        {
            throw new PersistenceConflictException(
                DomainErrorCodes.OwnershipTransferPendingConflict,
                "This organization already has a pending ownership transfer. Cancel it first.");
        }

        _byId[transfer.Id.Value] = transfer;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrganizationOwnershipTransfer transfer, CancellationToken cancellationToken = default)
    {
        _byId[transfer.Id.Value] = transfer;
        return Task.CompletedTask;
    }
}
