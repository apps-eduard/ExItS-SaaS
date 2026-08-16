namespace ExItS.PinoyBusinessPOS.Application.OperationalSetup;

/// <summary>
/// Reads Platform-owned TaxConfigurationEnabled for an organization.
/// Fail closed: missing or unknown capability is treated as disabled.
/// </summary>
public interface IOrganizationTaxConfigurationCapabilityReader
{
    Task<bool> IsTaxConfigurationEnabledAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default for unit tests and hosts without Platform linkage — always disabled.
/// </summary>
public sealed class DisabledOrganizationTaxConfigurationCapabilityReader
    : IOrganizationTaxConfigurationCapabilityReader
{
    public Task<bool> IsTaxConfigurationEnabledAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
