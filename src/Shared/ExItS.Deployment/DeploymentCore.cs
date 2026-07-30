using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ExItS.Deployment;

public enum ExItsEnvironmentKind
{
    Development = 1,
    Testing = 2,
    StagingPilot = 3,
    Production = 4
}

public enum ReleaseReadinessState
{
    ReadyForControlledInternalPilot = 1,
    ReadyForRestrictedExternalPilot = 2,
    ReadyForProduction = 3,
    Blocked = 4
}

public static class DeploymentConstants
{
    public const string PhaseMarker = "P9-WP05-pilot-and-deployment";
    public const string ToolVersion = "1.0.0";
    public const string KnownDevPasswordMarker = "exits_platform_dev_only";
    public const string ProductionConfirmationToken = "DEPLOY_PRODUCTION_CONFIRMED";
    public const string PilotConfirmationToken = "DEPLOY_PILOT_CONFIRMED";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

/// <summary>Safe deployment settings — never persist real secrets in repository artifacts.</summary>
public sealed record DeploymentSettings
{
    public required ExItsEnvironmentKind Environment { get; init; }
    public required string ApplicationGitCommit { get; init; }
    public string? PlatformConnectionString { get; init; }
    public string? PosConnectionString { get; init; }
    public string? AllowedHosts { get; init; }
    public IReadOnlyList<string> CorsAllowedOrigins { get; init; } = [];
    public bool EnforceHttps { get; init; }
    public string? PlatformApiBaseUrl { get; init; }
    public string? PosApiBaseUrl { get; init; }
    public string? MauiApiBaseUrl { get; init; }
    public bool BackupVerified { get; init; }
    public string? PlatformBackupSetId { get; init; }
    public string? PosBackupSetId { get; init; }
    public bool WorkingTreeClean { get; init; }
    public string? DestructiveConfirmation { get; init; }
}

public sealed record ValidationFinding(string Code, string Message, bool IsError);

public sealed class DeploymentValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<ValidationFinding> Findings { get; init; }
}

public static class SecretRedaction
{
    private static readonly Regex[] Patterns =
    [
        new(@"Password\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Pwd\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Host\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"Username\s*=\s*[^;\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var result = text;
        foreach (var pattern in Patterns)
        {
            result = pattern.Replace(result, "[REDACTED]");
        }

        return result;
    }

    public static void EnsureNoSecrets(string text, string context)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("password=", StringComparison.Ordinal)
            || lower.Contains("pgpassword=", StringComparison.Ordinal)
            || lower.Contains("bearer ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Potential secret leakage detected in {context}.");
        }
    }
}

