using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Offline;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosOfflineOperatingGrantApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private const string Grants = "/api/v1/pos/offline-operating-grants";

    [Fact]
    public async Task Issue_returns_server_signed_grant_with_valid_signature()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var branch = Guid.NewGuid();
        var installation = Guid.NewGuid().ToString("D");

        using var request = Scoped(HttpMethod.Post, Grants, org, branch, installation);
        request.Content = JsonContent.Create(
            new IssueOfflineOperatingGrantRequest(installation, "Test Org", "Main Branch"),
            options: JsonOptions);
        using var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadFromJsonAsync<IssueOfflineOperatingGrantResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(ServerSignedOfflineOperatingGrant.CurrentSchemaVersion, body.Grant.SchemaVersion);
        Assert.Matches("^[0-9a-f]+$", body.Grant.Signature);

        var service = factory.Services.GetRequiredService<IServerSignedOfflineOperatingGrantService>();
        var verification = service.Verify(
            Map(body.Grant),
            installation,
            body.Grant.UserId);
        Assert.True(verification.IsValid);
    }

    [Fact]
    public void Tampered_role_fails_server_verification()
    {
        var service = new ServerSignedOfflineOperatingGrantService(
            new FixedClock(DateTimeOffset.UtcNow),
            Microsoft.Extensions.Options.Options.Create(new OfflinePriceAuthorityOptions()));

        var issued = service.IssueOrganizationGrantAsync(
            Guid.Parse("248935e9-e462-425f-88f5-a9255bf12748"),
            Guid.Parse("ca023f5b-925e-4aa5-a843-d48c4c06fa14"),
            Guid.Parse("742fb3f3-14f9-4bee-a94e-f5acccc7cbc5"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "22222222-2222-4222-8222-222222222222",
            "Cashier",
            "Kizy Store",
            "Main Branch",
            null,
            null).GetAwaiter().GetResult();

        Assert.True(issued.IsSuccess);
        var tampered = issued.Value! with { RoleCode = "Owner" };
        var verification = service.Verify(tampered, tampered.InstallationDeviceId);
        Assert.False(verification.IsValid);
        Assert.Equal(ServerSignedOfflineGrantFailure.Tampered, verification.Failure);
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid branchId,
        string installationDeviceId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.OrganizationHeaderName, organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.BranchHeaderName, branchId.ToString("D"));
        request.Headers.TryAddWithoutValidation(PosOrganizationHeaders.ActorHeaderName, Actor.ToString("D"));
        request.Headers.TryAddWithoutValidation("X-Pos-Installation-Device-Id", installationDeviceId);
        return request;
    }

    private static ServerSignedOfflineOperatingGrant Map(ServerSignedOfflineOperatingGrantDto dto) =>
        new(
            dto.GrantId,
            dto.SchemaVersion,
            dto.UserId,
            Enum.Parse<OfflineGrantScopeKind>(dto.ScopeKind, ignoreCase: true),
            dto.OrganizationId,
            dto.OrganizationDisplayName,
            dto.BranchId,
            dto.BranchName,
            dto.InstallationDeviceId,
            dto.PosDeviceId,
            dto.RoleCode,
            dto.DisplayName,
            dto.Username,
            dto.IssuedAtUtc,
            dto.LastOnlineValidatedAtUtc,
            dto.ExpiresAtUtc,
            dto.Signature);

    private sealed class FixedClock(DateTimeOffset now) : ExItS.PinoyBusinessPOS.Domain.Abstractions.IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                });
            });
        }
    }
}
