using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

public enum ConnectedPoPaymentTiming
{
    PayBeforeFulfillment = 0,
    PayOnDeliveryOrReceipt = 1,
    SupplierCredit = 2
}

public sealed record OrganizationConnectedCommerceCategoryRule(Guid CategoryId, decimal DiscountPercent);

/// <summary>Relationship-level category discount override (absence = inherit).</summary>
public sealed record ConnectedCustomerCategoryDiscountOverride(Guid CategoryId, decimal DiscountPercent);

/// <summary>
/// Organization-level defaults for connected-commerce payment timing, B2B pricing,
/// proposal hold SLA, and voluntary connected-PO return policy.
/// </summary>
public sealed class OrganizationConnectedCommerceSettings
{
    public const int MinProposalReservationHoldHours = 1;
    public const int MaxProposalReservationHoldHours = 72;

    private OrganizationConnectedCommerceSettings(
        Guid settingId,
        PosOrganizationId organizationId,
        bool allowPayBeforeFulfillment,
        bool allowPayOnDeliveryOrReceipt,
        bool allowSupplierCredit,
        ConnectedPoPaymentTiming defaultPaymentTiming,
        decimal defaultB2bDiscountPercent,
        IReadOnlyList<OrganizationConnectedCommerceCategoryRule> categoryRules,
        int proposalReservationHoldHours,
        bool returnsAllowed,
        int? returnWindowDays,
        int receivingIssueWindowDays,
        bool requireReturnApproval,
        IReadOnlyList<OrganizationConnectedCommerceCategoryReturnRule> categoryReturnRules,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        SettingId = settingId;
        OrganizationId = organizationId;
        AllowPayBeforeFulfillment = allowPayBeforeFulfillment;
        AllowPayOnDeliveryOrReceipt = allowPayOnDeliveryOrReceipt;
        AllowSupplierCredit = allowSupplierCredit;
        DefaultPaymentTiming = defaultPaymentTiming;
        DefaultB2bDiscountPercent = NormalizeDiscount(defaultB2bDiscountPercent);
        CategoryRules = NormalizeCategoryRules(categoryRules);
        ProposalReservationHoldHours = NormalizeProposalReservationHoldHours(proposalReservationHoldHours);
        ReturnsAllowed = returnsAllowed;
        ReturnWindowDays = NormalizeOptionalWindowDays(returnWindowDays, "Return window days");
        ReceivingIssueWindowDays = NormalizeReceivingIssueWindowDays(receivingIssueWindowDays);
        RequireReturnApproval = requireReturnApproval;
        CategoryReturnRules = NormalizeCategoryReturnRules(categoryReturnRules);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        EnsureAtLeastOneAllowedPaymentTiming();
        EnsureDefaultPaymentTimingIsAllowed(defaultPaymentTiming);
    }

    public Guid SettingId { get; }
    public PosOrganizationId OrganizationId { get; }
    public bool AllowPayBeforeFulfillment { get; private set; }
    public bool AllowPayOnDeliveryOrReceipt { get; private set; }
    public bool AllowSupplierCredit { get; private set; }
    public ConnectedPoPaymentTiming DefaultPaymentTiming { get; private set; }
    public decimal DefaultB2bDiscountPercent { get; private set; }
    public IReadOnlyList<OrganizationConnectedCommerceCategoryRule> CategoryRules { get; private set; } = [];
    public int ProposalReservationHoldHours { get; private set; }

    /// <summary>When false, voluntary normal returns are disallowed at org default.</summary>
    public bool ReturnsAllowed { get; private set; }

    /// <summary>Null = unlimited voluntary return window after goods receipt.</summary>
    public int? ReturnWindowDays { get; private set; }

    /// <summary>
    /// Store-only SLA hint for post-receipt delivery-issue reporting.
    /// Never blocks Goods Receipt posting; discrepancies remain capturable at GRN.
    /// </summary>
    public int ReceivingIssueWindowDays { get; private set; }

    /// <summary>Informational; physical Connected PO ReturnBatch still awaits seller receipt.</summary>
    public bool RequireReturnApproval { get; private set; }

