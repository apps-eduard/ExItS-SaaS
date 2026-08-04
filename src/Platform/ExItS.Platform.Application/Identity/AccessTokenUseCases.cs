using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed class IssuePlatformAccessToken
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformAccessTokenRepository _tokens;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly EvaluateProductAuthorization _authorize;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformLockoutOptions _lockout;
    private readonly PlatformAccessTokenOptions _tokenOptions;
    private readonly ListEligibleOrganizationsForSession _eligible;
    private readonly IPlatformMfaReadinessService _mfa;

    public IssuePlatformAccessToken(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformAccessTokenRepository tokens,
        IPlatformAuthSessionRepository sessions,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        EvaluateProductAuthorization authorize,
        IPlatformPasswordHasher hasher,
        IPlatformSessionTokenService tokenService,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformLockoutOptions> lockout,
        IOptions<PlatformAccessTokenOptions> tokenOptions,
        ListEligibleOrganizationsForSession eligible,
        IPlatformMfaReadinessService mfa)
    {
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _sessions = sessions;
        _memberships = memberships;
        _organizations = organizations;
        _authorize = authorize;
        _hasher = hasher;
        _tokenService = tokenService;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lockout = lockout.Value;
        _tokenOptions = tokenOptions.Value;
        _eligible = eligible;
        _mfa = mfa;
    }

    public Task<ApplicationResult<PlatformAccessTokenIssueDto>> ExecutePasswordGrantAsync(
        string? usernameOrEmail,
        string? password,
        Guid? organizationId,
        string? productCode,
        CancellationToken cancellationToken = default) =>
        IssueFromCredentialsAsync(usernameOrEmail, password, organizationId, productCode, cancellationToken);

    public async Task<ApplicationResult<PlatformAccessTokenIssueDto>> ExecuteSessionGrantAsync(
        string? opaqueSessionToken,
        Guid? organizationId,
        string? productCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueSessionToken))
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var session = await _sessions
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueSessionToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var utcNow = _clock.UtcNow;
        if (session.ExpiresAtUtc <= utcNow || session.AbsoluteExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.SessionExpired,
                "Session has expired.");
        }

        var user = await _users.GetByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null
            || !string.Equals(credential.SecurityStamp, session.SecurityStampAtIssue, StringComparison.Ordinal))
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var orgId = organizationId ?? session.SelectedOrganizationId?.Value;
        return await IssueForUserAsync(user, credential.SecurityStamp, orgId, productCode, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PlatformAccessTokenIssueDto>> IssueFromCredentialsAsync(
        string? usernameOrEmail,
        string? password,
        Guid? organizationId,
        string? productCode,
        CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var identifier = (usernameOrEmail ?? string.Empty).Trim();
        if (identifier.Length == 0 || string.IsNullOrEmpty(password))
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.LoginFailed,
                "Invalid username/email or password.");
        }

        PlatformUser? user = null;
        try
        {
            if (identifier.Contains('@', StringComparison.Ordinal))
            {
                user = await _users
                    .GetByNormalizedEmailAsync(PlatformUser.NormalizeEmail(identifier), cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var (_, normalizedUsername) = PlatformUser.NormalizeUsername(identifier);
                user = await _users
                    .GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (DomainException)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.LoginFailed,
                "Invalid username/email or password.");
        }

        if (user is null)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.LoginFailed,
                "Invalid username/email or password.");
        }

        if (user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible for login.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null || !credential.SupportsPasswordLogin)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.LoginFailed,
                "Invalid username/email or password.");
        }

        if (credential.IsLockedOut(utcNow))
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.CredentialLockedOut,
                "Credential is locked out.");
        }

        var verification = _hasher.VerifyHashedPassword(credential.PasswordHash, password);
        if (verification == PlatformPasswordVerificationResult.Failed)
        {
            credential.RegisterFailedAccess(
                _lockout.MaxFailedAccessAttempts,
                TimeSpan.FromMinutes(Math.Max(1, _lockout.LockoutMinutes)),
                utcNow);
            await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.LoginFailed,
                "Invalid username/email or password.");
        }

        if (verification == PlatformPasswordVerificationResult.SuccessRehashNeeded)
        {
            credential.ReplacePasswordHash(_hasher.HashPassword(password), _hasher.Algorithm, utcNow);
        }

        credential.RegisterSuccessfulAccess(utcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);

        return await IssueForUserAsync(user, credential.SecurityStamp, organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PlatformAccessTokenIssueDto>> IssueForUserAsync(
        PlatformUser user,
        string securityStamp,
        Guid? organizationId,
        string? productCode,
        CancellationToken cancellationToken)
    {
        var eligible = await _eligible.LoadEligibleAsync(user.Id, cancellationToken).ConfigureAwait(false);
        PlatformOrganizationId? orgId = null;
        string? orgName = null;
        bool? productAllowed = null;
        string? productReason = null;
        string? productLocalRole = null;
        string? mappedPosRole = null;
        string? normalizedProduct = string.IsNullOrWhiteSpace(productCode) ? null : productCode.Trim();

        if (organizationId is Guid requestedOrg)
        {
            try
            {
                orgId = PlatformOrganizationId.From(requestedOrg);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(ex.ErrorCode, ex.Message);
            }

            var membership = await _memberships
                .FindActiveByUserAndOrganizationAsync(user.Id, orgId, cancellationToken)
                .ConfigureAwait(false);
            if (membership is null)
            {
                return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                    ApplicationErrorCodes.OrganizationContextNotEligible,
                    "Active organization membership is required for organization context.");
            }

            var organization = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (organization is null || organization.Status is not OrganizationStatus.Active)
            {
                return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                    ApplicationErrorCodes.OrganizationContextNotEligible,
                    "Active organization membership is required for organization context.");
            }

            orgName = organization.DisplayName;

            if (normalizedProduct is not null)
            {
                var access = await _authorize
                    .ExecuteAsync(user.Id, orgId, normalizedProduct, cancellationToken)
                    .ConfigureAwait(false);
                productAllowed = access.CanOperate;
                productReason = access.ReasonCode;
                productLocalRole = access.ProductLocalRoleCode;
                mappedPosRole = access.MappedPosRoleCode;
                if (!access.CanOperate)
                {
                    return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                        ApplicationErrorCodes.ProductEntryDenied,
                        access.ReasonCode == EffectiveAccessReasonCodes.ProductLocalRoleMissing
                            ? "Product-local role is required to operate this product."
                            : "Product access is not allowed for this organization.");
                }
            }
        }
        else if (eligible.Count == 1 && normalizedProduct is null)
        {
            orgId = PlatformOrganizationId.From(eligible[0].OrganizationId);
            orgName = eligible[0].DisplayName;
        }
        else if (eligible.Count == 1 && normalizedProduct is not null)
        {
            orgId = PlatformOrganizationId.From(eligible[0].OrganizationId);
            orgName = eligible[0].DisplayName;
            var access = await _authorize
                .ExecuteAsync(user.Id, orgId, normalizedProduct, cancellationToken)
                .ConfigureAwait(false);
            productAllowed = access.CanOperate;
            productReason = access.ReasonCode;
            productLocalRole = access.ProductLocalRoleCode;
            mappedPosRole = access.MappedPosRoleCode;
            if (!access.CanOperate)
            {
                return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                    ApplicationErrorCodes.ProductEntryDenied,
                    access.ReasonCode == EffectiveAccessReasonCodes.ProductLocalRoleMissing
                        ? "Product-local role is required to operate this product."
                        : "Product access is not allowed for this organization.");
            }
        }
        else if (normalizedProduct is not null)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.OrganizationContextNotEligible,
                "Organization context is required for product entry.");
        }

        var opaque = _tokenService.CreateOpaqueToken();
        var hash = _tokenService.HashToken(opaque);
        var lifetime = TimeSpan.FromHours(_tokenOptions.ResolveLifetimeHours());
        var token = PlatformAccessToken.Create(
            user.Id,
            hash,
            securityStamp,
            _clock.UtcNow,
            lifetime,
            orgId,
            normalizedProduct);

        await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthAccessTokenIssued,
            nameof(PlatformAccessToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: orgId,
            summary: "Platform API access token issued (raw token not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var selectionState = SetSessionOrganizationContext.ResolveSelectionState(orgId, eligible.Count);
        var mfa = await _mfa.GetForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
            opaque,
            "Bearer",
            token.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            token.ExpiresAtUtc,
            orgId?.Value,
            orgName,
            token.ProductCode,
            selectionState,
            eligible.Count,
            productAllowed,
            productReason,
            mfa,
            productLocalRole,
            mappedPosRole));
    }
}

