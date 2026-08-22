using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Offline;

public sealed record IssueOfflineOperatingGrantRequest(
    string InstallationDeviceId,
    string? OrganizationDisplayName = null,
    string? BranchName = null,
    string? DisplayName = null,
    string? Username = null);

public sealed record ServerSignedOfflineOperatingGrantDto(
    Guid GrantId,
    int SchemaVersion,
    Guid UserId,
    string ScopeKind,
    Guid? OrganizationId,
    string OrganizationDisplayName,
    Guid? BranchId,
    string? BranchName,
    string InstallationDeviceId,
    Guid? PosDeviceId,
    string? RoleCode,
    string? DisplayName,
    string? Username,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset LastOnlineValidatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Signature);

public sealed record IssueOfflineOperatingGrantResponse(ServerSignedOfflineOperatingGrantDto Grant);

internal static class OfflineOperatingGrantEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEndpointRouteBuilder MapOfflineOperatingGrantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pos/offline-operating-grants", async (
            HttpRequest request,
            IssueOfflineOperatingGrantRequest body,
            IServerSignedOfflineOperatingGrantService grants,
            IPosCommercialAccessAccessor access,
            IHttpClientFactory httpClientFactory,
            IOptions<PlatformAuthOptions> platformOptions,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem)
                || !PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCatalog, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId) || branchId is null)
            {
                return PosApiResults.Problem(
                    DomainErrorCodes.InvalidBranchId,
                    $"Header '{PosOrganizationHeaders.BranchHeaderName}' must be a non-empty GUID.",
                    StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(body.InstallationDeviceId))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.OfflineOperatingGrantDeviceRequired,
                    "installationDeviceId is required.",
                    StatusCodes.Status400BadRequest);
            }

            if (!TryGetActorUserId(request, out var actorUserId))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.ActorRequired,
                    "An authenticated user is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var roleCode = request.HttpContext.Items.TryGetValue(PosAuthItems.MappedPosRoleCode, out var role)
                ? role?.ToString()
                : null;

            var authorization = await AuthorizeDeviceAsync(
                    httpClientFactory,
                    platformOptions.Value,
                    request,
                    organizationId,
                    branchId.Value,
                    body.InstallationDeviceId.Trim(),
                    ct)
                .ConfigureAwait(false);
            if (authorization.Problem is not null)
            {
                return authorization.Problem;
            }

            var organizationDisplayName = body.OrganizationDisplayName?.Trim();
            if (string.IsNullOrWhiteSpace(organizationDisplayName))
            {
                organizationDisplayName = organizationId.ToString("D");
            }

            var branchName = body.BranchName?.Trim();

            var result = await grants.IssueOrganizationGrantAsync(
                actorUserId,
                organizationId,
                branchId.Value,
                authorization.PosDeviceId,
                body.InstallationDeviceId.Trim(),
                roleCode,
                organizationDisplayName,
                branchName,
                body.DisplayName,
                body.Username,
                ct).ConfigureAwait(false);

            return PosApiResults.FromResult(result, grant =>
                Results.Ok(new IssueOfflineOperatingGrantResponse(Map(grant))));
        });

        return app;
    }

    private static bool TryGetActorUserId(HttpRequest request, out Guid userId)
    {
        userId = Guid.Empty;
        if (request.HttpContext.Items.TryGetValue(PosAuthItems.UserId, out var raw)
            && raw is Guid parsed
            && parsed != Guid.Empty)
        {
            userId = parsed;
            return true;
        }

        var actorId = request.Headers[PosOrganizationHeaders.ActorHeaderName].FirstOrDefault();
        return Guid.TryParse(actorId, out userId) && userId != Guid.Empty;
    }

    private static async Task<(Guid PosDeviceId, IResult? Problem)> AuthorizeDeviceAsync(
        IHttpClientFactory httpClientFactory,
        PlatformAuthOptions platformOptions,
        HttpRequest request,
        Guid organizationId,
        Guid branchId,
        string installationDeviceId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(platformOptions.BaseUrl))
        {
            if (request.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsEnvironment("Testing"))
            {
                return (Guid.Parse("11111111-1111-1111-1111-111111111111"), null);
            }

            return (Guid.Empty, PosApiResults.Problem(
                ApplicationErrorCodes.PlatformAuthUnavailable,
                "Platform authorization is unavailable.",
                StatusCodes.Status503ServiceUnavailable));
        }

        var client = httpClientFactory.CreateClient(nameof(OfflineOperatingGrantEndpoints));
        client.BaseAddress = new Uri(platformOptions.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/v1/platform/organizations/{organizationId:D}/pos-devices/authorize")
        {
            Content = JsonContent.Create(new { installationDeviceId, branchId })
        };
        ForwardAuthenticationHeaders(platformRequest, request);

        try
        {
            using var response = await client.SendAsync(platformRequest, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return (Guid.Empty, PosApiResults.Problem(
                    ApplicationErrorCodes.OfflineOperatingGrantDenied,
                    "This POS device is not authorized for offline operating grant issuance.",
                    StatusCodes.Status403Forbidden));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("posDeviceId", out var posDeviceElement)
                || !Guid.TryParse(posDeviceElement.GetString(), out var posDeviceId)
                || posDeviceId == Guid.Empty)
            {
                return (Guid.Empty, PosApiResults.Problem(
                    ApplicationErrorCodes.OfflineOperatingGrantDenied,
                    "Device authorization did not return a POS device id.",
                    StatusCodes.Status403Forbidden));
            }

            return (posDeviceId, null);
        }
        catch (HttpRequestException)
        {
            return (Guid.Empty, PosApiResults.Problem(
                ApplicationErrorCodes.PlatformAuthUnavailable,
                "Platform authorization is temporarily unavailable.",
                StatusCodes.Status503ServiceUnavailable));
        }
    }

    private static void ForwardAuthenticationHeaders(HttpRequestMessage destination, HttpRequest source)
    {
        foreach (var name in new[] { "Authorization", "X-ExItS-Session-Token", "X-Dev-Platform-User-Id" })
        {
            if (source.Headers.TryGetValue(name, out var value))
            {
                destination.Headers.TryAddWithoutValidation(name, value.ToArray());
            }
        }
    }

    private static ServerSignedOfflineOperatingGrantDto Map(ServerSignedOfflineOperatingGrant grant) =>
        new(
            grant.GrantId,
            grant.SchemaVersion,
            grant.UserId,
            grant.ScopeKind.ToString(),
            grant.OrganizationId,
            grant.OrganizationDisplayName,
            grant.BranchId,
            grant.BranchName,
            grant.InstallationDeviceId,
            grant.PosDeviceId,
            grant.RoleCode,
            grant.DisplayName,
            grant.Username,
            grant.IssuedAtUtc,
            grant.LastOnlineValidatedAtUtc,
            grant.ExpiresAtUtc,
            grant.Signature);
}