    public IReadOnlyList<OrganizationConnectedCommerceCategoryReturnRule> CategoryReturnRules { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static OrganizationConnectedCommerceSettings CreateDefault(
        PosOrganizationId organizationId,
        DateTimeOffset nowUtc) =>
        new(
            Guid.NewGuid(),
            organizationId,
            allowPayBeforeFulfillment: true,
            allowPayOnDeliveryOrReceipt: true,
            allowSupplierCredit: false,
            defaultPaymentTiming: ConnectedPoPaymentTiming.PayBeforeFulfillment,
            defaultB2bDiscountPercent: 0m,
            categoryRules: [],
            proposalReservationHoldHours: 24,
            returnsAllowed: true,
            returnWindowDays: null,
            receivingIssueWindowDays: ConnectedPoReturnPolicyResolver.DefaultReceivingIssueWindowDays,
            requireReturnApproval: true,
            categoryReturnRules: [],
            createdAtUtc: nowUtc,
            updatedAtUtc: nowUtc);

    public static OrganizationConnectedCommerceSettings Rehydrate(
        Guid settingId,
        PosOrganizationId organizationId,
        bool allowPayBeforeFulfillment,
        bool allowPayOnDeliveryOrReceipt,
        bool allowSupplierCredit,
        ConnectedPoPaymentTiming defaultPaymentTiming,
        decimal defaultB2bDiscountPercent,
        IReadOnlyList<OrganizationConnectedCommerceCategoryRule> categoryRules,
        int proposalReservationHoldHours,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        bool returnsAllowed = true,
        int? returnWindowDays = null,
        int receivingIssueWindowDays = ConnectedPoReturnPolicyResolver.DefaultReceivingIssueWindowDays,
        bool requireReturnApproval = true,
        IReadOnlyList<OrganizationConnectedCommerceCategoryReturnRule>? categoryReturnRules = null) =>
        new(
            settingId,
            organizationId,
            allowPayBeforeFulfillment,
            allowPayOnDeliveryOrReceipt,
            allowSupplierCredit,
            defaultPaymentTiming,
            defaultB2bDiscountPercent,
            categoryRules,
            proposalReservationHoldHours,
            returnsAllowed,
            returnWindowDays,
            receivingIssueWindowDays,
            requireReturnApproval,
            categoryReturnRules ?? [],
            createdAtUtc,
            updatedAtUtc);

    public void ConfigurePaymentTiming(
        bool allowPayBeforeFulfillment,
        bool allowPayOnDeliveryOrReceipt,
        bool allowSupplierCredit,
        ConnectedPoPaymentTiming defaultPaymentTiming,
        DateTimeOffset nowUtc)
    {
        AllowPayBeforeFulfillment = allowPayBeforeFulfillment;
        AllowPayOnDeliveryOrReceipt = allowPayOnDeliveryOrReceipt;
        AllowSupplierCredit = allowSupplierCredit;
        EnsureAtLeastOneAllowedPaymentTiming();
        EnsureDefaultPaymentTimingIsAllowed(defaultPaymentTiming);
        DefaultPaymentTiming = defaultPaymentTiming;
        UpdatedAtUtc = nowUtc;
    }

    public void ConfigurePricing(
        decimal defaultB2bDiscountPercent,
        IReadOnlyList<OrganizationConnectedCommerceCategoryRule> categoryRules,
        DateTimeOffset nowUtc)
    {
        DefaultB2bDiscountPercent = NormalizeDiscount(defaultB2bDiscountPercent);
        CategoryRules = NormalizeCategoryRules(categoryRules);
        UpdatedAtUtc = nowUtc;
    }

    public void SetProposalReservationHoldHours(int holdHours, DateTimeOffset nowUtc)
    {
        ProposalReservationHoldHours = NormalizeProposalReservationHoldHours(holdHours);
        UpdatedAtUtc = nowUtc;
    }

    public void ConfigureReturnPolicy(
        bool returnsAllowed,
        int? returnWindowDays,
        int receivingIssueWindowDays,
        bool requireReturnApproval,
        IReadOnlyList<OrganizationConnectedCommerceCategoryReturnRule> categoryReturnRules,
        DateTimeOffset nowUtc)
    {
        ReturnsAllowed = returnsAllowed;
        ReturnWindowDays = NormalizeOptionalWindowDays(returnWindowDays, "Return window days");
        ReceivingIssueWindowDays = NormalizeReceivingIssueWindowDays(receivingIssueWindowDays);
        RequireReturnApproval = requireReturnApproval;
        CategoryReturnRules = NormalizeCategoryReturnRules(categoryReturnRules);
        UpdatedAtUtc = nowUtc;
    }

    public bool IsAllowed(ConnectedPoPaymentTiming timing) =>
        timing switch
        {
            ConnectedPoPaymentTiming.PayBeforeFulfillment => AllowPayBeforeFulfillment,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt => AllowPayOnDeliveryOrReceipt,
            ConnectedPoPaymentTiming.SupplierCredit => AllowSupplierCredit,
            _ => false
        };

    public decimal? FindCategoryDiscountPercent(Guid? categoryId)
    {
        if (categoryId is null || categoryId == Guid.Empty)
        {
            return null;
        }

        var match = CategoryRules.FirstOrDefault(r => r.CategoryId == categoryId.Value);
        return match?.DiscountPercent;
    }

    public OrganizationConnectedCommerceCategoryReturnRule? FindCategoryReturnRule(Guid? categoryId)
    {
        if (categoryId is null || categoryId == Guid.Empty)
        {
            return null;
        }

        return CategoryReturnRules.FirstOrDefault(r => r.CategoryId == categoryId.Value);
    }

    private void EnsureAtLeastOneAllowedPaymentTiming()
    {
        if (!AllowPayBeforeFulfillment && !AllowPayOnDeliveryOrReceipt && !AllowSupplierCredit)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOffer,
                "At least one connected purchase-order payment timing must be allowed.");
        }
    }

    private void EnsureDefaultPaymentTimingIsAllowed(ConnectedPoPaymentTiming timing)
    {
        if (!IsAllowed(timing))
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOffer,
                "Default connected purchase-order payment timing must be one of the allowed timings.");
        }
    }

    private static int NormalizeProposalReservationHoldHours(int value)
    {
        if (value < MinProposalReservationHoldHours || value > MaxProposalReservationHoldHours)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOffer,
                $"Proposal reservation hold hours must be between {MinProposalReservationHoldHours} and {MaxProposalReservationHoldHours}.");
        }

        return value;
    }

    public static decimal NormalizeDiscount(decimal value)
    {
        var rounded = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        if (rounded < 0m || rounded > 100m)
        {
            throw new DomainException(
                ConnectedSupplierDomainErrorCodes.InvalidOffer,
                "Discount percent must be between 0 and 100.");
        }

        return rounded;
    }

    private static int? NormalizeOptionalWindowDays(int? days, string label)
    {
        if (days is < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnWindowDays,
                $"{label} cannot be negative.");
        }

        return days;
    }

    private static int NormalizeReceivingIssueWindowDays(int days)
    {
        if (days < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueWindowDays,
                "Receiving issue window days cannot be negative.");
        }

        return days;
    }

    private static IReadOnlyList<OrganizationConnectedCommerceCategoryRule> NormalizeCategoryRules(
        IReadOnlyList<OrganizationConnectedCommerceCategoryRule> rules)
    {
        var normalized = new Dictionary<Guid, OrganizationConnectedCommerceCategoryRule>();
        foreach (var rule in rules ?? [])
        {
            if (rule.CategoryId == Guid.Empty)
            {
                throw new DomainException(
                    ConnectedSupplierDomainErrorCodes.InvalidOffer,
                    "Category rule requires a category id.");
            }

            var next = new OrganizationConnectedCommerceCategoryRule(
                rule.CategoryId,
                NormalizeDiscount(rule.DiscountPercent));
            if (!normalized.TryAdd(rule.CategoryId, next))
            {
                throw new DomainException(
                    ConnectedSupplierDomainErrorCodes.InvalidOffer,
                    "Duplicate category discount rules are not allowed.");
            }
        }

        return normalized.Values.OrderBy(x => x.CategoryId).ToArray();
    }

    private static IReadOnlyList<OrganizationConnectedCommerceCategoryReturnRule> NormalizeCategoryReturnRules(
        IReadOnlyList<OrganizationConnectedCommerceCategoryReturnRule> rules)
    {
        var normalized = new Dictionary<Guid, OrganizationConnectedCommerceCategoryReturnRule>();
        foreach (var rule in rules ?? [])
        {
            if (rule.CategoryId == Guid.Empty)
            {
                throw new DomainException(
                    ConnectedSupplierDomainErrorCodes.InvalidOffer,
                    "Category return rule requires a category id.");
            }

            if (!Enum.IsDefined(rule.Mode))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidConnectedPoReturnPolicyMode,
                    "Invalid category return policy mode.");
            }

            if (rule.Mode == ConnectedPoReturnPolicyMode.Custom)
            {
                NormalizeOptionalWindowDays(rule.ReturnWindowDays, "Category return window days");
            }

            var next = new OrganizationConnectedCommerceCategoryReturnRule(
                rule.CategoryId,
                rule.Mode,
                rule.Mode == ConnectedPoReturnPolicyMode.Custom ? rule.ReturnsAllowed : null,
                rule.Mode == ConnectedPoReturnPolicyMode.Custom ? rule.ReturnWindowDays : null);
            if (!normalized.TryAdd(rule.CategoryId, next))
            {
                throw new DomainException(
                    ConnectedSupplierDomainErrorCodes.InvalidOffer,
                    "Duplicate category return rules are not allowed.");
            }
        }

        return normalized.Values.OrderBy(x => x.CategoryId).ToArray();
    }
}
