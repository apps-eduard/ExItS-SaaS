using ExItS.Deployment;

if (args.Length == 0)
{
    PrintUsage();
    return 2;
}

try
{
    return args[0].ToLowerInvariant() switch
    {
        "validate-config" => RunValidateConfig(args),
        "backup-gate" => RunBackupGate(args),
        "readiness" => RunReadiness(args),
        "redact" => RunRedact(args),
        "smoke-catalog" => RunSmokeCatalog(),
        "migration-order" => RunMigrationOrder(),
        "rollback-advise" => RunRollbackAdvise(args),
        "package-version" => RunPackageVersion(args),
        "phase-marker" => RunPhaseMarker(),
        "closeout-board" => RunCloseoutBoard(),
        "closeout-risks" => RunCloseoutRisks(),
        _ => Fail("Unknown command.")
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine(SecretRedaction.Redact(ex.Message));
    return 1;
}

static void PrintUsage()
{
    Console.Error.WriteLine(
        """
        ExItS.Deployment.Cli (P9-WP06)
        Commands:
          validate-config --env <Development|Testing|StagingPilot|Production> [options]
          backup-gate --platform-verified <bool> --pos-verified <bool> --platform-set <id> --pos-set <id>
          readiness --tests-passed <bool> --android-release <bool> --pilot-config <bool> --backups <bool> --migration <bool> --smoke <bool>
          closeout-board
          closeout-risks
          redact <text>
          smoke-catalog
          migration-order
          rollback-advise <failureKind>
          package-version --commit <sha> [--build <n>]
          phase-marker
        """);
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 2;
}

static string? Opt(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool OptBool(string[] args, string name, bool defaultValue = false)
{
    var raw = Opt(args, name);
    return raw is null ? defaultValue : bool.Parse(raw);
}

static int RunValidateConfig(string[] args)
{
    var envRaw = Opt(args, "--env") ?? Opt(args, "--environment");
    if (string.IsNullOrWhiteSpace(envRaw) || !Enum.TryParse<ExItsEnvironmentKind>(envRaw, ignoreCase: true, out var env))
    {
        return Fail("--env <Development|Testing|StagingPilot|Production> is required.");
    }

    var settings = new DeploymentSettings
    {
        Environment = env,
        ApplicationGitCommit = Opt(args, "--commit") ?? "unknown",
        PlatformConnectionString = Opt(args, "--platform-cs") ?? Environment.GetEnvironmentVariable("EXITS_PLATFORM_DATABASE"),
        PosConnectionString = Opt(args, "--pos-cs") ?? Environment.GetEnvironmentVariable("EXITS_POS_DATABASE"),
        AllowedHosts = Opt(args, "--allowed-hosts"),
        EnforceHttps = OptBool(args, "--enforce-https"),
        MauiApiBaseUrl = Opt(args, "--maui-api"),
        PlatformApiBaseUrl = Opt(args, "--platform-api"),
        PosApiBaseUrl = Opt(args, "--pos-api"),
        BackupVerified = OptBool(args, "--backup-verified"),
        PlatformBackupSetId = Opt(args, "--platform-backup-set"),
        PosBackupSetId = Opt(args, "--pos-backup-set"),
        WorkingTreeClean = OptBool(args, "--working-tree-clean"),
        DestructiveConfirmation = Opt(args, "--confirm")
    };

    var result = DeploymentConfigValidator.Validate(settings);
    foreach (var finding in result.Findings)
    {
        var level = finding.IsError ? "ERROR" : "WARN";
        Console.WriteLine($"{level} {finding.Code}: {finding.Message}");
    }

    Console.WriteLine($"Valid={result.IsValid}");
    Console.WriteLine($"Environment={env}");
    Console.WriteLine($"Phase={DeploymentConstants.PhaseMarker}");
    return result.IsValid ? 0 : 1;
}

static int RunBackupGate(string[] args)
{
    var gate = BackupBeforeDeployGate.Evaluate(
        OptBool(args, "--platform-verified"),
        OptBool(args, "--pos-verified"),
        Opt(args, "--platform-set"),
        Opt(args, "--pos-set"));
    Console.WriteLine($"Allowed={gate.Allowed}");
    Console.WriteLine($"Message={gate.Message}");
    return gate.Allowed ? 0 : 1;
}

static int RunReadiness(string[] args)
{
    var assessment = ReleaseReadinessEvaluator.Evaluate(
        OptBool(args, "--tests-passed"),
        OptBool(args, "--android-release"),
        OptBool(args, "--pilot-config"),
        OptBool(args, "--backups"),
        OptBool(args, "--migration"),
        OptBool(args, "--smoke"),
        OptBool(args, "--auth-implemented"),
        OptBool(args, "--android-interactive"),
        OptBool(args, "--encryption-resolved"),
        OptBool(args, "--tls-validated"));

    Console.WriteLine($"State={assessment.State}");
    foreach (var blocker in assessment.OpenBlockers)
    {
        Console.WriteLine($"Blocker={blocker}");
    }

    foreach (var note in assessment.Notes)
    {
        Console.WriteLine($"Note={note}");
    }

    return assessment.State == ReleaseReadinessState.Blocked ? 1 : 0;
}

static int RunRedact(string[] args)
{
    if (args.Length < 2)
    {
        return Fail("redact <text>");
    }

    Console.WriteLine(SecretRedaction.Redact(string.Join(' ', args.Skip(1))));
    return 0;
}

static int RunSmokeCatalog()
{
    foreach (var c in SmokeTestCatalog.PlatformContracts)
    {
        Console.WriteLine($"Platform:{c}");
    }

    foreach (var c in SmokeTestCatalog.PosContracts)
    {
        Console.WriteLine($"Pos:{c}");
    }

    return 0;
}

static int RunMigrationOrder()
{
    foreach (var step in MigrationOrder.RequiredSteps)
    {
        Console.WriteLine($"{step.Order}|{step.DatabaseKind}|{step.Description}");
    }

    return 0;
}

static int RunRollbackAdvise(string[] args)
{
    if (args.Length < 2)
    {
        return Fail("rollback-advise <failureKind>");
    }

    var decision = RollbackAdvisor.Advise(args[1]);
    Console.WriteLine($"RestoreFromBackupRequired={decision.RestoreFromBackupRequired}");
    Console.WriteLine($"ApplicationVersionRollbackSufficient={decision.ApplicationVersionRollbackSufficient}");
    Console.WriteLine($"Guidance={decision.Guidance}");
    return 0;
}

static int RunPackageVersion(string[] args)
{
    var commit = Opt(args, "--commit");
    if (string.IsNullOrWhiteSpace(commit) || commit.Contains("dirty", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("--commit <sha> required (no dirty marker).");
    }

    var build = Opt(args, "--build") ?? "1";
    var version = PackageVersionGenerator.Create(commit, build);
    Console.WriteLine($"PackageVersion={version.Display}");
    Console.WriteLine($"Commit={version.CommitSha}");
    Console.WriteLine($"BuildNumber={version.BuildNumber}");
    Console.WriteLine($"Phase={DeploymentConstants.PhaseMarker}");
    return 0;
}

static int RunPhaseMarker()
{
    Console.WriteLine(DeploymentConstants.PhaseMarker);
    return 0;
}

static int RunCloseoutBoard()
{
    foreach (var decision in CommercialMvpReadinessBoard.Assess())
    {
        Console.WriteLine($"{decision.Environment}|{decision.State}|blockers={string.Join(',', decision.BlockingIds)}");
    }

    return 0;
}

static int RunCloseoutRisks()
{
    foreach (var risk in CommercialMvpRiskRegister.Current)
    {
        Console.WriteLine($"{risk.Id}|{risk.Classification}|{risk.Title}");
    }

    return 0;
}
