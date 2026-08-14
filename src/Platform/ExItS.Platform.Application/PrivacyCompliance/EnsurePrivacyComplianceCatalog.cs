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
            RequiresDpoLegalVerification: true),

        // P21-WP11 — Post-Phase-21 privacy delta (P25/P26). Status never Approved; DPO/legal review required.
        new(Guid.Parse("00002111-0001-4000-8000-000000000001"), "PIA_P25_TYPED_QR",
            "PIA — Typed Personal / Business / Device QR",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for typed QR resolution (Personal, Business, POS device registration). LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0001-4000-8000-000000000002"), "PIA_P25_OWNERSHIP_TRANSFER",
            "PIA — Organization Ownership Transfer",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for ownership handoff, access revocation, and historical actor retention. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0001-4000-8000-000000000003"), "PIA_P25_BUYER_PARTY",
            "PIA — Buyer-Party Personal/Organization Linking",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for seller-owned customer records linked to Personal or Organization identities. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0001-4000-8000-000000000004"), "PIA_P26_SALES_DOC_EDUCATION",
            "PIA — Sales-Document Education Acknowledgment",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for organization/Owner education acknowledgment (product education, not legal certification). LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0001-4000-8000-000000000005"), "PIA_P26_COMPLIANCE_ELIGIBILITY",
            "PIA — Organization Compliance Eligibility Review",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for Platform-controlled compliance eligibility lifecycle and reviewer access. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0001-4000-8000-000000000006"), "PIA_P26_COMPLIANCE_PROFILE",
            "PIA — Organization Compliance Profile",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for organization-scoped compliance profile anchor and future confirmed fields. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0001-4000-8000-000000000007"), "PIA_P26_FUTURE_EVIDENCE",
            "PIA — Future Compliance Evidence Intake",
            ComplianceItemCategory.PrivacyImpactAssessment,
            "Privacy impact for future regulatory evidence uploads (architecture only until implemented). LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/bir-compliance-activation-roadmap.md"),
        new(Guid.Parse("00002111-0002-4000-8000-000000000001"), "DATA_INV_ORG_IDENTITY_MEMBERSHIP",
            "Data Inventory — Organization Identity & Membership",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory for PublicOrganizationId, memberships, multi-org ownership. LEGAL/DPO REVIEW REQUIRED for lawful basis.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true,
            SourceReference: "docs/compliance/post-phase21-privacy-impact-refresh.md"),
        new(Guid.Parse("00002111-0002-4000-8000-000000000002"), "DATA_INV_OWNERSHIP_TRANSFER",
            "Data Inventory — Ownership Transfer",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory for ownership transfer records (handoff + audit/security). LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0002-4000-8000-000000000003"), "DATA_INV_BUYER_PARTY",
            "Data Inventory — Buyer-Party Identity Linking",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory for seller-owned buyer/customer linking. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0002-4000-8000-000000000004"), "DATA_INV_SALES_DOC_ACK",
            "Data Inventory — Sales-Document Education Acknowledgment",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory for OrganizationId+UserId+version+timestamp acknowledgments. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0002-4000-8000-000000000005"), "DATA_INV_COMPLIANCE_ELIGIBILITY",
            "Data Inventory — Compliance Eligibility Review",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory for organization compliance eligibility and Platform reviewer actions. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0002-4000-8000-000000000006"), "DATA_INV_COMPLIANCE_PROFILE",
            "Data Inventory — Organization Compliance Profile",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory for compliance profile anchor and future confirmed regulatory fields. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0002-4000-8000-000000000007"), "DATA_INV_FUTURE_COMPLIANCE_EVIDENCE",
            "Data Inventory — Future Compliance Evidence Intake",
            ComplianceItemCategory.DataInventory,
            "ROPA-style inventory placeholder for future evidence files. LEGAL/DPO REVIEW REQUIRED before production use.",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0003-4000-8000-000000000001"), "RETENTION_P25_P26_DELTA",
            "Retention — P25/P26 Processing Delta",
            ComplianceItemCategory.Retention,
            "Retention categories for ownership transfer, acknowledgments, compliance review/history/profile, future evidence, reviewer notes. RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION. No destructive purge from guessed periods.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0004-4000-8000-000000000001"), "INCIDENT_P25_P26_SCENARIOS",
            "Incident Taxonomy — P25/P26 Identity & Compliance Scenarios",
            ComplianceItemCategory.IncidentBreach,
            "Incident/breach readiness scenarios: QR enumeration, cross-org access, former-owner stale auth, compliance evidence exposure, reviewer compromise, offline device compromise. LEGAL/DPO REVIEW REQUIRED for notification duties.",
            ComplianceRequirementLevel.Required, "Security Lead", ComplianceItemStatus.InProgress,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0005-4000-8000-000000000001"), "PRIVACY_NOTICE_P25_P26_DRAFT",
            "Privacy Notice Draft — P25/P26 Processing",
            ComplianceItemCategory.CustomerFacing,
            "DRAFT — LEGAL/DPO REVIEW REQUIRED. Technical description of multi-org identity, QR lookup, ownership transfer, buyer linking, and compliance review processing for notice updates. Not final legal wording.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0006-4000-8000-000000000001"), "PIC_PIP_ROLE_CLASSIFICATION",
            "PIC/PIP Role Classification (Activity-Scoped)",
            ComplianceItemCategory.DpoNpc,
            "LEGAL/DPO CLASSIFICATION REQUIRED. ExItS role may differ by activity (platform account processing, organization-controlled business data, Platform compliance review, support diagnostics). Do not hard-code one global legal conclusion.",
            ComplianceRequirementLevel.Required, "Data Protection Officer", ComplianceItemStatus.NotStarted,
            RequiresDpoLegalVerification: true),
        new(Guid.Parse("00002111-0007-4000-8000-000000000001"), "VENDOR_FUTURE_EVIDENCE_STORAGE",
            "Vendor Register — Future Compliance Evidence Storage",
            ComplianceItemCategory.VendorProcessor,
            "Before production evidence storage, the chosen storage/email/support vendor must be added to the processor register. Do not invent vendors. LEGAL/DPO REVIEW REQUIRED.",
            ComplianceRequirementLevel.Conditional, "Data Protection Officer", ComplianceItemStatus.NotStarted,
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
            PiaStatus: ProcessingSystemPiaStatus.NotApplicable),

        new(Guid.Parse("00002111-0010-4000-8000-000000000001"), "SYS_TYPED_QR_IDENTITY",
            "Typed Public Identity QR Resolution",
            "Resolve Personal QR, Business QR, and POS device-registration QR for typed purposes only.",
            "Personal users; organization members; device onboarding actors",
            "PUBLIC BUSINESS IDENTITY (display name, PublicOrganizationId); PERSONAL ACCOUNT DATA (display name, PublicUserId, minimal status); device onboarding tokens",
            "PostgreSQL (platform schema); no public exposure of membership, TIN, compliance, or sales",
            "Identity Platform Owner",
            SensitiveDataCategories: "None in public payload; masked email only where self-resolution allows",
            RecipientsProcessors: "None beyond authenticated platform clients",
            RetentionSummary: "RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION; public IDs are durable identifiers",
            SecurityControls: "Typed purpose guards; org/user isolation tests; minimal DTOs",
            PiaStatus: ProcessingSystemPiaStatus.InProgress),
        new(Guid.Parse("00002111-0010-4000-8000-000000000002"), "SYS_OWNERSHIP_TRANSFER",
            "Organization Ownership Transfer",
            "Ownership handoff and audit/security for organization control plane.",
            "Current Organization Owner; recipient Personal user",
            "ORGANIZATION INTERNAL DATA (org id, transfer status, actor user ids, timestamps); not Personal profile contents",
            "PostgreSQL (platform.organization_ownership_transfers)",
            "Organization Services Owner",
            RetentionSummary: "RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION; historical ActorUserId retained for audit",
            SecurityControls: "Owner-only initiate; Personal QR target; accept revokes former Owner access; no Personal data copy",
            PiaStatus: ProcessingSystemPiaStatus.InProgress),
        new(Guid.Parse("00002111-0010-4000-8000-000000000003"), "SYS_BUYER_PARTY_LINKING",
            "Buyer-Party Identity Linking",
            "Seller Organization owns Customer records that may optionally link Personal or Organization ExItS identities.",
            "Business customers; linked Personal users; linked Organizations",
            "TRANSACTIONAL DATA (customer record, link status); limited identity references — not full Personal profile dumps",
            "PostgreSQL (platform + POS sales buyer snapshots)",
            "POS / Organization Services",
            RetentionSummary: "Organization business record lifetime; DSAR must not destroy accounting records solely because a user leaves — LEGAL/DPO REVIEW REQUIRED",
            SecurityControls: "Seller-org ownership; typed QR; no silent merge by contact; buyer != transaction owner",
            PiaStatus: ProcessingSystemPiaStatus.InProgress),
        new(Guid.Parse("00002111-0010-4000-8000-000000000004"), "SYS_SALES_DOC_EDUCATION",
            "Sales-Document Education Acknowledgment",
            "Record that the current Organization Owner reviewed current ExItS sales-document behavior.",
            "Organization Owners",
            "ORGANIZATION INTERNAL / PRODUCT EDUCATION: OrganizationId, UserId, version, timestamp",
            "PostgreSQL (platform.organization_sales_document_acknowledgments)",
            "Platform Compliance Readiness",
            RetentionSummary: "RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION; version history retained",
            SecurityControls: "Exact current Owner only; no IP/device fingerprint/GPS; never enables TaxDocument",
            PiaStatus: ProcessingSystemPiaStatus.InProgress),
        new(Guid.Parse("00002111-0010-4000-8000-000000000005"), "SYS_ORG_COMPLIANCE_ELIGIBILITY",
            "Organization Compliance Eligibility Review",
            "Internal Platform review/authorization workflow for future tax-document capability eligibility.",
            "Organization Owners (status); Platform administrators (manage)",
            "RESTRICTED COMPLIANCE DATA: eligibility status, issuance capability flag, actor references, audit events",
            "PostgreSQL (platform.organization_sales_document_capabilities); Platform audit",
            "Platform Compliance Operations",
            SensitiveDataCategories: "Internal reviewer actions; not public",
            RetentionSummary: "RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION",
            SecurityControls: "ManageOrganizations for transitions; Owner request only; cashiers see no review details; public QR exposes nothing",
            PiaStatus: ProcessingSystemPiaStatus.InProgress),
        new(Guid.Parse("00002111-0010-4000-8000-000000000006"), "SYS_ORG_COMPLIANCE_PROFILE",
            "Organization Compliance Profile",
            "Organization-scoped compliance profile anchor for future confirmed tax/compliance readiness fields.",
            "Organization Owners (limited view); Platform administrators",
            "RESTRICTED COMPLIANCE DATA: profile timestamps/actor; registered business identity currently from OrganizationProfile — no invented TIN/BIR fields",
            "PostgreSQL (platform.organization_compliance_profiles)",
            "Platform Compliance Operations",
            RetentionSummary: "Organization lifetime; RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION for future sensitive fields",
            SecurityControls: "Org-scoped; never auto-copy from Personal profile; not Public QR data",
            PiaStatus: ProcessingSystemPiaStatus.InProgress),
        new(Guid.Parse("00002111-0010-4000-8000-000000000007"), "SYS_FUTURE_COMPLIANCE_EVIDENCE",
            "Future Compliance Evidence Intake",
            "Future private storage for organization eligibility verification evidence (not implemented).",
            "Organization Owners (submit when built); Platform authorized reviewers",
            "HIGHER-SENSITIVITY: future government references, addresses, signatures, taxpayer identifiers, uploaded files — collect only when confirmed required",
            "TBD private object storage — must be registered as processor before production use",
            "Platform Compliance Operations",
            SensitiveDataCategories: "Uploaded evidence contents; reviewer notes",
            RecipientsProcessors: "VENDOR_FUTURE_EVIDENCE_STORAGE — add real vendor before production",
            RetentionSummary: "RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION; deletion/disposal process required before production",
            SecurityControls: "No public URLs; access-controlled; malware/type validation when implemented; no cashier/default staff access; no AI use unless explicitly authorized",
            PiaStatus: ProcessingSystemPiaStatus.NotStarted)
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
            "Production deployment architecture and support diagnostics reference."),

        new(Guid.Parse("00002111-0020-4000-8000-000000000001"), "PIA_P25_TYPED_QR", ComplianceEvidenceKind.Report,
            "P21-WP11 Post-Phase-21 Privacy Impact Refresh",
            "docs/compliance/post-phase21-privacy-impact-refresh.md",
            "P25/P26 privacy delta summary — readiness documentation only; not legal certification."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000002"), "DATA_INVENTORY_ROPA", ComplianceEvidenceKind.Report,
            "P21-WP11 Privacy Impact Refresh Report",
            "docs/reports/P21-WP11-post-phase21-privacy-impact-refresh.md",
            "Work-package report for catalog and documentation updates."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000003"), "SECURITY_ACCESS_CONTROL", ComplianceEvidenceKind.Test,
            "Organization Public Identity Isolation Tests",
            "tests/ExItS.Platform.UnitTests/Organizations/OrganizationPublicIdentityIsolationTests.cs",
            "Technical evidence: public org identity leak prevention."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000004"), "SECURITY_ACCESS_CONTROL", ComplianceEvidenceKind.Test,
            "Post-Phase-21 Public Identity Privacy Guards",
            "tests/ExItS.Platform.UnitTests/PrivacyCompliance/PostPhase21PublicIdentityPrivacyGuardTests.cs",
            "Technical evidence: public DTOs exclude compliance/TIN/ack fields."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000005"), "PIA_P25_OWNERSHIP_TRANSFER", ComplianceEvidenceKind.Report,
            "P25-WP09 Organization Ownership Transfer",
            "docs/reports/P25-WP09-organization-ownership-transfer.md",
            "Ownership transfer feature report — Privacy Impact cross-reference."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000006"), "PIA_P25_BUYER_PARTY", ComplianceEvidenceKind.Report,
            "P25-WP07 Sales Buyer-Party Isolation",
            "docs/reports/P25-WP07-sales-buyer-party-isolation.md",
            "Buyer-party isolation report — Privacy Impact cross-reference."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000007"), "PIA_P26_SALES_DOC_EDUCATION", ComplianceEvidenceKind.Report,
            "P26-WP02 Sales-Document Education Acknowledgment",
            "docs/reports/P26-WP02-organization-compliance-education-and-acknowledgment.md",
            "Education acknowledgment report — Privacy Impact cross-reference."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000008"), "PIA_P26_COMPLIANCE_ELIGIBILITY", ComplianceEvidenceKind.Report,
            "P26-WP03 Platform Compliance Eligibility",
            "docs/reports/P26-WP03-platform-controlled-compliance-capability-and-eligibility.md",
            "Eligibility lifecycle report — Privacy Impact cross-reference."),
        new(Guid.Parse("00002111-0020-4000-8000-000000000009"), "PIA_P26_COMPLIANCE_PROFILE", ComplianceEvidenceKind.Report,
            "P26-WP04 Organization Compliance Profile",
            "docs/reports/P26-WP04-organization-tax-compliance-profile-and-activation-foundation.md",
            "Compliance profile foundation report — Privacy Impact cross-reference."),
        new(Guid.Parse("00002111-0020-4000-8000-00000000000a"), "PIA_P26_FUTURE_EVIDENCE", ComplianceEvidenceKind.ArchitectureDoc,
            "BIR Compliance Activation Roadmap — Privacy/Data Handling",
            "docs/compliance/bir-compliance-activation-roadmap.md",
            "Future evidence privacy principles; uploads not implemented."),
        new(Guid.Parse("00002111-0020-4000-8000-00000000000b"), "INCIDENT_P25_P26_SCENARIOS", ComplianceEvidenceKind.Test,
            "Phase 26 Compliance Hardening Tests",
            "tests/ExItS.Platform.UnitTests/Organizations/Phase26SalesDocumentComplianceHardeningTests.cs",
            "Technical evidence: multi-org isolation and issuance gates.")
    ];
}