/// <summary>Validates deployment settings without contacting live Production.</summary>
public static class DeploymentConfigValidator
{
    public static DeploymentValidationResult Validate(DeploymentSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var findings = new List<ValidationFinding>();

        if (string.IsNullOrWhiteSpace(settings.ApplicationGitCommit)
            || settings.ApplicationGitCommit.Contains("dirty", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ValidationFinding("git.commit", "Deployment requires an explicit Git commit identity.", true));
        }

        if (!settings.WorkingTreeClean
            && settings.Environment is ExItsEnvironmentKind.StagingPilot or ExItsEnvironmentKind.Production)
        {
            findings.Add(new ValidationFinding(
                "git.dirty",
                "Staging/Pilot and Production deployments must not proceed from an unclean working tree.",
                true));
        }

        ValidateConnection(settings.PlatformConnectionString, "PlatformDatabase", findings, settings.Environment);
        ValidateConnection(settings.PosConnectionString, "PosDatabase", findings, settings.Environment);

        if (settings.Environment is ExItsEnvironmentKind.StagingPilot or ExItsEnvironmentKind.Production)
        {
            if (string.IsNullOrWhiteSpace(settings.AllowedHosts) || settings.AllowedHosts.Trim() == "*")
            {
                findings.Add(new ValidationFinding(
                    "hosts.wildcard",
                    $"{settings.Environment} requires explicit AllowedHosts (wildcard '*' is forbidden).",
                    true));
            }

            if (!settings.EnforceHttps)
            {
                findings.Add(new ValidationFinding(
                    "https.required",
                    $"{settings.Environment} requires Security:EnforceHttps=true.",
                    true));
            }

            if (!settings.BackupVerified
                || string.IsNullOrWhiteSpace(settings.PlatformBackupSetId)
                || string.IsNullOrWhiteSpace(settings.PosBackupSetId))
            {
                findings.Add(new ValidationFinding(
                    "backup.required",
                    "Verified Platform and POS backups (manifest + checksum) are required before migration/deploy.",
                    true));
            }

            if (LooksLikeCleartextHttp(settings.MauiApiBaseUrl)
                || LooksLikeCleartextHttp(settings.PlatformApiBaseUrl)
                || LooksLikeCleartextHttp(settings.PosApiBaseUrl))
            {
                findings.Add(new ValidationFinding(
                    "tls.cleartext",
                    $"{settings.Environment} must not use cleartext http:// API base URLs.",
                    true));
            }
        }

        if (settings.Environment == ExItsEnvironmentKind.Production)
        {
            if (!string.Equals(
                    settings.DestructiveConfirmation,
                    DeploymentConstants.ProductionConfirmationToken,
                    StringComparison.Ordinal))
            {
                findings.Add(new ValidationFinding(
                    "production.confirmation",
                    $"Production actions require confirmation token {DeploymentConstants.ProductionConfirmationToken}.",
                    true));
            }

            // Known open blockers — Production remains blocked unless separately resolved.
            findings.Add(new ValidationFinding(
                "blocker.R-091",
                "Production authentication (R-091) is not implemented; Production deployment remains blocked.",
                true));
            findings.Add(new ValidationFinding(
                "blocker.R-109",
                "Interactive Android validation (R-109) is incomplete; Production remains blocked.",
                true));
            findings.Add(new ValidationFinding(
                "blocker.R-129",
                "Local SQLite encryption / NU1903 risk (R-129) remains open; Production remains blocked.",
                true));
        }

        if (settings.Environment == ExItsEnvironmentKind.StagingPilot
            && !string.Equals(
                settings.DestructiveConfirmation,
                DeploymentConstants.PilotConfirmationToken,
                StringComparison.Ordinal))
        {
            findings.Add(new ValidationFinding(
                "pilot.confirmation",
                $"Staging/Pilot deploy requires confirmation token {DeploymentConstants.PilotConfirmationToken}.",
                true));
        }

        var errors = findings.Where(f => f.IsError).ToList();
        return new DeploymentValidationResult { IsValid = errors.Count == 0, Findings = findings };
    }

