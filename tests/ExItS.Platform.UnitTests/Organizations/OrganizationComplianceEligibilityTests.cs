using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationComplianceEligibilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 21, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Owner_request_sets_requested_without_enabling_issuance()
    {
        var fixture = await Fixture.CreateAsync();

        var result = await fixture.Request.ExecuteAsync(
            fixture.OrganizationId,
            fixture.OwnerId,
            fixture.OwnerId.Value.ToString("D"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.Requested, result.Value!.ComplianceEligibilityStatus);
        Assert.False(result.Value.TaxDocumentIssuanceEnabled);
        Assert.False(result.Value.TaxDocumentImplementationAvailable);
        Assert.Contains(PlatformAuditActions.OrganizationComplianceRequested, fixture.Audit.Actions);
    }

    [Fact]
    public async Task Non_owner_cannot_request()
    {
        var fixture = await Fixture.CreateAsync();
        var adminId = PlatformUserId.New();
        await fixture.Memberships.AddAsync(OrganizationMembership.Create(
            fixture.OrganizationId,
            adminId,
            OrganizationRole.OrganizationAdministrator,
            Now));

        var result = await fixture.Request.ExecuteAsync(
            fixture.OrganizationId,
            adminId,
            adminId.Value.ToString("D"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ComplianceOwnerRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Platform_lifecycle_requires_ack_before_enable_and_never_implements_issuance()
    {
        var fixture = await Fixture.CreateAsync();

        Assert.True((await fixture.Request.ExecuteAsync(
            fixture.OrganizationId, fixture.OwnerId, "owner")).IsSuccess);
        Assert.True((await fixture.Transition.ExecuteAsync(
            fixture.OrganizationId,
            OrganizationComplianceEligibilityStatuses.UnderReview,
            "platform-admin")).IsSuccess);
        Assert.True((await fixture.Transition.ExecuteAsync(
            fixture.OrganizationId,
            OrganizationComplianceEligibilityStatuses.Approved,
            "platform-admin")).IsSuccess);

        var enableWithoutAck = await fixture.SetIssuance.ExecuteAsync(
            fixture.OrganizationId, enabled: true, "platform-admin");
        Assert.False(enableWithoutAck.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SalesDocumentEducationOwnerRequired, enableWithoutAck.ErrorCode);

        await fixture.Acknowledgments.AddAsync(OrganizationSalesDocumentAcknowledgment.Create(
            fixture.OrganizationId,
            fixture.OwnerId,
            SalesDocumentEducationVersions.Current,
            Now,
            SalesDocumentEducationVersions.Current));

        var enable = await fixture.SetIssuance.ExecuteAsync(
            fixture.OrganizationId, enabled: true, "platform-admin");
        Assert.True(enable.IsSuccess);
        Assert.True(enable.Value!.TaxDocumentIssuanceEnabled);
        Assert.False(enable.Value.TaxDocumentImplementationAvailable);
        Assert.Contains(PlatformAuditActions.OrganizationTaxDocumentCapabilityEnabled, fixture.Audit.Actions);

        var ensure = await new EnsureTaxDocumentIssuanceAllowed(fixture.Capabilities)
            .ExecuteAsync(fixture.OrganizationId);
        Assert.False(ensure.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.TaxDocumentIssuanceNotImplemented, ensure.ErrorCode);

        var suspend = await fixture.Transition.ExecuteAsync(
            fixture.OrganizationId,
            OrganizationComplianceEligibilityStatuses.Suspended,
            "platform-admin");
        Assert.True(suspend.IsSuccess);
        Assert.False(suspend.Value!.TaxDocumentIssuanceEnabled);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.Suspended, suspend.Value.ComplianceEligibilityStatus);
    }

    [Fact]
    public async Task Multi_org_isolation_and_invalid_transition()
    {
        var fixture = await Fixture.CreateAsync();
        var orgB = PlatformOrganizationId.New();
        await fixture.Memberships.AddAsync(OrganizationMembership.Create(
            orgB,
            fixture.OwnerId,
            OrganizationRole.OrganizationOwner,
            Now));

        Assert.True((await fixture.Request.ExecuteAsync(
            fixture.OrganizationId, fixture.OwnerId, "owner")).IsSuccess);
        var statusB = await fixture.GetStatus.ExecuteAsync(orgB);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.NotRequested, statusB.Value!.ComplianceEligibilityStatus);

        var invalid = await fixture.Transition.ExecuteAsync(
            fixture.OrganizationId,
            OrganizationComplianceEligibilityStatuses.Approved,
            "platform-admin");
        Assert.False(invalid.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.ComplianceInvalidTransition, invalid.ErrorCode);
    }

    [Fact]
    public async Task Ownership_transfer_preserves_org_compliance_and_requires_new_owner_ack_for_enable()
    {
        var fixture = await Fixture.CreateAsync();

        Assert.True((await fixture.Request.ExecuteAsync(
            fixture.OrganizationId, fixture.OwnerId, "eduard")).IsSuccess);
        Assert.True((await fixture.Transition.ExecuteAsync(
            fixture.OrganizationId,
            OrganizationComplianceEligibilityStatuses.UnderReview,
            "platform")).IsSuccess);
        Assert.True((await fixture.Transition.ExecuteAsync(
            fixture.OrganizationId,
            OrganizationComplianceEligibilityStatuses.Approved,
            "platform")).IsSuccess);
        await fixture.Acknowledgments.AddAsync(OrganizationSalesDocumentAcknowledgment.Create(
            fixture.OrganizationId,
            fixture.OwnerId,
            SalesDocumentEducationVersions.Current,
            Now,
            SalesDocumentEducationVersions.Current));
        Assert.True((await fixture.SetIssuance.ExecuteAsync(
            fixture.OrganizationId, true, "platform")).IsSuccess);

        var formerOwner = await fixture.Memberships.FindActiveByUserAndOrganizationAsync(
            fixture.OwnerId,
            fixture.OrganizationId);
        formerOwner!.ChangeRole(OrganizationRole.OrganizationMember, Now.AddMinutes(1));
        await fixture.Memberships.UpdateAsync(formerOwner);

        var mariaId = PlatformUserId.New();
        await fixture.Memberships.AddAsync(OrganizationMembership.Create(
            fixture.OrganizationId,
            mariaId,
            OrganizationRole.OrganizationOwner,
            Now.AddMinutes(2)));

        var status = await fixture.GetStatus.ExecuteAsync(fixture.OrganizationId);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.Approved, status.Value!.ComplianceEligibilityStatus);
        Assert.True(status.Value.TaxDocumentIssuanceEnabled);
        Assert.False(status.Value.CurrentOwnerEducationAcknowledged);

        await fixture.SetIssuance.ExecuteAsync(fixture.OrganizationId, false, "platform");
        var enableAgain = await fixture.SetIssuance.ExecuteAsync(fixture.OrganizationId, true, "platform");
        Assert.False(enableAgain.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SalesDocumentEducationOwnerRequired, enableAgain.ErrorCode);
    }

    [Fact]
    public async Task Education_acknowledgment_does_not_enable_compliance_or_issuance()
    {
        var fixture = await Fixture.CreateAsync();
        var ack = await new AcknowledgeSalesDocumentEducation(
                fixture.Memberships,
                fixture.Acknowledgments,
                fixture.Capabilities,
                fixture.Versions,
                new NoOpUnitOfWork(),
                new FixedClock(Now),
                new NoOpAuditWriter())
            .ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);

        Assert.True(ack.IsSuccess);
        var status = await fixture.GetStatus.ExecuteAsync(fixture.OrganizationId);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.NotRequested, status.Value!.ComplianceEligibilityStatus);
        Assert.False(status.Value.TaxDocumentIssuanceEnabled);
    }

    private sealed class Fixture
    {
        public required PlatformOrganizationId OrganizationId { get; init; }
        public required PlatformUserId OwnerId { get; init; }
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required InMemoryAcknowledgmentRepository Acknowledgments { get; init; }
        public required InMemoryCapabilityRepository Capabilities { get; init; }
        public required RecordingAuditWriter Audit { get; init; }
        public required ISalesDocumentEducationVersionProvider Versions { get; init; }
        public required RequestOrganizationComplianceReview Request { get; init; }
        public required TransitionOrganizationComplianceEligibility Transition { get; init; }
        public required SetOrganizationTaxDocumentIssuanceCapability SetIssuance { get; init; }
        public required GetOrganizationComplianceStatus GetStatus { get; init; }

        public static async Task<Fixture> CreateAsync()
        {
            var organizationId = PlatformOrganizationId.New();
            var ownerId = PlatformUserId.New();
            var memberships = new InMemoryOrganizationMembershipRepository();
            await memberships.AddAsync(OrganizationMembership.Create(
                organizationId,
                ownerId,
                OrganizationRole.OrganizationOwner,
                Now));
            var acknowledgments = new InMemoryAcknowledgmentRepository();
            var capabilities = new InMemoryCapabilityRepository();
            var versions = new SalesDocumentEducationVersionProvider();
            var audit = new RecordingAuditWriter();
            var uow = new NoOpUnitOfWork();
            var clock = new FixedClock(Now);
            var ensure = new EnsureOrganizationSalesDocumentCapability(capabilities, uow, clock);
            var getStatus = new GetOrganizationComplianceStatus(
                capabilities,
                memberships,
                acknowledgments,
                versions);

            return new Fixture
            {
                OrganizationId = organizationId,
                OwnerId = ownerId,
                Memberships = memberships,
                Acknowledgments = acknowledgments,
                Capabilities = capabilities,
                Audit = audit,
                Versions = versions,
                GetStatus = getStatus,
                Request = new RequestOrganizationComplianceReview(
                    memberships, ensure, capabilities, getStatus, uow, clock, audit),
                Transition = new TransitionOrganizationComplianceEligibility(
                    ensure, capabilities, memberships, acknowledgments, versions, uow, clock, audit),
                SetIssuance = new SetOrganizationTaxDocumentIssuanceCapability(
                    ensure, capabilities, memberships, acknowledgments, versions, uow, clock, audit)
            };
        }
    }

    private sealed class InMemoryAcknowledgmentRepository : IOrganizationSalesDocumentAcknowledgmentRepository
    {
        public List<OrganizationSalesDocumentAcknowledgment> Items { get; } = [];

        public Task<OrganizationSalesDocumentAcknowledgment?> FindAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userId,
            string version,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x =>
                x.OrganizationId == organizationId
                && x.UserId == userId
                && x.Version == version));

        public Task AddAsync(
            OrganizationSalesDocumentAcknowledgment acknowledgment,
            CancellationToken cancellationToken = default)
        {
            Items.Add(acknowledgment);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCapabilityRepository : IOrganizationSalesDocumentCapabilityRepository
    {
        private readonly Dictionary<Guid, OrganizationSalesDocumentCapability> _items = [];

        public Task<OrganizationSalesDocumentCapability?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(organizationId.Value, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(
            OrganizationSalesDocumentCapability capability,
            CancellationToken cancellationToken = default)
        {
            _items[capability.OrganizationId.Value] = capability;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            OrganizationSalesDocumentCapability capability,
            CancellationToken cancellationToken = default)
        {
            _items[capability.OrganizationId.Value] = capability;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public List<string> Actions { get; } = [];

        public Task WriteAsync(
            string actorIdentifier,
            AuditActorType actorType,
            string actionCode,
            string targetType,
            string targetId,
            AuditOutcome outcome,
            PlatformOrganizationId? organizationId = null,
            ProductCode? productCode = null,
            string? correlationId = null,
            string? reason = null,
            string? summary = null,
            CancellationToken cancellationToken = default)
        {
            Actions.Add(actionCode);
            return Task.CompletedTask;
        }
    }
}
