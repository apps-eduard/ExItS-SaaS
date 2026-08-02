using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

/// <summary>Lists active organization memberships eligible for trusted session organization context.</summary>
public sealed class ListEligibleOrganizationsForSession
{
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;

    public ListEligibleOrganizationsForSession(
        IPlatformAuthSessionRepository sessions,
        IPlatformSessionTokenService tokens,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations)
    {
        _sessions = sessions;
        _tokens = tokens;
        _memberships = memberships;
        _organizations = organizations;
    }

    public async Task<ApplicationResult<IReadOnlyList<EligibleOrganizationDto>>> ExecuteAsync(
        string? opaqueToken,
        CancellationToken cancellationToken = default)
    {
        var session = await ResolveActiveSessionAsync(opaqueToken, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return ApplicationResult<IReadOnlyList<EligibleOrganizationDto>>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        if (session.AccountClass is not AccountClass.Organization)
        {
            return ApplicationResult<IReadOnlyList<EligibleOrganizationDto>>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Organization listing requires an Organization account session.");
        }

        var eligible = await LoadEligibleAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<EligibleOrganizationDto>>.Success(eligible);
    }

    private async Task<PlatformAuthSession?> ResolveActiveSessionAsync(
        string? opaqueToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return null;
        }

        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return null;
        }

