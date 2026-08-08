using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Permissions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosPermissionClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosPermissionClient
{
    private const string Path = "/api/v1/pos/permissions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<IReadOnlyList<PosRoleDto>>> ListRolesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PosRoleDto>>(HttpMethod.Get, $"{Path}/roles", null, ct);

    public Task<ApiResult<PosRoleAssignmentListDto>> ListAssignmentsAsync(
        string? status = null,
        Guid? actorId = null,
        string? role = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{Path}/assignments?");
        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Append("status=").Append(Uri.EscapeDataString(status.Trim())).Append('&');
        }

        if (actorId is Guid a)
        {
            query.Append("actorId=").Append(a.ToString("D")).Append('&');
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query.Append("role=").Append(Uri.EscapeDataString(role.Trim())).Append('&');
        }

        query.Append("page=").Append((page ?? 1).ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append((pageSize ?? 50).ToString(CultureInfo.InvariantCulture));
        return SendAsync<PosRoleAssignmentListDto>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosRoleAssignmentDto>> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default) =>
        SendAsync<PosRoleAssignmentDto>(HttpMethod.Get, $"{Path}/assignments/{assignmentId:D}", null, ct);

    public Task<ApiResult<PosRoleAssignmentDto>> AssignAsync(AssignPosRoleRequest request, CancellationToken ct = default) =>
        SendAsync<PosRoleAssignmentDto>(HttpMethod.Post, $"{Path}/assignments", request, ct);

    public Task<ApiResult<PosRoleAssignmentDto>> RevokeAsync(
        Guid assignmentId,
        RevokePosRoleRequest? request = null,
        CancellationToken ct = default) =>
        SendAsync<PosRoleAssignmentDto>(
            HttpMethod.Post,
            $"{Path}/assignments/{assignmentId:D}/revoke",
            request ?? new RevokePosRoleRequest(),
            ct);

    public Task<ApiResult<PosEffectivePermissionsDto>> GetEffectiveAsync(CancellationToken ct = default) =>
        SendAsync<PosEffectivePermissionsDto>(HttpMethod.Get, $"{Path}/effective", null, ct);

    public Task<ApiResult<PosEffectivePermissionsDto>> GetActorEffectiveAsync(Guid actorId, CancellationToken ct = default) =>
        SendAsync<PosEffectivePermissionsDto>(HttpMethod.Get, $"{Path}/actors/{actorId:D}/effective", null, ct);

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return ApiResult<TResponse>.Offline(new ApiError("Offline", "Reconnect required.", "offline", null, null));
        }

        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct).ConfigureAwait(false);
                return ApiResult<TResponse>.Success(data!);
            }

            var detail = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var error = new ApiError(response.ReasonPhrase, detail, null, null, (int)response.StatusCode);
            return response.StatusCode switch
            {
                HttpStatusCode.Forbidden => ApiResult<TResponse>.Forbidden(error),
                HttpStatusCode.NotFound => ApiResult<TResponse>.NotFound(error),
                HttpStatusCode.Conflict => ApiResult<TResponse>.Conflict(error),
                HttpStatusCode.BadRequest => ApiResult<TResponse>.Validation(error),
                _ => ApiResult<TResponse>.Failed(error)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ApiResult<TResponse>.Cancelled(new ApiError("Cancelled", "Request cancelled.", "cancelled", null, null));
        }
        catch (TaskCanceledException)
        {
            return ApiResult<TResponse>.Timeout(new ApiError("Timeout", "The request timed out.", "timeout", null, null));
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<TResponse>.Unavailable(new ApiError("Unavailable", ex.Message, "unavailable", null, null));
        }
    }
}
