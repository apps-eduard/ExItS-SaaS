using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Ensures inventory mutation body branch ids cannot override authenticated workspace branch authority.
/// </summary>
internal static class InventoryBranchBodyResolver
{
    public static (bool Success, Guid? BranchId, IResult? Problem) ResolveMutationBranch(
        HttpRequest request,
        Guid? bodyBranchId)
    {
        if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var headerBranch) || headerBranch is null)
        {
            return (true, bodyBranchId, null);
        }

        if (bodyBranchId is Guid body && body != Guid.Empty && body != headerBranch)
        {
            return (false, null, PosApiResults.Problem(
                ApplicationErrorCodes.InventoryBranchAuthorityMismatch,
                "Request branch does not match authenticated workspace branch.",
                StatusCodes.Status403Forbidden));
        }

        return (true, headerBranch, null);
    }
}
