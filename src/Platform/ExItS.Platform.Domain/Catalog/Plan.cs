using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>
/// Platform Plan — commercial package for one Product.
/// PlanKey is <see cref="Code"/> (immutable after create). Product association is fixed.
/// </summary>
public sealed class Plan
{
    public PlanId Id { get; }
    public ProductCode ProductCode { get; }
    public PlanCode Code { get; }
    public string DisplayName { get; private set; }
    public string? Description { get; private set; }
    public PlanStatus Status { get; private set; }
    public int MaxBranches { get; private set; }
    public int MaxActiveStaff { get; private set; }
    public int MaxActivePosDevices { get; private set; }
    public bool CustomerCreditEnabled { get; private set; }
    public bool AdvancedReportsEnabled { get; private set; }
    public bool ExportEnabled { get; private set; }
    public bool TrialAllowed { get; private set; }
    public int DefaultTrialDays { get; private set; }
    public int SortOrder { get; private set; }
    public decimal MonthlyPrice { get; private set; }
    public decimal AnnualPrice { get; private set; }
    public string CurrencyCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Stable internal Plan key (same as <see cref="Code"/>).</summary>
    public string PlanKey => Code.Value;

    private Plan(
        PlanId id,
        ProductCode productCode,
        PlanCode code,
        string displayName,
        string? description,
        PlanStatus status,
        int maxBranches,
        int maxActiveStaff,
        int maxActivePosDevices,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        ProductCode = productCode;
        Code = code;
        DisplayName = displayName;
        Description = description;
        Status = status;
        MaxBranches = maxBranches;
        MaxActiveStaff = maxActiveStaff;
        MaxActivePosDevices = maxActivePosDevices;
        CustomerCreditEnabled = customerCreditEnabled;
        AdvancedReportsEnabled = advancedReportsEnabled;
        ExportEnabled = exportEnabled;
        TrialAllowed = trialAllowed;
        DefaultTrialDays = defaultTrialDays;
        SortOrder = sortOrder;
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        CurrencyCode = currencyCode;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Plan CreateDraft(
        ProductCode productCode,
        PlanCode code,
        string displayName,
        DateTimeOffset utcNow,
        PlanId? id = null,
        string? description = null,
        int maxBranches = 1,
        int maxActiveStaff = 3,
        int maxActivePosDevices = 1,
        bool customerCreditEnabled = false,
        bool advancedReportsEnabled = false,
        bool exportEnabled = false,
        bool trialAllowed = true,
        int defaultTrialDays = 14,
        int sortOrder = 100,
        decimal monthlyPrice = 0m,
        decimal annualPrice = 0m,
        string currencyCode = Payments.CurrencyCode.PHP)
    {
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(code);
        DomainTime.EnsureUtc(utcNow);
        ValidateCommercialLimits(maxBranches, maxActiveStaff, maxActivePosDevices, defaultTrialDays, sortOrder);
        ValidatePricing(monthlyPrice, annualPrice, currencyCode);
        return new Plan(
            id ?? PlanId.New(),
            productCode,
            code,
            DomainTime.NormalizeDisplayName(displayName),
            NormalizeDescription(description),
            PlanStatus.Draft,
            maxBranches,
            maxActiveStaff,
            maxActivePosDevices,
            customerCreditEnabled,
            advancedReportsEnabled,
            exportEnabled,
            trialAllowed,
            defaultTrialDays,
            sortOrder,
            monthlyPrice,
            annualPrice,
            Payments.CurrencyCode.Create(currencyCode).Value,
            utcNow,
            utcNow);
    }

    internal static Plan Rehydrate(
        PlanId id,
        ProductCode productCode,
        PlanCode code,
        string displayName,
        PlanStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? description = null,
        int maxBranches = 1,
        int maxActiveStaff = 3,
        int maxActivePosDevices = 1,
        bool customerCreditEnabled = false,
        bool advancedReportsEnabled = false,
        bool exportEnabled = false,
        bool trialAllowed = true,
        int defaultTrialDays = 14,
        int sortOrder = 100,
        decimal monthlyPrice = 0m,
        decimal annualPrice = 0m,
        string currencyCode = Payments.CurrencyCode.PHP) =>
        new(
            id,
            productCode,
            code,
            displayName,
            description,
            status,
            maxBranches,
            maxActiveStaff,
            maxActivePosDevices,
            customerCreditEnabled,
            advancedReportsEnabled,
            exportEnabled,
            trialAllowed,
            defaultTrialDays,
            sortOrder,
            monthlyPrice,
            annualPrice,
            currencyCode,
            createdAtUtc,
            updatedAtUtc);

    public void Rename(string displayName, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PlanStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "A retired Plan cannot be renamed.");
        }

