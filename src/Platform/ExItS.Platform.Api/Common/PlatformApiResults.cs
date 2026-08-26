using System.Globalization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
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

    public static IResult ImageFile(HttpResponse response, GlobalProductImageBytes image)
    {
        response.Headers["X-ExItS-Image-Version"] = image.Version.ToString(CultureInfo.InvariantCulture);
        return Results.File(image.Content, image.ContentType);
    }

    public static IResult Problem(string errorCode, string detail, int statusCode) =>
        Results.Problem(
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["errorCode"] = errorCode });

    public static int MapStatusCode(string errorCode) => errorCode switch
    {
        DomainErrorCodes.AuthorizationDenied
            or DomainErrorCodes.OwnershipTransferActorMismatch
            or ApplicationErrorCodes.BootstrapUnauthorized
            or ApplicationErrorCodes.BootstrapForbiddenInEnvironment
            or ApplicationErrorCodes.AccountNotEligibleForLogin
            or ApplicationErrorCodes.OrganizationContextNotEligible
            or ApplicationErrorCodes.AccountScopeDenied
            or ApplicationErrorCodes.ProductEntryDenied
            or ApplicationErrorCodes.ProductLocalRoleMissing
            or ApplicationErrorCodes.BranchAccessDenied
            or ApplicationErrorCodes.PosDeviceNotAuthorized
            or ApplicationErrorCodes.PosDeviceRegistrationRequired
            or ApplicationErrorCodes.PosDeviceRevoked
            or ApplicationErrorCodes.PersonalUtangUnauthorized
            or ApplicationErrorCodes.PersonalConnectionUnauthorized
            or ApplicationErrorCodes.PersonalTodoUnauthorized
            or DomainErrorCodes.PersonalReminderUnauthorized
            or DomainErrorCodes.PersonalTodoUnauthorized
            or DomainErrorCodes.CustomerToStaffConversionDenied
            or DomainErrorCodes.CustomerLinkMustNotCreateStaff
            or DomainErrorCodes.CustomerLinkPersonalIdentityRequired
            or DomainErrorCodes.StaffCannotAccessUnrelatedPersonalRecords
            or ApplicationErrorCodes.UtangMigrationConsentRequired
            or ApplicationErrorCodes.StartBusinessOwnerRequired
            or DomainErrorCodes.PersonalUtangMigrationConsentRequired
            or ApplicationErrorCodes.BusinessTypeNotEntitled
            or ApplicationErrorCodes.BusinessTypeInactive => StatusCodes.Status403Forbidden,

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
            or         ApplicationErrorCodes.InvitationNotFound
            or ApplicationErrorCodes.OwnershipTransferNotFound
            or ApplicationErrorCodes.PersonalContactNotFound
            or ApplicationErrorCodes.PersonalUtangRelationshipNotFound
            or ApplicationErrorCodes.PersonalUtangInvitationNotFound
            or ApplicationErrorCodes.PersonalReminderNotFound
            or ApplicationErrorCodes.PersonalNotificationNotFound
            or ApplicationErrorCodes.PersonalTodoNotFound
            or ApplicationErrorCodes.BusinessCustomerNotFound
            or ApplicationErrorCodes.CreditCustomerNotFound
            or ApplicationErrorCodes.CustomerLinkRequestNotFound
            or ApplicationErrorCodes.LinkedCustomerAppUserNotFound
            or ApplicationErrorCodes.UtangMigrationBatchNotFound
            or ApplicationErrorCodes.BusinessTypeActivationNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.PosDeviceBranchConflict
            or ApplicationErrorCodes.SlugConflict
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
            or ApplicationErrorCodes.TrialNotAllowed
            or ApplicationErrorCodes.TrialAlreadyConsumed
            or ApplicationErrorCodes.ConcurrencyConflict
            or ApplicationErrorCodes.PersonalRewardBalanceConflict
            or ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported
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
            or ApplicationErrorCodes.StepUpRequired
            or ApplicationErrorCodes.MfaStepUpRequired
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
            or DomainErrorCodes.InvitationExpired
            or DomainErrorCodes.OwnershipTransferExpired
            or DomainErrorCodes.OwnershipTransferPendingConflict
            or DomainErrorCodes.OwnershipTransferOwnerInvariant
            or DomainErrorCodes.OwnershipTransferSelfDenied
            or DomainErrorCodes.PersonalUtangInvitationExpired
            or ApplicationErrorCodes.PersonalUtangInvitationConflict
            or ApplicationErrorCodes.PersonalConnectionRequestConflict
            or ApplicationErrorCodes.PersonalConnectionBlocked
            or ApplicationErrorCodes.OwnershipTransferConflict
            or ApplicationErrorCodes.PersonalContactEmailConflict
            or ApplicationErrorCodes.PersonalContactIdentityConflict
            or ApplicationErrorCodes.PersonalContactLinkConflict
            or ApplicationErrorCodes.PersonalUtangPendingLimitReached
            or ApplicationErrorCodes.PersonalUtangDuplicateSubmission
            or DomainErrorCodes.PersonalUtangPendingLimitReached
            or DomainErrorCodes.PersonalUtangDuplicateSubmission
            or ApplicationErrorCodes.CreditCustomerConflict
            or ApplicationErrorCodes.CustomerLinkRequestConflict
            or DomainErrorCodes.CustomerLinkRequestExpired
            or DomainErrorCodes.BusinessCustomerAlreadyLinked
            or ApplicationErrorCodes.UtangMigrationAlreadyMigrated
            or ApplicationErrorCodes.UtangMigrationConfirmationMismatch
            or ApplicationErrorCodes.UtangMigrationPreviewRequired
            or ApplicationErrorCodes.ProductLocalRoleGrantConflict
            or ApplicationErrorCodes.InvitationRequiresAuthenticatedPersonal
            or ApplicationErrorCodes.InvitationPersonalEmailUnverified
            or DomainErrorCodes.PersonalUtangAlreadyMigrated => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.PersonalReminderRateLimited
            or DomainErrorCodes.PersonalReminderRateLimited
            or ApplicationErrorCodes.PersonalUtangInvitationRateLimited
            or ApplicationErrorCodes.PersonalUtangDailyLimitReached
            or DomainErrorCodes.PersonalUtangInvitationRateLimited
            or DomainErrorCodes.PersonalUtangDailyLimitReached => StatusCodes.Status429TooManyRequests,

        ApplicationErrorCodes.OrganizationContextRequired
            or ApplicationErrorCodes.AuthPublicSurfaceInvalid
            or ApplicationErrorCodes.PaymentAmountInvalid
            or ApplicationErrorCodes.PaymentCurrencyInvalid
            or ApplicationErrorCodes.PaymentAmountMismatch
            or ApplicationErrorCodes.PaymentCurrencyMismatch
            or ApplicationErrorCodes.PaymentPeriodMismatch
            or ApplicationErrorCodes.EntitlementSnapshotInvalid
            or ApplicationErrorCodes.EntitlementSchemaUnsupported
            or ApplicationErrorCodes.EntitlementRefreshPolicyMissing
            or ApplicationErrorCodes.UtangMigrationSelectionRequired
            or DomainErrorCodes.PersonalUtangMigrationSelectionRequired
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
