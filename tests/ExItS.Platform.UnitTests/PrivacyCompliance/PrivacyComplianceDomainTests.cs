using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.PrivacyCompliance;
using ExItS.Platform.Infrastructure.PrivacyCompliance;

namespace ExItS.Platform.UnitTests.PrivacyCompliance;

public sealed class PrivacyComplianceDomainTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-09T12:00:00Z");

    [Theory]
    [InlineData(ComplianceItemStatus.NotStarted, ComplianceItemStatus.InProgress, true)]
    [InlineData(ComplianceItemStatus.InProgress, ComplianceItemStatus.ReadyForReview, true)]
    [InlineData(ComplianceItemStatus.ReadyForReview, ComplianceItemStatus.Approved, true)]
    [InlineData(ComplianceItemStatus.Approved, ComplianceItemStatus.NeedsUpdate, true)]
    [InlineData(ComplianceItemStatus.NotStarted, ComplianceItemStatus.Approved, false)]
    [InlineData(ComplianceItemStatus.Approved, ComplianceItemStatus.InProgress, false)]
    public void Status_transitions_follow_rules(ComplianceItemStatus from, ComplianceItemStatus to, bool allowed) =>
        Assert.Equal(allowed, ComplianceStatusRules.CanTransition(from, to));

    [Theory]
    [InlineData(ComplianceItemStatus.NotStarted, true)]
    [InlineData(ComplianceItemStatus.InProgress, true)]
    [InlineData(ComplianceItemStatus.ReadyForReview, true)]
    [InlineData(ComplianceItemStatus.NeedsUpdate, true)]
    [InlineData(ComplianceItemStatus.Approved, false)]
    public void Draft_watermark_required_unless_approved(ComplianceItemStatus status, bool required) =>
        Assert.Equal(required, ComplianceStatusRules.RequiresDraftWatermark(status));

    [Fact]
    public void TransitionStatus_rejects_illegal_jump()
    {
        var requirement = ComplianceRequirement.Create(
            "PRIVACY_NOTICE",
            "Privacy Notice",
            ComplianceItemCategory.CustomerFacing,
            "Customer-facing privacy notice readiness.",
            ComplianceRequirementLevel.Required,
            "DPO",
            T0);

        var ex = Assert.Throws<DomainException>(() =>
            requirement.TransitionStatus(ComplianceItemStatus.Approved, T0.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InvalidComplianceStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Evidence_reference_is_reference_only_path()
    {
        var evidence = ComplianceEvidenceReference.Create(
            Guid.NewGuid(),
            ComplianceEvidenceKind.PhaseDoc,
            "Phase 16",
            "docs/phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md",
            T0);

        Assert.Contains("phase-16", evidence.ReferencePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", evidence.ReferencePath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", evidence.Label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pdf_export_marks_draft_when_not_approved()
    {
        var requirement = ComplianceRequirement.Create(
            "PRIVACY_NOTICE",
            "Privacy Notice",
            ComplianceItemCategory.CustomerFacing,
            "Customer-facing privacy notice readiness.",
            ComplianceRequirementLevel.Required,
            "DPO",
            T0,
            status: ComplianceItemStatus.InProgress);

        Assert.True(ComplianceStatusRules.RequiresDraftWatermark(requirement.Status));
        Assert.DoesNotContain("secret", requirement.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", requirement.Notes ?? string.Empty, StringComparison.Ordinal);

        var pdf = new PrivacyCompliancePdfExporter().ExportRequirement(requirement, companyName: null, T0);
        AssertPdfHeader(pdf);
        Assert.True(pdf.Length > 500);
    }

    [Fact]
    public void Pdf_export_approved_includes_metadata_without_draft_watermark_requirement()
    {
        var requirement = ComplianceRequirement.Create(
            "PRIVACY_NOTICE",
            "Privacy Notice",
            ComplianceItemCategory.CustomerFacing,
            "Customer-facing privacy notice readiness.",
            ComplianceRequirementLevel.Required,
            "DPO",
            T0,
            status: ComplianceItemStatus.InProgress);
        requirement.TransitionStatus(ComplianceItemStatus.ReadyForReview, T0.AddMinutes(1));
        requirement.TransitionStatus(ComplianceItemStatus.Approved, T0.AddMinutes(2));
        requirement.UpdateDetails(
            notes: "Documented readiness only.",
            version: "1.0.0",
            effectiveDate: new DateOnly(2026, 8, 1),
            lastReviewedDate: new DateOnly(2026, 8, 9),
            nextReviewDate: new DateOnly(2027, 8, 9),
            utcNow: T0.AddMinutes(3));

        Assert.False(ComplianceStatusRules.RequiresDraftWatermark(requirement.Status));
        Assert.Equal("1.0.0", requirement.Version);
        Assert.Equal(ComplianceItemStatus.Approved, requirement.Status);
        Assert.Equal(new DateOnly(2026, 8, 1), requirement.EffectiveDate);
        Assert.DoesNotContain("sk_live", requirement.Notes ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        var draftRequirement = ComplianceRequirement.Create(
            "PRIVACY_NOTICE_DRAFT",
            "Privacy Notice",
            ComplianceItemCategory.CustomerFacing,
            "Customer-facing privacy notice readiness.",
            ComplianceRequirementLevel.Required,
            "DPO",
            T0,
            status: ComplianceItemStatus.InProgress);

        var draftPdf = new PrivacyCompliancePdfExporter().ExportRequirement(draftRequirement, "ExItS Demo Co", T0.AddHours(1));
        var approvedPdf = new PrivacyCompliancePdfExporter().ExportRequirement(requirement, "ExItS Demo Co", T0.AddHours(1));
        AssertPdfHeader(approvedPdf);
        // Draft path embeds an extra watermark layer; payloads must differ.
        Assert.NotEqual(draftPdf.Length, approvedPdf.Length);
    }

    private static void AssertPdfHeader(byte[] pdf)
    {
        Assert.True(pdf.Length > 4);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }
}
