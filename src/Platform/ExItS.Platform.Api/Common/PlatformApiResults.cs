using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Api.Common;

/// <summary>Shared ProblemDetails mapping for Organization and Subscription endpoints (mirrors CatalogResults).</summary>
internal static class PlatformApiResults
{
    public static IResult FromResult<T>(ApplicationResult<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return Problem(result.ErrorCode!, result.ErrorMessage!, MapStatusCode(result.ErrorCode!));
    }

    public static IResult Problem(string errorCode, string detail, int statusCode) =>
        Results.Problem(
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["errorCode"] = errorCode });

    public static int MapStatusCode(string errorCode) => errorCode switch
    {
        DomainErrorCodes.AuthorizationDenied
            or ApplicationErrorCodes.BootstrapUnauthorized
            or ApplicationErrorCodes.BootstrapForbiddenInEnvironment
            or ApplicationErrorCodes.AccountNotEligibleForLogin
            or ApplicationErrorCodes.OrganizationContextNotEligible
            or ApplicationErrorCodes.AccountScopeDenied
            or ApplicationErrorCodes.ProductEntryDenied => StatusCodes.Status403Forbidden,

        ApplicationErrorCodes.LoginFailed
            or ApplicationErrorCodes.SessionInvalid
            or ApplicationErrorCodes.SessionExpired
            or ApplicationErrorCodes.CurrentPasswordInvalid
            or ApplicationErrorCodes.CredentialTokenInvalid
            or ApplicationErrorCodes.CredentialTokenExpired
            or ApplicationErrorCodes.AccessTokenInvalid => StatusCodes.Status401Unauthorized,

        ApplicationErrorCodes.OrganizationNotFound
            or ApplicationErrorCodes.UserNotFound
            or ApplicationErrorCodes.MembershipNotFound
            or ApplicationErrorCodes.ProductAccessNotFound
            or ApplicationErrorCodes.SubscriptionNotFound
            or ApplicationErrorCodes.PlanNotFound
            or ApplicationErrorCodes.PlanVersionNotFound
            or ApplicationErrorCodes.ProductNotFound
            or ApplicationErrorCodes.TrialNotFound
            or ApplicationErrorCodes.FeatureNotFound
            or ApplicationErrorCodes.FeatureOverrideNotFound
            or ApplicationErrorCodes.EntitlementSnapshotNotFound
            or ApplicationErrorCodes.PaymentNotFound
            or ApplicationErrorCodes.RoleAssignmentNotFound
            or ApplicationErrorCodes.AuditRecordNotFound
            or ApplicationErrorCodes.CredentialNotFound
            or ApplicationErrorCodes.InvitationNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.SlugConflict
            or ApplicationErrorCodes.EmailConflict
            or ApplicationErrorCodes.UsernameConflict
            or ApplicationErrorCodes.MembershipConflict
            or ApplicationErrorCodes.ProductAccessConflict
            or ApplicationErrorCodes.CrossOrganizationMismatch
            or ApplicationErrorCodes.SubscriptionIneligible
            or ApplicationErrorCodes.EntitlementMissing
            or ApplicationErrorCodes.EntitlementStale
            or ApplicationErrorCodes.EntitlementDenied
            or ApplicationErrorCodes.ActiveSubscriptionConflict
            or ApplicationErrorCodes.ConcurrencyConflict
            or ApplicationErrorCodes.OrganizationNotEligible
            or ApplicationErrorCodes.ProductNotActive
            or ApplicationErrorCodes.PaymentReferenceConflict
            or ApplicationErrorCodes.PaymentAlreadyConfirmed
            or ApplicationErrorCodes.PaymentNotConfirmed
            or ApplicationErrorCodes.PaymentAlreadyUsed
            or ApplicationErrorCodes.PaymentInvalidTransition
            or ApplicationErrorCodes.PaymentProductMismatch
            or ApplicationErrorCodes.PaymentOrganizationMismatch
            or ApplicationErrorCodes.PaymentSubscriptionConflict
            or ApplicationErrorCodes.SnapshotVersionConflict
            or ApplicationErrorCodes.FeatureOverrideConflict
            or ApplicationErrorCodes.FeatureOverrideInvalidTransition
            or ApplicationErrorCodes.EntitlementProductMismatch
            or ApplicationErrorCodes.EntitlementSubscriptionInvalid
            or ApplicationErrorCodes.RoleAssignmentConflict
            or ApplicationErrorCodes.LastPlatformAdministratorProtected
            or ApplicationErrorCodes.RoleDefinitionConflict
            or ApplicationErrorCodes.CustomRoleAssignmentConflict
            or ApplicationErrorCodes.OrganizationRoleDefinitionConflict
            or ApplicationErrorCodes.OrganizationCustomRoleAssignmentConflict
            or ApplicationErrorCodes.CredentialAlreadyExists
            or ApplicationErrorCodes.BootstrapAlreadyCompleted
            or ApplicationErrorCodes.CredentialLockedOut
            or DomainErrorCodes.UserNotActive
            or DomainErrorCodes.OrganizationNotActive
            or DomainErrorCodes.MembershipNotActive
            or DomainErrorCodes.InvalidAccountStatusTransition
            or DomainErrorCodes.InvalidMembershipStatusTransition
            or DomainErrorCodes.PaymentAlreadyConfirmed
            or DomainErrorCodes.PaymentAlreadyUsed
            or DomainErrorCodes.InvalidSaaSPaymentTransition
            or DomainErrorCodes.UnsupportedSubscriptionStatus
            or DomainErrorCodes.LastGoverningAdminProtected
            or DomainErrorCodes.LastPlatformAdministratorProtected
            or DomainErrorCodes.BuiltInRoleProtected
            or DomainErrorCodes.RoleDefinitionNotAssignable
            or DomainErrorCodes.InvalidPlatformRoleStatusTransition
            or DomainErrorCodes.InvalidOrganizationRoleStatusTransition
            or DomainErrorCodes.InvitationExpired => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.PaymentAmountInvalid
            or ApplicationErrorCodes.PaymentCurrencyInvalid
            or ApplicationErrorCodes.EntitlementSnapshotInvalid
            or ApplicationErrorCodes.EntitlementSchemaUnsupported
            or ApplicationErrorCodes.EntitlementRefreshPolicyMissing
            or DomainErrorCodes.PaymentAmountInvalid
            or DomainErrorCodes.PaymentCurrencyInvalid
            or DomainErrorCodes.PaymentReferenceRequired
            or DomainErrorCodes.PaymentReasonRequired => StatusCodes.Status400BadRequest,

        _ when errorCode.Contains("not_found", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("invalid_transition", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("not_eligible", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,

        _ => StatusCodes.Status400BadRequest
    };
}
