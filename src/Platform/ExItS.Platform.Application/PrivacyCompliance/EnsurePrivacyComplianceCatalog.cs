using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.PrivacyCompliance;

namespace ExItS.Platform.Application.PrivacyCompliance;

public sealed class EnsurePrivacyComplianceCatalog
{
    private readonly IComplianceRequirementRepository _requirements;
    private readonly IComplianceEvidenceRepository _evidence;
    private readonly IProcessingSystemRepository _systems;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public EnsurePrivacyComplianceCatalog(
        IComplianceRequirementRepository requirements,
        IComplianceEvidenceRepository evidence,
        IProcessingSystemRepository systems,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _requirements = requirements;
        _evidence = evidence;
        _systems = systems;
        _uow = uow;
        _clock = clock;
    }

    public async Task<EnsurePrivacyComplianceCatalogResultDto> ExecuteAsync(CancellationToken ct = default)
    {
        var utcNow = _clock.UtcNow;
        var requirementsAdded = 0;
        var systemsAdded = 0;
        var evidenceAdded = 0;

        foreach (var seed in RequirementSeeds)
        {
            if (await _requirements.GetByCodeAsync(seed.Code, ct).ConfigureAwait(false) is not null)
            {
                continue;
            }

            var requirement = ComplianceRequirement.Create(
                seed.Code,
                seed.Title,
                seed.Category,
                seed.Description,
                seed.RequirementLevel,
                seed.OwnerRole,
                utcNow,
                seed.Status,
                sourceReference: seed.SourceReference,
                requiresDpoLegalVerification: seed.RequiresDpoLegalVerification,
                id: seed.Id);
            await _requirements.AddAsync(requirement, ct).ConfigureAwait(false);
            requirementsAdded++;
        }

        foreach (var seed in SystemSeeds)
        {
            if (await _systems.GetByCodeAsync(seed.Code, ct).ConfigureAwait(false) is not null)
            {
                continue;
            }

            var system = ProcessingSystemRecord.Create(
                seed.Code,
                seed.SystemName,
                seed.Purpose,
                seed.DataSubjects,
                seed.PersonalDataCategories,
                seed.StorageLocation,
                seed.Owner,
                utcNow,
                seed.SensitiveDataCategories,
                seed.RecipientsProcessors,
                seed.RetentionSummary,
                seed.SecurityControls,
                seed.PiaStatus,
                seed.Id);
            await _systems.AddAsync(system, ct).ConfigureAwait(false);
            systemsAdded++;
        }

        foreach (var seed in EvidenceSeeds)
        {
            var requirement = await _requirements.GetByCodeAsync(seed.RequirementCode, ct).ConfigureAwait(false);
            if (requirement is null)
            {
                continue;
            }

            if (await _evidence.ExistsAsync(requirement.Id, seed.ReferencePath, ct).ConfigureAwait(false))
            {
                continue;
            }

            var reference = ComplianceEvidenceReference.Create(
                requirement.Id,
                seed.Kind,
                seed.Label,
                seed.ReferencePath,
                utcNow,
                seed.Notes,
                seed.Id);
            await _evidence.AddAsync(reference, ct).ConfigureAwait(false);
            evidenceAdded++;
        }

        if (requirementsAdded > 0 || systemsAdded > 0 || evidenceAdded > 0)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return new EnsurePrivacyComplianceCatalogResultDto(requirementsAdded, systemsAdded, evidenceAdded);
    }

    private sealed record RequirementSeed(
        Guid Id,
        string Code,
        string Title,
        ComplianceItemCategory Category,
        string Description,
        ComplianceRequirementLevel RequirementLevel,
        string OwnerRole,
        ComplianceItemStatus Status,
        bool RequiresDpoLegalVerification = false,
        string? SourceReference = null);

    private sealed record SystemSeed(
        Guid Id,
        string Code,
        string SystemName,
        string Purpose,
        string DataSubjects,
        string PersonalDataCategories,
        string StorageLocation,
        string Owner,
        string? SensitiveDataCategories = null,
        string? RecipientsProcessors = null,
        string? RetentionSummary = null,
        string? SecurityControls = null,
        ProcessingSystemPiaStatus PiaStatus = ProcessingSystemPiaStatus.NotStarted);

    private sealed record EvidenceSeed(
        Guid Id,
        string RequirementCode,
        ComplianceEvidenceKind Kind,
        string Label,
        string ReferencePath,
        string? Notes = null);

    private static readonly RequirementSeed[] RequirementSeeds =
    [
        new(Guid.Parse("00002101-0001-4000-8000-000000000001"), "PRIVACY_NOTICE", "Privacy Notice",
            ComplianceItemCategory.CustomerFacing,
            "Customer-facing privacy notice describing personal data collection, use, and rights.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002101-0002-4000-8000-000000000002"), "TERMS_OF_SERVICE", "Terms of Service",
            ComplianceItemCategory.CustomerFacing,
            "Customer-facing terms governing platform and product use.",
            ComplianceRequirementLevel.Required, "Legal / Compliance", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002101-0003-4000-8000-000000000003"), "CONSENT_NOTICES", "Consent Notices",
            ComplianceItemCategory.CustomerFacing,
            "Consent capture notices for optional processing activities.",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted),

