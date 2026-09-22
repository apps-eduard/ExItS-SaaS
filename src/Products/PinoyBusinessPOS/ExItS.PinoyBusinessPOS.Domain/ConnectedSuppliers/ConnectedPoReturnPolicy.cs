namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>Override mode for category or product connected-PO return policy.</summary>
public enum ConnectedPoReturnPolicyMode : short
{
    UseDefault = 0,
    Custom = 1,
    NonReturnable = 2
}

/// <summary>Which hierarchy level produced the effective connected-PO return policy.</summary>
public enum ConnectedPoReturnPolicySource : short
{
    Organization = 0,
    Category = 1,
    Product = 2
}

/// <summary>Resolved voluntary return policy for a supplier product at a point in time.</summary>
public sealed record EffectiveConnectedPoReturnPolicy(
    bool ReturnsAllowed,
    int? ReturnWindowDays,
    int ReceivingIssueWindowDays,
    bool RequireReturnApproval,
    ConnectedPoReturnPolicySource Source);

/// <summary>Category-level override of organization connected-PO return defaults.</summary>
public sealed record OrganizationConnectedCommerceCategoryReturnRule(
    Guid CategoryId,
    ConnectedPoReturnPolicyMode Mode,
    bool? ReturnsAllowed = null,
    int? ReturnWindowDays = null);
