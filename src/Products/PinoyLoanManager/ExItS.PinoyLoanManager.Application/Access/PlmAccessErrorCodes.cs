namespace ExItS.PinoyLoanManager.Application.Access;

/// <summary>
/// Stable Application-layer access error codes for API ProblemDetails mapping.
/// </summary>
public static class PlmAccessErrorCodes
{
    public const string ContextUnavailable = "plm.access.context_unavailable";
    public const string ActorRequired = "plm.access.actor_required";
    public const string OrganizationRequired = "plm.access.organization_required";
    public const string WrongProduct = "plm.access.wrong_product";
    public const string ProductAccessDenied = "plm.access.product_access_denied";
}