public sealed class BindPlatformAccessTokenProductContext
{
    private readonly IPlatformAccessTokenRepository _tokens;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductLocalRoleGrantRepository _roleGrants;
    private readonly EvaluateProductAuthorization _authorize;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ListEligibleOrganizationsForSession _eligible;
    private readonly IPlatformMfaReadinessService _mfa;

    public BindPlatformAccessTokenProductContext(
        IPlatformAccessTokenRepository tokens,
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IProductLocalRoleGrantRepository roleGrants,
        EvaluateProductAuthorization authorize,
        IPlatformSessionTokenService tokenService,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        ListEligibleOrganizationsForSession eligible,
        IPlatformMfaReadinessService mfa)
    {
        _tokens = tokens;
        _users = users;
        _credentials = credentials;
        _memberships = memberships;
        _organizations = organizations;
        _roleGrants = roleGrants;
        _authorize = authorize;
        _tokenService = tokenService;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _eligible = eligible;
        _mfa = mfa;
    }

    public async Task<ApplicationResult<PlatformAccessTokenIssueDto>> ExecuteAsync(
        string? opaqueAccessToken,
        Guid organizationId,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveActiveTokenAsync(opaqueAccessToken, cancellationToken).ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(resolved.ErrorCode!, resolved.ErrorMessage!);
        }

