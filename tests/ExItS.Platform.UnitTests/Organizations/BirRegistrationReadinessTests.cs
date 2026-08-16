using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BirRegistrationReadinessTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Tin_normalize_accepts_nine_digits_and_strips_non_digits()
    {
        Assert.Equal("123456789", TinMask.NormalizeOrThrow("123-456-789"));
        Assert.Equal("123456789", TinMask.NormalizeOrThrow(" 123456789 "));
        Assert.Null(TinMask.NormalizeOrThrow(null));
        Assert.Null(TinMask.NormalizeOrThrow("   "));
    }

    [Fact]
    public void Tin_normalize_rejects_wrong_length()
    {
        var ex = Assert.Throws<DomainException>(() => TinMask.NormalizeOrThrow("12345678"));
        Assert.Equal(DomainErrorCodes.InvalidTaxpayerTin, ex.ErrorCode);
        Assert.Throws<DomainException>(() => TinMask.NormalizeOrThrow("1234567890"));
    }

    [Fact]
    public void Tin_mask_shows_last_three_digits()
    {
        Assert.Equal("***-***-789", TinMask.Mask("123456789"));
        Assert.Null(TinMask.Mask(null));
        Assert.Null(TinMask.Mask("12"));
    }

    [Fact]
    public void TaxDocument_runtime_remains_unavailable()
    {
        Assert.False(TaxDocumentIssuanceRuntime.ImplementationAvailable);
    }

    [Fact]
    public async Task Branch_foreign_injection_is_rejected()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var branchB = OrganizationBranch.CreateMainBranch(orgB, Now);
        var branches = new InMemoryBranchRepository();
        await branches.AddAsync(branchB);
        var profiles = new InMemoryBranchComplianceRepository();
        var useCase = new UpsertBranchComplianceProfile(
            profiles, branches, new NoOpUnitOfWork(), new FixedClock(Now), new NoOpAuditWriter());

        var result = await useCase.ExecuteAsync(
            orgA, branchB.Id, "00000", null, null, "owner", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BranchComplianceOrganizationMismatch, result.ErrorCode);
        Assert.Empty(profiles.Items);
    }

    [Fact]
    public void Owner_cannot_accept_registration_for_readiness()
    {
        var orgId = PlatformOrganizationId.New();
        var record = ComplianceRegistrationRecord.Create(
            orgId,
            ComplianceRegistrationTypes.PosPermitToUse,
            "owner",
            Now,
            status: ComplianceRegistrationStatuses.Provided);

        var ex = Assert.Throws<DomainException>(() =>
            record.UpdateByOwner(
                null,
                null,
                "REF-1",
                ComplianceRegistrationStatuses.AcceptedForReadiness,
                null,
                null,
                null,
                null,
                null,
                "owner",
                Now));
        Assert.Equal(DomainErrorCodes.ComplianceSelfReviewUnauthorized, ex.ErrorCode);
    }

    [Fact]
    public void Platform_can_accept_registration_for_readiness()
    {
        var orgId = PlatformOrganizationId.New();
        var record = ComplianceRegistrationRecord.Create(
            orgId,
            ComplianceRegistrationTypes.PosPermitToUse,
            "owner",
            Now);

        record.AcceptForReadiness("platform-admin", "Looks complete", Now);

        Assert.Equal(ComplianceRegistrationStatuses.AcceptedForReadiness, record.Status);
        Assert.Equal("platform-admin", record.ReviewedBy);
        Assert.Equal("Looks complete", record.ReviewNotes);
        Assert.Equal(Now, record.ReviewedAtUtc);
    }

    [Fact]
    public void Evaluator_blocks_when_runtime_unavailable_even_if_checklist_otherwise_complete()
    {
        var orgId = PlatformOrganizationId.New();
        var profile = OrganizationComplianceProfile.Create(orgId, Now, "owner");
        profile.UpdateRegisteredTaxpayerInfo("Acme Trading", "123456789", "owner", Now);

        var branch = OrganizationBranch.CreateMainBranch(orgId, Now);
        var branchProfile = BranchComplianceProfile.Create(orgId, branch.Id, Now, "owner");
        branchProfile.Update("00000", ComplianceSetupStatuses.ReadyForReview, null, "owner", Now);

        var capability = ApprovedCapability(orgId);

        var registration = ComplianceRegistrationRecord.Create(
            orgId, ComplianceRegistrationTypes.PosPermitToUse, "owner", Now);
        registration.AcceptForReadiness("platform", null, Now);

        var result = new ComplianceActivationReadinessEvaluator().Evaluate(
            profile,
            capability,
            currentOwnerEducationAcknowledged: true,
            [branch],
            [branchProfile],
            [registration]);

        Assert.False(TaxDocumentIssuanceRuntime.ImplementationAvailable);
        Assert.False(result.IsReadyForTaxDocumentActivation);
        Assert.Equal(ComplianceSetupStatuses.ActivationBlocked, result.OverallStatus);
        Assert.Contains(
            ComplianceActivationReadinessEvaluator.RuntimeUnavailableReason,
            result.BlockingReasons);
    }

    [Fact]
    public void Incomplete_profile_is_not_ready()
    {
        var result = new ComplianceActivationReadinessEvaluator().Evaluate(
            profile: null,
            capability: null,
            currentOwnerEducationAcknowledged: false,
            branches: [],
            branchProfiles: [],
            registrationRecords: []);

        Assert.False(result.IsReadyForTaxDocumentActivation);
        Assert.Contains(result.BlockingReasons, r => r.Contains("taxpayer name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.BlockingReasons, r => r.Contains("TIN", StringComparison.OrdinalIgnoreCase));
        Assert.NotEqual(ComplianceSetupStatuses.Activated, result.OverallStatus);
    }

    [Fact]
    public void One_branch_ready_does_not_satisfy_all_active_branches()
    {
        var orgId = PlatformOrganizationId.New();
        var profile = OrganizationComplianceProfile.Create(orgId, Now, "owner");
        profile.UpdateRegisteredTaxpayerInfo("Acme", "123456789", "owner", Now);

        var main = OrganizationBranch.CreateMainBranch(orgId, Now);
        var second = OrganizationBranch.Create(orgId, "BR2", "Second", Now);
        var mainProfile = BranchComplianceProfile.Create(orgId, main.Id, Now, "owner");
        mainProfile.Update("00000", null, null, "owner", Now);

        var capability = ApprovedCapability(orgId);
        var registration = ComplianceRegistrationRecord.Create(
            orgId, ComplianceRegistrationTypes.PosPermitToUse, "owner", Now);
        registration.AcceptForReadiness("platform", null, Now);

        var result = new ComplianceActivationReadinessEvaluator().Evaluate(
            profile,
            capability,
            currentOwnerEducationAcknowledged: true,
            [main, second],
            [mainProfile],
            [registration]);

        Assert.False(result.IsReadyForTaxDocumentActivation);
        Assert.Contains(
            result.BlockingReasons,
            r => r.Contains("Every active branch", StringComparison.OrdinalIgnoreCase));
        var branchItem = Assert.Single(result.Checklist, c => c.Code == "branch_codes");
        Assert.False(branchItem.Done);
    }

    [Fact]
    public void Public_identity_dtos_still_have_no_tin()
    {
        AssertNoTin(typeof(OrganizationPublicIdentityDto));
        AssertNoTin(typeof(ResolvedPublicOrganizationDto));
        Assert.DoesNotContain(
            typeof(OrganizationComplianceProfileDto).GetProperties(),
            p => p.Name is "Tin" or "TinNormalized" or "FullTin");
        Assert.Contains(
            typeof(OrganizationComplianceProfileDto).GetProperties(),
            p => p.Name == "MaskedTin");
    }

    [Fact]
    public async Task Update_registered_taxpayer_stores_normalized_tin_and_returns_mask_only()
    {
        var orgId = PlatformOrganizationId.New();
        var orgs = new InMemoryPlatformOrganizationRepository();
        await orgs.AddAsync(CreateOrg(orgId, "Shop"));
        var profiles = new InMemoryComplianceProfileRepository();
        var capabilities = new InMemoryCapabilityRepository();
        var ensure = new EnsureOrganizationComplianceProfile(profiles, new NoOpUnitOfWork(), new FixedClock(Now));
        var update = new UpdateOrganizationRegisteredTaxpayerInfo(
            profiles, orgs, capabilities, ensure, new NoOpUnitOfWork(), new FixedClock(Now), new NoOpAuditWriter());

        var result = await update.ExecuteAsync(orgId, "Registered Name Co", "123-456-789", "owner");

        Assert.True(result.IsSuccess);
        Assert.Equal("***-***-789", result.Value!.MaskedTin);
        Assert.Equal("Registered Name Co", result.Value.RegisteredTaxpayerName);
        Assert.Equal("123456789", profiles.Items[orgId.Value].TinNormalized);
        Assert.DoesNotContain(
            result.Value.GetType().GetProperties(),
            p => p.Name is "TinNormalized" or "Tin");
    }

    private static OrganizationSalesDocumentCapability ApprovedCapability(PlatformOrganizationId orgId)
    {
        var capability = OrganizationSalesDocumentCapability.CreateDefault(orgId, Now);
        capability.TransitionEligibility(OrganizationComplianceEligibilityStatuses.UnderReview, "platform", Now);
        capability.TransitionEligibility(OrganizationComplianceEligibilityStatuses.Approved, "platform", Now);
        return capability;
    }

    private static void AssertNoTin(Type type) =>
        Assert.DoesNotContain(
            type.GetProperties(),
            p => p.Name.Contains("Tin", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Tax", StringComparison.OrdinalIgnoreCase)
                 || p.Name.Contains("Compliance", StringComparison.OrdinalIgnoreCase));

    private static PlatformOrganization CreateOrg(PlatformOrganizationId id, string legalName) =>
        PlatformOrganization.Rehydrate(
            id,
            legalName,
            "org-" + id.Value.ToString("N")[..8],
            publicOrganizationId: null,
            primaryBusinessTypeId: null,
            OrganizationStatus.Active,
            OrganizationProfile.Create(
                legalName: legalName,
                contactEmail: null,
                contactPhone: null,
                addressLine1: "1 Main St",
                addressLine2: null,
                city: "Manila",
                region: "NCR",
                postalCode: "1000",
                countryCode: "PH",
                timeZoneId: null,
                locale: null,
                currencyCode: null),
            OrganizationBranding.Empty,
            Now,
            Now);

    private sealed class InMemoryComplianceProfileRepository : IOrganizationComplianceProfileRepository
    {
        public Dictionary<Guid, OrganizationComplianceProfile> Items { get; } = [];

        public Task<OrganizationComplianceProfile?> GetByOrganizationIdAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default)
        {
            Items.TryGetValue(organizationId.Value, out var value);
            return Task.FromResult(value);
        }

        public Task AddAsync(OrganizationComplianceProfile profile, CancellationToken cancellationToken = default)
        {
            Items[profile.OrganizationId.Value] = profile;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationComplianceProfile profile, CancellationToken cancellationToken = default)
        {
            Items[profile.OrganizationId.Value] = profile;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBranchComplianceRepository : IBranchComplianceProfileRepository
    {
        public List<BranchComplianceProfile> Items { get; } = [];

        public Task<BranchComplianceProfile?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OrganizationBranchId == branchId));

        public Task<IReadOnlyList<BranchComplianceProfile>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchComplianceProfile>>(
                Items.Where(x => x.OrganizationId == organizationId).ToList());

        public Task AddAsync(BranchComplianceProfile profile, CancellationToken cancellationToken = default)
        {
            Items.Add(profile);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BranchComplianceProfile profile, CancellationToken cancellationToken = default)
        {
            var idx = Items.FindIndex(x => x.Id == profile.Id);
            if (idx >= 0)
            {
                Items[idx] = profile;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryBranchRepository : IOrganizationBranchRepository
    {
        private readonly List<OrganizationBranch> _items = [];

        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                _items.Where(x => x.OrganizationId == organizationId).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(x =>
                x.OrganizationId == organizationId && x.Status == OrganizationBranchStatus.Active));

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId == organizationId && x.IsPrimary));

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _items.Add(branch);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
}
