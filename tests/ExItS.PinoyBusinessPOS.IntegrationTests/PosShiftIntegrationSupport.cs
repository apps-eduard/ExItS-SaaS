using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

internal static class PosShiftIntegrationSupport
{
    private const string Shifts = "/api/v1/pos/cashier-shifts";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<PosCashierShiftDto> EnsureOpenShiftAsync(
        HttpClient client,
        Guid organizationId,
        Guid actorId,
        decimal openingCashAmount = 0m)
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

        using var open = PosIntegrationRequest.Scoped(HttpMethod.Post, Shifts, organizationId, actorId);
        open.Content = JsonContent.Create(new OpenCashierShiftRequest(openingCashAmount), options: JsonOptions);
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
