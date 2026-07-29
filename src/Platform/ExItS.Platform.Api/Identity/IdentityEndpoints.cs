using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Identity;

/// <summary>
/// Platform User lifecycle endpoints. Development-stage only: unauthenticated.
/// Authentication and authorization enforcement remain deferred.
/// </summary>
internal static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/platform/users");

        users.MapGet("/", async (
            string? status,
            string? search,
            int? page,
            int? pageSize,
            PlatformUserQueryService queries,
            CancellationToken ct) =>
        {
            AccountStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidAccountStatusTransition,
                        $"Unrecognized account status '{status}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsed = value;
            }

            var result = await queries.ListAsync(parsed, search, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        users.MapPost("/", async (
            CreateUserRequest body,
            CreatePlatformUser useCase,
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(body.Username, body.DisplayName, body.Email, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, u => Results.Created(
                $"/api/v1/platform/users/{u.Id.Value}",
                PlatformUserQueryService.Map(u)));
        });

        users.MapGet("/{userId:guid}", async (
            Guid userId,
            PlatformUserQueryService queries,
            CancellationToken ct) =>
        {
            var user = await queries.GetByIdAsync(userId, ct).ConfigureAwait(false);
            return user is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.UserNotFound,
                    "Platform User was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(user);
        });

        users.MapPut("/{userId:guid}", async (
            Guid userId,
            UpdateUserRequest body,
            UpdatePlatformUserProfile useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase
                    .ExecuteAsync(PlatformUserId.From(userId), body.DisplayName, body.Email, ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, u => Results.Ok(PlatformUserQueryService.Map(u)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        users.MapPost("/{userId:guid}/suspend", async (
            Guid userId,
            LifecycleReasonRequest? body,
            SuspendPlatformUser useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase
                    .ExecuteAsync(PlatformUserId.From(userId), body?.Reason, ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, u => Results.Ok(PlatformUserQueryService.Map(u)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        users.MapPost("/{userId:guid}/reactivate", async (
            Guid userId,
            ReactivatePlatformUser useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, u => Results.Ok(PlatformUserQueryService.Map(u)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        users.MapPost("/{userId:guid}/disable", async (
            Guid userId,
            DeactivatePlatformUser useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, u => Results.Ok(PlatformUserQueryService.Map(u)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app;
    }
}

internal sealed record CreateUserRequest(string Username, string DisplayName, string Email);
internal sealed record UpdateUserRequest(string DisplayName, string Email);
internal sealed record LifecycleReasonRequest(string? Reason);