        DisplayName = DomainTime.NormalizeDisplayName(displayName);
        UpdatedAtUtc = utcNow;
    }

    public void UpdateCommercialPackage(
        string? description,
        int maxBranches,
        int maxActiveStaff,
        int maxActivePosDevices,
        bool customerCreditEnabled,
        bool advancedReportsEnabled,
        bool exportEnabled,
        bool trialAllowed,
        int defaultTrialDays,
        int sortOrder,
        decimal monthlyPrice,
        decimal annualPrice,
        string currencyCode,
        DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PlanStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "A retired Plan cannot be edited.");
        }

        ValidateCommercialLimits(maxBranches, maxActiveStaff, maxActivePosDevices, defaultTrialDays, sortOrder);
        ValidatePricing(monthlyPrice, annualPrice, currencyCode);
        Description = NormalizeDescription(description);
        MaxBranches = maxBranches;
        MaxActiveStaff = maxActiveStaff;
        MaxActivePosDevices = maxActivePosDevices;
        CustomerCreditEnabled = customerCreditEnabled;
        AdvancedReportsEnabled = advancedReportsEnabled;
        ExportEnabled = exportEnabled;
        TrialAllowed = trialAllowed;
        DefaultTrialDays = defaultTrialDays;
        SortOrder = sortOrder;
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        CurrencyCode = Payments.CurrencyCode.Create(currencyCode).Value;
        UpdatedAtUtc = utcNow;
    }

    public decimal PriceForCycle(BillingCycle cycle) =>
        cycle switch
        {
            BillingCycle.Monthly => MonthlyPrice,
            BillingCycle.Annual => AnnualPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(cycle))
        };

    public void Activate(DateTimeOffset utcNow) => TransitionTo(PlanStatus.Active, utcNow);

    public void Deactivate(DateTimeOffset utcNow) => TransitionTo(PlanStatus.Inactive, utcNow);

    public void Retire(DateTimeOffset utcNow) => TransitionTo(PlanStatus.Retired, utcNow);

    /// <summary>True when the Plan may accept a new Organization Subscription.</summary>
    public bool AcceptsNewSubscriptions => Status == PlanStatus.Active;

    private void TransitionTo(PlanStatus target, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            PlanStatus.Draft => target is PlanStatus.Active or PlanStatus.Retired,
            PlanStatus.Active => target is PlanStatus.Inactive or PlanStatus.Retired,
            PlanStatus.Inactive => target is PlanStatus.Active or PlanStatus.Retired,
            PlanStatus.Retired => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                $"Cannot transition Plan from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private static void ValidateCommercialLimits(int maxBranches, int maxActiveStaff, int maxActivePosDevices, int defaultTrialDays, int sortOrder)
    {
        if (maxBranches < 1 || maxBranches > 10_000)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "MaxBranches must be between 1 and 10000.");
        }

        if (maxActiveStaff < 1 || maxActiveStaff > 100_000)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "MaxActiveStaff must be between 1 and 100000.");
        }

        if (maxActivePosDevices < 1 || maxActivePosDevices > 10_000)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "MaxActivePosDevices must be between 1 and 10000.");
        }

        if (defaultTrialDays < 0 || defaultTrialDays > 365)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "DefaultTrialDays must be between 0 and 365.");
        }

        if (sortOrder < 0 || sortOrder > 1_000_000)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "SortOrder must be between 0 and 1000000.");
        }
    }

    private static void ValidatePricing(decimal monthlyPrice, decimal annualPrice, string currencyCode)
    {
        if (monthlyPrice < 0 || annualPrice < 0)
        {
            throw new DomainException(
                DomainErrorCodes.PaymentAmountInvalid,
                "Plan prices must be greater than or equal to zero.");
        }

        Payments.CurrencyCode.Create(currencyCode);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        return trimmed.Length > 2000 ? trimmed[..2000] : trimmed;
    }
}
