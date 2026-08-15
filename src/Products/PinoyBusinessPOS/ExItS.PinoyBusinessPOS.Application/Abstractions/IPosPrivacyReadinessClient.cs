using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Privacy;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosPrivacyReadinessClient
{
    Task<ApiResult<OrganizationPrivacyReadinessDto>> GetAsync(CancellationToken ct = default);
}
