using System.Net;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.UnitTests.Common;

public sealed class PosDeviceTransactionAuthorizerTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Enforcement_disabled_allows_without_installation_id_and_skips_platform()
    {
        var handler = new RecordingHandler();
        var authorizer = CreateAuthorizer(enforcementEnabled: false, handler);

        var request = CreateRequest(installationDeviceId: null);
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        Assert.Null(denied);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Enforcement_disabled_allows_when_platform_would_be_unavailable()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => throw new HttpRequestException("Platform down"),
        };
        var authorizer = CreateAuthorizer(enforcementEnabled: false, handler);

        var request = CreateRequest(installationDeviceId: "device-1");
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        Assert.Null(denied);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Enforcement_enabled_missing_installation_id_returns_registration_required()
    {
        var handler = new RecordingHandler();
        var authorizer = CreateAuthorizer(enforcementEnabled: true, handler, environmentName: "Development");

        var request = CreateRequest(installationDeviceId: null);
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        AssertProblem(denied, StatusCodes.Status403Forbidden, "application.pos_device.registration_required");
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Enforcement_enabled_revoked_returns_denied()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => JsonProblem(HttpStatusCode.Forbidden, "application.pos_device.revoked"),
        };
        var authorizer = CreateAuthorizer(enforcementEnabled: true, handler);

        var request = CreateRequest(installationDeviceId: "device-1");
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        AssertProblem(denied, StatusCodes.Status403Forbidden, "application.pos_device.revoked");
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Enforcement_enabled_not_authorized_returns_denied()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => JsonProblem(HttpStatusCode.Forbidden, "application.pos_device.not_authorized"),
        };
        var authorizer = CreateAuthorizer(enforcementEnabled: true, handler);

        var request = CreateRequest(installationDeviceId: "device-1");
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        AssertProblem(denied, StatusCodes.Status403Forbidden, "application.pos_device.not_authorized");
    }

    [Fact]
    public async Task Enforcement_enabled_platform_unavailable_returns_unavailable()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
        };
        var authorizer = CreateAuthorizer(enforcementEnabled: true, handler);

        var request = CreateRequest(installationDeviceId: "device-1");
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        AssertProblem(denied, StatusCodes.Status503ServiceUnavailable, "pos.device_authorization.unavailable");
    }

    [Fact]
    public async Task Enforcement_enabled_active_device_allows()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"posDeviceId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"}""",
                    Encoding.UTF8,
                    "application/json"),
            },
        };
        var authorizer = CreateAuthorizer(enforcementEnabled: true, handler);

        var request = CreateRequest(installationDeviceId: "device-1");
        var denied = await authorizer.EnsureAuthorizedAsync(request, OrgId, CancellationToken.None);

        Assert.Null(denied);
        Assert.Equal(1, handler.CallCount);
    }

    private static PosDeviceTransactionAuthorizer CreateAuthorizer(
        bool enforcementEnabled,
        RecordingHandler handler,
        string environmentName = "Development")
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://platform.test/", UriKind.Absolute),
        };
        var httpContextAccessor = new HttpContextAccessor();
        return new PosDeviceTransactionAuthorizer(
            client,
            httpContextAccessor,
            Options.Create(new PlatformAuthOptions { BaseUrl = "http://platform.test/" }),
            Options.Create(new PosDeviceAuthorizationOptions { EnforcementEnabled = enforcementEnabled }),
            new TestHostEnvironment(environmentName));
    }

    private static HttpRequest CreateRequest(string? installationDeviceId)
    {
        var context = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(installationDeviceId))
        {
            context.Request.Headers[PosDeviceTransactionAuthorizer.DeviceHeaderName] = installationDeviceId;
        }

        return context.Request;
    }

    private static HttpResponseMessage JsonProblem(HttpStatusCode status, string errorCode) =>
        new(status)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { errorCode }),
                Encoding.UTF8,
                "application/json"),
        };

    private static void AssertProblem(IResult? result, int statusCode, string errorCode)
    {
        Assert.NotNull(result);
        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(statusCode, problem.StatusCode);
        Assert.True(problem.ProblemDetails.Extensions.TryGetValue("errorCode", out var code));
        Assert.Equal(errorCode, code?.ToString());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Responder(request));
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
