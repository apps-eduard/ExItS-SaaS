using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Identity;

/// <summary>
/// Platform User lifecycle endpoints. Development-stage only: actor identity is unauthenticated
/// (<see cref="ExItS.Platform.Infrastructure.Authorization.DevelopmentPlatformActorAccessor"/>), but
/// mutations enforce <see cref="PlatformPermission.ManagePlatformUsers"/> and record audit trail entries.
/// </summary>
internal static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/platform/users");

        users.MapGet("/", async (
            string? status,
            string? search,
            string? directory,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
            PlatformUserQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformUser),
                "list",
                summary: "List Platform Users.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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

            UserDirectoryFilter? directoryFilter = null;
            if (!string.IsNullOrWhiteSpace(directory))
            {
                if (!Enum.TryParse<UserDirectoryFilter>(directory, ignoreCase: true, out var parsedFilter))
                {
                    return PlatformApiResults.Problem(
                        "platform.user.directory.invalid",
                        $"Unrecognized directory filter '{directory}'. Use All, Unassigned, Organization, PlatformStaff, or Personal.",
                        StatusCodes.Status400BadRequest);
                }

                directoryFilter = parsedFilter == UserDirectoryFilter.All ? null : parsedFilter;
            }

            var result = await queries
                .ListAsync(parsed, search, page, pageSize, directoryFilter, sortBy, sortDesc ?? false, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        users.MapPost("/", async (
            CreateUserRequest body,
            CreatePlatformUser useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserCreated,
                nameof(PlatformUser),
                body.Username,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(body.Username, body.DisplayName, body.Email, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformUserCreated,
                    nameof(PlatformUser),
                    result.Value!.Id.Value.ToString("D"),
                    summary: $"Created Platform User {result.Value.Username}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, u => Results.Created(
                $"/api/v1/platform/users/{u.Id.Value}",
                PlatformUserQueryService.Map(u)));
        });

        users.MapGet("/{userId:guid}", async (
            Guid userId,
            PlatformUserQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformUser),
                userId.ToString("D"),
                summary: "Get Platform User.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserProfileUpdated,
                nameof(PlatformUser),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(PlatformUserId.From(userId), body.DisplayName, body.Email, ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserProfileUpdated,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        cancellationToken: ct).ConfigureAwait(false);
                }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserSuspended,
                nameof(PlatformUser),
                userId.ToString("D"),
                reason: body?.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(PlatformUserId.From(userId), body?.Reason, ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserSuspended,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        reason: body?.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserReactivated,
                nameof(PlatformUser),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserReactivated,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        cancellationToken: ct).ConfigureAwait(false);
                }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserDeactivated,
                nameof(PlatformUser),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserDeactivated,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        cancellationToken: ct).ConfigureAwait(false);
                }

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
