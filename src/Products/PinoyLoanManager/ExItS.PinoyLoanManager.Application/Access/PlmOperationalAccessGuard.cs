using ExItS.PinoyLoanManager.Domain.Access;

namespace ExItS.PinoyLoanManager.Application.Access;

/// <summary>
/// Fail-closed operational entry guard for future PLM APIs.
/// Enforces trusted actor, organization, product identity, and Platform product access only.
/// Does not evaluate product-local grants (PLM-D-00-06 remains open).
/// </summary>
public sealed class PlmOperationalAccessGuard : IPlmOperationalAccessGuard
{
    private readonly IPlmAccessContextProvider _provider;

    public PlmOperationalAccessGuard(IPlmAccessContextProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public async ValueTask<PlmOperationalAccessDecision> EvaluateAsync(
        CancellationToken cancellationToken = default)
    {
        var context = await _provider.GetAsync(cancellationToken).ConfigureAwait(false);
        if (context is null)
        {
            return PlmOperationalAccessDecision.Deny(
                PlmOperationalAccessDenialReason.ContextUnavailable,
                PlmAccessErrorCodes.ContextUnavailable,
                "Trusted PLM access context is unavailable.");
        }

        if (context.ActorId == Guid.Empty)
        {
            return PlmOperationalAccessDecision.Deny(
                PlmOperationalAccessDenialReason.ActorMissing,
                PlmAccessErrorCodes.ActorRequired,
                "A trusted actor is required.");
        }

        if (context.OrganizationId == Guid.Empty)
        {
            return PlmOperationalAccessDecision.Deny(
                PlmOperationalAccessDenialReason.OrganizationMissing,
                PlmAccessErrorCodes.OrganizationRequired,
                "A trusted organization context is required.");
        }

        if (!PlmProductIdentity.IsPinoyLoanManager(context.ProductCode))
        {
            return PlmOperationalAccessDecision.Deny(
                PlmOperationalAccessDenialReason.WrongProduct,
                PlmAccessErrorCodes.WrongProduct,
                "Product identity is not authorized for Pinoy Loan Manager.");
        }

        if (!context.HasTrustedProductAccess)
        {
            return PlmOperationalAccessDecision.Deny(
                PlmOperationalAccessDenialReason.ProductAccessDenied,
                PlmAccessErrorCodes.ProductAccessDenied,
                "Pinoy Loan Manager product access is not granted for this context.");
        }

        return PlmOperationalAccessDecision.Allow(context);
    }
}
