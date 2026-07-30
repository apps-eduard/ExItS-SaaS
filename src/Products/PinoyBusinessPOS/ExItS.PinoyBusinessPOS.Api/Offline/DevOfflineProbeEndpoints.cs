using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Api.Offline;

/// <summary>
/// Development/Testing-only offline probe used to prove queue + idempotency behavior.
/// Not a production business endpoint and not a customer/credit mutation.
/// </summary>
internal static class DevOfflineProbeEndpoints
{
    public static IEndpointRouteBuilder MapDevOfflineProbeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pos/dev/offline-probe", async (
            HttpRequest request,
            DevOfflineProbeRequest body,
            IPosIdempotencyService idempotency,
            IHostEnvironment environment,
            CancellationToken ct) =>
        {
            var isDevLike = environment.IsDevelopment()
                || string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
            if (!isDevLike)
            {
                return Results.NotFound();
            }

            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (body is null
                || body.OperationId == Guid.Empty
                || string.IsNullOrWhiteSpace(body.IdempotencyKey)
                || string.IsNullOrWhiteSpace(body.PayloadHash)
                || string.IsNullOrWhiteSpace(body.EchoToken))
            {
                return Results.Problem(
                    title: "validation_failed",
                    detail: "OperationId, IdempotencyKey, PayloadHash, and EchoToken are required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var outcome = await idempotency.ExecuteAsync(
                    new PosIdempotencyRequest(
                        organizationId,
                        PosProductCodes.PinoyBusinessPos,
                        OfflineOperationTypes.DevOfflineProbe,
                        body.IdempotencyKey.Trim(),
                        body.PayloadHash.Trim(),
                        body.OperationId),
                    async _ =>
                    {
                        var reference = $"probe-{body.OperationId:N}";
                        var payload = JsonSerializer.Serialize(new
                        {
                            echoToken = body.EchoToken,
                            operationId = body.OperationId,
                            serverReference = reference
                        });
                        return new PosIdempotencyExecutionResult("succeeded", payload, reference);
                    },
                    ct)
                .ConfigureAwait(false);

            if (outcome.IsConflict)
            {
                return Results.Conflict(new DevOfflineProbeResponse(
                    outcome.IsReplay,
                    outcome.IsConflict,
                    outcome.OutcomeCode,
                    outcome.ServerReference,
                    outcome.OutcomeBodyJson));
            }

            return Results.Ok(new DevOfflineProbeResponse(
                outcome.IsReplay,
                outcome.IsConflict,
                outcome.OutcomeCode,
                outcome.ServerReference,
                outcome.OutcomeBodyJson));
        });

        return app;
    }
}

public sealed record DevOfflineProbeRequest(
    Guid OperationId,
    string IdempotencyKey,
    string PayloadHash,
    string EchoToken,
    int PayloadVersion = 1,
    string? DeviceId = null);

public sealed record DevOfflineProbeResponse(
    bool IsReplay,
    bool IsConflict,
    string OutcomeCode,
    string? ServerReference,
    string? OutcomeBodyJson);
