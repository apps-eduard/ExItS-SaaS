using System.Reflection;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
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
            "MedicalNote", "RetailPayment", "CreditPayment", "GCashPayment", "GCashClient",
            "Appointment", "Amendment", "ClinicalRole", "Diagnosis", "Prescription"
        };

        foreach (var assembly in new[] { Domain, Application })
        {
            var typeNames = assembly.GetTypes().Select(t => t.Name).ToArray();
            foreach (var name in typeNames)
            {
                Assert.False(forbidden.Contains(name), $"{assembly.GetName().Name} must not define type '{name}'.");
            }
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
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Payments.ISaaSPaymentRepository"));
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Entitlements.IFeatureOverrideRepository"));
        Assert.NotNull(Application.GetType("ExItS.Platform.Application.Entitlements.IEntitlementSnapshotRepository"));
        Assert.NotNull(typeof(PlatformUser));
    }

    [Fact]
    public void Domain_and_Application_have_no_pos_retail_or_utang_payment_types()
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            "SalePayment", "CreditPayment", "UtangPayment", "RetailPayment"
        };

        foreach (var assembly in new[] { Domain, Application })
        {
            foreach (var type in assembly.GetTypes())
            {
                Assert.False(forbidden.Contains(type.Name), $"{assembly.GetName().Name} must not define type '{type.Name}'.");
            }
        }
    }

    [Fact]
    public void Platform_assemblies_have_no_payment_gateway_webhook_or_qrcode_types()
    {
        var forbiddenFragments = new[] { "PaymentGateway", "Webhook", "QrCode", "QRCode", "CardVault", "CardToken" };

        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var fragment in forbiddenFragments)
                {
                    Assert.False(
                        type.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase),
                        $"{assembly.GetName().Name} must not define type '{type.Name}' (matches forbidden fragment '{fragment}').");
                }
            }
        }
    }

    [Fact]
    public void Platform_assemblies_have_no_payment_provider_sdk_references()
    {
        var forbiddenReferenceFragments = new[]
        {
            "Stripe", "PayMongo", "Xendit", "Adyen", "Braintree", "PayPal", "Square", "GCash"
        };

        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            foreach (var fragment in forbiddenReferenceFragments)
            {
                Assert.DoesNotContain(referenced, name => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public void SaaS_payment_types_use_the_SaaSPayment_prefix()
    {
        var paymentNamespaceTypes = Domain.GetTypes()
            .Where(t => t.Namespace == "ExItS.Platform.Domain.Payments" && (t.IsPublic || t.IsNestedPublic))
            .ToArray();

        Assert.NotEmpty(paymentNamespaceTypes);
        foreach (var type in paymentNamespaceTypes.Where(t => t.Name != nameof(ExItS.Platform.Domain.Payments.CurrencyCode)))
        {
            Assert.StartsWith("SaaSPayment", type.Name, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AddManualSaaSPayments_migration_creates_only_the_saas_payments_table()
    {
        var root = FindRepositoryRoot();
        var migrationsDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Infrastructure", "Persistence", "Migrations");
        var migrationFile = Directory.GetFiles(migrationsDir, "*_AddManualSaaSPayments.cs").SingleOrDefault();
        Assert.NotNull(migrationFile);

        var migration = File.ReadAllText(migrationFile!);
        Assert.Contains("name: \"saas_payments\"", migration, StringComparison.Ordinal);
        Assert.Contains("ux_saas_payments_reference", migration, StringComparison.Ordinal);
        Assert.Contains("ck_saas_payments_positive_amount", migration, StringComparison.Ordinal);

        var forbiddenTableNames = new[]
        {
            "pos_payments", "utang_payments", "retail_payments", "invoices", "webhooks", "qr_codes", "patients", "payments"
        };
        foreach (var table in forbiddenTableNames)
        {
            Assert.DoesNotContain($"name: \"{table}\"", migration, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("Patient", migration, StringComparison.Ordinal);
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

    [Fact]
    public void Domain_has_no_product_specific_trial_duration_helpers()
    {
        var trialType = typeof(TrialDefinition);
        Assert.Null(trialType.GetMethod("CreatePinoyBusinessPosUtangTrial"));
        Assert.DoesNotContain(
            trialType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance),
            m => m.Name.Contains("ThreeMonth", StringComparison.OrdinalIgnoreCase)
                 || m.Name.Contains("Ninety", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_has_no_messaging_or_serialization_framework_dependencies()
    {
        foreach (var assembly in new[] { Domain, Application, Infrastructure, Api })
        {
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
            Assert.DoesNotContain(referenced, name =>
                name.Contains("MassTransit", StringComparison.OrdinalIgnoreCase)
                || name.Contains("RabbitMQ", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Kafka", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Azure.Messaging", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Rebus", StringComparison.OrdinalIgnoreCase)
                || name.Contains("protobuf", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Avro", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void HealthCare_adapter_interfaces_exist_without_DbContext_or_clinical_types()
    {
        Assert.NotNull(Application.GetType(
            "ExItS.Platform.Application.Integration.HealthCare.IHealthCareUserProjectionDelivery"));
        Assert.NotNull(Application.GetType(
            "ExItS.Platform.Application.Integration.HealthCare.IPlatformProjectionReconciliationService"));

        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            "Patient", "Doctor", "Nurse", "Appointment", "MedicalNote", "Amendment",
            "ClinicalRole", "ClinicPermission", "Diagnosis", "Prescription", "ClinicalAuditRecord",
            "DbContext"
        };

        foreach (var type in Application.GetTypes().Where(t =>
                     t.Namespace?.Contains("Integration.HealthCare", StringComparison.Ordinal) == true))
        {
            Assert.False(forbidden.Contains(type.Name), $"Unexpected type {type.Name}");
            foreach (var method in type.GetMethods())
            {
                foreach (var p in method.GetParameters())
                {
                    Assert.False(forbidden.Contains(p.ParameterType.Name),
                        $"{type.Name}.{method.Name} exposes {p.ParameterType.Name}");
                }
            }
        }
    }

    [Fact]
    public void Application_has_no_EfCore_or_Npgsql_dependencies()
    {
        var referenced = Application.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain(referenced, name =>
            name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referenced, name =>
            name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Only_Infrastructure_may_reference_EfCore_or_Npgsql_at_runtime()
    {
        var root = FindRepositoryRoot();

        var domainCsproj = ReadCsproj(root, "ExItS.Platform.Domain");
        var applicationCsproj = ReadCsproj(root, "ExItS.Platform.Application");
        var infrastructureCsproj = ReadCsproj(root, "ExItS.Platform.Infrastructure");
        var apiCsproj = ReadCsproj(root, "ExItS.Platform.Api");

        Assert.DoesNotContain("EntityFrameworkCore", domainCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", domainCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", applicationCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", applicationCsproj, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Microsoft.EntityFrameworkCore", infrastructureCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Npgsql.EntityFrameworkCore.PostgreSQL", infrastructureCsproj, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Microsoft.EntityFrameworkCore.Design", apiCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql.EntityFrameworkCore.PostgreSQL", apiCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "<PackageReference Include=\"Microsoft.EntityFrameworkCore\"",
            apiCsproj,
            StringComparison.OrdinalIgnoreCase);

        var apiSource = Directory.GetFiles(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText);
        Assert.DoesNotContain(apiSource, text => text.Contains("PlatformDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(apiSource, text => text.Contains("DbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void Program_does_not_call_Database_Migrate()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        Assert.DoesNotContain(".Migrate(", program, StringComparison.Ordinal);
        Assert.DoesNotContain(".MigrateAsync(", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_endpoints_are_under_platform_catalog_route_prefix()
    {
        var root = FindRepositoryRoot();
        var catalog = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Catalog", "CatalogEndpoints.cs"));
        Assert.Contains("/api/v1/platform/catalog", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/v1/platform/subscriptions", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/payments", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gcash", catalog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Api_has_no_payment_or_gcash_MapGet_routes()
    {
        var root = FindRepositoryRoot();
        var apiRoot = Path.Combine(root, "src", "Platform", "ExItS.Platform.Api");
        var sources = Directory.GetFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText)
            .ToArray();

        foreach (var source in sources)
        {
            Assert.DoesNotContain("MapGet(\"/payments", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapGet(\"/gcash", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPost(\"/payments", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPost(\"/gcash", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Subscription_and_organization_endpoints_are_under_platform_route_prefix()
    {
        var root = FindRepositoryRoot();
        var subscriptionEndpoints = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Subscriptions", "SubscriptionEndpoints.cs"));
        var organizationEndpoints = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Organizations", "OrganizationEndpoints.cs"));

        Assert.Contains("/api/v1/platform/subscriptions", subscriptionEndpoints, StringComparison.Ordinal);
        Assert.Contains(
            "/api/v1/platform/organizations/{organizationId:guid}/subscriptions",
            subscriptionEndpoints,
            StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/organizations", organizationEndpoints, StringComparison.Ordinal);

        foreach (var source in new[] { subscriptionEndpoints, organizationEndpoints })
        {
            Assert.DoesNotContain("MapGet(\"/payments", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPost(\"/payments", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapGet(\"/gcash", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MapPost(\"/gcash", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Hangfire.", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Src_never_uses_a_hardcoded_ninety_day_TimeSpan()
    {
        var root = FindRepositoryRoot();
        var srcFiles = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

        foreach (var file in srcFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("FromDays(90)", text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Cancelled_and_Expired_subscription_statuses_reject_every_transition(SubscriptionStatus terminalStatus)
    {
        var utc = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("utang-terminal"),
            "Utang Terminal",
            utc);
        plan.Activate(utc);
        var version = PlanVersion.CreateDraft(
            plan,
            1,
            utc,
            BillingPeriod.Monthly,
            true,
            new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) },
            utc);
        version.Publish(utc);

        var subscription = Subscription.ActivatePaid(
            PlatformOrganizationId.New(), plan, version, utc, utc.AddDays(30), utc);

        if (terminalStatus == SubscriptionStatus.Cancelled)
        {
            subscription.Cancel(utc.AddMinutes(1));
        }
        else
        {
            subscription.Expire(utc.AddMinutes(1));
        }

        Assert.Equal(terminalStatus, subscription.Status);
        Assert.Throws<DomainException>(() => subscription.ActivateFromTrial(utc, utc.AddDays(1), utc.AddMinutes(2)));
        Assert.Throws<DomainException>(() => subscription.EnterGracePeriod(utc.AddDays(60), utc.AddMinutes(2)));
        Assert.Throws<DomainException>(() => subscription.MarkPastDue(utc.AddMinutes(2)));
        Assert.Throws<DomainException>(() => subscription.Suspend(utc.AddMinutes(2)));
        Assert.Throws<DomainException>(() => subscription.Reactivate(utc.AddMinutes(2)));

        // Re-issuing the same terminal transition is a same-state no-op, not a rejected transition;
        // only the *other* terminal transition is actually disallowed from this state.
        if (terminalStatus == SubscriptionStatus.Cancelled)
        {
            Assert.Throws<DomainException>(() => subscription.Expire(utc.AddMinutes(2)));
        }
        else
        {
            Assert.Throws<DomainException>(() => subscription.Cancel(utc.AddMinutes(2)));
        }
    }

    [Fact]
    public void InitialPlatformCatalog_migration_creates_catalog_tables_only()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Platform",
            "ExItS.Platform.Infrastructure",
            "Persistence",
            "Migrations",
            "20260729171154_InitialPlatformCatalog.cs"));

        Assert.Contains("name: \"products\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"feature_definitions\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"plans\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"plan_versions\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"trial_definitions\"", migration, StringComparison.Ordinal);

        var forbiddenTableNames = new[] { "users", "organizations", "subscriptions", "payments", "patients" };
        foreach (var table in forbiddenTableNames)
        {
            Assert.DoesNotContain($"name: \"{table}\"", migration, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain($"CreateTable(\n                name: \"{table}\"", migration, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("Patient", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPlatformOrganizationsAndSubscriptions_migration_creates_only_organizations_and_subscriptions_tables()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Platform",
            "ExItS.Platform.Infrastructure",
            "Persistence",
            "Migrations",
            "20260729173841_AddPlatformOrganizationsAndSubscriptions.cs"));

        Assert.Contains("name: \"organizations\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"subscriptions\"", migration, StringComparison.Ordinal);
        Assert.Contains("ux_subscriptions_one_active_like", migration, StringComparison.Ordinal);

        var forbiddenTableNames = new[]
        {
            "users", "memberships", "payments", "invoices", "entitlement_snapshots",
            "hangfire", "gcash_clients", "patients"
        };
        foreach (var table in forbiddenTableNames)
        {
            Assert.DoesNotContain($"name: \"{table}\"", migration, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("Patient", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("Hangfire", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddEntitlementSnapshotsAndOverrides_migration_creates_only_the_expected_entitlement_tables()
    {
        var root = FindRepositoryRoot();
        var migrationsDir = Path.Combine(root, "src", "Platform", "ExItS.Platform.Infrastructure", "Persistence", "Migrations");
        var migrationFile = Directory.GetFiles(migrationsDir, "*_AddEntitlementSnapshotsAndOverrides.cs").SingleOrDefault();
        Assert.NotNull(migrationFile);

        var migration = File.ReadAllText(migrationFile!);
        Assert.Contains("name: \"feature_overrides\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"entitlement_snapshots\"", migration, StringComparison.Ordinal);
        Assert.Contains("name: \"entitlement_snapshot_grants\"", migration, StringComparison.Ordinal);
        Assert.Contains("ux_entitlement_snapshots_org_product_version", migration, StringComparison.Ordinal);
        Assert.Contains("ck_feature_overrides_expiry_range", migration, StringComparison.Ordinal);
        Assert.Contains("ck_feature_overrides_numeric_limit", migration, StringComparison.Ordinal);
        Assert.Contains("ck_entitlement_snapshots_version_positive", migration, StringComparison.Ordinal);
        Assert.Contains("ck_entitlement_snapshots_schema_positive", migration, StringComparison.Ordinal);
        Assert.Contains("ck_entitlement_snapshots_refresh_range", migration, StringComparison.Ordinal);
        Assert.Contains("ck_entitlement_snapshots_expiry_range", migration, StringComparison.Ordinal);

        // Down must correctly roll back all three new tables (validated live via Docker by the
        // parent workflow); statically verify the Down section names each one for a symmetric migration.
        var downIndex = migration.IndexOf("protected override void Down", StringComparison.Ordinal);
        Assert.True(downIndex >= 0);
        var downSection = migration[downIndex..];
        Assert.Contains("name: \"feature_overrides\"", downSection, StringComparison.Ordinal);
        Assert.Contains("name: \"entitlement_snapshots\"", downSection, StringComparison.Ordinal);
        Assert.Contains("name: \"entitlement_snapshot_grants\"", downSection, StringComparison.Ordinal);

        var forbiddenTableNames = new[]
        {
            "users", "memberships", "payments", "invoices", "hangfire", "gcash_clients", "patients"
        };
        foreach (var table in forbiddenTableNames)
        {
            Assert.DoesNotContain($"name: \"{table}\"", migration, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("Patient", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Api_exposes_catalog_subscription_and_manual_payment_endpoints()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Program.cs"));
        var catalog = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Catalog", "CatalogEndpoints.cs"));

        Assert.Contains("MapGet(\"/\"".Replace("\\", ""), program);
        Assert.Contains("/health", program);
        Assert.Contains("MapCatalogEndpoints", program);
        Assert.Contains("MapOrganizationEndpoints", program);
        Assert.Contains("MapIdentityEndpoints", program);
        Assert.Contains("MapMembershipEndpoints", program);
        Assert.Contains("MapAccessEndpoints", program);
        Assert.Contains("MapSubscriptionEndpoints", program);
        Assert.Contains("MapPaymentEndpoints", program);
        Assert.Contains("MapEntitlementEndpoints", program);
        Assert.Contains("MapAdminEndpoints", program);
        Assert.Contains("MapAuthorizationEndpoints", program);
        Assert.Contains("MapAuditEndpoints", program);
        Assert.Contains("P5-WP05", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("catalog", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subscription", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("payment", program, StringComparison.OrdinalIgnoreCase);

        // P3-WP05 closeout: Platform entitlement snapshot/feature-override persistence and
        // development-stage APIs remain; product-local projection, delivery, and migration-import
        // concerns remain out of scope.
        Assert.Contains("entitlement", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("projection", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapGet(\"/migration", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapGet(\"/mapping", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapGet(\"/import", program, StringComparison.OrdinalIgnoreCase);

        // Program.cs may legitimately mention "payment" (manual SaaS payment activation is in
        // scope for this phase) but must not pull in gateway/webhook/QR/card/invoice concerns.
        Assert.DoesNotContain("gcash", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("invoice", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hangfire", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gcashclient", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("webhook", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("qrcode", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gateway", program, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("card", program, StringComparison.OrdinalIgnoreCase);

        // Catalog remains entirely payment-free, matching P3-WP01 scope.
        Assert.DoesNotContain("payment", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gcash", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("invoice", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hangfire", catalog, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gcashclient", catalog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_validation_types_exist_without_persistence_or_sql_or_credentials()
    {
        Assert.NotNull(Application.GetType(
            "ExItS.Platform.Application.MigrationValidation.IMigrationPreflightValidator"));
        Assert.NotNull(Application.GetType(
            "ExItS.Platform.Application.MigrationValidation.IMigrationSimulationService"));
        Assert.NotNull(Application.GetType(
            "ExItS.Platform.Application.MigrationValidation.IRollbackReadinessValidator"));

        var forbiddenTypeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Patient", "Doctor", "Nurse", "MedicalNote", "Diagnosis", "Prescription",
            "DbContext", "MigrationDbContext", "IGenericMigrationRepository"
        };

        foreach (var type in Application.GetTypes().Where(t =>
                     t.Namespace?.Contains("MigrationValidation", StringComparison.Ordinal) == true))
        {
            Assert.False(forbiddenTypeNames.Contains(type.Name), $"Unexpected type {type.Name}");
            foreach (var prop in type.GetProperties())
            {
                Assert.DoesNotContain(
                    new[] { "Password", "PasswordHash", "RefreshToken", "MfaSecret", "Patient", "MedicalNote" },
                    f => prop.Name.Equals(f, StringComparison.OrdinalIgnoreCase)
                         || prop.Name.Contains(f, StringComparison.OrdinalIgnoreCase));
            }
        }

        var root = FindRepositoryRoot();
        var sqlHits = Directory.GetFiles(Path.Combine(root, "src"), "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                        || p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        // P7-WP01: approved Microsoft.Data.Sqlite foundation lives only in LocalStore.
                        && !p.Contains($"{Path.DirectorySeparatorChar}ExItS.PinoyBusinessPOS.LocalStore{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(p => File.ReadAllLines(p).Select((line, i) => (p, i, line)))
            .Where(x => x.line.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase)
                        || x.line.Contains("ALTER TABLE", StringComparison.OrdinalIgnoreCase)
                        || x.line.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToArray();

        Assert.Empty(sqlHits);
    }

    [Fact]
    public void Admin_project_is_isolated_from_infrastructure_and_forbidden_ui_frameworks()
    {
        var root = FindRepositoryRoot();
        var adminCsproj = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "ExItS.Platform.Admin.csproj"));
        Assert.DoesNotContain("ExItS.Platform.Infrastructure", adminCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", adminCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", adminCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AntDesign", adminCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tailwind", adminCsproj, StringComparison.OrdinalIgnoreCase);

        var adminSources = Directory.GetFiles(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText);
        Assert.DoesNotContain(adminSources, text => text.Contains("PlatformDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(adminSources, text => text.Contains("DbContext", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static string ReadCsproj(string root, string projectName) =>
        File.ReadAllText(Path.Combine(root, "src", "Platform", projectName, $"{projectName}.csproj"));

    private static string Format(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Failing types: " + string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>());
}
