using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Infrastructure.Authorization;
using ExItS.Platform.Infrastructure.GlobalCatalog;
using ExItS.Platform.Infrastructure.Identity;
using ExItS.Platform.Infrastructure.LocalValidation;
using ExItS.Platform.Infrastructure.Organizations;
using ExItS.Platform.Infrastructure.Persistence;
using ExItS.Platform.Infrastructure.Payments;
using ExItS.Platform.Infrastructure.Persistence.Repositories;
using ExItS.Platform.Infrastructure.PrivacyCompliance;
using ExItS.Platform.Infrastructure.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformPersistence(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("PlatformDatabase")
            ?? throw new InvalidOperationException("Connection string 'PlatformDatabase' is not configured.");

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IFeatureDefinitionRepository, FeatureDefinitionRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ITrialDefinitionRepository, TrialDefinitionRepository>();
        services.AddScoped<IBusinessTypeRepository, BusinessTypeRepository>();
        services.AddScoped<IGlobalCategoryRepository, GlobalCategoryRepository>();
        services.AddScoped<IGlobalProductRepository, GlobalProductRepository>();
        services.AddScoped<ICatalogTemplateRepository, CatalogTemplateRepository>();
        services.AddScoped<ICatalogImportJobRepository, CatalogImportJobRepository>();
        services.AddScoped<ICatalogImportFileParser, CatalogImportFileParser>();
        services.AddScoped<IPlatformOrganizationRepository, PlatformOrganizationRepository>();
        services.AddScoped<IOrganizationBusinessTypeActivationRepository, OrganizationBusinessTypeActivationRepository>();
        services.AddScoped<IOrganizationBranchRepository, OrganizationBranchRepository>();
        services.AddScoped<IPosDeviceRepository, PosDeviceRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISaaSPaymentRepository, SaaSPaymentRepository>();
        services.AddScoped<IProviderPaymentRepository, ProviderPaymentRepository>();
        services.AddScoped<IFeatureOverrideRepository, FeatureOverrideRepository>();
        services.AddScoped<IEntitlementSnapshotRepository, EntitlementSnapshotRepository>();
        services.AddScoped<IAdminPortfolioReadStore, AdminPortfolioReadStore>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IStaffNumberGenerator, EfStaffNumberGenerator>();
        services.AddScoped<IPublicUserIdGenerator, EfPublicUserIdGenerator>();
        services.AddScoped<IPublicOrganizationIdGenerator, EfPublicOrganizationIdGenerator>();
        services.AddScoped<IStaffLoginNameAllocator, EfStaffLoginNameAllocator>();
        services.AddScoped<IPlatformUserCredentialRepository, PlatformUserCredentialRepository>();
        services.AddScoped<IPlatformAuthSessionRepository, PlatformAuthSessionRepository>();
        services.AddScoped<IAccountProfileRepository, AccountProfileRepository>();
        services.AddScoped<IOrganizationContextPreferenceRepository, OrganizationContextPreferenceRepository>();
        services.AddScoped<IPlatformAccessTokenRepository, PlatformAccessTokenRepository>();
        services.AddScoped<IPlatformExternalLoginRepository, PlatformExternalLoginRepository>();
        services.AddScoped<IPlatformCredentialTokenRepository, PlatformCredentialTokenRepository>();
        services.AddSingleton<IPlatformPasswordHasher, AspNetCorePlatformPasswordHasher>();
        services.AddSingleton<IPlatformSessionTokenService, PlatformSessionTokenService>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IOrganizationInvitationRepository, OrganizationInvitationRepository>();
        services.AddScoped<IBusinessCustomerRepository, BusinessCustomerRepository>();
        services.AddScoped<ICreditCustomerRepository, CreditCustomerRepository>();
        services.AddScoped<ICustomerLinkRequestRepository, CustomerLinkRequestRepository>();
        services.AddScoped<ILinkedCustomerAppUserRepository, LinkedCustomerAppUserRepository>();
        services.AddScoped<IProductAccessAssignmentRepository, ProductAccessAssignmentRepository>();
        services.AddScoped<IPlatformRoleAssignmentRepository, PlatformRoleAssignmentRepository>();
        services.AddScoped<IPlatformRoleDefinitionRepository, PlatformRoleDefinitionRepository>();
        services.AddScoped<IPlatformCustomRoleAssignmentRepository, PlatformCustomRoleAssignmentRepository>();
        services.AddScoped<IOrganizationRoleDefinitionRepository, OrganizationRoleDefinitionRepository>();
        services.AddScoped<IOrganizationCustomRoleAssignmentRepository, OrganizationCustomRoleAssignmentRepository>();
        services.AddScoped<IAuditRecordRepository, AuditRecordRepository>();
        services.AddScoped<IComplianceRequirementRepository, ComplianceRequirementRepository>();
        services.AddScoped<IComplianceEvidenceRepository, ComplianceEvidenceRepository>();
        services.AddScoped<IProcessingSystemRepository, ProcessingSystemRepository>();
        services.AddScoped<IPrivacyCompliancePdfExporter, PrivacyCompliancePdfExporter>();
        services.AddScoped<IPersonalAccountSettingsRepository, PersonalAccountSettingsRepository>();
        services.AddScoped<IPersonalContactRepository, PersonalContactRepository>();
        services.AddScoped<IPersonalDebtRelationshipRepository, PersonalDebtRelationshipRepository>();
        services.AddScoped<IPersonalUtangEntryRepository, PersonalUtangEntryRepository>();
        services.AddScoped<IPersonalUtangInvitationRepository, PersonalUtangInvitationRepository>();
        services.AddScoped<IPersonalReminderRepository, PersonalReminderRepository>();
        services.AddScoped<IPersonalInAppNotificationRepository, PersonalInAppNotificationRepository>();
        services.AddScoped<IPersonalNotificationDeliveryRepository, PersonalNotificationDeliveryRepository>();
        services.AddScoped<IPersonalUtangMigrationBatchRepository, PersonalUtangMigrationBatchRepository>();
        services.AddScoped<IPersonalUtangMigrationItemRepository, PersonalUtangMigrationItemRepository>();
        services.AddScoped<IPersonalFeatureDefinitionRepository, PersonalFeatureDefinitionRepository>();
        services.AddScoped<IPersonalFeatureEntitlementRepository, PersonalFeatureEntitlementRepository>();
        services.AddScoped<IPersonalFeatureEntitlementService, PersonalFeatureEntitlementService>();
        services.AddScoped<IBusinessCreditOpeningBalanceRepository, BusinessCreditOpeningBalanceRepository>();
        services.AddScoped<IProductLocalRoleGrantRepository, ProductLocalRoleGrantRepository>();
        services.AddSingleton<IPersonalPushNotificationSink, NullPersonalPushNotificationSink>();
        services.AddScoped<IPlatformUnitOfWork, PlatformUnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IEntitlementRefreshPolicy, ProvisionalEntitlementRefreshPolicy>();
        services.AddHttpContextAccessor();
        services.AddScoped<IPlatformActorAccessor, DevelopmentPlatformActorAccessor>();
        services.AddScoped<IPlatformAuthorizationService, PlatformAuthorizationService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<ILocalValidationBaselinePurge, LocalValidationBaselinePurge>();

        services.AddScoped<MembershipStaffUsageReader>();
        services.AddScoped<OrganizationBranchUsageReader>();
        services.AddScoped<IOrganizationProductUsageReader, CompositeOrganizationProductUsageReader>();

        services.Configure<PlatformPasswordOptions>(config.GetSection(PlatformPasswordOptions.SectionName));
        services.Configure<PlatformLockoutOptions>(config.GetSection(PlatformLockoutOptions.SectionName));
        services.Configure<PlatformAuthBootstrapOptions>(config.GetSection(PlatformAuthBootstrapOptions.SectionName));
        services.Configure<PlatformSessionOptions>(config.GetSection(PlatformSessionOptions.SectionName));
        services.Configure<PlatformAccessTokenOptions>(config.GetSection(PlatformAccessTokenOptions.SectionName));
        services.Configure<PlatformCredentialLifecycleOptions>(config.GetSection(PlatformCredentialLifecycleOptions.SectionName));
        services.Configure<PlatformEmailDeliveryOptions>(config.GetSection(PlatformEmailDeliveryOptions.SectionName));
        services.Configure<PlatformMfaOptions>(config.GetSection(PlatformMfaOptions.SectionName));
        services.Configure<PlatformExternalAuthOptions>(config.GetSection(PlatformExternalAuthOptions.SectionName));
        services.AddSingleton<IPlatformMfaFactorStore, NullPlatformMfaFactorStore>();
        services.AddScoped<IPlatformMfaReadinessService, PlatformMfaReadinessService>();
        services.AddSingleton<IPlatformAuthOutboundMessageSink>(sp =>
        {
            var email = sp.GetRequiredService<IOptions<PlatformEmailDeliveryOptions>>().Value;
            if (email.IsConfigured)
            {
                return ActivatorUtilities.CreateInstance<SmtpPlatformAuthOutboundMessageSink>(sp);
            }

            return ActivatorUtilities.CreateInstance<NullPlatformAuthOutboundMessageSink>(sp);
        });

        return services;
    }
}
