using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Qr;

public sealed record ResolveExItsQrRequest(string Payload, string? ExpectedPurpose = null);

public sealed record ResolvedExItsQrDto(
    string Purpose,
    string? PublicUserId = null,
    Guid? UserIdentityId = null,
    string? PublicOrganizationId = null,
    Guid? OrganizationId = null,
    string? DisplayName = null,
    string? Status = null,
    bool? IsSelf = null,
    string? RegistrationToken = null,
    DateTimeOffset? ExpiresAtUtc = null,
    string? TokenStatus = null);

/// <summary>
/// Typed QR resolve dispatcher for scanners. Optional <see cref="ResolveExItsQrRequest.ExpectedPurpose"/>
/// rejects purpose mismatches with a plain mismatch code.
/// </summary>
public sealed class ResolveExItsQr(
    IPlatformUserRepository users,
    IPlatformOrganizationRepository organizations,
    IPosDeviceRegistrationTokenRepository registrationTokens,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<ResolvedExItsQrDto>> ExecuteAsync(
        PlatformUserId actorUserId,
        ResolveExItsQrRequest request,
        CancellationToken cancellationToken = default)
    {
        ExItsQrEnvelope.Parsed parsed;
        try
        {
            parsed = ExItsQrEnvelope.Parse(request.Payload);
            if (!string.IsNullOrWhiteSpace(request.ExpectedPurpose))
            {
                if (!Enum.TryParse<ExItsQrPurpose>(request.ExpectedPurpose.Trim(), ignoreCase: true, out var expected))
                {
                    return ApplicationResult<ResolvedExItsQrDto>.Failure(
                        ApplicationErrorCodes.QrPurposeMismatch,
                        "expected_purpose_invalid");
                }

                if (parsed.Purpose != expected)
                {
                    return ApplicationResult<ResolvedExItsQrDto>.Failure(
                        ApplicationErrorCodes.QrPurposeMismatch,
                        "qr_purpose_mismatch");
                }
            }
        }
        catch (DomainException ex) when (ex.ErrorCode is DomainErrorCodes.ExItsQrPurposeMismatch
                                             or DomainErrorCodes.InvalidExItsQrPurpose)
        {
            return ApplicationResult<ResolvedExItsQrDto>.Failure(
                ApplicationErrorCodes.QrPurposeMismatch,
                "qr_purpose_mismatch");
        }
        catch (DomainException)
        {
            return ApplicationResult<ResolvedExItsQrDto>.Failure(
                ApplicationErrorCodes.QrPayloadInvalid,
                "qr_payload_invalid");
        }

        ApplicationResult<ResolvedExItsQrDto> result = parsed.Purpose switch
        {
            ExItsQrPurpose.Personal => await ResolvePersonalAsync(actorUserId, parsed.Subject, cancellationToken)
                .ConfigureAwait(false),
            ExItsQrPurpose.Organization => await ResolveOrganizationAsync(parsed.Subject, cancellationToken)
                .ConfigureAwait(false),
            ExItsQrPurpose.PosDeviceRegistration => await ResolveRegistrationTokenAsync(
                    parsed.Subject,
                    cancellationToken)
                .ConfigureAwait(false),
            _ => ApplicationResult<ResolvedExItsQrDto>.Failure(
                ApplicationErrorCodes.QrPayloadInvalid,
                "qr_payload_invalid")
        };

        await audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.ExItsQrResolved,
            "exits_qr",
            parsed.Purpose.ToString(),
            result.IsSuccess ? AuditOutcome.Succeeded : AuditOutcome.Denied,
            summary: $"purpose={parsed.Purpose}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async Task<ApplicationResult<ResolvedExItsQrDto>> ResolvePersonalAsync(
        PlatformUserId actorUserId,
        string publicUserId,
        CancellationToken cancellationToken)
    {
        var target = await users.GetByPublicUserIdAsync(publicUserId, cancellationToken).ConfigureAwait(false);
        if (target is null || target.Status is not AccountStatus.Active)
        {
            return ApplicationResult<ResolvedExItsQrDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "No active user matched that ExItS ID.");
        }

        return ApplicationResult<ResolvedExItsQrDto>.Success(new ResolvedExItsQrDto(
            Purpose: nameof(ExItsQrPurpose.Personal),
            PublicUserId: target.PublicUserId!,
            UserIdentityId: target.Id.Value,
            DisplayName: target.DisplayName,
            Status: target.Status.ToString(),
            IsSelf: target.Id == actorUserId));
    }

    private async Task<ApplicationResult<ResolvedExItsQrDto>> ResolveOrganizationAsync(
        string publicOrganizationId,
        CancellationToken cancellationToken)
    {
        var target = await organizations
            .GetByPublicOrganizationIdAsync(publicOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (target is null || target.Status is not OrganizationStatus.Active)
        {
            return ApplicationResult<ResolvedExItsQrDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "No active organization matched that public ID.");
        }

        return ApplicationResult<ResolvedExItsQrDto>.Success(new ResolvedExItsQrDto(
            Purpose: nameof(ExItsQrPurpose.Organization),
            PublicOrganizationId: target.PublicOrganizationId!,
            OrganizationId: target.Id.Value,
            DisplayName: target.DisplayName,
            Status: target.Status.ToString()));
    }

    private async Task<ApplicationResult<ResolvedExItsQrDto>> ResolveRegistrationTokenAsync(
        string opaqueToken,
        CancellationToken cancellationToken)
    {
        var hash = PosDeviceRegistrationToken.HashToken(opaqueToken);
        var token = await registrationTokens.GetByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (token is null)
        {
            return ApplicationResult<ResolvedExItsQrDto>.Failure(
                ApplicationErrorCodes.PosDeviceRegistrationTokenNotFound,
                "Registration token was not found.");
        }

        token.RefreshExpired(clock.UtcNow);

        return ApplicationResult<ResolvedExItsQrDto>.Success(new ResolvedExItsQrDto(
            Purpose: nameof(ExItsQrPurpose.PosDeviceRegistration),
            OrganizationId: token.OrganizationId.Value,
            RegistrationToken: opaqueToken,
            ExpiresAtUtc: token.ExpiresAtUtc,
            TokenStatus: token.Status.ToString()));
    }
}
