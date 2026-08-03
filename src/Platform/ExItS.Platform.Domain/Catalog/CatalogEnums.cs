namespace ExItS.Platform.Domain.Catalog;

public enum ProductStatus
{
    Active = 1,
    Inactive = 2,
    Retired = 3
}

public enum FeatureValueType
{
    Boolean = 1,
    NumericLimit = 2,
    QuantityLimit = 3
}

public enum FeatureDefinitionStatus
{
    Active = 1,
    Retired = 2
}

public enum PlanStatus
{
    Draft = 1,
    Active = 2,
    Retired = 3,
    Inactive = 4
}

public enum PlanVersionStatus
{
    Draft = 1,
    Published = 2,
    Retired = 3
}

public enum BillingPeriod
{
    None = 0,
    Monthly = 1,
    Yearly = 2
}

public enum TrialDefinitionStatus
{
    Active = 1,
    Retired = 2
}