        new(Guid.Parse("00002102-0001-4000-8000-000000000001"), "PRIVACY_MANUAL", "Privacy Manual",
            ComplianceItemCategory.Internal,
            "Internal privacy governance manual and operating procedures.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002102-0002-4000-8000-000000000002"), "PIA", "Privacy Impact Assessment Program",
            ComplianceItemCategory.Internal,
            "Privacy impact assessment process for new or changed processing activities.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress),
        new(Guid.Parse("00002102-0003-4000-8000-000000000003"), "DATA_INVENTORY_ROPA", "Data Inventory / ROPA",
            ComplianceItemCategory.Internal,
            "Record of processing activities and data inventory for platform systems.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress),
        new(Guid.Parse("00002102-0004-4000-8000-000000000004"), "RETENTION_DISPOSAL", "Retention and Disposal",
            ComplianceItemCategory.Internal,
            "Retention schedules and secure disposal procedures for personal data.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002102-0005-4000-8000-000000000005"), "DSAR_PROCEDURE", "DSAR Procedure",
            ComplianceItemCategory.Internal,
            "Data subject access request intake, verification, and fulfillment workflow.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002102-0006-4000-8000-000000000006"), "SECURITY_ACCESS_CONTROL", "Security and Access Control",
            ComplianceItemCategory.Internal,
            "Technical and organizational security controls including access management.",
            ComplianceRequirementLevel.Required, "Security Lead", ComplianceItemStatus.InProgress),
        new(Guid.Parse("00002102-0007-4000-8000-000000000007"), "INCIDENT_BREACH_RESPONSE", "Incident and Breach Response",
            ComplianceItemCategory.Internal,
            "Security incident and personal data breach response playbooks.",
            ComplianceRequirementLevel.Required, "Security Lead", ComplianceItemStatus.InProgress),
        new(Guid.Parse("00002102-0008-4000-8000-000000000008"), "BACKUP_BCP_PRIVACY", "Backup and BCP (Privacy)",
            ComplianceItemCategory.Internal,
            "Backup, recovery, and business continuity considerations for personal data.",
            ComplianceRequirementLevel.Required, "Operations Lead", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002102-0009-4000-8000-000000000009"), "VENDOR_PROCESSOR_REGISTER", "Vendor / Processor Register",
            ComplianceItemCategory.Internal,
            "Register of third-party processors and sub-processors handling personal data.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted),
        new(Guid.Parse("00002102-000a-4000-8000-00000000000a"), "DPO_APPOINTMENT", "DPO Appointment Record",
            ComplianceItemCategory.Internal,
            "Documentation of Data Protection Officer appointment and contact details.",
            ComplianceRequirementLevel.Conditional, "Executive Sponsor", ComplianceItemStatus.InProgress),

        new(Guid.Parse("00002103-0001-4000-8000-000000000001"), "DPO_REGISTRATION_READINESS", "DPO Registration Readiness",
            ComplianceItemCategory.RegulatoryReadiness,
            "Readiness checklist for NPC DPO registration (documentation only; not legal certification).",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002103-0002-4000-8000-000000000002"), "DPS_REGISTRATION_READINESS", "DPS Registration Readiness",
            ComplianceItemCategory.RegulatoryReadiness,
            "Readiness checklist for NPC data processing system registration.",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002103-0003-4000-8000-000000000003"), "NPC_CERTIFICATE_RECORDS", "NPC Certificate Records",
            ComplianceItemCategory.RegulatoryReadiness,
            "Storage of NPC registration certificates and related correspondence.",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002103-0004-4000-8000-000000000004"), "BREACH_REPORTING_RECORDS", "Breach Reporting Records",
            ComplianceItemCategory.RegulatoryReadiness,
            "Records of breach assessments and NPC reporting decisions (documentation only).",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002103-0005-4000-8000-000000000005"), "OTHER_NPC_SUBMISSIONS", "Other NPC Submissions",
            ComplianceItemCategory.RegulatoryReadiness,
            "Miscellaneous NPC submissions and regulatory correspondence.",
            ComplianceRequirementLevel.Optional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true)
    ];

    private static readonly SystemSeed[] SystemSeeds =
    [
        new(Guid.Parse("00002104-0001-4000-8000-000000000001"), "SYS_PLATFORM", "ExItS Platform Control Plane",
            "SaaS portfolio administration, subscriptions, entitlements, and audit.",
            "Platform administrators, organization owners, staff, customers",
            "Identity, contact, organization profile, subscription, audit metadata",
            "PostgreSQL (platform schema), application logs",
            "Platform Operations",
            SensitiveDataCategories: "Authentication credentials (hashed), recovery contact",
            RecipientsProcessors: "Cloud hosting provider, email delivery provider",
            RetentionSummary: "Per platform retention policy; audit records append-only",
            SecurityControls: "RBAC, session tokens, audit trail, encryption in transit"),
        new(Guid.Parse("00002104-0002-4000-8000-000000000002"), "SYS_PERSONAL_UTANG", "Personal Utang",
            "Personal debt tracking between individuals.",
            "Personal account holders, invited contacts",
            "Identity, contact, debt relationships, transaction history, reminders",
            "PostgreSQL (platform schema)",
            "Personal Product Owner",
            RecipientsProcessors: "Email/push notification providers (when configured)",
            RetentionSummary: "Until account closure or user deletion request",
            SecurityControls: "Account-scoped authorization, staff separation guards"),
        new(Guid.Parse("00002104-0003-4000-8000-000000000003"), "SYS_ORGANIZATION", "Organization Management",
            "Organization profiles, memberships, invitations, and customer linking.",
            "Organization owners, staff, business customers",
            "Identity, organization profile, membership roles, customer records",
            "PostgreSQL (platform schema)",
            "Organization Services Owner",
            RetentionSummary: "Organization lifetime plus audit retention",
            SecurityControls: "Organization-scoped RBAC, invitation tokens"),
        new(Guid.Parse("00002104-0004-4000-8000-000000000004"), "SYS_POS", "Pinoy Business POS",
            "Retail point-of-sale operations for merchant organizations.",
            "Cashiers, managers, customers (transaction context)",
            "Staff identity, sales transactions, inventory, payment metadata (no card PAN)",
            "PostgreSQL (product database), optional offline cache on device",
            "POS Product Owner",
            RecipientsProcessors: "Payment gateway (tokenized/simulated in development)",
            RetentionSummary: "Per merchant organization policy and legal requirements",
            SecurityControls: "Product-local roles, offline sync boundaries"),
        new(Guid.Parse("00002104-0005-4000-8000-000000000005"), "SYS_MAUI_OFFLINE", "MAUI Offline Client",
            "Mobile offline cache and sync for POS operations.",
            "Device operators, cached transaction data",
            "Cached catalog, cart, shift, and pending sync payloads",
            "Device-local encrypted storage (product-dependent)",
            "POS Product Owner",
            RetentionSummary: "Until successful sync or explicit purge",
            SecurityControls: "Device session, local validation guards in development"),
        new(Guid.Parse("00002104-0006-4000-8000-000000000006"), "SYS_AUTH_IDENTITY", "Authentication and Identity",
            "Credential, session, MFA readiness, and external login linkage.",
            "All platform and product users",
            "Credentials (hashed), sessions, tokens, external login identifiers",
            "PostgreSQL (platform schema)",
            "Identity Platform Owner",
            SensitiveDataCategories: "Password hashes, recovery tokens",
            SecurityControls: "Lockout, step-up, token rotation, fail-closed production guards"),
        new(Guid.Parse("00002104-0007-4000-8000-000000000007"), "SYS_FUTURE_INTEGRATIONS", "Future Integrations",
            "Placeholder registry entry for planned external integrations.",
            "TBD per integration",
            "TBD per integration contract",
            "TBD",
            "Platform Architecture",
            PiaStatus: ProcessingSystemPiaStatus.NotApplicable)
    ];

    private static readonly EvidenceSeed[] EvidenceSeeds =
    [
        new(Guid.Parse("00002105-0001-4000-8000-000000000001"), "PIA", ComplianceEvidenceKind.PhaseDoc,
            "Phase 16 — Account Profiles and Personal Utang",
            "docs/phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md",
            "Phase 16 scope for isolated account profiles and personal data boundaries."),
        new(Guid.Parse("00002105-0002-4000-8000-000000000002"), "INCIDENT_BREACH_RESPONSE", ComplianceEvidenceKind.PhaseDoc,
            "Phase 19 — Mobile POS Operations",
            "docs/phases/phase-19-mobile-pos-operations-and-cashier-experience.md",
            "Operational context for POS incident handling documentation."),
        new(Guid.Parse("00002105-0003-4000-8000-000000000003"), "SECURITY_ACCESS_CONTROL", ComplianceEvidenceKind.SecurityControl,
            "P9-WP01 — Security and Privacy Hardening",
            "docs/reports/P9-WP01-security-and-privacy-hardening.md",
            "Security and privacy hardening baseline report."),
        new(Guid.Parse("00002105-0004-4000-8000-000000000004"), "DPO_APPOINTMENT", ComplianceEvidenceKind.Implementation,
            "Org-Scoped Staff Identity",
            "docs/reports/P16-WP11-organization-staff-onboarding-invite.md",
            "Organization-scoped staff identity and onboarding boundaries."),
        new(Guid.Parse("00002105-0005-4000-8000-000000000005"), "DATA_INVENTORY_ROPA", ComplianceEvidenceKind.ArchitectureDoc,
            "Support Diagnostics and Production Architecture",
            "docs/engineering/production-deployment-architecture.md",
            "Production deployment architecture and support diagnostics reference.")
    ];
}
