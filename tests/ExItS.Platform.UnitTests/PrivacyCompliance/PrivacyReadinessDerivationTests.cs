using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.UnitTests.PrivacyCompliance;

public sealed class PrivacyReadinessDerivationTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-15T12:00:00Z");

    [Fact]
    public void DeriveOverall_empty_is_not_assessed() =>
        Assert.Equal(
            PrivacyReadinessOverallStatus.NotAssessed,
            PrivacyReadinessDerivation.DeriveOverall([]));

    [Fact]
    public void DeriveOverall_all_not_started_required_is_not_started()
    {
        var items = new[]
        {
            Req("A", ComplianceItemStatus.NotStarted),
            Req("B", ComplianceItemStatus.NotStarted)
        };
        Assert.Equal(
            PrivacyReadinessOverallStatus.NotStarted,
            PrivacyReadinessDerivation.DeriveOverall(items));
    }

    [Fact]
    public void DeriveOverall_action_needed_beats_in_progress()
    {
        var items = new[]
        {
            Req("A", ComplianceItemStatus.InProgress),
            Req("B", ComplianceItemStatus.NeedsUpdate)
        };
        Assert.Equal(
            PrivacyReadinessOverallStatus.ActionNeeded,
            PrivacyReadinessDerivation.DeriveOverall(items));
    }

    [Fact]
    public void DeriveOverall_external_legal_when_dpo_flag_outstanding()
    {
        var items = new[]
        {
            Req("A", ComplianceItemStatus.InProgress),
            Req("NPC", ComplianceItemStatus.InProgress, requiresDpo: true)
        };
        Assert.Equal(
            PrivacyReadinessOverallStatus.ExternalLegalReviewRequired,
            PrivacyReadinessDerivation.DeriveOverall(items));
    }

    [Fact]
    public void Evidence_display_keeps_tests_technical_even_on_dpo_requirement()
    {
        var kind = PrivacyReadinessDerivation.ResolveEvidenceDisplayKind(
            ComplianceEvidenceKind.Test,
            requirementRequiresDpoLegalVerification: true,
            "DPO_REGISTRATION_READINESS");
        Assert.Equal(PrivacyEvidenceDisplayKind.TechnicalEvidence, kind);
    }

    [Fact]
    public void Evidence_display_marks_docs_on_npc_requirement_as_regulatory()
    {
        var kind = PrivacyReadinessDerivation.ResolveEvidenceDisplayKind(
            ComplianceEvidenceKind.PhaseDoc,
            requirementRequiresDpoLegalVerification: true,
            "NPC_CERTIFICATE_RECORDS");
        Assert.Equal(PrivacyEvidenceDisplayKind.RegulatoryEvidence, kind);
    }

    [Fact]
    public void Missing_evidence_does_not_equal_approved_ready()
    {
        var requirement = Req("PRIVACY_NOTICE", ComplianceItemStatus.NotStarted);
        Assert.False(PrivacyReadinessDerivation.IsReadyStatus(requirement.Status));
        Assert.True(PrivacyReadinessDerivation.IsActionNeededStatus(requirement.Status));
    }

    [Theory]
    [InlineData("PRIVACY_NOTICE", ComplianceItemCategory.CustomerFacing, PrivacyReadinessCategoryGroup.NoticesAndConsent)]
    [InlineData("SECURITY_ACCESS_CONTROL", ComplianceItemCategory.Internal, PrivacyReadinessCategoryGroup.SecurityAndAccess)]
    [InlineData("PIA_P25_TYPED_QR", ComplianceItemCategory.PrivacyImpactAssessment, PrivacyReadinessCategoryGroup.PrivacyImpact)]
    [InlineData("DPO_REGISTRATION_READINESS", ComplianceItemCategory.RegulatoryReadiness, PrivacyReadinessCategoryGroup.DpoRegulatoryReadiness)]
    public void Category_group_mapping(
        string code,
        ComplianceItemCategory category,
        PrivacyReadinessCategoryGroup expected) =>
        Assert.Equal(expected, PrivacyReadinessDerivation.ResolveCategoryGroup(code, category));

    private static ComplianceRequirement Req(
        string code,
        ComplianceItemStatus status,
        bool requiresDpo = false) =>
        ComplianceRequirement.Create(
            code,
            code,
            ComplianceItemCategory.Internal,
            "test",
            ComplianceRequirementLevel.Required,
            "DPO",
            T0,
            status: status,
            requiresDpoLegalVerification: requiresDpo);
}
