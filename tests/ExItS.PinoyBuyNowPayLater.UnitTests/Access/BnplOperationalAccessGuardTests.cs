using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Access;

namespace ExItS.PinoyBuyNowPayLater.UnitTests.Access;

public sealed class BnplOperationalAccessGuardTests
{
    private static readonly Guid ActorId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OrgA = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid OrgB = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid BranchA = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid BranchB = Guid.Parse("55555555-5555-4555-8555-555555555555");

    [Fact]
    public async Task Missing_context_denies()
    {
        var decision = await CreateGuard(null).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.ContextUnavailable, decision.DenialReason);
        Assert.Equal(BnplAccessErrorCodes.ContextUnavailable, decision.ErrorCode);
    }

    [Fact]
    public async Task Empty_actor_denies()
    {
        var decision = await CreateGuard(ValidContext(actorId: Guid.Empty)).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.ActorMissing, decision.DenialReason);
    }

    [Fact]
    public async Task Empty_organization_denies()
    {
        var decision = await CreateGuard(ValidContext(organizationId: Guid.Empty)).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.OrganizationMissing, decision.DenialReason);
    }

    [Fact]
    public async Task Wrong_product_code_denies()
    {
        var decision = await CreateGuard(ValidContext(productCode: "pinoy-business-pos")).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.WrongProduct, decision.DenialReason);
    }

    [Fact]
    public async Task Missing_membership_denies()
    {
        var decision = await CreateGuard(ValidContext(membership: false)).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.MembershipMissing, decision.DenialReason);
    }

    [Fact]
    public async Task Missing_entitlement_denies()
    {
        var decision = await CreateGuard(ValidContext(entitlement: false)).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.EntitlementMissing, decision.DenialReason);
    }

    [Fact]
    public async Task Missing_product_assignment_denies_even_with_pos_style_facts()
    {
        var decision = await CreateGuard(ValidContext(assignment: false)).EvaluateAsync();
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.ProductAccessDenied, decision.DenialReason);
    }

    [Fact]
    public async Task Wrong_branch_denies()
    {
        var decision = await CreateGuard(ValidContext(branches: BnplBranchScope.Restricted([BranchA])))
            .EvaluateAsync(BnplAccessRequirement.ForBranchAndCapability(BranchB, BnplCapabilityCodes.PlanRead));
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.BranchDenied, decision.DenialReason);
    }

    [Fact]
    public async Task Missing_capability_denies()
    {
        var decision = await CreateGuard(ValidContext(capabilities: [BnplCapabilityCodes.PlanRead]))
            .EvaluateAsync(BnplAccessRequirement.ForCapability(BnplCapabilityCodes.ApplicationApprove));
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.CapabilityDenied, decision.DenialReason);
    }

    [Fact]
    public async Task Unknown_capability_denies()
    {
        var decision = await CreateGuard(ValidContext())
            .EvaluateAsync(BnplAccessRequirement.ForCapability("bnpl.unknown.power"));
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.CapabilityUnknown, decision.DenialReason);
    }

    [Fact]
    public async Task Create_capability_does_not_imply_approve()
    {
        var decision = await CreateGuard(ValidContext(capabilities: [BnplCapabilityCodes.ApplicationCreate]))
            .EvaluateAsync(BnplAccessRequirement.ForCapability(BnplCapabilityCodes.ApplicationApprove));
        Assert.False(decision.IsAllowed);
        Assert.Equal(BnplOperationalAccessDenialReason.CapabilityDenied, decision.DenialReason);
    }

    [Fact]
    public async Task Repayment_capability_does_not_imply_settlement()
    {
        var decision = await CreateGuard(ValidContext(capabilities: [BnplCapabilityCodes.RepaymentCreate]))
            .EvaluateAsync(BnplAccessRequirement.ForCapability(BnplCapabilityCodes.SettlementManage));
        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public async Task Organization_isolation_is_context_bound()
    {
        var decision = await CreateGuard(ValidContext(organizationId: OrgA)).EvaluateAsync();
        Assert.True(decision.IsAllowed);
        Assert.Equal(OrgA, decision.Context!.OrganizationId);
        Assert.NotEqual(OrgB, decision.Context.OrganizationId);
    }

    [Fact]
    public async Task Valid_org_entitlement_assignment_branch_and_capability_allows()
    {
        var decision = await CreateGuard(ValidContext(
                branches: BnplBranchScope.Restricted([BranchA]),
                capabilities: [BnplCapabilityCodes.ApplicationCreate]))
            .EvaluateAsync(BnplAccessRequirement.ForBranchAndCapability(BranchA, BnplCapabilityCodes.ApplicationCreate));

        Assert.True(decision.IsAllowed);
        Assert.NotNull(decision.Context);
        Assert.Equal(ActorId, decision.Context!.ActorId);
    }

    [Fact]
    public async Task Organization_wide_branch_scope_allows_any_non_empty_branch()
    {
        var decision = await CreateGuard(ValidContext(
                branches: BnplBranchScope.OrganizationWide(),
                capabilities: [BnplCapabilityCodes.PlanRead]))
            .EvaluateAsync(BnplAccessRequirement.ForBranchAndCapability(BranchB, BnplCapabilityCodes.PlanRead));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Preset_name_is_not_used_for_authorization()
    {
        // Context carries capabilities only — "Owner" string is never consulted by the guard.
        var decision = await CreateGuard(ValidContext(capabilities: [BnplCapabilityCodes.PlanRead]))
            .EvaluateAsync(BnplAccessRequirement.ForCapability(BnplCapabilityCodes.Config));
        Assert.False(decision.IsAllowed);
        Assert.DoesNotContain("Owner", decision.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static BnplOperationalAccessGuard CreateGuard(BnplAccessContext? context) =>
        new(new FixedProvider(context));

    private static BnplAccessContext ValidContext(
        Guid? actorId = null,
        Guid? organizationId = null,
        string? productCode = null,
        bool membership = true,
        bool entitlement = true,
        bool assignment = true,
        BnplBranchScope? branches = null,
        IEnumerable<string>? capabilities = null) =>
        new(
            actorId ?? ActorId,
            organizationId ?? OrgA,
            productCode ?? BnplProductIdentity.ProductCode,
            membership,
            entitlement,
            assignment,
            branches ?? BnplBranchScope.OrganizationWide(),
            capabilities ?? BnplCapabilityPresets.OwnerCapabilities);

    private sealed class FixedProvider : IBnplAccessContextProvider
    {
        private readonly BnplAccessContext? _context;

        public FixedProvider(BnplAccessContext? context) => _context = context;

        public ValueTask<BnplAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_context);
    }
}
