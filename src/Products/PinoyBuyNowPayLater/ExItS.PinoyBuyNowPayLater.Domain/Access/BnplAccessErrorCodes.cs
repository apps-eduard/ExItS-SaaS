namespace ExItS.PinoyBuyNowPayLater.Domain.Access;

public static class BnplAccessErrorCodes
{
    public const string ContextUnavailable = "bnpl.access.context_unavailable";
    public const string ActorRequired = "bnpl.access.actor_required";
    public const string OrganizationRequired = "bnpl.access.organization_required";
    public const string MembershipRequired = "bnpl.access.membership_required";
    public const string EntitlementRequired = "bnpl.access.entitlement_required";
    public const string ProductAccessDenied = "bnpl.access.product_access_denied";
    public const string WrongProduct = "bnpl.access.wrong_product";
    public const string BranchRequired = "bnpl.access.branch_required";
    public const string BranchDenied = "bnpl.access.branch_denied";
    public const string CapabilityRequired = "bnpl.access.capability_required";
    public const string CapabilityDenied = "bnpl.access.capability_denied";
    public const string CapabilityUnknown = "bnpl.access.capability_unknown";
}
