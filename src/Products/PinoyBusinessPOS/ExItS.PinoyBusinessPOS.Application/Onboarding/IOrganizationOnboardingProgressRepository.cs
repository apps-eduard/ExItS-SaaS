using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Onboarding;

namespace ExItS.PinoyBusinessPOS.Application.Onboarding;

public interface IOrganizationOnboardingProgressRepository
{
    Task<OrganizationOnboardingProgress?> GetByOrganizationIdAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationOnboardingProgress progress, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationOnboardingProgress progress, CancellationToken cancellationToken = default);
}
