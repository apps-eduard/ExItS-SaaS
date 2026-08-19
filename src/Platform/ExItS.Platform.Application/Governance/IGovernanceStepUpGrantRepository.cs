using ExItS.Platform.Domain.Governance;

namespace ExItS.Platform.Application.Governance;

public interface IGovernanceStepUpGrantRepository
{
    Task<GovernanceStepUpGrant?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(GovernanceStepUpGrant grant, CancellationToken cancellationToken = default);

    Task UpdateAsync(GovernanceStepUpGrant grant, CancellationToken cancellationToken = default);
}
