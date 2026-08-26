namespace ExItS.PinoyLoanManager.Application.Access;

public sealed class PlmOperationalAccessDecision
{
    private PlmOperationalAccessDecision(
        bool isAllowed,
        PlmOperationalAccessDenialReason? denialReason,
        string? errorCode,
        string? detail,
        PlmAccessContext? context)
    {
        IsAllowed = isAllowed;
        DenialReason = denialReason;
        ErrorCode = errorCode;
        Detail = detail;
        Context = context;
    }

    public bool IsAllowed { get; }

    public PlmOperationalAccessDenialReason? DenialReason { get; }

    public string? ErrorCode { get; }

    public string? Detail { get; }

    public PlmAccessContext? Context { get; }

    public static PlmOperationalAccessDecision Allow(PlmAccessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new PlmOperationalAccessDecision(true, null, null, null, context);
    }

    public static PlmOperationalAccessDecision Deny(
        PlmOperationalAccessDenialReason reason,
        string errorCode,
        string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        return new PlmOperationalAccessDecision(false, reason, errorCode, detail, null);
    }
}
