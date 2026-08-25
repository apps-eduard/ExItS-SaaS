using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Options;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Verifies that a money-affecting POS request originates from a registered active installation.
/// Authorization remains authoritative in Platform; no role, including Owner, bypasses this check
/// when <see cref="PosDeviceAuthorizationOptions.EnforcementEnabled"/> is true.
/// </summary>
internal interface IPosDeviceTransactionAuthorizer
{
    Task<IResult?> EnsureAuthorizedAsync(HttpRequest request, Guid organizationId, CancellationToken ct);

    /// <summary>Current enforcement flag (for POS runtime policy exposure).</summary>
    bool EnforcementEnabled { get; }
}

internal sealed class PosDeviceTransactionAuthorizer(
    HttpClient client,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    IOptions<PosDeviceAuthorizationOptions> deviceAuthorizationOptions,
    IHostEnvironment environment) : IPosDeviceTransactionAuthorizer
{
    internal const string DeviceHeaderName = "X-Pos-Installation-Device-Id";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool EnforcementEnabled => deviceAuthorizationOptions.Value.EnforcementEnabled;

    public async Task<IResult?> EnsureAuthorizedAsync(HttpRequest request, Guid organizationId, CancellationToken ct)
    {
        // Pure React PWA: Local Validation sets EnforcementEnabled=false so browsers need not register.
        // Re-enable with PosDeviceAuthorization__EnforcementEnabled=true for Capacitor/native.
        if (!deviceAuthorizationOptions.Value.EnforcementEnabled)
        {
            return null;
        }

        var deviceId = request.Headers[DeviceHeaderName].FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            // Integration WebApplicationFactory has no Platform device service. Production and real
            // Development clients must still send a registered installation device id.
            if (environment.IsEnvironment("Testing"))
            {
                return null;
            }

            return Denied(
                "application.pos_device.registration_required",
                "Register this device before executing POS sales.");
        }

        if (client.BaseAddress is null)
        {
            var baseUrl = options.Value.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return Unavailable();
            }

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        using var platformRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/v1/platform/organizations/{organizationId:D}/pos-devices/authorize")
        {
            Content = JsonContent.Create(new
            {
                installationDeviceId = deviceId,
                branchId = ParseOptionalBranchId(request)
            })
        };
        ForwardAuthenticationHeaders(platformRequest, httpContextAccessor.HttpContext?.Request);

        try
        {
            using var response = await client.SendAsync(platformRequest, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return null;
            }

            var errorCode = await ReadErrorCodeAsync(response, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Forbidden
                && errorCode is "application.pos_device.not_authorized"
                    or "application.pos_device.revoked"
                    or "application.pos_device.registration_required")
            {
                return Denied(
                    errorCode,
                    errorCode switch
                    {
                        "application.pos_device.revoked" => "This POS device has been revoked.",
                        "application.pos_device.registration_required" =>
                            "Register this device before executing POS sales.",
                        _ => "This POS device is not authorized for transactions.",
                    });
            }

            // Do not disguise a Platform outage as a device rejection. The mobile client must retain
            // its offline grant unless it receives an explicit server authorization denial.
            return Unavailable();
        }
        catch (HttpRequestException)
        {
            return Unavailable();
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Unavailable();
        }
    }

    private static Guid? ParseOptionalBranchId(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(PosOrganizationHeaders.BranchHeaderName, out var values)
            || string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return null;
        }

        return Guid.TryParse(values.First(), out var parsed) && parsed != Guid.Empty
            ? parsed
            : null;
    }

    private static void ForwardAuthenticationHeaders(HttpRequestMessage destination, HttpRequest? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var name in new[] { "Authorization", "X-ExItS-Session-Token", "X-Dev-Platform-User-Id" })
        {
            if (source.Headers.TryGetValue(name, out var value))
            {
                destination.Headers.TryAddWithoutValidation(name, value.ToArray());
            }
        }
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            return document.RootElement.TryGetProperty("errorCode", out var value) ? value.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IResult Denied(string errorCode, string detail) =>
        PosApiResults.Problem(errorCode, detail, StatusCodes.Status403Forbidden);

    private static IResult Unavailable() =>
        PosApiResults.Problem(
            "pos.device_authorization.unavailable",
            "Device authorization is temporarily unavailable. Retry when Platform is reachable.",
            StatusCodes.Status503ServiceUnavailable);
}