    private static void ValidateConnection(
        string? connectionString,
        string name,
        List<ValidationFinding> findings,
        ExItsEnvironmentKind environment)
    {
        if (environment is ExItsEnvironmentKind.Development or ExItsEnvironmentKind.Testing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            findings.Add(new ValidationFinding(
                $"db.{name}.missing",
                $"{environment} requires {name} from an environment-owned secret source.",
                true));
            return;
        }

        if (connectionString.Contains(DeploymentConstants.KnownDevPasswordMarker, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ValidationFinding(
                $"db.{name}.dev_password",
                $"{environment} must not use the documented development database password marker.",
                true));
        }

        if (connectionString.Contains("Healthcare", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("HealthCare", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new ValidationFinding(
                $"db.{name}.healthcare",
                "Deployment must not target HealthCare databases.",
                true));
        }
    }

    private static bool LooksLikeCleartextHttp(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        && !url.Contains("localhost", StringComparison.OrdinalIgnoreCase)
        && !url.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
}

public sealed record BackupGateResult(bool Allowed, string Message);

/// <summary>Blocks migration/deploy when backups are missing or unverified.</summary>
public static class BackupBeforeDeployGate
{
    public static BackupGateResult Evaluate(bool platformVerified, bool posVerified, string? platformSetId, string? posSetId)
    {
        if (!platformVerified || string.IsNullOrWhiteSpace(platformSetId))
        {
            return new BackupGateResult(false, "Platform backup verification failed or backup-set ID missing.");
        }

        if (!posVerified || string.IsNullOrWhiteSpace(posSetId))
        {
            return new BackupGateResult(false, "POS backup verification failed or backup-set ID missing.");
        }

        return new BackupGateResult(true, "Platform and POS backups verified; migration may proceed.");
    }
}

public sealed record MigrationOrderStep(int Order, string DatabaseKind, string Description);

public static class MigrationOrder
{
    public static IReadOnlyList<MigrationOrderStep> RequiredSteps { get; } =
    [
        new(1, "Platform", "Apply Platform EF migrations only to the Platform database."),
        new(2, "Platform", "Validate Platform schema and migration history."),
        new(3, "PinoyBusinessPos", "Apply POS EF migrations only to the POS database."),
        new(4, "PinoyBusinessPos", "Validate POS schema and migration history.")
    ];
}

public sealed class ReleaseReadinessAssessment
{
    public required ReleaseReadinessState State { get; init; }
    public required IReadOnlyList<string> OpenBlockers { get; init; }
    public required IReadOnlyList<string> Notes { get; init; }
}

public static class ReleaseReadinessEvaluator
{
    /// <summary>
    /// Honest readiness decision. Does not close R-091/R-109/R-129.
    /// Default with known blockers: ReadyForControlledInternalPilot (not Production).
    /// </summary>
    public static ReleaseReadinessAssessment Evaluate(
        bool automatedTestsPassed,
        bool androidReleaseBuildSucceeded,
        bool pilotConfigValid,
        bool backupsVerified,
        bool migrationRehearsalPassed,
        bool smokePassed,
        bool productionAuthImplemented = false,
        bool interactiveAndroidValidated = false,
        bool localEncryptionResolved = false,
        bool productionTlsValidated = false)
    {
        var blockers = new List<string>();
        if (!productionAuthImplemented)
        {
            blockers.Add("R-091 production authentication");
        }

        if (!interactiveAndroidValidated)
        {
            blockers.Add("R-109 interactive Android validation");
        }

        if (!localEncryptionResolved)
        {
            blockers.Add("R-129 / NU1903 local SQLite encryption risk");
        }

        if (!productionTlsValidated)
        {
            blockers.Add("Production TLS / MAUI HTTPS-only validation");
        }

        var notes = new List<string>
        {
            "Pilot data must not be represented as Production data.",
            "Development/Testing identity headers must remain unavailable outside approved environments.",
            "PITR remains deferred; logical backup/restore is the recovery path."
        };

        if (!automatedTestsPassed || !androidReleaseBuildSucceeded || !pilotConfigValid
            || !backupsVerified || !migrationRehearsalPassed || !smokePassed)
        {
            return new ReleaseReadinessAssessment
            {
                State = ReleaseReadinessState.Blocked,
                OpenBlockers = blockers,
                Notes = notes.Concat(["Mandatory pilot entry criteria unmet."]).ToList()
            };
        }

        if (blockers.Count == 0)
        {
            return new ReleaseReadinessAssessment
            {
                State = ReleaseReadinessState.ReadyForProduction,
                OpenBlockers = blockers,
                Notes = notes
            };
        }

        // External pilot still blocked while auth is missing.
        if (!productionAuthImplemented)
        {
            return new ReleaseReadinessAssessment
            {
                State = ReleaseReadinessState.ReadyForControlledInternalPilot,
                OpenBlockers = blockers,
                Notes = notes.Concat([
                    "Internal technical / staging pilot only.",
                    "Restricted external pilot and Production remain blocked while R-091 is open."
                ]).ToList()
            };
        }

        return new ReleaseReadinessAssessment
        {
            State = ReleaseReadinessState.ReadyForRestrictedExternalPilot,
            OpenBlockers = blockers,
            Notes = notes
        };
    }
}

public static class SmokeTestCatalog
{
    public static IReadOnlyList<string> PlatformContracts { get; } =
    [
        "platform.process_startup",
        "platform.health_liveness",
        "platform.health_readiness",
        "platform.organization_access_read",
        "platform.product_access_read",
        "platform.commercial_state_read",
        "platform.denied_access_behavior"
    ];

    public static IReadOnlyList<string> PosContracts { get; } =
    [
        "pos.process_startup",
        "pos.health_readiness",
        "pos.catalog_search",
        "pos.customer_search",
        "pos.cash_sale",
        "pos.manual_gcash_sale",
        "pos.product_based_utang",
        "pos.inventory_deduction",
        "pos.sale_void_stock_restore",
        "pos.expense_create_void",
        "pos.dashboard_report_read",
        "pos.organization_isolation",
        "pos.idempotent_replay"
    ];
}

public sealed record RollbackDecision(
    bool RestoreFromBackupRequired,
    bool ApplicationVersionRollbackSufficient,
    string Guidance);

public static class RollbackAdvisor
{
    public static RollbackDecision Advise(string failureKind)
    {
        return failureKind.ToLowerInvariant() switch
        {
            "config" or "readiness" or "smoke" or "proxy" =>
                new RollbackDecision(
                    false,
                    true,
                    "Stop traffic; redeploy previous application package; re-run health and smoke. Database restore not required."),
            "migration" or "schema" or "data_integrity" =>
                new RollbackDecision(
                    true,
                    false,
                    "Application rollback alone is insufficient. Authorized restore from verified pre-deploy backup required."),
            "backup_verify" =>
                new RollbackDecision(
                    false,
                    false,
                    "Do not deploy. Resolve backup verification failure first."),
            _ =>
                new RollbackDecision(
                    false,
                    true,
                    "Suspend pilot access; escalate; prefer application redeploy unless data corruption is suspected.")
        };
    }
}

public sealed record PackageVersionInfo(string Display, string CommitSha, string BuildNumber);

public static class PackageVersionGenerator
{
    public static PackageVersionInfo Create(string commitSha, string buildNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(buildNumber);
        if (commitSha.Contains("dirty", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Package version must not include a dirty working-tree marker.");
        }

        var shortSha = commitSha.Length >= 12 ? commitSha[..12] : commitSha;
        return new PackageVersionInfo($"exits-mvp-{shortSha}+{buildNumber}", commitSha, buildNumber);
    }
}

/// <summary>Validates MAUI pilot/release API base URL policy without weakening network security config.</summary>
public static class AndroidEnvironmentValidator
{
    public static DeploymentValidationResult ValidatePilotOrRelease(string? apiBaseUrl, ExItsEnvironmentKind target)
    {
        var findings = new List<ValidationFinding>();
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            findings.Add(new ValidationFinding("maui.api.missing", "MAUI API base URL is required for pilot/release packages.", true));
        }
        else if (target is ExItsEnvironmentKind.StagingPilot or ExItsEnvironmentKind.Production
                 && apiBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 && !apiBaseUrl.Contains("10.0.2.2", StringComparison.Ordinal)
                 && !apiBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                 && !apiBaseUrl.Contains("127.0.0.1", StringComparison.Ordinal))
        {
            findings.Add(new ValidationFinding(
                "maui.api.cleartext",
                "Pilot/Production MAUI packages must not target arbitrary cleartext http endpoints.",
                true));
        }

        return new DeploymentValidationResult { IsValid = findings.TrueForAll(f => !f.IsError), Findings = findings };
    }
}

public static class EnvironmentMatrix
{
    public static IReadOnlyDictionary<ExItsEnvironmentKind, string> Purposes { get; } =
        new Dictionary<ExItsEnvironmentKind, string>
        {
            [ExItsEnvironmentKind.Development] = "Local developer workstations; Development identity headers allowed.",
            [ExItsEnvironmentKind.Testing] = "CI and disposable automated tests; Testing identity headers allowed.",
            [ExItsEnvironmentKind.StagingPilot] = "Controlled internal technical / staging pilot; no Dev/Testing identity headers.",
            [ExItsEnvironmentKind.Production] = "Public production; blocked while R-091/R-109/R-129/TLS remain open."
        };
}

public static class HealthCareExclusion
{
    public static bool IsForbiddenTarget(string? connectionOrName) =>
        !string.IsNullOrWhiteSpace(connectionOrName)
        && (connectionOrName.Contains("HealthCare", StringComparison.OrdinalIgnoreCase)
            || connectionOrName.Contains("Healthcare", StringComparison.OrdinalIgnoreCase));
}
