using ExItS.PinoyBuyNowPayLater.Domain.Access;

namespace ExItS.PinoyBuyNowPayLater.Application.Access;

/// <summary>
/// Fail-closed BNPL operational access evaluator.
/// Formula: authenticated actor + org membership + BNPL entitlement + product assignment
/// + branch scope (when required) + capability (when required). Deny by default.
/// Does not authorize by role/preset name. Does not imply POS/PLM access.
/// </summary>
public sealed class BnplOperationalAccessGuard : IBnplOperationalAccessGuard
{
    private readonly IBnplAccessContextProvider _provider;

    public BnplOperationalAccessGuard(IBnplAccessContextProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public async ValueTask<BnplOperationalAccessDecision> EvaluateAsync(
        BnplAccessRequirement? requirement = null,
        CancellationToken cancellationToken = default)
    {
        requirement ??= BnplAccessRequirement.None;

        var context = await _provider.GetAsync(cancellationToken).ConfigureAwait(false);
        if (context is null)
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.ContextUnavailable,
                BnplAccessErrorCodes.ContextUnavailable,
                "Trusted BNPL access context is unavailable.");
        }

        if (context.ActorId == Guid.Empty)
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.ActorMissing,
                BnplAccessErrorCodes.ActorRequired,
                "A trusted actor is required.");
        }

        if (context.OrganizationId == Guid.Empty)
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.OrganizationMissing,
                BnplAccessErrorCodes.OrganizationRequired,
                "A trusted organization context is required.");
        }

        if (!BnplProductIdentity.IsPinoyBuyNowPayLater(context.ProductCode))
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.WrongProduct,
                BnplAccessErrorCodes.WrongProduct,
                "Product identity is not authorized for Pinoy Buy Now Pay Later.");
        }

        if (!context.HasTrustedOrganizationMembership)
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.MembershipMissing,
                BnplAccessErrorCodes.MembershipRequired,
                "Active organization membership is required.");
        }

        if (!context.HasTrustedOrganizationEntitlement)
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.EntitlementMissing,
                BnplAccessErrorCodes.EntitlementRequired,
                "Organization BNPL entitlement is not active.");
        }

        if (!context.HasTrustedProductAssignment)
        {
            return BnplOperationalAccessDecision.Deny(
                BnplOperationalAccessDenialReason.ProductAccessDenied,
                BnplAccessErrorCodes.ProductAccessDenied,
                "BNPL product access is not assigned for this actor.");
        }

        if (requirement.RequiredBranchId is Guid requiredBranch)
        {
            if (requiredBranch == Guid.Empty)
            {
                return BnplOperationalAccessDecision.Deny(
                    BnplOperationalAccessDenialReason.BranchMissing,
                    BnplAccessErrorCodes.BranchRequired,
                    "A valid branch reference is required.");
            }

            if (!context.BranchScope.Allows(requiredBranch))
            {
                return BnplOperationalAccessDecision.Deny(
                    BnplOperationalAccessDenialReason.BranchDenied,
                    BnplAccessErrorCodes.BranchDenied,
                    "Branch access is not granted for this actor.");
            }
        }

        if (!string.IsNullOrWhiteSpace(requirement.RequiredCapability))
        {
            var capability = requirement.RequiredCapability.Trim();
            if (!BnplCapabilityCodes.IsKnown(capability))
            {
                return BnplOperationalAccessDecision.Deny(
                    BnplOperationalAccessDenialReason.CapabilityUnknown,
                    BnplAccessErrorCodes.CapabilityUnknown,
                    "Unknown BNPL capability.");
            }

            if (!context.HasCapability(capability))
            {
                return BnplOperationalAccessDecision.Deny(
                    BnplOperationalAccessDenialReason.CapabilityDenied,
                    BnplAccessErrorCodes.CapabilityDenied,
                    "Required BNPL capability is not granted.");
            }
        }

        return BnplOperationalAccessDecision.Allow(context);
    }
}
