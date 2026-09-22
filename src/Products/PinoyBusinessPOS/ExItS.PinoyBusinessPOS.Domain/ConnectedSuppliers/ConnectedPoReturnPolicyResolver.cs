using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Pure hierarchy resolver: product override → category override → organization default.
/// Does not touch persistence. Receiving-issue flows must not call this for voluntary gating.
/// </summary>
public static class ConnectedPoReturnPolicyResolver
{
    public const int DefaultReceivingIssueWindowDays = 2;

    public static EffectiveConnectedPoReturnPolicy Resolve(
        bool organizationReturnsAllowed,
        int? organizationReturnWindowDays,
        int organizationReceivingIssueWindowDays,
        bool organizationRequireReturnApproval,
        OrganizationConnectedCommerceCategoryReturnRule? categoryRule,
        ConnectedPoReturnPolicyMode productMode,
        bool? productReturnsAllowed,
        int? productReturnWindowDays)
    {
        NormalizeWindow(organizationReturnWindowDays, "Organization return window days");
        NormalizeReceivingIssueWindow(organizationReceivingIssueWindowDays);
        if (productMode == ConnectedPoReturnPolicyMode.Custom)
        {
            NormalizeWindow(productReturnWindowDays, "Product return window days");
        }

        if (productMode == ConnectedPoReturnPolicyMode.NonReturnable)
        {
            return new EffectiveConnectedPoReturnPolicy(
                ReturnsAllowed: false,
                ReturnWindowDays: null,
                ReceivingIssueWindowDays: organizationReceivingIssueWindowDays,
                RequireReturnApproval: organizationRequireReturnApproval,
                Source: ConnectedPoReturnPolicySource.Product);
        }

        if (productMode == ConnectedPoReturnPolicyMode.Custom)
        {
            return new EffectiveConnectedPoReturnPolicy(
                ReturnsAllowed: productReturnsAllowed ?? true,
                ReturnWindowDays: productReturnWindowDays,
                ReceivingIssueWindowDays: organizationReceivingIssueWindowDays,
                RequireReturnApproval: organizationRequireReturnApproval,
                Source: ConnectedPoReturnPolicySource.Product);
        }

        if (categoryRule is not null && categoryRule.Mode != ConnectedPoReturnPolicyMode.UseDefault)
        {
            if (categoryRule.Mode == ConnectedPoReturnPolicyMode.NonReturnable)
            {
                return new EffectiveConnectedPoReturnPolicy(
                    ReturnsAllowed: false,
                    ReturnWindowDays: null,
                    ReceivingIssueWindowDays: organizationReceivingIssueWindowDays,
                    RequireReturnApproval: organizationRequireReturnApproval,
                    Source: ConnectedPoReturnPolicySource.Category);
            }

            if (categoryRule.Mode == ConnectedPoReturnPolicyMode.Custom)
            {
                NormalizeWindow(categoryRule.ReturnWindowDays, "Category return window days");
                return new EffectiveConnectedPoReturnPolicy(
                    ReturnsAllowed: categoryRule.ReturnsAllowed ?? true,
                    ReturnWindowDays: categoryRule.ReturnWindowDays,
                    ReceivingIssueWindowDays: organizationReceivingIssueWindowDays,
                    RequireReturnApproval: organizationRequireReturnApproval,
                    Source: ConnectedPoReturnPolicySource.Category);
            }
        }

        return new EffectiveConnectedPoReturnPolicy(
            ReturnsAllowed: organizationReturnsAllowed,
            ReturnWindowDays: organizationReturnWindowDays,
            ReceivingIssueWindowDays: organizationReceivingIssueWindowDays,
            RequireReturnApproval: organizationRequireReturnApproval,
            Source: ConnectedPoReturnPolicySource.Organization);
    }

    public static EffectiveConnectedPoReturnPolicy ResolveFromSettings(
        OrganizationConnectedCommerceSettings settings,
        Guid? categoryId,
        ConnectedPoReturnPolicyMode productMode,
        bool? productReturnsAllowed,
        int? productReturnWindowDays)
    {
        OrganizationConnectedCommerceCategoryReturnRule? categoryRule = null;
        if (categoryId is Guid cid && cid != Guid.Empty)
        {
            categoryRule = settings.CategoryReturnRules.FirstOrDefault(r => r.CategoryId == cid);
        }

        return Resolve(
            settings.ReturnsAllowed,
            settings.ReturnWindowDays,
            settings.ReceivingIssueWindowDays,
            settings.RequireReturnApproval,
            categoryRule,
            productMode,
            productReturnsAllowed,
            productReturnWindowDays);
    }

    public static DateTimeOffset? ComputeReturnExpiresAtUtc(
        DateTimeOffset receivedAtUtc,
        int? returnWindowDays)
    {
        if (returnWindowDays is null)
        {
            return null;
        }

        if (returnWindowDays.Value < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnWindowDays,
                "Return window days cannot be negative.");
        }

        return receivedAtUtc.AddDays(returnWindowDays.Value);
    }

    public static bool IsWithinVoluntaryReturnWindow(
        EffectiveConnectedPoReturnPolicy policy,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset utcNow)
    {
        if (!policy.ReturnsAllowed)
        {
            return false;
        }

        var expires = ComputeReturnExpiresAtUtc(receivedAtUtc, policy.ReturnWindowDays);
        return expires is null || utcNow <= expires.Value;
    }

    private static void NormalizeWindow(int? days, string label)
    {
        if (days is < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnWindowDays,
                $"{label} cannot be negative.");
        }
    }

    private static void NormalizeReceivingIssueWindow(int days)
    {
        if (days < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueWindowDays,
                "Receiving issue window days cannot be negative.");
        }
    }
}
