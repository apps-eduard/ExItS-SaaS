using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.Api.Registers;

/// <summary>Organization-scoped POS register endpoints (P10-WP07). Online-only management.</summary>
internal static class RegisterEndpoints
{
    public const string CreateOperation = "pos.register.create";
    public const string ActivateOperation = "pos.register.activate";
    public const string DeactivateOperation = "pos.register.deactivate";

    public static IEndpointRouteBuilder MapRegisterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/registers");

        group.MapGet("/available-for-shift", async (
            HttpRequest request,
            RegisterQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            // Open Shift needs the eligible-register list; allow ViewRegisters or ManageShifts so
            // cashiers/owners are not stranded when commercial ViewRegisters was omitted from a
            // partial plan snapshot but shift manage is granted (or merged in Local Validation).
            if (!TryAuthorizeAny(
                    request,
                    access,
                    [UtangCapability.ViewRegisters, UtangCapability.ManageShifts],
                    out var organizationId,
                    out var problem))
            {
                return problem!;
            }

            var items = await queries.ListAvailableForShiftAsync(organizationId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapGet("/", async (
            HttpRequest request,
            string? registerCode,
            string? name,
            string? status,
            bool? hasOpenShift,
            int? page,
            int? pageSize,
            RegisterQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!TryParseStatus(status, out var parsedStatus, out problem))
            {
                return problem!;
            }

            var result = await queries
                .ListAsync(
                    organizationId,
                    new RegisterFilter(registerCode, name, parsedStatus, hasOpenShift),
                    page,
                    pageSize,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            HttpRequest request,
            CreateRegisterRequest body,
            CreateRegister useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    CreateOperation,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, actorId, body, ct2),
                    dto => dto,
                    dto => Results.Created($"/api/v1/pos/registers/{dto.RegisterId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{registerId:guid}", async (
            HttpRequest request,
            Guid registerId,
            RegisterQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            var register = await queries.GetByIdAsync(organizationId, registerId, ct).ConfigureAwait(false);
            return register is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.RegisterNotFound,
                    "Register was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(register);
        });

        group.MapGet("/{registerId:guid}/activity", async (
            HttpRequest request,
            Guid registerId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            RegisterQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            var activity = await queries
                .GetActivityAsync(organizationId, registerId, fromUtc, toUtc, ct)
                .ConfigureAwait(false);
            return activity is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.RegisterNotFound,
                    "Register was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(activity);
        });

        group.MapPut("/{registerId:guid}", async (
            HttpRequest request,
            Guid registerId,
            UpdateRegisterRequest body,
            UpdateRegister useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, actorId, registerId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/{registerId:guid}/activate", async (
            HttpRequest request,
            Guid registerId,
            ActivateRegister useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    ActivateOperation,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, actorId, registerId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/{registerId:guid}/deactivate", async (
            HttpRequest request,
            Guid registerId,
            DeactivateRegister useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageRegisters, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    DeactivateOperation,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, actorId, registerId, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        return app;
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryAuthorizeAny(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        IReadOnlyList<UtangCapability> capabilities,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        problem = null;
        foreach (var capability in capabilities)
        {
            if (PosCommercialScope.TryAuthorize(access, capability, out problem))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseStatus(string? status, out RegisterStatus? parsed, out IResult? problem)
    {
        parsed = null;
        problem = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (Enum.TryParse<RegisterStatus>(status.Trim(), ignoreCase: true, out var value)
            && (value is RegisterStatus.Active or RegisterStatus.Inactive))
        {
            parsed = value;
            return true;
        }

        problem = PosApiResults.Problem(
            DomainErrorCodes.InvalidRegisterStatus,
            "Register status must be Active or Inactive.",
            StatusCodes.Status400BadRequest);
        return false;
    }
}
