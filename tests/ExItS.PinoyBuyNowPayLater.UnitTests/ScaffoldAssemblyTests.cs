using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Domain;
using ExItS.PinoyBuyNowPayLater.Domain.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Customers;

namespace ExItS.PinoyBuyNowPayLater.UnitTests;

public sealed class ScaffoldAssemblyTests
{
    [Fact]
    public void Domain_and_application_assemblies_load()
    {
        Assert.Equal("ExItS.PinoyBuyNowPayLater.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
        Assert.Equal("ExItS.PinoyBuyNowPayLater.Application", typeof(ApplicationAssembly).Assembly.GetName().Name);
    }

    [Fact]
    public void Product_identity_literal_matches_platform_product_code_value()
    {
        Assert.Equal("pinoy-buy-now-pay-later", BnplProductIdentity.ProductCode);
        Assert.True(BnplProductIdentity.IsPinoyBuyNowPayLater("Pinoy-Buy-Now-Pay-Later"));
    }

    [Fact]
    public void Scaffold_has_customer_foundation_without_financing_domain_types()
    {
        var domain = typeof(DomainAssembly).Assembly;
        var typeNames = domain.GetTypes().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(nameof(BnplCustomer), typeNames);
        Assert.Contains("BnplFinancingApplication", typeNames);
        Assert.DoesNotContain("FinancingPlan", typeNames);
        Assert.DoesNotContain("Installment", typeNames);
        Assert.DoesNotContain("Repayment", typeNames);
        Assert.DoesNotContain("Settlement", typeNames);
        Assert.False(Enum.GetNames(typeof(ExItS.PinoyBuyNowPayLater.Domain.Financing.BnplFinancingApplicationStatus))
            .Contains("Active", StringComparer.Ordinal));
    }
}
