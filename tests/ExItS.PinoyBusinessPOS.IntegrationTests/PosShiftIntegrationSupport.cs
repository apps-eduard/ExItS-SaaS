using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

internal static class PosShiftIntegrationSupport
{
    private const string Shifts = "/api/v1/pos/cashier-shifts";
    private const string Registers = "/api/v1/pos/registers";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<PosRegisterDto> EnsureRegisterAsync(
        HttpClient client,
        Guid organizationId,
        Guid actorId,
        string? name = null)
    {
        using var available = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            $"{Registers}/available-for-shift",
            organizationId,
            actorId);
        using var availableResponse = await client.SendAsync(available).ConfigureAwait(false);
        if (availableResponse.IsSuccessStatusCode)
        {
            var items = await availableResponse.Content
                .ReadFromJsonAsync<List<PosRegisterSummaryDto>>(JsonOptions)
                .ConfigureAwait(false);
            if (items is { Count: > 0 })
            {
                using var get = PosIntegrationRequest.Scoped(
                    HttpMethod.Get,
                    $"{Registers}/{items[0].RegisterId:D}",
                    organizationId,
                    actorId);
                using var getResponse = await client.SendAsync(get).ConfigureAwait(false);
                getResponse.EnsureSuccessStatusCode();
                return (await getResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions).ConfigureAwait(false))!;
            }
        }

        using var create = PosIntegrationRequest.Scoped(HttpMethod.Post, Registers, organizationId, actorId);
        create.Content = JsonContent.Create(
            new CreateRegisterRequest(name ?? $"Register {Guid.NewGuid():N}"),
            options: JsonOptions);
        using var createResponse = await client.SendAsync(create).ConfigureAwait(false);
        if (createResponse.IsSuccessStatusCode)
        {
            return (await createResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions).ConfigureAwait(false))!;
        }

        // Concurrent create race: fall back to any available register for shift.
        using var retryAvailable = PosIntegrationRequest.Scoped(
            HttpMethod.Get,
            $"{Registers}/available-for-shift",
            organizationId,
            actorId);
        using var retryResponse = await client.SendAsync(retryAvailable).ConfigureAwait(false);
        if (retryResponse.IsSuccessStatusCode)
        {
            var items = await retryResponse.Content
                .ReadFromJsonAsync<List<PosRegisterSummaryDto>>(JsonOptions)
                .ConfigureAwait(false);
            if (items is { Count: > 0 })
            {
                using var get = PosIntegrationRequest.Scoped(
                    HttpMethod.Get,
                    $"{Registers}/{items[0].RegisterId:D}",
                    organizationId,
                    actorId);
                using var getResponse = await client.SendAsync(get).ConfigureAwait(false);
                getResponse.EnsureSuccessStatusCode();
                return (await getResponse.Content.ReadFromJsonAsync<PosRegisterDto>(JsonOptions).ConfigureAwait(false))!;
            }
        }

        createResponse.EnsureSuccessStatusCode();
        throw new InvalidOperationException("Unable to ensure a register.");
    }

    public static async Task<PosCashierShiftDto> EnsureOpenShiftAsync(
        HttpClient client,
        Guid organizationId,
        Guid actorId,
        decimal openingCashAmount = 0m,
        Guid? registerId = null)
    {
        using var current = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Shifts}/current", organizationId, actorId);
        using var currentResponse = await client.SendAsync(current).ConfigureAwait(false);
        if (currentResponse.IsSuccessStatusCode)
        {
            var existing = await currentResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing;
            }
        }

        var register = registerId is null
            ? await EnsureRegisterAsync(client, organizationId, actorId).ConfigureAwait(false)
            : null;
        var resolvedRegisterId = registerId ?? register!.RegisterId;

        using var open = PosIntegrationRequest.Scoped(HttpMethod.Post, Shifts, organizationId, actorId);
        open.Content = JsonContent.Create(
            new OpenCashierShiftRequest(resolvedRegisterId, openingCashAmount),
            options: JsonOptions);
        using var openResponse = await client.SendAsync(open).ConfigureAwait(false);
        if (openResponse.IsSuccessStatusCode)
        {
            var shift = await openResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions).ConfigureAwait(false);
            return shift!;
        }

        using var retryCurrent = PosIntegrationRequest.Scoped(HttpMethod.Get, $"{Shifts}/current", organizationId, actorId);
        using var retryResponse = await client.SendAsync(retryCurrent).ConfigureAwait(false);
        if (retryResponse.IsSuccessStatusCode)
        {
            var existingAfterRace = await retryResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions)
                .ConfigureAwait(false);
            if (existingAfterRace is not null)
            {
                return existingAfterRace;
            }
        }

        openResponse.EnsureSuccessStatusCode();
        throw new InvalidOperationException("Unable to ensure an open cashier shift.");
    }
}

internal static class PosIntegrationRequest
{
    public static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        Guid actorId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            actorId.ToString("D"));
        return request;
    }
}
