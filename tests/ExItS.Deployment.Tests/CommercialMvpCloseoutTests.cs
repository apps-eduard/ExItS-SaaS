using ExItS.Deployment;

namespace ExItS.Deployment.Tests;

public sealed class CommercialMvpCloseoutTests
{
    [Fact]
    public void Closeout_phase_marker_is_p9_wp06()
    {
        Assert.Equal("P9-WP06-commercial-mvp-closeout", CloseoutConstants.PhaseMarker);
        Assert.Equal("P10-WP01-suppliers", DeploymentConstants.PhaseMarker);
        Assert.Equal("Phase 10 — Full POS", CloseoutConstants.ExactNextPhase);
    }

    [Fact]
    public void Risk_register_keeps_mandatory_release_blockers_open()
    {
        Assert.Contains(CommercialMvpRiskRegister.Current, r => r.Id == "R-091" && r.Classification == RiskClassification.ReleaseBlocker);
        Assert.Contains(CommercialMvpRiskRegister.Current, r => r.Id == "R-109" && r.Classification == RiskClassification.ReleaseBlocker);
        Assert.Contains(CommercialMvpRiskRegister.Current, r => r.Id == "R-129" && r.Classification == RiskClassification.ReleaseBlocker);
        Assert.Contains(CommercialMvpRiskRegister.Current, r => r.Id == "TLS-PROD" && r.Classification == RiskClassification.ReleaseBlocker);
        Assert.Contains(CommercialMvpRiskRegister.Current, r => r.Id == "PITR" && r.Classification == RiskClassification.DeferredEnhancement);
        Assert.Contains(CommercialMvpRiskRegister.Current, r => r.Id == "GCASH-MANUAL" && r.Classification == RiskClassification.AcceptedCommercialLimitation);
    }

    [Fact]
    public void Readiness_board_blocks_external_pilot_and_production()
    {
        var board = CommercialMvpReadinessBoard.Assess();
        Assert.Equal(EnvironmentDecisionState.ReadyWithDocumentedNonBlockingRisks,
            board.Single(d => d.Environment == CloseoutTargetEnvironment.Development).State);
        Assert.Equal(EnvironmentDecisionState.ReadyWithDocumentedNonBlockingRisks,
            board.Single(d => d.Environment == CloseoutTargetEnvironment.TestingCi).State);
        Assert.Equal(EnvironmentDecisionState.ReadyWithDocumentedNonBlockingRisks,
            board.Single(d => d.Environment == CloseoutTargetEnvironment.ControlledInternalTechnicalPilot).State);
        Assert.Equal(EnvironmentDecisionState.Blocked,
            board.Single(d => d.Environment == CloseoutTargetEnvironment.RestrictedExternalPilot).State);
        Assert.Equal(EnvironmentDecisionState.Blocked,
            board.Single(d => d.Environment == CloseoutTargetEnvironment.Production).State);
        Assert.Contains("R-091", board.Single(d => d.Environment == CloseoutTargetEnvironment.Production).BlockingIds);
    }

    [Fact]
    public void Readiness_board_blocks_internal_pilot_when_entry_unmet()
    {
        var decision = CommercialMvpReadinessBoard.Assess(internalPilotEntryMet: false)
            .Single(d => d.Environment == CloseoutTargetEnvironment.ControlledInternalTechnicalPilot);
        Assert.Equal(EnvironmentDecisionState.Blocked, decision.State);
    }

    [Fact]
    public void Production_ready_only_when_all_release_blockers_cleared()
    {
        var production = CommercialMvpReadinessBoard.Assess(
            productionAuthImplemented: true,
            interactiveAndroidValidated: true,
            localEncryptionResolved: true,
            productionTlsValidated: true,
            mauiHttpsOnlyProduction: true,
            posOperationalRolesImplemented: true)
            .Single(d => d.Environment == CloseoutTargetEnvironment.Production);
        Assert.Equal(EnvironmentDecisionState.Ready, production.State);
        Assert.Empty(production.BlockingIds);
    }

    [Fact]
    public void Capability_inventory_covers_platform_and_pos_without_deferred_claims()
    {
        Assert.True(CommercialMvpCapabilityInventory.Platform.Count >= 10);
        Assert.True(CommercialMvpCapabilityInventory.PinoyBusinessPos.Count >= 10);
        Assert.All(CommercialMvpCapabilityInventory.Platform, i => Assert.Equal("Delivered", i.DeliveredStatus));
        Assert.DoesNotContain(CommercialMvpCapabilityInventory.PinoyBusinessPos, i =>
            i.Name.Contains("payroll", StringComparison.OrdinalIgnoreCase)
            || i.Name.Contains("purchasing", StringComparison.OrdinalIgnoreCase)
            || i.Name.Contains("tax", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(CommercialMvpCapabilityInventory.Platform, i =>
            i.Name.Contains("SaaS payments", StringComparison.Ordinal)
            && i.ImportantLimitation.Contains("Cash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Database_boundaries_forbid_healthcare_and_cross_db_fks()
    {
        Assert.Equal("ExItS_Platform", DatabaseOwnershipBoundaries.PlatformDatabase);
        Assert.Equal("ExItS_PinoyBusinessPOS", DatabaseOwnershipBoundaries.PosDatabase);
        Assert.True(DatabaseOwnershipBoundaries.ForbidsCrossDatabaseForeignKeys);
        Assert.True(DatabaseOwnershipBoundaries.ForbidsHealthCareCoupling);
        Assert.Contains("organizations", DatabaseOwnershipBoundaries.PlatformOwns);
        Assert.Contains("sales", DatabaseOwnershipBoundaries.PosOwns);
        Assert.DoesNotContain(DatabaseOwnershipBoundaries.PlatformOwns, o =>
            o.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Phase9_reconciliation_tokens_remain_documented()
    {
        Assert.Contains("ValidateProductionConfigurationOrThrow", Phase9ReconciliationGuard.RequiredSecurityTokens);
        Assert.Contains("/health/ready", Phase9ReconciliationGuard.RequiredReliabilityTokens);
        Assert.Contains("DESTROY_AND_RESTORE", Phase9ReconciliationGuard.RequiredBackupTokens);
        Assert.Contains("NON-PRODUCTION", Phase9ReconciliationGuard.RequiredDeployTokens);
    }
}
