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
            CreatePlatformStaffUser createStaff,
            CreatePlatformUser createIdentity,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserCreated,
                nameof(PlatformUser),
                body.Username ?? body.Email,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            // Platform Admin staff create: PlatformRole is required and assigns Platform Account only.
            // Omitting PlatformRole remains identity-only (fixtures / provisional create) without an account profile.
            if (!string.IsNullOrWhiteSpace(body.PlatformRole))
            {
                if (!Enum.TryParse<PlatformSystemRole>(body.PlatformRole, ignoreCase: true, out var platformRole))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidPlatformSystemRole,
                        $"Unrecognized platform system role '{body.PlatformRole}'. Use PlatformAdministrator, BillingAdministrator, or PlatformSupport.",
                        StatusCodes.Status400BadRequest);
                }

                if (body.RequireEmailVerification == true || body.SendEmailVerification == true)
                {
                    if (string.IsNullOrWhiteSpace(body.InitialPassword))
                    {
                        return PlatformApiResults.Problem(
                            ApplicationErrorCodes.DomainViolation,
                            "InitialPassword is required when RequireEmailVerification is true.",
                            StatusCodes.Status400BadRequest);
                    }
                }

                if (string.IsNullOrWhiteSpace(body.FirstName) || string.IsNullOrWhiteSpace(body.LastName))
                {
                    return PlatformApiResults.Problem(
                        ApplicationErrorCodes.DomainViolation,
                        "FirstName and LastName are required for Platform Staff create.",
                        StatusCodes.Status400BadRequest);
                }

                var actor = authz.CurrentActor;
                var requireEmailVerification = body.RequireEmailVerification == true || body.SendEmailVerification == true;
                var staffResult = await createStaff
                    .ExecuteAsync(
                        body.FirstName,
                        body.LastName,
                        body.DisplayName,
                        body.Email,
                        platformRole,
                        actorIdentifier: actor.ActorIdentifier,
                        actorType: actor.ActorType,
                        username: body.Username,
                        phone: body.Phone,
                        employeeCode: body.EmployeeCode,
                        requireEmailVerification: requireEmailVerification,
                        initialPassword: body.InitialPassword,
                        createdByUserId: body.CreatedByUserId ?? actor.PlatformUserId?.Value,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                if (staffResult.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserCreated,
                        nameof(PlatformUser),
                        staffResult.Value!.User.Id.Value.ToString("D"),
                        summary: $"Created Platform staff {staffResult.Value.User.Username} with role {platformRole}.",
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(
                    staffResult,
                    staff => Results.Created(
                        $"/api/v1/platform/users/{staff.User.Id.Value}",
                        PlatformUserQueryService.Map(staff.User)));
            }

            if (string.IsNullOrWhiteSpace(body.Username))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidUsername,
                    "Username is required for identity-only Platform User create.",
                    StatusCodes.Status400BadRequest);
            }

            var identityResult = await createIdentity
                .ExecuteAsync(body.Username, body.DisplayName, body.Email, cancellationToken: ct)
                .ConfigureAwait(false);
            if (identityResult.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformUserCreated,
                    nameof(PlatformUser),
                    identityResult.Value!.Id.Value.ToString("D"),
                    summary: $"Created Platform User {identityResult.Value.Username}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                identityResult,
                u => Results.Created(
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
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        body.DisplayName,
                        body.Email,
                        body.FirstName,
                        body.LastName,
                        body.Phone,
                        body.EmployeeCode,
                        body.StaffNumber,
                        ct)
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
            var isGlobal = body?.Global == true;
            var action = isGlobal
                ? PlatformAuditActions.PlatformUserGlobalSuspended
                : PlatformAuditActions.PlatformUserSuspended;
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                action,
                nameof(PlatformUser),
                userId.ToString("D"),
                reason: body?.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (isGlobal && string.IsNullOrWhiteSpace(body?.Reason))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "A reason is required for Global Account Suspension.",
                    StatusCodes.Status400BadRequest);
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        body?.Reason,
                        requireReason: isGlobal,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        action,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        reason: body?.Reason,
                        summary: isGlobal
                            ? "Global Account Suspension applied."
                            : "Platform Account suspended.",
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
            ReactivateUserRequest? body,
            ReactivatePlatformUser useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var isGlobal = body?.Global == true;
            var action = isGlobal
                ? PlatformAuditActions.PlatformUserGlobalReactivated
                : PlatformAuditActions.PlatformUserReactivated;
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                action,
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
                var actor = authz.CurrentActor;
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        actingUserId: actor.PlatformUserId,
                        reason: body?.Reason,
                        actorPassword: body?.ActorPassword,
                        mfaCode: body?.MfaCode,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    if (!string.IsNullOrWhiteSpace(body?.ActorPassword))
                    {
                        await authz.AuditSucceededAsync(
                            PlatformAuditActions.PlatformUserReactivationStepUpSucceeded,
                            nameof(PlatformUser),
                            userId.ToString("D"),
                            reason: body?.Reason,
                            summary: "Deactivated-account reactivation step-up succeeded (password/MFA verified; secrets not recorded).",
                            cancellationToken: ct).ConfigureAwait(false);
                    }

                    await authz.AuditSucceededAsync(
                        action,
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

        users.MapPost("/{userId:guid}/deactivate", async (
            Guid userId,
            LifecycleReasonRequest? body,
            DeactivatePlatformUser useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body?.Reason))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "A reason is required to deactivate a Platform User.",
                    StatusCodes.Status400BadRequest);
            }

            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserDeactivated,
                nameof(PlatformUser),
                userId.ToString("D"),
                reason: body.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        body.Reason,
                        actingUserId: authz.CurrentActor.PlatformUserId,
                        actorPassword: body.ActorPassword,
                        mfaCode: body.MfaCode,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserDeactivated,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, u => Results.Ok(PlatformUserQueryService.Map(u)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        // Legacy alias — prefer /deactivate.
        users.MapPost("/{userId:guid}/disable", async (
            Guid userId,
            LifecycleReasonRequest? body,
            DeactivatePlatformUser useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body?.Reason))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "A reason is required to deactivate a Platform User.",
                    StatusCodes.Status400BadRequest);
            }

            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserDeactivated,
                nameof(PlatformUser),
                userId.ToString("D"),
                reason: body.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        body.Reason,
                        actingUserId: authz.CurrentActor.PlatformUserId,
                        actorPassword: body.ActorPassword,
                        mfaCode: body.MfaCode,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserDeactivated,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, u => Results.Ok(PlatformUserQueryService.Map(u)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        users.MapPost("/{userId:guid}/move-to-suspended", async (
            Guid userId,
            LifecycleReasonRequest? body,
            MovePlatformUserToSuspended useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body?.Reason))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "A reason is required to move a deactivated Platform User to Suspended.",
                    StatusCodes.Status400BadRequest);
            }

            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserMovedToSuspended,
                nameof(PlatformUser),
                userId.ToString("D"),
                reason: body.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        body.Reason,
                        actingUserId: authz.CurrentActor.PlatformUserId,
                        actorPassword: body.ActorPassword,
                        mfaCode: body.MfaCode,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.PlatformUserMovedToSuspended,
                        nameof(PlatformUser),
                        userId.ToString("D"),
                        reason: body.Reason,
                        summary: "Deactivated Platform User moved to Suspended (login remains blocked).",
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

internal sealed record CreateUserRequest(
    string DisplayName,
    string Email,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    string? PlatformRole = null,
    string? Phone = null,
    string? EmployeeCode = null,
    bool? RequireEmailVerification = null,
    bool? SendEmailVerification = null,
    string? InitialPassword = null,
    Guid? CreatedByUserId = null);
internal sealed record UpdateUserRequest(
    string DisplayName,
    string Email,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? EmployeeCode = null,
    string? StaffNumber = null);
internal sealed record LifecycleReasonRequest(
    string? Reason,
    bool? Global = null,
    string? ActorPassword = null,
    string? MfaCode = null);
internal sealed record ReactivateUserRequest(
    string? Reason = null,
    string? ActorPassword = null,
    string? MfaCode = null,
    bool? Global = null);
