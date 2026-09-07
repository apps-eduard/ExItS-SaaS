using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ExItS.LocalValidation.Supervisor;

internal static class Program
{
    private static readonly object OrchestrationLock = new();
    private static string? _activeOperation;
    private static string? _progressMessage;

    public static async Task<int> Main(string[] args)
    {
        if (IsProductionEnvironment())
        {
            Console.Error.WriteLine("Refusing to start Local Validation supervisor: environment is Production.");
            return 2;
        }

        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls(ResolveBindUrl(builder.Configuration));

        var repoRoot = ResolveRepoRoot(builder.Configuration);
        var controlScript = Path.Combine(repoRoot, "tools", "Invoke-LocalValidationControl.ps1");
        if (!File.Exists(controlScript))
        {
            Console.Error.WriteLine($"Missing control script: {controlScript}");
            return 3;
        }

        if (!IsSupervisorEnabled(builder.Configuration))
        {
            Console.Error.WriteLine("LocalValidation__Supervisor__Enabled is not true. Refusing to start.");
            return 4;
        }

        var app = builder.Build();

        app.MapGet("/health", () => Results.Json(new
        {
            status = "ok",
            service = "local-validation-supervisor",
            bind = "127.0.0.1",
            localValidation = true,
        }));

        app.MapGet("/health/services", async (CancellationToken ct) =>
        {
            if (!TryEnterIdleRead(out var busy))
            {
                return Results.Json(new
                {
                    checkedAtUtc = DateTimeOffset.UtcNow,
                    busy = true,
                    operation = _activeOperation,
                    progress = _progressMessage,
                    services = Array.Empty<object>(),
                });
            }

            try
            {
                var payload = await InvokeControlAsync(controlScript, ["-Action", "Status"], ct)
                    .ConfigureAwait(false);
                using var doc = JsonDocument.Parse(payload.StdOut);
                var root = doc.RootElement.Clone();
                return Results.Json(new
                {
                    checkedAtUtc = root.TryGetProperty("checkedAtUtc", out var at) ? at.GetString() : DateTimeOffset.UtcNow.ToString("o"),
                    busy = false,
                    operation = (string?)null,
                    progress = (string?)null,
                    services = root.TryGetProperty("services", out var services) ? services : root,
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        });

        app.MapPost("/services/{serviceKey}/restart", async (string serviceKey, CancellationToken ct) =>
        {
            if (!SupervisorGuards.IsRestartableServiceKey(serviceKey))
            {
                return Results.Json(new
                {
                    ok = false,
                    error = $"Service '{serviceKey}' is not restartable or unknown.",
                }, statusCode: 400);
            }

            if (!TryBeginOperation($"restart:{serviceKey}", out var conflict))
            {
                return conflict!;
            }

            try
            {
                SetProgress($"Restarting {serviceKey}...");
                var payload = await InvokeControlAsync(
                        controlScript,
                        ["-Action", "Restart", "-ServiceKey", serviceKey],
                        ct)
                    .ConfigureAwait(false);
                if (payload.ExitCode != 0)
                {
                    return Results.Json(new
                    {
                        ok = false,
                        error = string.IsNullOrWhiteSpace(payload.StdErr) ? payload.StdOut : payload.StdErr,
                    }, statusCode: 500);
                }

                return Results.Json(JsonSerializer.Deserialize<JsonElement>(payload.StdOut));
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
            }
            finally
            {
                EndOperation();
            }
        });

        app.MapPost("/services/restart-all", async (CancellationToken ct) =>
        {
            if (!TryBeginOperation("restart-all", out var conflict))
            {
                return conflict!;
            }

            try
            {
                SetProgress("Restarting applications...");
                var payload = await InvokeControlAsync(
                        controlScript,
                        ["-Action", "RestartAll"],
                        ct)
                    .ConfigureAwait(false);
                if (payload.ExitCode != 0)
                {
                    return Results.Json(new
                    {
                        ok = false,
                        error = string.IsNullOrWhiteSpace(payload.StdErr) ? payload.StdOut : payload.StdErr,
                    }, statusCode: 500);
                }

                return Results.Json(JsonSerializer.Deserialize<JsonElement>(payload.StdOut));
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
            }
            finally
            {
                EndOperation();
            }
        });

        app.MapPost("/reset", async (CancellationToken ct) =>
        {
            if (!TryBeginOperation("reset", out var conflict))
            {
                return conflict!;
            }

            try
            {
                SetProgress("Resetting Local Validation test data...");
                var payload = await InvokeControlAsync(
                        controlScript,
                        ["-Action", "Reset", "-ConfirmReset"],
                        ct)
                    .ConfigureAwait(false);
                if (payload.ExitCode != 0)
                {
                    return Results.Json(new
                    {
                        ok = false,
                        error = string.IsNullOrWhiteSpace(payload.StdErr) ? payload.StdOut : payload.StdErr,
                    }, statusCode: 500);
                }

                return Results.Json(JsonSerializer.Deserialize<JsonElement>(payload.StdOut));
            }
            catch (Exception ex)
            {
                return Results.Json(new { ok = false, error = ex.Message }, statusCode: 500);
            }
            finally
            {
                EndOperation();
            }
        });

        // Explicitly reject arbitrary command surfaces.
        app.MapPost("/execute", () => Results.StatusCode(StatusCodes.Status404NotFound));
        app.MapPost("/powershell", () => Results.StatusCode(StatusCodes.Status404NotFound));
        app.MapPost("/command", () => Results.StatusCode(StatusCodes.Status404NotFound));

        app.MapGet("/operation", () => Results.Json(new
        {
            busy = _activeOperation is not null,
            operation = _activeOperation,
            progress = _progressMessage,
        }));

        Console.WriteLine($"[local-validation-supervisor] Listening on {ResolveBindUrl(builder.Configuration)} (repo={repoRoot})");
        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }

    private static bool TryEnterIdleRead(out bool busy)
    {
        lock (OrchestrationLock)
        {
            busy = _activeOperation is not null;
            return !busy;
        }
    }

    private static bool TryBeginOperation(string operation, out IResult? conflict)
    {
        lock (OrchestrationLock)
        {
            if (_activeOperation is not null)
            {
                conflict = Results.Json(new
                {
                    ok = false,
                    error = "busy",
                    operation = _activeOperation,
                    progress = _progressMessage,
                    message = $"Local Validation control is busy ({_activeOperation}).",
                }, statusCode: StatusCodes.Status409Conflict);
                return false;
            }

            _activeOperation = operation;
            _progressMessage = null;
            conflict = null;
            return true;
        }
    }

    private static void SetProgress(string message)
    {
        lock (OrchestrationLock)
        {
            _progressMessage = message;
        }
    }

    private static void EndOperation()
    {
        lock (OrchestrationLock)
        {
            _activeOperation = null;
            _progressMessage = null;
        }
    }

    private static string ResolveBindUrl(IConfiguration configuration)
    {
        var port = configuration.GetValue("LocalValidation:Supervisor:Port", 8099);
        // Hard bind loopback only — never 0.0.0.0.
        return $"http://127.0.0.1:{port}";
    }

    private static bool IsSupervisorEnabled(IConfiguration configuration)
    {
        return configuration.GetValue("LocalValidation:Supervisor:Enabled", false)
            || string.Equals(
                Environment.GetEnvironmentVariable("LocalValidation__Supervisor__Enabled"),
                "true",
                StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRepoRoot(IConfiguration configuration)
    {
        var configured = configuration["LocalValidation:Supervisor:RepoRoot"]
            ?? Environment.GetEnvironmentVariable("LocalValidation__Supervisor__RepoRoot");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(Path.Combine(configured, "ExItS.slnx")))
        {
            return Path.GetFullPath(configured);
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not resolve ExItS repo root for Local Validation supervisor.");
    }

    private static bool IsProductionEnvironment()
    {
        foreach (var key in new[] { "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT" })
        {
            if (SupervisorGuards.IsProductionEnvironmentName(Environment.GetEnvironmentVariable(key)))
            {
                return true;
            }
        }

        return false;
    }

    private static string ExtractControlJson(string stdOut)
    {
        const string marker = "___LV_CONTROL_JSON___";
        var lines = stdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (line.StartsWith(marker, StringComparison.Ordinal))
            {
                return line[marker.Length..];
            }

            if (line.StartsWith('{') && line.EndsWith('}'))
            {
                return line;
            }
        }

        throw new InvalidOperationException("Local Validation control did not return JSON.");
    }

    private static async Task<ControlResult> InvokeControlAsync(
        string scriptPath,
        IReadOnlyList<string> actionArgs,
        CancellationToken cancellationToken)
    {
        var args = new StringBuilder();
        args.Append("-NoProfile -ExecutionPolicy Bypass -File ");
        args.Append('"').Append(scriptPath).Append('"');
        foreach (var arg in actionArgs)
        {
            args.Append(' ').Append(QuoteArg(arg));
        }

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = args.ToString(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
        };

        using var process = new Process { StartInfo = psi };
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdOut.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdErr.AppendLine(e.Data);
            if (e.Data.StartsWith("[local-validation-control]", StringComparison.Ordinal))
            {
                SetProgress(e.Data["[local-validation-control]".Length..].Trim());
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start Local Validation control PowerShell.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var raw = stdOut.ToString().Trim();
        var json = ExtractControlJson(raw);
        return new ControlResult(process.ExitCode, json, stdErr.ToString().Trim());
    }

    private static string QuoteArg(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        if (value.IndexOfAny([' ', '\t', '"']) < 0)
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private sealed record ControlResult(int ExitCode, string StdOut, string StdErr);
}
