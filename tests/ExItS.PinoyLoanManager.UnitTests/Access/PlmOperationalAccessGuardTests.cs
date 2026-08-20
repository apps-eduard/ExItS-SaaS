using ExItS.PinoyLoanManager.Application.Access;
using ExItS.PinoyLoanManager.Domain.Access;

namespace ExItS.PinoyLoanManager.UnitTests.Access;

public sealed class PlmOperationalAccessGuardTests
{
    private static readonly Guid ActorId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OrganizationId = Guid.Parse("22222222-2222-4222-8222-222222222222");

    [Fact]
    public async Task Missing_trusted_context_is_denied()
    {
        var decision = await EvaluateAsync(null);

        Assert.False(decision.IsAllowed);
        Assert.Equal(PlmOperationalAccessDenialReason.ContextUnavailable, decision.DenialReason);
        Assert.Equal(PlmAccessErrorCodes.ContextUnavailable, decision.ErrorCode);
    }

    [Fact]
    public async Task Missing_actor_is_denied()
    {
        var decision = await EvaluateAsync(new PlmAccessContext(
            Guid.Empty,
            OrganizationId,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: true));

        Assert.False(decision.IsAllowed);
        Assert.Equal(PlmOperationalAccessDenialReason.ActorMissing, decision.DenialReason);
        Assert.Equal(PlmAccessErrorCodes.ActorRequired, decision.ErrorCode);
    }

    [Fact]
    public async Task Missing_organization_is_denied()
    {
        var decision = await EvaluateAsync(new PlmAccessContext(
            ActorId,
            Guid.Empty,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: true));

        Assert.False(decision.IsAllowed);
        Assert.Equal(PlmOperationalAccessDenialReason.OrganizationMissing, decision.DenialReason);
        Assert.Equal(PlmAccessErrorCodes.OrganizationRequired, decision.ErrorCode);
    }

    [Fact]
    public async Task Wrong_product_is_denied()
    {
        var decision = await EvaluateAsync(new PlmAccessContext(
            ActorId,
            OrganizationId,
            "pinoy-business-pos",
            hasTrustedProductAccess: true));

        Assert.False(decision.IsAllowed);
        Assert.Equal(PlmOperationalAccessDenialReason.WrongProduct, decision.DenialReason);
        Assert.Equal(PlmAccessErrorCodes.WrongProduct, decision.ErrorCode);
    }

    [Fact]
    public async Task Product_access_denied_is_denied()
    {
        var decision = await EvaluateAsync(new PlmAccessContext(
            ActorId,
            OrganizationId,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: false));

        Assert.False(decision.IsAllowed);
        Assert.Equal(PlmOperationalAccessDenialReason.ProductAccessDenied, decision.DenialReason);
        Assert.Equal(PlmAccessErrorCodes.ProductAccessDenied, decision.ErrorCode);
    }

    [Fact]
    public async Task Valid_trusted_context_passes()
    {
        var context = new PlmAccessContext(
            ActorId,
            OrganizationId,
            PlmProductIdentity.PinoyLoanManagerCode,
            hasTrustedProductAccess: true);

        var decision = await EvaluateAsync(context);

        Assert.True(decision.IsAllowed);
        Assert.Null(decision.DenialReason);
        Assert.Same(context, decision.Context);
    }

    private static async Task<PlmOperationalAccessDecision> EvaluateAsync(PlmAccessContext? context)
    {
        var guard = new PlmOperationalAccessGuard(new FixedPlmAccessContextProvider(context));
        return await guard.EvaluateAsync();
    }

    private sealed class FixedPlmAccessContextProvider : IPlmAccessContextProvider
    {
        private readonly PlmAccessContext? _context;

        public FixedPlmAccessContextProvider(PlmAccessContext? context) => _context = context;

        public ValueTask<PlmAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_context);
    }
}