        var (token, user) = resolved.Value;
        PlatformOrganizationId orgId;
        try
        {
            orgId = PlatformOrganizationId.From(organizationId);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(user.Id, orgId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.OrganizationContextNotEligible,
                "Active organization membership is required for organization context.");
        }

        var organization = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status is not OrganizationStatus.Active)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.OrganizationContextNotEligible,
                "Active organization membership is required for organization context.");
        }

        var access = await _authorize
            .ExecuteAsync(user.Id, orgId, productCode, cancellationToken)
            .ConfigureAwait(false);

        // Organization Owner + active POS entitlement without a product-local role is a provisioning gap
        // (Start a Business normally grants first POS Owner). Bootstrap once on bind so Mobile matches Web.
        if (!access.CanOperate
            && access.EntitlementAllowed
            && access.ReasonCode == EffectiveAccessReasonCodes.ProductLocalRoleMissing
            && membership.Role is OrganizationRole.OrganizationOwner)
        {
            var existingGrant = await _roleGrants
                .FindActiveByUserOrganizationProductAsync(orgId, user.Id, access.ProductCode, cancellationToken)
                .ConfigureAwait(false);
            if (existingGrant is null)
            {
                var grant = ProductLocalRoleGrant.Create(
                    orgId,
                    user.Id,
                    access.ProductCode,
                    ProductLocalRoleGrant.PosOwnerRoleCode,
                    user.Id,
                    _clock.UtcNow,
                    source: "OrganizationOwnerBindBootstrap");
                await _roleGrants.AddAsync(grant, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await _auditWriter.WriteAsync(
                    $"platform-user:{user.Id.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.ProductLocalRoleGranted,
                    nameof(ProductLocalRoleGrant),
                    grant.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    organizationId: orgId,
                    productCode: ProductCode.Create(access.ProductCode),
                    summary: "POS Owner product-local role granted on bind for Organization Owner.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            access = await _authorize
                .ExecuteAsync(user.Id, orgId, productCode, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!access.CanOperate)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(
                ApplicationErrorCodes.ProductEntryDenied,
                access.ReasonCode == EffectiveAccessReasonCodes.ProductLocalRoleMissing
                    ? "Product-local role is required to operate this product."
                    : "Product access is not allowed for this organization.");
        }

        try
        {
            token.BindProductContext(orgId, productCode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformAccessTokenIssueDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthAccessTokenBound,
            nameof(PlatformAccessToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: orgId,
            summary: "Platform API access token bound to organization/product context.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var eligible = await _eligible.LoadEligibleAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var mfa = await _mfa.GetForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
            opaqueAccessToken!,
            "Bearer",
            token.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            token.ExpiresAtUtc,
            orgId.Value,
            organization.DisplayName,
            token.ProductCode,
            OrganizationSelectionStates.Selected,
            eligible.Count,
            true,
            access.ReasonCode,
            mfa,
            access.ProductLocalRoleCode,
            access.MappedPosRoleCode));
    }

    private async Task<ApplicationResult<(PlatformAccessToken Token, PlatformUser User)>> ResolveActiveTokenAsync(
        string? opaqueAccessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opaqueAccessToken))
        {
            return ApplicationResult<(PlatformAccessToken, PlatformUser)>.Failure(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Access token is invalid.");
        }

        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueAccessToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null || !token.IsActive(_clock.UtcNow))
        {
            return ApplicationResult<(PlatformAccessToken, PlatformUser)>.Failure(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Access token is invalid.");
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<(PlatformAccessToken, PlatformUser)>.Failure(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Access token is invalid.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null
            || !string.Equals(credential.SecurityStamp, token.SecurityStampAtIssue, StringComparison.Ordinal))
        {
            return ApplicationResult<(PlatformAccessToken, PlatformUser)>.Failure(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Access token is invalid.");
        }

        return ApplicationResult<(PlatformAccessToken, PlatformUser)>.Success((token, user));
    }
}

public sealed class IntrospectPlatformAccessToken
{
    private readonly IPlatformAccessTokenRepository _tokens;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly EvaluateProductAuthorization _authorize;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPlatformMfaReadinessService _mfa;

    public IntrospectPlatformAccessToken(
        IPlatformAccessTokenRepository tokens,
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        EvaluateProductAuthorization authorize,
        IPlatformSessionTokenService tokenService,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IPlatformMfaReadinessService mfa)
    {
        _tokens = tokens;
        _users = users;
        _credentials = credentials;
        _memberships = memberships;
        _organizations = organizations;
        _authorize = authorize;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _mfa = mfa;
    }

    public async Task<PlatformAccessTokenIntrospectionDto> ExecuteAsync(
        string? opaqueAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueAccessToken))
        {
            return Inactive();
        }

        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueAccessToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null || !token.IsActive(_clock.UtcNow))
        {
            return Inactive();
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return Inactive();
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null
            || !string.Equals(credential.SecurityStamp, token.SecurityStampAtIssue, StringComparison.Ordinal))
        {
            return Inactive();
        }

        string? orgName = null;
        bool? allowed = null;
        string? reason = null;
        string? subscriptionStatus = null;
        IReadOnlyList<string>? features = null;
        string? productLocalRole = null;
        string? mappedPosRole = null;

        if (token.OrganizationId is not null)
        {
            var membership = await _memberships
                .FindActiveByUserAndOrganizationAsync(user.Id, token.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            var organization = await _organizations
                .GetByIdAsync(token.OrganizationId, cancellationToken)
                .ConfigureAwait(false);

            if (membership is null
                || organization is null
                || organization.Status is not OrganizationStatus.Active)
            {
                token.ClearProductContext();
                await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                orgName = organization.DisplayName;
                if (!string.IsNullOrWhiteSpace(token.ProductCode))
                {
                    var access = await _authorize
                        .ExecuteAsync(user.Id, token.OrganizationId, token.ProductCode, cancellationToken)
                        .ConfigureAwait(false);
                    allowed = access.CanOperate;
                    reason = access.ReasonCode;
                    subscriptionStatus = access.SubscriptionStatus;
                    features = access.EnabledFeatureCodes;
                    productLocalRole = access.ProductLocalRoleCode;
                    mappedPosRole = access.MappedPosRoleCode;
                    if (!access.CanOperate)
                    {
                        token.ClearProductContext();
                        await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        orgName = null;
                        allowed = false;
                    }
                }
            }
        }

        var mfa = await _mfa.GetForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return new PlatformAccessTokenIntrospectionDto(
            Active: true,
            token.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            token.OrganizationId?.Value,
            orgName,
            token.ProductCode,
            token.ExpiresAtUtc,
            allowed,
            reason,
            subscriptionStatus,
            features,
            mfa,
            productLocalRole,
            mappedPosRole);
    }

    private static PlatformAccessTokenIntrospectionDto Inactive() =>
        new(false, null, null, null, null, null, null, null, null, null, null, null, null, null);
}

public sealed class RevokePlatformAccessToken
{
    private readonly IPlatformAccessTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokePlatformAccessToken(
        IPlatformAccessTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _tokens = tokens;
        _tokenService = tokenService;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<bool>> ExecuteAsync(
        string? opaqueAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueAccessToken))
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Access token is invalid.");
        }

        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueAccessToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Access token is invalid.");
        }

        token.Revoke(_clock.UtcNow);
        await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{token.UserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthAccessTokenRevoked,
            nameof(PlatformAccessToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Platform API access token revoked (raw token not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<bool>.Success(true);
    }
}
