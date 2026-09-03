using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Infrastructure.Persistence.Catalog;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class CatalogEntityMapper
{
    public static Product ToDomain(ProductRecord record) =>
        Product.Rehydrate(
            ProductId.From(record.Id),
            ProductCode.Create(record.Code),
            record.DisplayName,
            Enum.Parse<ProductStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static ProductRecord ToRecord(Product product) =>
        new()
        {
            Id = product.Id.Value,
            Code = product.Code.Value,
            DisplayName = product.DisplayName,
            Status = product.Status.ToString(),
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        };

    public static void ApplyToRecord(Product product, ProductRecord record)
    {
        record.DisplayName = product.DisplayName;
        record.Status = product.Status.ToString();
        record.UpdatedAtUtc = product.UpdatedAtUtc;
    }

    public static FeatureDefinition ToDomain(FeatureDefinitionRecord record) =>
        FeatureDefinition.Rehydrate(
            FeatureCode.Create(record.FeatureCode),
            ProductCode.Create(record.ProductCode),
            record.DisplayName,
            Enum.Parse<FeatureValueType>(record.ValueType),
            Enum.Parse<FeatureDefinitionStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static FeatureDefinitionRecord ToRecord(FeatureDefinition feature) =>
        new()
        {
            ProductCode = feature.ProductCode.Value,
            FeatureCode = feature.Code.Value,
            DisplayName = feature.DisplayName,
            ValueType = feature.ValueType.ToString(),
            Status = feature.Status.ToString(),
            CreatedAtUtc = feature.CreatedAtUtc,
            UpdatedAtUtc = feature.UpdatedAtUtc
        };

    public static void ApplyToRecord(FeatureDefinition feature, FeatureDefinitionRecord record)
    {
        record.DisplayName = feature.DisplayName;
        record.Status = feature.Status.ToString();
        record.UpdatedAtUtc = feature.UpdatedAtUtc;
    }

    public static Plan ToDomain(PlanRecord record) =>
        Plan.Rehydrate(
            PlanId.From(record.Id),
            ProductCode.Create(record.ProductCode),
            PlanCode.Create(record.Code),
            record.DisplayName,
            Enum.Parse<PlanStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Description,
            record.MaxBranches <= 0 ? 1 : record.MaxBranches,
            record.MaxActiveStaff <= 0 ? 3 : record.MaxActiveStaff,
            record.MaxActivePosDevices <= 0 ? 1 : record.MaxActivePosDevices,
            record.MaxActiveBusinessTypes <= 0 ? 1 : record.MaxActiveBusinessTypes,
            record.CustomerCreditEnabled,
            record.AdvancedReportsEnabled,
            record.ExportEnabled,
            record.TrialAllowed,
            record.DefaultTrialDays < 0 ? 14 : record.DefaultTrialDays,
            record.SortOrder,
            record.MonthlyPrice,
            record.AnnualPrice,
            string.IsNullOrWhiteSpace(record.CurrencyCode) ? "PHP" : record.CurrencyCode,
            record.MaxAreas <= 0 ? 1 : record.MaxAreas);

    public static PlanRecord ToRecord(Plan plan) =>
        new()
        {
            Id = plan.Id.Value,
            ProductCode = plan.ProductCode.Value,
            Code = plan.Code.Value,
            DisplayName = plan.DisplayName,
            Description = plan.Description,
            Status = plan.Status.ToString(),
            MaxBranches = plan.MaxBranches,
            MaxActiveStaff = plan.MaxActiveStaff,
            MaxActivePosDevices = plan.MaxActivePosDevices,
            MaxActiveBusinessTypes = plan.MaxActiveBusinessTypes,
            MaxAreas = plan.MaxAreas,
            CustomerCreditEnabled = plan.CustomerCreditEnabled,
            AdvancedReportsEnabled = plan.AdvancedReportsEnabled,
            ExportEnabled = plan.ExportEnabled,
            TrialAllowed = plan.TrialAllowed,
            DefaultTrialDays = plan.DefaultTrialDays,
            SortOrder = plan.SortOrder,
            MonthlyPrice = plan.MonthlyPrice,
            AnnualPrice = plan.AnnualPrice,
            CurrencyCode = plan.CurrencyCode,
            CreatedAtUtc = plan.CreatedAtUtc,
            UpdatedAtUtc = plan.UpdatedAtUtc
        };

    public static void ApplyToRecord(Plan plan, PlanRecord record)
    {
        record.DisplayName = plan.DisplayName;
        record.Description = plan.Description;
        record.Status = plan.Status.ToString();
        record.MaxBranches = plan.MaxBranches;
        record.MaxActiveStaff = plan.MaxActiveStaff;
        record.MaxActivePosDevices = plan.MaxActivePosDevices;
        record.MaxActiveBusinessTypes = plan.MaxActiveBusinessTypes;
        record.MaxAreas = plan.MaxAreas;
        record.CustomerCreditEnabled = plan.CustomerCreditEnabled;
        record.AdvancedReportsEnabled = plan.AdvancedReportsEnabled;
        record.ExportEnabled = plan.ExportEnabled;
        record.TrialAllowed = plan.TrialAllowed;
        record.DefaultTrialDays = plan.DefaultTrialDays;
        record.SortOrder = plan.SortOrder;
        record.MonthlyPrice = plan.MonthlyPrice;
        record.AnnualPrice = plan.AnnualPrice;
        record.CurrencyCode = plan.CurrencyCode;
        record.UpdatedAtUtc = plan.UpdatedAtUtc;
    }

    public static PlanVersion ToDomain(PlanVersionRecord record)
    {
        var grants = record.FeatureGrants
            .Select(g => new FeatureGrantSpec(
                FeatureCode.Create(g.FeatureCode),
                g.Enabled,
                g.NumericLimit))
            .ToList();

        var businessTypeGrants = record.BusinessTypeGrants
            .Select(g => BusinessTypeId.From(g.BusinessTypeId))
            .ToList();

        return PlanVersion.Rehydrate(
            PlanVersionId.From(record.Id),
            PlanId.From(record.PlanId),
            ProductCode.Create(record.ProductCode),
            record.VersionNumber,
            record.EffectiveFromUtc,
            record.EffectiveToUtc,
            Enum.Parse<BillingPeriod>(record.BillingPeriod),
            record.TrialEligible,
            Enum.Parse<PlanVersionStatus>(record.Status),
            grants,
            businessTypeGrants,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }

    public static PlanVersionRecord ToRecord(PlanVersion version)
    {
        var record = new PlanVersionRecord
        {
            Id = version.Id.Value,
            PlanId = version.PlanId.Value,
            ProductCode = version.ProductCode.Value,
            VersionNumber = version.VersionNumber,
            EffectiveFromUtc = version.EffectiveFromUtc,
            EffectiveToUtc = version.EffectiveToUtc,
            BillingPeriod = version.BillingPeriod.ToString(),
            TrialEligible = version.TrialEligible,
            Status = version.Status.ToString(),
            CreatedAtUtc = version.CreatedAtUtc,
            UpdatedAtUtc = version.UpdatedAtUtc,
            PublishedAtUtc = version.Status == PlanVersionStatus.Published ? version.UpdatedAtUtc : null,
            FeatureGrants = version.Grants.Select(g => new PlanVersionFeatureGrantRecord
            {
                PlanVersionId = version.Id.Value,
                FeatureCode = g.FeatureCode.Value,
                Enabled = g.Enabled,
                NumericLimit = g.NumericLimit
            }).ToList(),
            BusinessTypeGrants = version.BusinessTypeGrants.Select(id => new PlanVersionBusinessTypeGrantRecord
            {
                PlanVersionId = version.Id.Value,
                BusinessTypeId = id.Value
            }).ToList()
        };

        return record;
    }

    public static void ApplyToRecord(PlanVersion version, PlanVersionRecord record)
    {
        record.EffectiveFromUtc = version.EffectiveFromUtc;
        record.EffectiveToUtc = version.EffectiveToUtc;
        record.BillingPeriod = version.BillingPeriod.ToString();
        record.TrialEligible = version.TrialEligible;
        record.Status = version.Status.ToString();
        record.UpdatedAtUtc = version.UpdatedAtUtc;
        record.PublishedAtUtc = version.Status == PlanVersionStatus.Published ? version.UpdatedAtUtc : record.PublishedAtUtc;

        record.FeatureGrants.Clear();
        foreach (var grant in version.Grants)
        {
            record.FeatureGrants.Add(new PlanVersionFeatureGrantRecord
            {
                PlanVersionId = record.Id,
                FeatureCode = grant.FeatureCode.Value,
                Enabled = grant.Enabled,
                NumericLimit = grant.NumericLimit
            });
        }

        record.BusinessTypeGrants.Clear();
        foreach (var businessTypeId in version.BusinessTypeGrants)
        {
            record.BusinessTypeGrants.Add(new PlanVersionBusinessTypeGrantRecord
            {
                PlanVersionId = record.Id,
                BusinessTypeId = businessTypeId.Value
            });
        }
    }

    public static TrialDefinition ToDomain(TrialDefinitionRecord record)
    {
        var duringTrial = record.FeatureGrants
            .Where(g => g.GrantKind == TrialGrantKind.DuringTrial.ToString())
            .Select(g => new FeatureGrantSpec(
                FeatureCode.Create(g.FeatureCode),
                g.Enabled,
                g.NumericLimit))
            .ToList();

        var postExpiry = record.FeatureGrants
            .Where(g => g.GrantKind == TrialGrantKind.PostExpiry.ToString())
            .Select(g => new FeatureGrantSpec(
                FeatureCode.Create(g.FeatureCode),
                g.Enabled,
                g.NumericLimit))
            .ToList();

        return TrialDefinition.Rehydrate(
            TrialDefinitionId.From(record.Id),
            ProductCode.Create(record.ProductCode),
            record.PlanId is null ? null : PlanId.From(record.PlanId.Value),
            record.DisplayName,
            TimeSpan.FromTicks(record.DurationTicks),
            Enum.Parse<TrialDefinitionStatus>(record.Status),
            duringTrial,
            postExpiry,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }

    public static TrialDefinitionRecord ToRecord(TrialDefinition trial)
    {
        var grants = trial.FeatureGrants
            .Select(g => new TrialDefinitionFeatureGrantRecord
            {
                TrialDefinitionId = trial.Id.Value,
                FeatureCode = g.FeatureCode.Value,
                GrantKind = TrialGrantKind.DuringTrial.ToString(),
                Enabled = g.Enabled,
                NumericLimit = g.NumericLimit
            })
            .Concat(trial.PostExpiryFeatureGrants.Select(g => new TrialDefinitionFeatureGrantRecord
            {
                TrialDefinitionId = trial.Id.Value,
                FeatureCode = g.FeatureCode.Value,
                GrantKind = TrialGrantKind.PostExpiry.ToString(),
                Enabled = g.Enabled,
                NumericLimit = g.NumericLimit
            }))
            .ToList();

        return new TrialDefinitionRecord
        {
            Id = trial.Id.Value,
            ProductCode = trial.ProductCode.Value,
            PlanId = trial.PlanId?.Value,
            DisplayName = trial.DisplayName,
            DurationTicks = trial.Duration.Ticks,
            Status = trial.Status.ToString(),
            CreatedAtUtc = trial.CreatedAtUtc,
            UpdatedAtUtc = trial.UpdatedAtUtc,
            FeatureGrants = grants
        };
    }

    public static void ApplyToRecord(TrialDefinition trial, TrialDefinitionRecord record)
    {
        record.DisplayName = trial.DisplayName;
        record.Status = trial.Status.ToString();
        record.UpdatedAtUtc = trial.UpdatedAtUtc;

        record.FeatureGrants.Clear();
        foreach (var grant in trial.FeatureGrants)
        {
            record.FeatureGrants.Add(new TrialDefinitionFeatureGrantRecord
            {
                TrialDefinitionId = record.Id,
                FeatureCode = grant.FeatureCode.Value,
                GrantKind = TrialGrantKind.DuringTrial.ToString(),
                Enabled = grant.Enabled,
                NumericLimit = grant.NumericLimit
            });
        }

        foreach (var grant in trial.PostExpiryFeatureGrants)
        {
            record.FeatureGrants.Add(new TrialDefinitionFeatureGrantRecord
            {
                TrialDefinitionId = record.Id,
                FeatureCode = grant.FeatureCode.Value,
                GrantKind = TrialGrantKind.PostExpiry.ToString(),
                Enabled = grant.Enabled,
                NumericLimit = grant.NumericLimit
            });
        }
    }
}
