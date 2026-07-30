namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Platform system roles only (platform-wide or organization-scoped operator roles).
/// Not organization membership roles (<see cref="Organizations.OrganizationRole"/>) and not
/// product-local roles (Doctor, Cashier, Clinic Admin, Store Manager, etc.).
/// </summary>
public enum PlatformSystemRole
{
    PlatformAdministrator = 1,
    BillingAdministrator = 2,
    PlatformSupport = 3
}
