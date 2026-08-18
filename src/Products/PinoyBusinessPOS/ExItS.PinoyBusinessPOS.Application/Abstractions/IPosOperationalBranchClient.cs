using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosOperationalBranchClient
{
    Task<ApiResult<OperationalBranchContextDto>> SelectAsync(
        SelectOperationalBranchRequest request,
        CancellationToken ct = default);
}