        return session;
    }

    internal async Task<IReadOnlyList<EligibleOrganizationDto>> LoadEligibleAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken)
    {
        var (items, _) = await _memberships
            .ListByUserAsync(userId, MembershipStatus.Active, skip: 0, take: 200, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<EligibleOrganizationDto>(items.Count);
        foreach (var membership in items)
        {
            var organization = await _organizations
                .GetByIdAsync(membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (organization is null || organization.Status is not OrganizationStatus.Active)
            {
                continue;
            }

            result.Add(new EligibleOrganizationDto(
                organization.Id.Value,
                organization.DisplayName,
                organization.Slug,
                membership.Role.ToString(),
                membership.Id.Value));
        }

        return result
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>Selects, switches, or clears trusted organization context on the current session.</summary>
public sealed class SetSessionOrganizationContext
{
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationContextPreferenceRepository _preferences;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ListEligibleOrganizationsForSession _eligible;

    public SetSessionOrganizationContext(
        IPlatformAuthSessionRepository sessions,
        IPlatformSessionTokenService tokens,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IOrganizationContextPreferenceRepository preferences,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        ListEligibleOrganizationsForSession eligible)
    {
        _sessions = sessions;
        _tokens = tokens;
        _memberships = memberships;
        _organizations = organizations;
        _preferences = preferences;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _eligible = eligible;
    }

    public async Task<ApplicationResult<OrganizationContextResultDto>> ExecuteAsync(
        string? opaqueToken,
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<OrganizationContextResultDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return ApplicationResult<OrganizationContextResultDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        if (session.AccountClass is not AccountClass.Organization)
        {
            return ApplicationResult<OrganizationContextResultDto>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Organization context requires an Organization account session.");
        }

        if (organizationId is null)
        {
            session.ClearSelectedOrganization();
            await _sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            await _preferences
                .UpsertLastActiveOrganizationAsync(session.UserId, null, _clock.UtcNow, cancellationToken)
                .ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await WriteAuditAsync(session, null, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationContextResultDto>.Success(
                await MapResultAsync(session, cancellationToken).ConfigureAwait(false));
        }

        PlatformOrganizationId orgId;
        try
        {
            orgId = PlatformOrganizationId.From(organizationId.Value);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationContextResultDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(session.UserId, orgId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationContextResultDto>.Failure(
                ApplicationErrorCodes.OrganizationContextNotEligible,
                "Active organization membership is required for organization context.");
        }

        var organization = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status is not OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationContextResultDto>.Failure(
                ApplicationErrorCodes.OrganizationContextNotEligible,
                "Active organization membership is required for organization context.");
        }

        session.SelectOrganization(orgId);
        await _sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        await _preferences
            .UpsertLastActiveOrganizationAsync(session.UserId, orgId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await WriteAuditAsync(session, organization.DisplayName, cancellationToken).ConfigureAwait(false);

        return ApplicationResult<OrganizationContextResultDto>.Success(
            new OrganizationContextResultDto(
                organization.Id.Value,
                organization.DisplayName,
                OrganizationSelectionStates.Selected,
                (await _eligible.LoadEligibleAsync(session.UserId, cancellationToken).ConfigureAwait(false)).Count));
    }

    private async Task WriteAuditAsync(
        PlatformAuthSession session,
        string? organizationDisplayName,
        CancellationToken cancellationToken)
    {
        var summary = session.SelectedOrganizationId is null
            ? "Platform User cleared trusted organization context."
            : $"Platform User selected trusted organization context ({organizationDisplayName}).";

        await _auditWriter.WriteAsync(
            $"platform-user:{session.UserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthOrganizationContextChanged,
            nameof(PlatformAuthSession),
            session.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: session.SelectedOrganizationId,
            summary: summary,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<OrganizationContextResultDto> MapResultAsync(
        PlatformAuthSession session,
        CancellationToken cancellationToken)
    {
        var eligible = await _eligible.LoadEligibleAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        string? displayName = null;
        if (session.SelectedOrganizationId is not null)
        {
            var org = await _organizations
                .GetByIdAsync(session.SelectedOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            displayName = org?.DisplayName;
        }

        return new OrganizationContextResultDto(
            session.SelectedOrganizationId?.Value,
            displayName,
            ResolveSelectionState(session.SelectedOrganizationId, eligible.Count),
            eligible.Count);
    }

    internal static string ResolveSelectionState(PlatformOrganizationId? selectedOrganizationId, int activeCount)
    {
        if (selectedOrganizationId is not null)
        {
            return OrganizationSelectionStates.Selected;
        }

        return activeCount switch
        {
            0 => OrganizationSelectionStates.None,
            1 => OrganizationSelectionStates.None,
            _ => OrganizationSelectionStates.SelectionRequired
        };
    }
}

/// <summary>Shared helpers for resolving and refreshing trusted organization context on sessions.</summary>
public static class OrganizationContextResolver
{
    public static async Task<(Guid? OrganizationId, string? DisplayName, string SelectionState, int ActiveCount)> ResolveAsync(
        PlatformAuthSession session,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IPlatformAuthSessionRepository sessions,
        IPlatformUnitOfWork unitOfWork,
        CancellationToken cancellationToken,
        IOrganizationContextPreferenceRepository? preferences = null,
        DateTimeOffset? utcNow = null)
    {
        var (activeMemberships, _) = await memberships
            .ListByUserAsync(session.UserId, MembershipStatus.Active, skip: 0, take: 200, cancellationToken)
            .ConfigureAwait(false);

        var eligible = new List<(PlatformOrganizationId Id, string DisplayName)>();
        foreach (var membership in activeMemberships)
        {
            var organization = await organizations
                .GetByIdAsync(membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (organization is null || organization.Status is not OrganizationStatus.Active)
            {
                continue;
            }

            eligible.Add((organization.Id, organization.DisplayName));
        }

        var changed = false;
        if (session.SelectedOrganizationId is not null)
        {
            var stillEligible = eligible.Any(x => x.Id == session.SelectedOrganizationId);
            if (!stillEligible)
            {
                session.ClearSelectedOrganization();
                changed = true;
            }
        }
        else if (eligible.Count == 1)
        {
            session.SelectOrganization(eligible[0].Id);
            changed = true;
        }
        else if (eligible.Count > 1 && preferences is not null)
        {
            var lastActive = await preferences
                .GetLastActiveOrganizationIdAsync(session.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (lastActive is not null && eligible.Any(x => x.Id == lastActive))
            {
                session.SelectOrganization(lastActive);
                changed = true;
            }
        }

        if (changed)
        {
            await sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            if (preferences is not null && session.SelectedOrganizationId is not null && utcNow is DateTimeOffset now)
            {
                await preferences
                    .UpsertLastActiveOrganizationAsync(
                        session.UserId,
                        session.SelectedOrganizationId,
                        now,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        string? displayName = null;
        if (session.SelectedOrganizationId is not null)
        {
            displayName = eligible.FirstOrDefault(x => x.Id == session.SelectedOrganizationId).DisplayName;
            if (displayName is null)
            {
                var org = await organizations
                    .GetByIdAsync(session.SelectedOrganizationId, cancellationToken)
                    .ConfigureAwait(false);
                displayName = org?.DisplayName;
            }
        }

        return (
            session.SelectedOrganizationId?.Value,
            displayName,
            SetSessionOrganizationContext.ResolveSelectionState(session.SelectedOrganizationId, eligible.Count),
            eligible.Count);
    }
}
