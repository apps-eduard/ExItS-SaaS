namespace ExItS.PinoyBuyNowPayLater.Application.Access;

public enum BnplOperationalAccessDenialReason
{
    ContextUnavailable = 1,
    ActorMissing = 2,
    OrganizationMissing = 3,
    MembershipMissing = 4,
    EntitlementMissing = 5,
    ProductAccessDenied = 6,
    WrongProduct = 7,
    BranchMissing = 8,
    BranchDenied = 9,
    CapabilityMissing = 10,
    CapabilityDenied = 11,
    CapabilityUnknown = 12
}

public sealed class BnplOperationalAccessDecision
{
    private BnplOperationalAccessDecision(
        bool isAllowed,
        BnplAccessContext? context,
        BnplOperationalAccessDenialReason? denialReason,
        string? errorCode,
        string? detail)
    {
        IsAllowed = isAllowed;
        Context = context;
        DenialReason = denialReason;
        ErrorCode = errorCode;
        Detail = detail;
    }

    public bool IsAllowed { get; }

    public BnplAccessContext? Context { get; }

    public BnplOperationalAccessDenialReason? DenialReason { get; }

    public string? ErrorCode { get; }

    public string? Detail { get; }

    public static BnplOperationalAccessDecision Allow(BnplAccessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new(true, context, null, null, null);
    }

    public static BnplOperationalAccessDecision Deny(
        BnplOperationalAccessDenialReason reason,
        string errorCode,
        string detail) =>
        new(false, null, reason, errorCode, detail);
}

public interface IBnplOperationalAccessGuard
{
    ValueTask<BnplOperationalAccessDecision> EvaluateAsync(
        BnplAccessRequirement? requirement = null,
        CancellationToken cancellationToken = default);
}
