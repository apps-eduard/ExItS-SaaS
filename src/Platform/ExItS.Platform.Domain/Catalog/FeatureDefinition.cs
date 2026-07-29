using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>Commercial feature definition owned by Platform for one ProductCode.</summary>
public sealed class FeatureDefinition
{
    public FeatureCode Code { get; }
    public ProductCode ProductCode { get; }
    public string DisplayName { get; private set; }
    public FeatureValueType ValueType { get; }
    public FeatureDefinitionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private FeatureDefinition(
        FeatureCode code,
        ProductCode productCode,
        string displayName,
        FeatureValueType valueType,
        FeatureDefinitionStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Code = code;
        ProductCode = productCode;
        DisplayName = displayName;
        ValueType = valueType;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static FeatureDefinition Create(
        ProductCode productCode,
        FeatureCode code,
        string displayName,
        FeatureValueType valueType,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(code);
        DomainTime.EnsureUtc(utcNow);
        if (!Enum.IsDefined(valueType))
        {
            throw new DomainException(DomainErrorCodes.InvalidFeatureValueType, "Feature value type is not defined.");
        }

        return new FeatureDefinition(
            code,
            productCode,
            DomainTime.NormalizeDisplayName(displayName),
            valueType,
            FeatureDefinitionStatus.Active,
            utcNow,
            utcNow);
    }

    public void Retire(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == FeatureDefinitionStatus.Retired)
        {
            return;
        }

        Status = FeatureDefinitionStatus.Retired;
        UpdatedAtUtc = utcNow;
    }

    public void EnsureAssignable()
    {
        if (Status == FeatureDefinitionStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.FeatureRetired,
                "A retired feature cannot be newly assigned.");
        }
    }
}
