using ExItS.Deployment;

namespace ExItS.Deployment.Tests;

public sealed class DeploymentConfigValidatorTests
{
    [Fact]
    public void Development_allows_empty_connections()
    {
        var result = DeploymentConfigValidator.Validate(Base(ExItsEnvironmentKind.Development) with
        {
            WorkingTreeClean = false,
            ApplicationGitCommit = "abc123"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void StagingPilot_rejects_wildcard_hosts_and_dev_password()
    {
        var result = DeploymentConfigValidator.Validate(Base(ExItsEnvironmentKind.StagingPilot) with
        {
            AllowedHosts = "*",
            PlatformConnectionString = "Host=db;Password=exits_platform_dev_only;Database=platform",
            PosConnectionString = "Host=db;Password=secret;Database=pos",
            EnforceHttps = true,
            BackupVerified = true,
            PlatformBackupSetId = "p1",
            PosBackupSetId = "s1",
            DestructiveConfirmation = DeploymentConstants.PilotConfirmationToken,
            WorkingTreeClean = true,
            ApplicationGitCommit = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Code == "hosts.wildcard");
        Assert.Contains(result.Findings, f => f.Code.Contains("dev_password", StringComparison.Ordinal));
    }

    [Fact]
    public void StagingPilot_accepts_valid_pilot_settings()
    {
        var result = DeploymentConfigValidator.Validate(ValidPilot());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Production_always_blocked_by_known_release_blockers()
    {
        var result = DeploymentConfigValidator.Validate(ValidPilot() with
        {
            Environment = ExItsEnvironmentKind.Production,
            DestructiveConfirmation = DeploymentConstants.ProductionConfirmationToken
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Code == "blocker.R-091");
        Assert.Contains(result.Findings, f => f.Code == "blocker.R-109");
        Assert.Contains(result.Findings, f => f.Code == "blocker.R-129");
    }

    [Fact]
    public void StagingPilot_requires_explicit_confirmation()
    {
        var result = DeploymentConfigValidator.Validate(ValidPilot() with { DestructiveConfirmation = null });
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Code == "pilot.confirmation");
    }

    [Fact]
    public void StagingPilot_rejects_dirty_tree()
    {
        var result = DeploymentConfigValidator.Validate(ValidPilot() with { WorkingTreeClean = false });
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Code == "git.dirty");
    }

    [Fact]
    public void StagingPilot_rejects_cleartext_api_urls()
    {
        var result = DeploymentConfigValidator.Validate(ValidPilot() with
        {
            MauiApiBaseUrl = "http://pilot.example.com/api"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Code == "tls.cleartext");
    }

    [Fact]
    public void Rejects_HealthCare_database_target()
    {
        var result = DeploymentConfigValidator.Validate(ValidPilot() with
        {
            PlatformConnectionString = "Host=db;Password=secret;Database=HealthCare"
        });
        Assert.False(result.IsValid);
        Assert.Contains(result.Findings, f => f.Code.Contains("healthcare", StringComparison.OrdinalIgnoreCase));
    }

    private static DeploymentSettings ValidPilot() => Base(ExItsEnvironmentKind.StagingPilot) with
    {
        AllowedHosts = "pilot.example.internal",
        PlatformConnectionString = "Host=platform-db;Username=platform_app;Password=not_dev_marker;Database=exits_platform",
        PosConnectionString = "Host=pos-db;Username=pos_app;Password=not_dev_marker;Database=exits_pos",
        EnforceHttps = true,
        BackupVerified = true,
        PlatformBackupSetId = "platform-set-1",
        PosBackupSetId = "pos-set-1",
        DestructiveConfirmation = DeploymentConstants.PilotConfirmationToken,
        WorkingTreeClean = true,
        ApplicationGitCommit = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        MauiApiBaseUrl = "https://pilot.example.internal/pos",
        PlatformApiBaseUrl = "https://pilot.example.internal/platform",
        PosApiBaseUrl = "https://pilot.example.internal/pos"
    };

    private static DeploymentSettings Base(ExItsEnvironmentKind env) => new()
    {
        Environment = env,
        ApplicationGitCommit = "cccccccccccccccccccccccccccccccccccccccc",
        WorkingTreeClean = true
    };
}

public sealed class BackupGateAndMigrationTests
{
    [Fact]
    public void Backup_gate_blocks_when_unverified()
    {
        var gate = BackupBeforeDeployGate.Evaluate(false, true, "a", "b");
        Assert.False(gate.Allowed);
    }

    [Fact]
    public void Backup_gate_allows_when_both_verified()
    {
        var gate = BackupBeforeDeployGate.Evaluate(true, true, "a", "b");
        Assert.True(gate.Allowed);
    }

    [Fact]
    public void Migration_order_is_platform_then_pos()
    {
        Assert.Equal(4, MigrationOrder.RequiredSteps.Count);
        Assert.Equal("Platform", MigrationOrder.RequiredSteps[0].DatabaseKind);
        Assert.Equal("PinoyBusinessPos", MigrationOrder.RequiredSteps[2].DatabaseKind);
        Assert.DoesNotContain(MigrationOrder.RequiredSteps, s =>
            s.DatabaseKind.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class SecretRedactionTests
{
    [Fact]
    public void Redacts_password_and_bearer()
    {
        var redacted = SecretRedaction.Redact("Host=db;Password=supersecret;Username=u Bearer abc.def.ghi");
        Assert.DoesNotContain("supersecret", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("abc.def.ghi", redacted, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureNoSecrets_throws_on_password()
    {
        Assert.Throws<InvalidOperationException>(() => SecretRedaction.EnsureNoSecrets("password=x", "log"));
    }
}

public sealed class PackageVersionAndSmokeTests
{
    [Fact]
    public void Package_version_is_deterministic_for_commit_and_build()
    {
        var a = PackageVersionGenerator.Create("deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", "7");
        var b = PackageVersionGenerator.Create("deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", "7");
        Assert.Equal(a.Display, b.Display);
        Assert.Contains("deadbeefdead", a.Display, StringComparison.Ordinal);
        Assert.EndsWith("+7", a.Display, StringComparison.Ordinal);
    }

    [Fact]
    public void Package_version_rejects_dirty_marker()
    {
        Assert.Throws<InvalidOperationException>(() => PackageVersionGenerator.Create("abc-dirty", "1"));
    }

    [Fact]
    public void Smoke_catalog_covers_platform_and_pos_contracts()
    {
        Assert.Contains("platform.health_readiness", SmokeTestCatalog.PlatformContracts);
        Assert.Contains("pos.cash_sale", SmokeTestCatalog.PosContracts);
        Assert.Contains("pos.idempotent_replay", SmokeTestCatalog.PosContracts);
    }

    [Fact]
    public void Phase_marker_is_p9_wp05()
    {
        Assert.Equal("P10-WP06-advanced-permissions-operational-reports", DeploymentConstants.PhaseMarker);
    }
}

public sealed class ReleaseReadinessAndRollbackTests
{
    [Fact]
    public void Readiness_is_internal_pilot_when_entry_met_but_blockers_open()
    {
        var assessment = ReleaseReadinessEvaluator.Evaluate(
            true, true, true, true, true, true);
        Assert.Equal(ReleaseReadinessState.ReadyForControlledInternalPilot, assessment.State);
        Assert.Contains(assessment.OpenBlockers, b => b.Contains("R-091", StringComparison.Ordinal));
        Assert.Contains(assessment.OpenBlockers, b => b.Contains("R-109", StringComparison.Ordinal));
        Assert.Contains(assessment.OpenBlockers, b => b.Contains("R-129", StringComparison.Ordinal));
    }

    [Fact]
    public void Readiness_blocked_when_smoke_fails()
    {
        var assessment = ReleaseReadinessEvaluator.Evaluate(
            true, true, true, true, true, false);
        Assert.Equal(ReleaseReadinessState.Blocked, assessment.State);
    }

    [Fact]
    public void Production_ready_only_when_all_blockers_cleared()
    {
        var assessment = ReleaseReadinessEvaluator.Evaluate(
            true, true, true, true, true, true,
            productionAuthImplemented: true,
            interactiveAndroidValidated: true,
            localEncryptionResolved: true,
            productionTlsValidated: true);
        Assert.Equal(ReleaseReadinessState.ReadyForProduction, assessment.State);
    }

    [Fact]
    public void Rollback_migration_requires_backup_restore()
    {
        var d = RollbackAdvisor.Advise("migration");
        Assert.True(d.RestoreFromBackupRequired);
        Assert.False(d.ApplicationVersionRollbackSufficient);
    }

    [Fact]
    public void Rollback_smoke_is_application_redeploy()
    {
        var d = RollbackAdvisor.Advise("smoke");
        Assert.False(d.RestoreFromBackupRequired);
        Assert.True(d.ApplicationVersionRollbackSufficient);
    }

    [Fact]
    public void Backup_verify_failure_blocks_deploy()
    {
        var d = RollbackAdvisor.Advise("backup_verify");
        Assert.False(d.ApplicationVersionRollbackSufficient);
        Assert.False(d.RestoreFromBackupRequired);
    }
}

public sealed class AndroidAndCompatibilityTests
{
    [Fact]
    public void Android_rejects_arbitrary_cleartext_for_pilot()
    {
        var result = AndroidEnvironmentValidator.ValidatePilotOrRelease(
            "http://api.example.com", ExItsEnvironmentKind.StagingPilot);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Android_allows_https_for_pilot()
    {
        var result = AndroidEnvironmentValidator.ValidatePilotOrRelease(
            "https://pilot.example.internal/pos", ExItsEnvironmentKind.StagingPilot);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Platform_and_pos_share_same_phase_marker_constant()
    {
        Assert.Equal(DeploymentConstants.PhaseMarker, "P10-WP06-advanced-permissions-operational-reports");
    }

    [Fact]
    public void HealthCare_exclusion_detects_forbidden_names()
    {
        Assert.True(HealthCareExclusion.IsForbiddenTarget("Server=x;Database=HealthCare"));
        Assert.False(HealthCareExclusion.IsForbiddenTarget("Server=x;Database=exits_platform"));
    }

    [Fact]
    public void Environment_matrix_documents_all_four_environments()
    {
        Assert.Equal(4, EnvironmentMatrix.Purposes.Count);
        Assert.Contains("identity headers", EnvironmentMatrix.Purposes[ExItsEnvironmentKind.Development], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("blocked", EnvironmentMatrix.Purposes[ExItsEnvironmentKind.Production], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_confirmation_token_is_explicit_phrase()
    {
        Assert.Equal("DEPLOY_PRODUCTION_CONFIRMED", DeploymentConstants.ProductionConfirmationToken);
        Assert.Equal("DEPLOY_PILOT_CONFIRMED", DeploymentConstants.PilotConfirmationToken);
    }
}

public sealed class DryRunAndPreservationTests
{
    [Fact]
    public void Dry_run_validation_does_not_require_live_databases()
    {
        // Validate with placeholders only — no network.
        var result = DeploymentConfigValidator.Validate(new DeploymentSettings
        {
            Environment = ExItsEnvironmentKind.Testing,
            ApplicationGitCommit = "ffffffffffffffffffffffffffffffffffffffff",
            WorkingTreeClean = true
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Known_dev_password_marker_matches_platform_guard()
    {
        Assert.Equal("exits_platform_dev_only", DeploymentConstants.KnownDevPasswordMarker);
    }

    [Fact]
    public void Deployment_library_exposes_no_tax_payroll_or_gateway_surface()
    {
        var typeNames = typeof(DeploymentConstants).Assembly.GetTypes().Select(t => t.Name);
        Assert.DoesNotContain(typeNames, n => n.Contains("Payroll", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, n => n.Contains("TaxVat", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, n => n.Contains("PaymentGateway", StringComparison.OrdinalIgnoreCase));
    }
}
