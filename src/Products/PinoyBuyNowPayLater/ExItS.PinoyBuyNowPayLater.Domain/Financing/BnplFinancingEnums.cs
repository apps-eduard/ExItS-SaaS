namespace ExItS.PinoyBuyNowPayLater.Domain.Financing;

/// <summary>
/// BNPL-04 lifecycle states through APPROVED_PENDING_SALE.
/// ACTIVE is intentionally absent — reserved for BNPL-07 after Commerce sale.
/// </summary>
public enum BnplFinancingApplicationStatus
{
    Draft = 0,
    PendingEligibility = 1,
    Offered = 2,
    CustomerAccepted = 3,
    ApprovedPendingSale = 4,
    Declined = 5,
    Cancelled = 6
}

public enum BnplFinancingDecisionStage
{
    Eligibility = 0,
    Approval = 1,
    Cancellation = 2
}

public enum BnplFinancingDecisionOutcome
{
    Approved = 0,
    Declined = 1,
    Cancelled = 2
}
