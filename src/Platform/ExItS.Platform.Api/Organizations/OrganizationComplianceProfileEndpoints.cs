using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class OrganizationComplianceProfileEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationComplianceProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance-profile",
            async (
                Guid organizationId,
                GetOrganizationComplianceProfile useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var viewDenied = await orgAuthz
                    .EnsureCanViewOrganizationAsync(organizationId, ct)
                    .ConfigureAwait(false);
                if (viewDenied is not null)
                {
                    var memberDenied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                        PlatformAuditActions.PlatformAccessChecked,
                        nameof(OrganizationComplianceProfile),
                        organizationId.ToString("D"),
                        organizationId,
                        summary: "Read organization compliance profile.",
                        cancellationToken: ct).ConfigureAwait(false);
                    if (memberDenied is not null)
                    {
                        return viewDenied;
                    }
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance-profile/ensure",
            async (
                Guid organizationId,
                EnsureOrganizationComplianceProfile ensure,
                GetOrganizationComplianceProfile get,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(
                        organizationId,
                        PlatformAuditActions.PlatformAccessChecked,
                        ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                await ensure
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        actor.PlatformUserId?.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);

                var result = await get
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPut(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance-profile/registered-taxpayer",
            async (
                Guid organizationId,
                RegisteredTaxpayerRequest body,
                UpdateOrganizationRegisteredTaxpayerInfo useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureOwnerOrManageOrganizationsAsync(
                    organizationId,
                    orgAuthz,
                    membershipAuthz,
                    PlatformAuditActions.OrganizationComplianceProfileUpdated,
                    ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor.PlatformUserId
                            ?? membershipAuthz.Inner.CurrentActor.PlatformUserId;
                if (actor is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        body.RegisteredTaxpayerName,
                        body.Tin,
                        actor.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/readiness",
            async (
                Guid organizationId,
                GetComplianceActivationReadiness useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureActiveMemberOrPlatformViewAsync(
                    organizationId, orgAuthz, membershipAuthz, ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/readiness/submit",
            async (
                Guid organizationId,
                SubmitComplianceReadinessForReview useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureOwnerOrManageOrganizationsAsync(
                    organizationId,
                    orgAuthz,
                    membershipAuthz,
                    PlatformAuditActions.OrganizationComplianceReadinessSubmitted,
                    ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor.PlatformUserId
                            ?? membershipAuthz.Inner.CurrentActor.PlatformUserId;
                if (actor is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        actor.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/branches/{branchId:guid}/compliance-profile",
            async (
                Guid organizationId,
                Guid branchId,
                GetBranchComplianceProfile useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureActiveMemberOrPlatformViewAsync(
                    organizationId, orgAuthz, membershipAuthz, ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        OrganizationBranchId.From(branchId),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPut(
            "/api/v1/platform/organizations/{organizationId:guid}/branches/{branchId:guid}/compliance-profile",
            async (
                Guid organizationId,
                Guid branchId,
                BranchComplianceProfileRequest body,
                UpsertBranchComplianceProfile useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureOwnerOrManageOrganizationsAsync(
                    organizationId,
                    orgAuthz,
                    membershipAuthz,
                    PlatformAuditActions.OrganizationBranchComplianceUpdated,
                    ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor.PlatformUserId
                            ?? membershipAuthz.Inner.CurrentActor.PlatformUserId;
                if (actor is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        OrganizationBranchId.From(branchId),
                        body.BirBranchCode,
                        body.SetupStatus,
                        body.Notes,
                        actor.Value.ToString("D"),
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/branch-profiles",
            async (
                Guid organizationId,
                ListBranchComplianceProfiles useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureActiveMemberOrPlatformViewAsync(
                    organizationId, orgAuthz, membershipAuthz, ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapGet(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/registration-records",
            async (
                Guid organizationId,
                ListComplianceRegistrationRecords useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureActiveMemberOrPlatformViewAsync(
                    organizationId, orgAuthz, membershipAuthz, ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var result = await useCase
                    .ExecuteAsync(PlatformOrganizationId.From(organizationId), ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/registration-records",
            async (
                Guid organizationId,
                ComplianceRegistrationCreateRequest body,
                AddComplianceRegistrationRecord useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureOwnerOrManageOrganizationsAsync(
                    organizationId,
                    orgAuthz,
                    membershipAuthz,
                    PlatformAuditActions.OrganizationComplianceRegistrationCreated,
                    ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor.PlatformUserId
                            ?? membershipAuthz.Inner.CurrentActor.PlatformUserId;
                if (actor is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        body.RegistrationType,
                        actor.Value.ToString("D"),
                        body.OrganizationBranchId,
                        body.ReferenceNumber,
                        body.Status ?? ComplianceRegistrationStatuses.Provided,
                        body.EvidenceReference,
                        body.DocumentType,
                        body.IssuedAt,
                        body.EffectiveAt,
                        body.ExpiresAt,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPut(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/registration-records/{id:guid}",
            async (
                Guid organizationId,
                Guid id,
                ComplianceRegistrationUpdateRequest body,
                UpdateComplianceRegistrationRecord useCase,
                PlatformOrganizationAuthz orgAuthz,
                PlatformMembershipAuthz membershipAuthz,
                CancellationToken ct) =>
            {
                var denied = await EnsureOwnerOrManageOrganizationsAsync(
                    organizationId,
                    orgAuthz,
                    membershipAuthz,
                    PlatformAuditActions.OrganizationComplianceRegistrationUpdated,
                    ct).ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor.PlatformUserId
                            ?? membershipAuthz.Inner.CurrentActor.PlatformUserId;
                if (actor is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        id,
                        actor.Value.ToString("D"),
                        body.RegistrationType,
                        body.OrganizationBranchId,
                        body.ClearBranch,
                        body.ReferenceNumber,
                        body.Status,
                        body.EvidenceReference,
                        body.DocumentType,
                        body.IssuedAt,
                        body.EffectiveAt,
                        body.ExpiresAt,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        app.MapPost(
            "/api/v1/platform/organizations/{organizationId:guid}/compliance/registration-records/{id:guid}/review",
            async (
                Guid organizationId,
                Guid id,
                ComplianceRegistrationReviewRequest body,
                ReviewComplianceRegistrationRecord useCase,
                PlatformOrganizationAuthz orgAuthz,
                CancellationToken ct) =>
            {
                var denied = await orgAuthz
                    .EnsureCanManageOrganizationLifecycleAsync(
                        organizationId,
                        PlatformAuditActions.OrganizationComplianceRegistrationReviewed,
                        ct)
                    .ConfigureAwait(false);
                if (denied is not null)
                {
                    return denied;
                }

                var actor = orgAuthz.Inner.CurrentActor;
                if (actor.PlatformUserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        id,
                        body.Accept,
                        actor.PlatformUserId.Value.ToString("D"),
                        body.ReviewNotes,
                        ct)
                    .ConfigureAwait(false);
                return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
            });

        return app;
    }

    private static async Task<IResult?> EnsureActiveMemberOrPlatformViewAsync(
        Guid organizationId,
        PlatformOrganizationAuthz orgAuthz,
        PlatformMembershipAuthz membershipAuthz,
        CancellationToken ct)
    {
        var viewDenied = await orgAuthz
            .EnsureCanViewOrganizationAsync(organizationId, ct)
            .ConfigureAwait(false);
        if (viewDenied is null)
        {
            return null;
        }

        var memberDenied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
            PlatformAuditActions.PlatformAccessChecked,
            nameof(OrganizationComplianceProfile),
            organizationId.ToString("D"),
            organizationId,
            summary: "Read organization compliance readiness data.",
            cancellationToken: ct).ConfigureAwait(false);
        return memberDenied is not null ? viewDenied : null;
    }

    private static async Task<IResult?> EnsureOwnerOrManageOrganizationsAsync(
        Guid organizationId,
        PlatformOrganizationAuthz orgAuthz,
        PlatformMembershipAuthz membershipAuthz,
        string auditAction,
        CancellationToken ct)
    {
        if (await orgAuthz.HasPlatformManageOrganizationsAsync(organizationId, ct).ConfigureAwait(false))
        {
            return null;
        }

        var memberDenied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
            auditAction,
            nameof(OrganizationComplianceProfile),
            organizationId.ToString("D"),
            organizationId,
            summary: "Mutate organization compliance readiness data.",
            cancellationToken: ct).ConfigureAwait(false);
        if (memberDenied is not null)
        {
            return memberDenied;
        }

        var authority = await membershipAuthz
            .ResolveActorMembershipAuthorityAsync(organizationId, ct)
            .ConfigureAwait(false);
        if (authority.ActorMembershipRole != OrganizationRole.OrganizationOwner)
        {
            return Results.Forbid();
        }

        return null;
    }

    private sealed record RegisteredTaxpayerRequest(string? RegisteredTaxpayerName, string? Tin);

    private sealed record BranchComplianceProfileRequest(
        string? BirBranchCode,
        string? SetupStatus,
        string? Notes);

    private sealed record ComplianceRegistrationCreateRequest(
        string RegistrationType,
        Guid? OrganizationBranchId,
        string? ReferenceNumber,
        string? Status,
        string? EvidenceReference,
        string? DocumentType,
        DateOnly? IssuedAt,
        DateOnly? EffectiveAt,
        DateOnly? ExpiresAt);

    private sealed record ComplianceRegistrationUpdateRequest(
        string? RegistrationType,
        Guid? OrganizationBranchId,
        bool ClearBranch,
        string? ReferenceNumber,
        string? Status,
        string? EvidenceReference,
        string? DocumentType,
        DateOnly? IssuedAt,
        DateOnly? EffectiveAt,
        DateOnly? ExpiresAt);

    private sealed record ComplianceRegistrationReviewRequest(bool Accept, string? ReviewNotes);
}
