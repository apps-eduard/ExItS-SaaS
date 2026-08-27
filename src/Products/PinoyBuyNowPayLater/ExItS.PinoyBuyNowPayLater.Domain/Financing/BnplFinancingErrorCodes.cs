namespace ExItS.PinoyBuyNowPayLater.Domain.Financing;

public static class BnplFinancingErrorCodes
{
    public const string InvalidApplicationId = "bnpl.financing.invalid_application_id";
    public const string InvalidOfferId = "bnpl.financing.invalid_offer_id";
    public const string InvalidOrganizationId = "bnpl.financing.invalid_organization_id";
    public const string InvalidBranchId = "bnpl.financing.invalid_branch_id";
    public const string InvalidCustomerId = "bnpl.financing.invalid_customer_id";
    public const string InvalidActorId = "bnpl.financing.invalid_actor_id";
    public const string InvalidAmount = "bnpl.financing.invalid_amount";
    public const string InvalidState = "bnpl.financing.invalid_state";
    public const string IdempotencyConflict = "bnpl.financing.idempotency_conflict";
    public const string ConcurrencyConflict = "bnpl.financing.concurrency_conflict";
    public const string NotFound = "bnpl.financing.not_found";
    public const string CustomerOrgMismatch = "bnpl.financing.customer_org_mismatch";
    public const string OfferSuperseded = "bnpl.financing.offer_superseded";
    public const string OfferExpired = "bnpl.financing.offer_expired";
    public const string OfferImmutable = "bnpl.financing.offer_immutable";
    public const string EligibilityRequired = "bnpl.financing.eligibility_required";
    public const string ActiveProhibited = "bnpl.financing.active_prohibited";
    public const string InvalidPlanId = "bnpl.financing.invalid_plan_id";
    public const string InvalidPlanItemId = "bnpl.financing.invalid_plan_item_id";
    public const string PlanEmpty = "bnpl.financing.plan_empty";
    public const string PlanTooLarge = "bnpl.financing.plan_too_large";
    public const string PlanTotalMismatch = "bnpl.financing.plan_total_mismatch";
    public const string InvalidPlanSequence = "bnpl.financing.invalid_plan_sequence";
    public const string InvalidPlanAmount = "bnpl.financing.invalid_plan_amount";
    public const string InvalidPlanDueDate = "bnpl.financing.invalid_plan_due_date";
    public const string DuplicatePlanItemId = "bnpl.financing.duplicate_plan_item_id";
    public const string PlanRequired = "bnpl.financing.plan_required";
    public const string PlanImmutable = "bnpl.financing.plan_immutable";
}

public sealed class BnplFinancingDomainException : Exception
{
    public BnplFinancingDomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
