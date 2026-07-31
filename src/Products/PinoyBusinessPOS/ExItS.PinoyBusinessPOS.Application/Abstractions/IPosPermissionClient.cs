using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosPermissionClient
{
    Task<ApiResult<IReadOnlyList<PosRoleDto>>> ListRolesAsync(CancellationToken ct = default);

    Task<ApiResult<PosRoleAssignmentListDto>> ListAssignmentsAsync(
        string? status = null,
        Guid? actorId = null,
        string? role = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default);

    Task<ApiResult<PosRoleAssignmentDto>> GetAssignmentAsync(Guid assignmentId, CancellationToken ct = default);

    Task<ApiResult<PosRoleAssignmentDto>> AssignAsync(AssignPosRoleRequest request, CancellationToken ct = default);

    Task<ApiResult<PosRoleAssignmentDto>> RevokeAsync(Guid assignmentId, RevokePosRoleRequest? request = null, CancellationToken ct = default);

    Task<ApiResult<PosEffectivePermissionsDto>> GetEffectiveAsync(CancellationToken ct = default);

    Task<ApiResult<PosEffectivePermissionsDto>> GetActorEffectiveAsync(Guid actorId, CancellationToken ct = default);
}
