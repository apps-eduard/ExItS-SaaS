using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Access;

public sealed class EvaluateCurrentSessionProductAccessTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Unauthenticated_session_is_invalid()
    {
        var (current, _) = await CreateAsync();
        var result = await current.ExecuteAsync(null, ProductCode.PinoyLoanManager);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SessionInvalid, result.ErrorCode);
    }

    [Fact]
    public async Task Personal_session_is_account_scope_denied()
    {
        var (current, addSession) = await CreateAsync();
        var token = addSession(AccountClass.Personal, null);
        var result = await current.ExecuteAsync(token, ProductCode.PinoyLoanManager);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Platform_session_is_account_scope_denied()
    {
        var (current, addSession) = await CreateAsync();
        var token = addSession(AccountClass.Platform, null);
        var result = await current.ExecuteAsync(token, ProductCode.PinoyLoanManager);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.AccountScopeDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Organization_session_without_selected_org_requires_context()
    {
        var (current, addSession) = await CreateAsync();
        var token = addSession(AccountClass.Organization, null);
        var result = await current.ExecuteAsync(token, ProductCode.PinoyBusinessPos);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OrganizationContextRequired, result.ErrorCode);
    }

    [Fact]
    public async Task Invalid_product_code_is_rejected_after_session_resolution()
    {
        var (current, addSession, harness) = await CreateWithHarnessAsync();
        var token = addSession(AccountClass.Organization, harness.Organization.Id);
        var result = await current.ExecuteAsync(token, "not a code");
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidProductCode, result.ErrorCode);
    }

    [Fact]
    public async Task Allowed_result_delegates_to_existing_evaluator_for_session_user_and_org()
    {
        var (current, addSession, harness) = await CreateWithHarnessAsync();
        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);

        var token = addSession(AccountClass.Organization, harness.Organization.Id);
        var result = await current.ExecuteAsync(token, ProductCode.PinoyBusinessPos);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.Allowed, result.Value.ReasonCode);
        Assert.Equal(harness.User.Id.Value, result.Value.UserId);
        Assert.Equal(harness.Organization.Id.Value, result.Value.OrganizationId);
    }

    [Fact]
    public async Task Missing_assignment_returns_allowed_false()
    {
        var (current, addSession, harness) = await CreateWithHarnessAsync();
        var token = addSession(AccountClass.Organization, harness.Organization.Id);
        var result = await current.ExecuteAsync(token, ProductCode.PinoyBusinessPos);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Allowed);
        Assert.Equal(EffectiveAccessReasonCodes.ProductAssignmentMissing, result.Value.ReasonCode);
        Assert.Equal(harness.User.Id.Value, result.Value.UserId);
        Assert.Equal(harness.Organization.Id.Value, result.Value.OrganizationId);
    }

    [Fact]
    public async Task Session_derived_ids_ignore_any_other_caller_identity()
    {
        var (current, addSession, harness) = await CreateWithHarnessAsync();
        Assert.True((await harness.Grant.ExecuteAsync(
            harness.Organization.Id,
            harness.User.Id,
            harness.Product.Code.Value,
            "dev-admin")).IsSuccess);

        var token = addSession(AccountClass.Organization, harness.Organization.Id);
        var result = await current.ExecuteAsync(token, ProductCode.PinoyBusinessPos);
        Assert.True(result.IsSuccess);
        Assert.Equal(harness.User.Id.Value, result.Value!.UserId);
        Assert.Equal(harness.Organization.Id.Value, result.Value.OrganizationId);
        Assert.NotEqual(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), result.Value.UserId);
        Assert.NotEqual(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), result.Value.OrganizationId);
    }

    private static async Task<(EvaluateCurrentSessionProductAccess Current, Func<AccountClass, PlatformOrganizationId?, string> AddSession)> CreateAsync()
    {
        var (current, addSession, _) = await CreateWithHarnessAsync();
        return (current, addSession);
    }

    private static async Task<(
        EvaluateCurrentSessionProductAccess Current,
        Func<AccountClass, PlatformOrganizationId?, string> AddSession,
        ProductAccessUseCaseTests.AccessHarness Harness)> CreateWithHarnessAsync()
    {
        var harness = await ProductAccessUseCaseTests.AccessHarness.CreateAsync();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var tokens = new StubSessionTokenService();
        var current = new EvaluateCurrentSessionProductAccess(sessions, tokens, harness.Evaluate, harness.Clock);
        string AddSession(AccountClass accountClass, PlatformOrganizationId? selectedOrganizationId)
        {
            var token = "session-token";
            var session = PlatformAuthSession.Create(
                harness.User.Id,
                AccountProfileId.New(),
                accountClass,
                tokens.HashToken(token),
                securityStampAtIssue: Guid.NewGuid().ToString("N"),
                utcNow: T0,
                idleLifetime: TimeSpan.FromMinutes(30),
                absoluteLifetime: TimeSpan.FromHours(12),
                selectedOrganizationId: selectedOrganizationId);
            sessions.AddAsync(session).GetAwaiter().GetResult();
            return token;
        }

        return (current, AddSession, harness);
    }
}
