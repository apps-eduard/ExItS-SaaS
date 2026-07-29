namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform organization membership roles only.
/// Not product-local roles (Doctor, Nurse, Cashier, StoreManager, InventoryStaff).
/// Not Platform system roles (Platform Admin / Support).
/// </summary>
public enum OrganizationRole
{
    OrganizationOwner = 1,
    OrganizationAdministrator = 2,
    OrganizationMember = 3
}
