using System.Reflection;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using NetArchTest.Rules;

namespace ExItS.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(ExItS.Platform.Domain.AssemblyMarker).Assembly;
    private static readonly Assembly Application = typeof(ExItS.Platform.Application.AssemblyMarker).Assembly;
    private static readonly Assembly Infrastructure = typeof(ExItS.Platform.Infrastructure.AssemblyMarker).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_Application_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ExItS.Platform.Application",
                "ExItS.Platform.Infrastructure",
                "ExItS.Platform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ExItS.Platform.Infrastructure",
                "ExItS.Platform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("ExItS.Platform.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Domain_has_no_AspNetCore_EfCore_or_Npgsql_dependencies()
    {
        var referenced = Domain.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain(referenced, name =>
            name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, name =>
            name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, name =>
            name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_assemblies_do_not_reference_HealthCare_or_AntDesign()
    {
        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name =>
                name.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(referenced, name =>
                name.Contains("AntDesign", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(referenced, name =>
                name.Contains("AntDesign.Components", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Platform_projects_do_not_reference_Tailwind_packages()
    {
        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name =>
                name.Contains("Tailwind", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Domain_does_not_contain_product_local_or_clinical_entity_type_names()
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            "Patient", "Clinic", "Store", "Branch", "Sale", "Inventory", "Customer",
            "Doctor", "Nurse", "Cashier", "StoreManager", "InventoryStaff",
            "MedicalNote", "RetailPayment", "CreditPayment", "GCashPayment", "GCashClient"
        };

        var typeNames = Domain.GetTypes().Select(t => t.Name).ToArray();
        foreach (var name in typeNames)
        {
            Assert.False(forbidden.Contains(name), $"Domain must not define type '{name}'.");
        }
    }

    [Fact]
    public void Domain_organization_roles_are_platform_only()
    {
        var roleNames = Enum.GetNames<OrganizationRole>();
        Assert.Contains(nameof(OrganizationRole.OrganizationOwner), roleNames);
        Assert.Contains(nameof(OrganizationRole.OrganizationAdministrator), roleNames);
        Assert.Contains(nameof(OrganizationRole.OrganizationMember), roleNames);
        Assert.Equal(3, roleNames.Length);
        Assert.DoesNotContain(roleNames, n => n.Contains("Doctor", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(roleNames, n => n.Contains("Cashier", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(roleNames, n => n.Contains("Patient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void No_generic_repository_abstraction_exists_in_Platform_assemblies()
    {
        var forbiddenNameFragments = new[]
        {
            "IRepository`1",
            "IGenericRepository`1",
            "IBaseRepository",
            "IRepository",
            "IGenericRepository"
        };

        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            foreach (var type in assembly.GetTypes())
            {
                var name = type.IsGenericType ? type.GetGenericTypeDefinition().Name : type.Name;
                Assert.DoesNotContain(forbiddenNameFragments, fragment =>
                    string.Equals(name, fragment, StringComparison.Ordinal)
                    || (fragment.EndsWith("`1", StringComparison.Ordinal)
                        && string.Equals(name, fragment, StringComparison.Ordinal)));

                // Explicit named generic repository patterns
                Assert.False(
                    type.IsInterface
                    && type.IsGenericType
                    && type.Name.StartsWith("IRepository", StringComparison.Ordinal),
                    $"Generic repository interface not allowed: {type.FullName}");
                Assert.False(
                    type.IsInterface
                    && type.Name is "IGenericRepository" or "IBaseRepository",
                    $"Generic repository interface not allowed: {type.FullName}");
            }
        }

        // Positive control: specific repositories exist
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Identity.IPlatformUserRepository"));
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Organizations.IPlatformOrganizationRepository"));
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Organizations.IOrganizationMembershipRepository"));
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Catalog.IProductRepository"));
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Subscriptions.ISubscriptionRepository"));
        Assert.NotNull(typeof(PlatformUser));
    }

    [Fact]
    public void Domain_has_no_retail_payment_or_gcash_implementation_types()
    {
        var typeNames = Domain.GetTypes().Select(t => t.Name).ToArray();
        Assert.DoesNotContain(typeNames, n => n.Contains("GCash", StringComparison.OrdinalIgnoreCase)
            && !n.Contains("CustomerCredit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeNames, n =>
            n is "RetailPayment" or "Invoice" or "PaymentProcessor" or "BillingInvoice");

        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name =>
                name.Contains("GCash", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(referenced, name =>
                name.Contains("PayMongo", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Published_plan_version_rejects_grant_mutation()
    {
        var utc = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("utang"),
            "Utang",
            utc);
        var version = PlanVersion.CreateDraft(
            plan,
            1,
            utc,
            BillingPeriod.Monthly,
            true,
            new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) },
            utc);
        version.Publish(utc);

        var ex = Assert.Throws<DomainException>(() =>
            version.ReplaceDraftGrants(
                new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), false) },
                utc.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.PlanVersionImmutable, ex.ErrorCode);
    }

    private static string Format(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Failing types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
