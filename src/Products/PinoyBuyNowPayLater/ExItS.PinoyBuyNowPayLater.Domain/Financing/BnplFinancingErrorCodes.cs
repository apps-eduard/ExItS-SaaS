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
