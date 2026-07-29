namespace ExItS.Platform.Domain.Products;

/// <summary>
/// Product access status. Distinct from subscription entitlement and product-local permissions.
/// </summary>
public enum ProductAccessStatus
{
    Active = 1,
    Revoked = 2
}
