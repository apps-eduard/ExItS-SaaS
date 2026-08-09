namespace ExItS.Platform.Domain.PrivacyCompliance;

public static class ComplianceStatusRules
{
    /// <summary>
    /// PDF exports must show DRAFT / NOT APPROVED unless the requirement status is Approved.
    /// Approved never implies legal/NPC certification — only that the readiness record omits the draft watermark.
    /// </summary>
    public static bool RequiresDraftWatermark(ComplianceItemStatus status) =>
        status != ComplianceItemStatus.Approved;

    public static bool CanTransition(ComplianceItemStatus from, ComplianceItemStatus to)
    {
        if (from == to)
        {
            return true;
        }

        return (from, to) switch
        {
            (ComplianceItemStatus.NotStarted, ComplianceItemStatus.InProgress) => true,
            (ComplianceItemStatus.NotStarted, ComplianceItemStatus.ReadyForReview) => true,
            (ComplianceItemStatus.InProgress, ComplianceItemStatus.ReadyForReview) => true,
            (ComplianceItemStatus.InProgress, ComplianceItemStatus.NotStarted) => true,
            (ComplianceItemStatus.ReadyForReview, ComplianceItemStatus.Approved) => true,
            (ComplianceItemStatus.ReadyForReview, ComplianceItemStatus.InProgress) => true,
            (ComplianceItemStatus.ReadyForReview, ComplianceItemStatus.NeedsUpdate) => true,
            (ComplianceItemStatus.Approved, ComplianceItemStatus.NeedsUpdate) => true,
            (ComplianceItemStatus.NeedsUpdate, ComplianceItemStatus.InProgress) => true,
            (ComplianceItemStatus.NeedsUpdate, ComplianceItemStatus.ReadyForReview) => true,
            _ => false
        };
    }
}
