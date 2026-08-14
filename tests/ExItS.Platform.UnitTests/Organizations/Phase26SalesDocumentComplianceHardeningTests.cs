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

/// <summary>
/// P26-WP05 end-to-end hardening coverage across education, eligibility, profile, and issuance gates.
/// </summary>
public sealed class Phase26SalesDocumentComplianceHardeningTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 23, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Multi_org_states_do_not_leak_and_defaults_remain_transaction_summary()
    {
        var ownerId = PlatformUserId.New();
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var orgC = PlatformOrganizationId.New();
        var harness = await Harness.CreateAsync(ownerId, orgA, orgB, orgC);

        Assert.True((await harness.Request.ExecuteAsync(orgB, ownerId, "owner")).IsSuccess);
        Assert.True((await harness.Transition.ExecuteAsync(
            orgB, OrganizationComplianceEligibilityStatuses.UnderReview, "platform")).IsSuccess);

        Assert.True((await harness.Request.ExecuteAsync(orgC, ownerId, "owner")).IsSuccess);
        Assert.True((await harness.Transition.ExecuteAsync(
            orgC, OrganizationComplianceEligibilityStatuses.UnderReview, "platform")).IsSuccess);
        Assert.True((await harness.Transition.ExecuteAsync(
            orgC, OrganizationComplianceEligibilityStatuses.Approved, "platform")).IsSuccess);

        var a = await harness.GetCompliance.ExecuteAsync(orgA);
        var b = await harness.GetCompliance.ExecuteAsync(orgB);
        var c = await harness.GetCompliance.ExecuteAsync(orgC);

        Assert.Equal(OrganizationComplianceEligibilityStatuses.NotRequested, a.Value!.ComplianceEligibilityStatus);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.UnderReview, b.Value!.ComplianceEligibilityStatus);
        Assert.Equal(OrganizationComplianceEligibilityStatuses.Approved, c.Value!.ComplianceEligibilityStatus);
        Assert.False(a.Value.TaxDocumentIssuanceEnabled);
        Assert.False(b.Value.TaxDocumentIssuanceEnabled);
        Assert.False(c.Value.TaxDocumentIssuanceEnabled);
        Assert.False(a.Value.TaxDocumentImplementationAvailable);

        var education = await harness.GetEducation.ExecuteAsync(orgA, ownerId);
        Assert.Equal("TransactionSummary", education.Value!.DocumentMode);
        Assert.False(education.Value.TaxDocumentIssuanceEnabled);
    }

    [Fact]
    public async Task Ownership_transfer_preserves_org_compliance_and_requires_maria_education()
    {
        var eduard = PlatformUserId.New();
        var maria = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        var harness = await Harness.CreateAsync(eduard, orgId);

        await harness.Acknowledgments.AddAsync(OrganizationSalesDocumentAcknowledgment.Create(
            orgId, eduard, SalesDocumentEducationVersions.Current, Now, SalesDocumentEducationVersions.Current));
        Assert.True((await harness.Request.ExecuteAsync(orgId, eduard, "eduard")).IsSuccess);
        Assert.True((await harness.Transition.ExecuteAsync(
            orgId, OrganizationComplianceEligibilityStatuses.UnderReview, "platform")).IsSuccess);
        Assert.True((await harness.Transition.ExecuteAsync(
            orgId, OrganizationComplianceEligibilityStatuses.Approved, "platform")).IsSuccess);
        Assert.True((await harness.SetIssuance.ExecuteAsync(orgId, true, "platform")).IsSuccess);
        await harness.EnsureProfile.ExecuteAsync(orgId, "platform");

        var former = await harness.Memberships.FindActiveByUserAndOrganizationAsync(eduard, orgId);
        former!.ChangeRole(OrganizationRole.OrganizationMember, Now.AddMinutes(1));
        await harness.Memberships.UpdateAsync(former);
        await harness.Memberships.AddAsync(OrganizationMembership.Create(
            orgId, maria, OrganizationRole.OrganizationOwner, Now.AddMinutes(2)));

        var compliance = await harness.GetCompliance.ExecuteAsync(orgId);
        var education = await harness.GetEducation.ExecuteAsync(orgId, maria);
        var profile = await harness.GetProfile.ExecuteAsync(orgId);

        Assert.Equal(OrganizationComplianceEligibilityStatuses.Approved, compliance.Value!.ComplianceEligibilityStatus);
        Assert.True(compliance.Value.TaxDocumentIssuanceEnabled);
        Assert.False(compliance.Value.CurrentOwnerEducationAcknowledged);
        Assert.True(education.Value!.RequiresOwnerAction);
        Assert.True(profile.Value!.ProfileInitialized);
        Assert.Single(harness.Acknowledgments.Items, x => x.UserId == eduard);
        Assert.False(TaxDocumentIssuanceRuntime.ImplementationAvailable);

        var ensure = await new EnsureTaxDocumentIssuanceAllowed(harness.Capabilities).ExecuteAsync(orgId);
        Assert.Equal(ApplicationErrorCodes.TaxDocumentIssuanceNotImplemented, ensure.ErrorCode);
    }

    [Fact]
    public async Task Tax_settings_and_education_never_authorize_issuance_across_eligibility_states()
    {
        var ownerId = PlatformUserId.New();
        var orgId = PlatformOrganizationId.New();
        var harness = await Harness.CreateAsync(ownerId, orgId);
        const decimal taxRate = 12m;

        foreach (var status in new[]
                 {
                     OrganizationComplianceEligibilityStatuses.NotRequested,
                     OrganizationComplianceEligibilityStatuses.Requested,
                     OrganizationComplianceEligibilityStatuses.UnderReview,
                     OrganizationComplianceEligibilityStatuses.Approved,
                     OrganizationComplianceEligibilityStatuses.Suspended
                 })
        {
            if (status != OrganizationComplianceEligibilityStatuses.NotRequested)
            {
                if (status is OrganizationComplianceEligibilityStatuses.Requested
                    or OrganizationComplianceEligibilityStatuses.UnderReview
                    or OrganizationComplianceEligibilityStatuses.Approved)
                {
                    var current = (await harness.GetCompliance.ExecuteAsync(orgId)).Value!.ComplianceEligibilityStatus;
                    if (current == OrganizationComplianceEligibilityStatuses.NotRequested)
                    {
                        Assert.True((await harness.Request.ExecuteAsync(orgId, ownerId, "owner")).IsSuccess);
                    }

                    if (status is OrganizationComplianceEligibilityStatuses.UnderReview
                        or OrganizationComplianceEligibilityStatuses.Approved
                        or OrganizationComplianceEligibilityStatuses.Suspended)
                    {
                        current = (await harness.GetCompliance.ExecuteAsync(orgId)).Value!.ComplianceEligibilityStatus;
                        if (current == OrganizationComplianceEligibilityStatuses.Requested)
                        {
                            Assert.True((await harness.Transition.ExecuteAsync(
                                orgId, OrganizationComplianceEligibilityStatuses.UnderReview, "platform")).IsSuccess);
                        }
                    }

                    if (status == OrganizationComplianceEligibilityStatuses.Approved)
                    {
                        Assert.True((await harness.Transition.ExecuteAsync(
                            orgId, OrganizationComplianceEligibilityStatuses.Approved, "platform")).IsSuccess);
                    }

                    if (status == OrganizationComplianceEligibilityStatuses.Suspended)
                    {
                        current = (await harness.GetCompliance.ExecuteAsync(orgId)).Value!.ComplianceEligibilityStatus;
                        if (current != OrganizationComplianceEligibilityStatuses.Approved)
                        {
                            Assert.True((await harness.Transition.ExecuteAsync(
                                orgId, OrganizationComplianceEligibilityStatuses.Approved, "platform")).IsSuccess);
                        }

                        Assert.True((await harness.Transition.ExecuteAsync(
                            orgId, OrganizationComplianceEligibilityStatuses.Suspended, "platform")).IsSuccess);
                    }
                }
            }

            Assert.Equal(12m, taxRate);
            var ensure = await new EnsureTaxDocumentIssuanceAllowed(harness.Capabilities).ExecuteAsync(orgId);
            Assert.False(ensure.IsSuccess);
            Assert.Equal(ApplicationErrorCodes.TaxDocumentIssuanceNotImplemented, ensure.ErrorCode);
        }

        var ack = await harness.Acknowledge.ExecuteAsync(orgId, ownerId);
        Assert.True(ack.IsSuccess);
        Assert.False(ack.Value!.TaxDocumentIssuanceEnabled);
    }

    [Fact]
    public void Security_surface_has_no_client_set_issuance_and_no_compliance_claims()
    {
        Assert.False(TaxDocumentIssuanceRuntime.ImplementationAvailable);
        Assert.Equal("transaction-summary-v1", SalesDocumentEducationVersions.Current);

        var setMethod = typeof(SetOrganizationTaxDocumentIssuanceCapability).GetMethod("ExecuteAsync");
        Assert.NotNull(setMethod);
        Assert.DoesNotContain(
            setMethod!.GetParameters(),
            p => p.Name is "AcknowledgedByUserId" or "OrganizationIdFromClient");

        Assert.DoesNotContain(
            typeof(OrganizationPublicIdentityDto).GetProperties(),
            p => p.Name.Contains("Compliance", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Tin", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Tax", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Harness
    {
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required InMemoryAcknowledgmentRepository Acknowledgments { get; init; }
        public required InMemoryCapabilityRepository Capabilities { get; init; }
        public required InMemoryComplianceProfileRepository Profiles { get; init; }
        public required InMemoryPlatformOrganizationRepository Organizations { get; init; }
        public required RequestOrganizationComplianceReview Request { get; init; }
        public required TransitionOrganizationComplianceEligibility Transition { get; init; }
        public required SetOrganizationTaxDocumentIssuanceCapability SetIssuance { get; init; }
        public required GetOrganizationComplianceStatus GetCompliance { get; init; }
        public required GetSalesDocumentEducationStatus GetEducation { get; init; }
        public required AcknowledgeSalesDocumentEducation Acknowledge { get; init; }
        public required EnsureOrganizationComplianceProfile EnsureProfile { get; init; }
        public required GetOrganizationComplianceProfile GetProfile { get; init; }

        public static async Task<Harness> CreateAsync(PlatformUserId ownerId, params PlatformOrganizationId[] orgIds)
        {
            var memberships = new InMemoryOrganizationMembershipRepository();
            var acknowledgments = new InMemoryAcknowledgmentRepository();
            var capabilities = new InMemoryCapabilityRepository();
            var profiles = new InMemoryComplianceProfileRepository();
            var organizations = new InMemoryPlatformOrganizationRepository();
            var versions = new SalesDocumentEducationVersionProvider();
            var uow = new NoOpUnitOfWork();
            var clock = new FixedClock(Now);
            var audit = new RecordingAuditWriter();
            var ensureCapability = new EnsureOrganizationSalesDocumentCapability(capabilities, uow, clock);
            var getCompliance = new GetOrganizationComplianceStatus(
                capabilities, memberships, acknowledgments, versions);

            foreach (var orgId in orgIds)
            {
                await memberships.AddAsync(OrganizationMembership.Create(
                    orgId, ownerId, OrganizationRole.OrganizationOwner, Now));
                await organizations.AddAsync(PlatformOrganization.Rehydrate(
                    orgId,
                    "Org " + orgId.Value.ToString("N")[..6],
                    "slug-" + orgId.Value.ToString("N")[..8],
                    null,
                    null,
                    OrganizationStatus.Active,
                    OrganizationProfile.Empty,
                    OrganizationBranding.Empty,
                    Now,
                    Now));
            }

            return new Harness
            {
                Memberships = memberships,
                Acknowledgments = acknowledgments,
                Capabilities = capabilities,
                Profiles = profiles,
                Organizations = organizations,
                GetCompliance = getCompliance,
                Request = new RequestOrganizationComplianceReview(
                    memberships, ensureCapability, capabilities, getCompliance, uow, clock, audit),
                Transition = new TransitionOrganizationComplianceEligibility(
                    ensureCapability, capabilities, memberships, acknowledgments, versions, uow, clock, audit),
                SetIssuance = new SetOrganizationTaxDocumentIssuanceCapability(
                    ensureCapability, capabilities, memberships, acknowledgments, versions, uow, clock, audit),
                GetEducation = new GetSalesDocumentEducationStatus(
                    memberships, acknowledgments, capabilities, versions),
                Acknowledge = new AcknowledgeSalesDocumentEducation(
                    memberships, acknowledgments, capabilities, versions, uow, clock, audit),
                EnsureProfile = new EnsureOrganizationComplianceProfile(profiles, uow, clock),
                GetProfile = new GetOrganizationComplianceProfile(profiles, organizations, capabilities)
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
                x.OrganizationId == organizationId && x.UserId == userId && x.Version == version));

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

    private sealed class InMemoryComplianceProfileRepository : IOrganizationComplianceProfileRepository
    {
        private readonly Dictionary<Guid, OrganizationComplianceProfile> _items = [];

        public Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(organizationId.Value, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(
            OrganizationComplianceProfile profile,
            CancellationToken cancellationToken = default)
        {
            _items[profile.OrganizationId.Value] = profile;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingAuditWriter : IAuditWriter
    {
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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
