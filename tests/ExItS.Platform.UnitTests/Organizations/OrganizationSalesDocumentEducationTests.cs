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

public sealed class OrganizationSalesDocumentEducationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 14, 20, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task New_organization_requires_current_owner_action()
    {
        var fixture = await Fixture.CreateAsync();

        var result = await fixture.Get.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.RequiresOwnerAction);
        Assert.False(result.Value.CurrentOwnerAcknowledged);
        Assert.Equal(SalesDocumentEducationVersions.Current, result.Value.CurrentVersion);
        Assert.Equal("TransactionSummary", result.Value.DocumentMode);
    }

    [Fact]
    public async Task Owner_acknowledgment_is_idempotent()
    {
        var fixture = await Fixture.CreateAsync();

        var first = await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);
        var second = await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.False(second.Value!.RequiresOwnerAction);
        Assert.Single(fixture.Acknowledgments.Items);
        Assert.Equal(1, fixture.Audit.WriteCount);
    }

    [Fact]
    public async Task Acknowledgments_are_isolated_by_organization()
    {
        var fixture = await Fixture.CreateAsync();
        var secondOrg = PlatformOrganizationId.New();
        await fixture.Memberships.AddAsync(OrganizationMembership.Create(
            secondOrg,
            fixture.OwnerId,
            OrganizationRole.OrganizationOwner,
            Now));

        await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);
        var other = await fixture.Get.ExecuteAsync(secondOrg, fixture.OwnerId);

        Assert.True(other.IsSuccess);
        Assert.True(other.Value!.RequiresOwnerAction);
    }

    [Fact]
    public async Task Non_owner_cannot_acknowledge()
    {
        var fixture = await Fixture.CreateAsync();
        var cashierId = PlatformUserId.New();
        await fixture.Memberships.AddAsync(OrganizationMembership.Create(
            fixture.OrganizationId,
            cashierId,
            OrganizationRole.OrganizationMember,
            Now));

        var result = await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, cashierId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SalesDocumentEducationOwnerRequired, result.ErrorCode);
        Assert.Empty(fixture.Acknowledgments.Items);
    }

    [Fact]
    public async Task Ownership_transfer_requires_new_owner_ack_and_retains_history()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);
        var formerOwnerAcknowledgment = Assert.Single(fixture.Acknowledgments.Items);

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
            Now.AddMinutes(1)));

        var beforeMaria = await fixture.Get.ExecuteAsync(fixture.OrganizationId, mariaId);
        var afterMaria = await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, mariaId);

        Assert.True(beforeMaria.Value!.RequiresOwnerAction);
        Assert.False(afterMaria.Value!.RequiresOwnerAction);
        Assert.Equal(2, fixture.Acknowledgments.Items.Count);
        Assert.Contains(formerOwnerAcknowledgment, fixture.Acknowledgments.Items);
    }

    [Fact]
    public async Task Version_change_requires_new_ack_and_preserves_v1()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);
        fixture.Versions.CurrentVersion = "transaction-summary-v2";

        var beforeV2 = await fixture.Get.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);
        await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);

        Assert.True(beforeV2.Value!.RequiresOwnerAction);
        Assert.Equal(2, fixture.Acknowledgments.Items.Count);
        Assert.Contains(fixture.Acknowledgments.Items, x => x.Version == SalesDocumentEducationVersions.Current);
        Assert.Contains(fixture.Acknowledgments.Items, x => x.Version == "transaction-summary-v2");
    }

    [Fact]
    public async Task Acknowledgment_does_not_enable_tax_documents_or_change_document_kind()
    {
        var fixture = await Fixture.CreateAsync();

        var result = await fixture.Acknowledge.ExecuteAsync(fixture.OrganizationId, fixture.OwnerId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.TransactionSummaryAvailable);
        Assert.False(result.Value.TaxDocumentIssuanceEnabled);
        Assert.Equal("TransactionSummary", result.Value.DocumentMode);
    }

    private sealed class Fixture
    {
        private Fixture(
            PlatformOrganizationId organizationId,
            PlatformUserId ownerId,
            InMemoryOrganizationMembershipRepository memberships,
            InMemoryAcknowledgmentRepository acknowledgments,
            MutableVersionProvider versions,
            RecordingAuditWriter audit,
            GetSalesDocumentEducationStatus get,
            AcknowledgeSalesDocumentEducation acknowledge)
        {
            OrganizationId = organizationId;
            OwnerId = ownerId;
            Memberships = memberships;
            Acknowledgments = acknowledgments;
            Versions = versions;
            Audit = audit;
            Get = get;
            Acknowledge = acknowledge;
        }

        public PlatformOrganizationId OrganizationId { get; }
        public PlatformUserId OwnerId { get; }
        public InMemoryOrganizationMembershipRepository Memberships { get; }
        public InMemoryAcknowledgmentRepository Acknowledgments { get; }
        public MutableVersionProvider Versions { get; }
        public RecordingAuditWriter Audit { get; }
        public GetSalesDocumentEducationStatus Get { get; }
        public AcknowledgeSalesDocumentEducation Acknowledge { get; }

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
            var versions = new MutableVersionProvider();
            var audit = new RecordingAuditWriter();
            var get = new GetSalesDocumentEducationStatus(
                memberships,
                acknowledgments,
                capabilities,
                versions);
            var acknowledge = new AcknowledgeSalesDocumentEducation(
                memberships,
                acknowledgments,
                capabilities,
                versions,
                new NoOpUnitOfWork(),
                new FixedClock(Now),
                audit);
            return new(
                organizationId,
                ownerId,
                memberships,
                acknowledgments,
                versions,
                audit,
                get,
                acknowledge);
        }
    }

    private sealed class MutableVersionProvider : ISalesDocumentEducationVersionProvider
    {
        public string CurrentVersion { get; set; } = SalesDocumentEducationVersions.Current;
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
        public Task<OrganizationSalesDocumentCapability?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationSalesDocumentCapability?>(null);

        public Task AddAsync(
            OrganizationSalesDocumentCapability capability,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
        public int WriteCount { get; private set; }

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
            Assert.Equal(PlatformAuditActions.OrganizationSalesDocumentEducationAcknowledged, actionCode);
            WriteCount++;
            return Task.CompletedTask;
        }
    }
}
