using ExItS.PinoyBusinessPOS.Domain.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Privacy;

/// <summary>
/// Organization-facing privacy readiness projection. Never claims legal/NPC compliance.
/// Does not expose Platform reviewer notes or other-organization data.
/// </summary>
public sealed record OrganizationPrivacyReadinessDto(
    string OverallReadiness,
    string LegalVerificationStatus,
    string NpcVerificationStatus,
    DateTimeOffset GeneratedAtUtc,
    bool HasOwnerAttentionItems,
    IReadOnlyList<PrivacySafeguardItemDto> TechnicalSafeguards,
    IReadOnlyList<PrivacyBusinessActionDto> BusinessActions,
    IReadOnlyList<PrivacyResponsibilityItemDto> Responsibilities,
    string LegalVerificationMessageKey);

public sealed record PrivacySafeguardItemDto(
    string Code,
    string TitleKey,
    string Status,
    string ManagedBy,
    string EvidenceNoteKey);

public sealed record PrivacyBusinessActionDto(
    string Code,
    string TitleKey,
    string Status,
    string DetailKey,
    bool OwnerOnly,
    bool NeedsAttention);

public sealed record PrivacyResponsibilityItemDto(
    string Code,
    string TitleKey,
    string ManagedBy);

public static class OrganizationPrivacyReadinessStatuses
{
    public const string Implemented = "Implemented";
    public const string ActionNeeded = "ActionNeeded";
    public const string ReviewRecommended = "ReviewRecommended";
    public const string Complete = "Complete";
    public const string NotVerified = "NotVerified";
    public const string InProgress = "InProgress";
    public const string ManagedByPlatform = "Platform";
    public const string ManagedByBusiness = "Business";
}

/// <summary>
/// Host-provided signals for organization privacy readiness (owner role + sales-doc education).
/// Keeps POS API free of the full Maui/Web <c>IPlatformAccessClient</c> graph.
/// </summary>
public interface IOrganizationPrivacyContext
{
    Task<bool> IsExactOrganizationOwnerAsync(Guid organizationId, CancellationToken ct = default);

    Task<bool> SalesDocumentEducationRequiresOwnerActionAsync(
        Guid organizationId,
        CancellationToken ct = default);
}

/// <summary>Safe defaults when education/owner signals are unavailable.</summary>
public sealed class NullOrganizationPrivacyContext : IOrganizationPrivacyContext
{
    public Task<bool> IsExactOrganizationOwnerAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        Task.FromResult(false);

    public Task<bool> SalesDocumentEducationRequiresOwnerActionAsync(
        Guid organizationId,
        CancellationToken ct = default) =>
        Task.FromResult(false);
}

/// <summary>
/// Builds a safe Organization projection from known POS technical boundaries and
/// organization-local signals (e.g. sales-document education). Never reads Platform
/// privacy-compliance reviewer notes.
/// </summary>
public sealed class GetOrganizationPrivacyReadiness
{
    private readonly IOrganizationPrivacyContext _privacy;
    private readonly IClock _clock;

    public GetOrganizationPrivacyReadiness(
        IOrganizationPrivacyContext privacy,
        IClock clock)
    {
        _privacy = privacy;
        _clock = clock;
    }

    public async Task<OrganizationPrivacyReadinessDto> ExecuteAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var isOwner = await _privacy
            .IsExactOrganizationOwnerAsync(organizationId, ct)
            .ConfigureAwait(false);

        var educationActionNeeded = false;
        if (isOwner)
        {
            educationActionNeeded = await _privacy
                .SalesDocumentEducationRequiresOwnerActionAsync(organizationId, ct)
                .ConfigureAwait(false);
        }

        var safeguards = BuildTechnicalSafeguards();
        var actions = BuildBusinessActions(educationActionNeeded, isOwner);
        var responsibilities = BuildResponsibilities();

        var hasAttention = actions.Any(a => a.NeedsAttention);
        var overall = hasAttention
            ? OrganizationPrivacyReadinessStatuses.ActionNeeded
            : OrganizationPrivacyReadinessStatuses.InProgress;

        return new OrganizationPrivacyReadinessDto(
            overall,
            OrganizationPrivacyReadinessStatuses.NotVerified,
            OrganizationPrivacyReadinessStatuses.NotVerified,
            _clock.UtcNow,
            hasAttention,
            safeguards,
            actions,
            responsibilities,
            "Privacy_LegalVerification_Default");
    }

    private static IReadOnlyList<PrivacySafeguardItemDto> BuildTechnicalSafeguards() =>
    [
        new(
            "ORG_ISOLATION",
            "Privacy_Safeguard_OrgIsolation",
            OrganizationPrivacyReadinessStatuses.Implemented,
            OrganizationPrivacyReadinessStatuses.ManagedByPlatform,
            "Privacy_Evidence_OrgIsolation"),
        new(
            "RBAC",
            "Privacy_Safeguard_Roles",
            OrganizationPrivacyReadinessStatuses.Implemented,
            OrganizationPrivacyReadinessStatuses.ManagedByPlatform,
            "Privacy_Evidence_Roles"),
        new(
            "AUTH_SESSION",
            "Privacy_Safeguard_AuthSession",
            OrganizationPrivacyReadinessStatuses.Implemented,
            OrganizationPrivacyReadinessStatuses.ManagedByPlatform,
            "Privacy_Evidence_AuthSession"),
        new(
            "PERSONAL_ORG_BOUNDARY",
            "Privacy_Safeguard_PersonalOrgBoundary",
            OrganizationPrivacyReadinessStatuses.Implemented,
            OrganizationPrivacyReadinessStatuses.ManagedByPlatform,
            "Privacy_Evidence_PersonalOrgBoundary"),
        new(
            "DEVICE_SECRETS",
            "Privacy_Safeguard_DeviceSecrets",
            OrganizationPrivacyReadinessStatuses.Implemented,
            OrganizationPrivacyReadinessStatuses.ManagedByPlatform,
            "Privacy_Evidence_DeviceSecrets"),
        new(
            "SUPPLIER_SCOPE",
            "Privacy_Safeguard_SupplierScope",
            OrganizationPrivacyReadinessStatuses.Implemented,
            OrganizationPrivacyReadinessStatuses.ManagedByPlatform,
            "Privacy_Evidence_SupplierScope")
    ];

    private static IReadOnlyList<PrivacyBusinessActionDto> BuildBusinessActions(
        bool educationActionNeeded,
        bool isOwner)
    {
        var list = new List<PrivacyBusinessActionDto>
        {
            new(
                "STAFF_ACCESS_REVIEW",
                "Privacy_Action_StaffAccess",
                OrganizationPrivacyReadinessStatuses.ReviewRecommended,
                "Privacy_Action_StaffAccess_Detail",
                OwnerOnly: false,
                NeedsAttention: false),
            new(
                "CUSTOMER_DATA_HANDLING",
                "Privacy_Action_CustomerData",
                OrganizationPrivacyReadinessStatuses.ReviewRecommended,
                "Privacy_Action_CustomerData_Detail",
                OwnerOnly: false,
                NeedsAttention: false),
            new(
                "VENDOR_AWARENESS",
                "Privacy_Action_VendorAwareness",
                OrganizationPrivacyReadinessStatuses.ReviewRecommended,
                "Privacy_Action_VendorAwareness_Detail",
                OwnerOnly: false,
                NeedsAttention: false)
        };

        if (isOwner)
        {
            list.Insert(0, new(
                "SALES_DOCUMENT_EDUCATION",
                "Privacy_Action_SalesDocumentEducation",
                educationActionNeeded
                    ? OrganizationPrivacyReadinessStatuses.ActionNeeded
                    : OrganizationPrivacyReadinessStatuses.Complete,
                "Privacy_Action_SalesDocumentEducation_Detail",
                OwnerOnly: true,
                NeedsAttention: educationActionNeeded));
        }

        return list;
    }

    private static IReadOnlyList<PrivacyResponsibilityItemDto> BuildResponsibilities() =>
    [
        new("ACCOUNT_SECURITY", "Privacy_Responsibility_AccountSecurity", OrganizationPrivacyReadinessStatuses.ManagedByPlatform),
        new("PLATFORM_GOVERNANCE", "Privacy_Responsibility_PlatformGovernance", OrganizationPrivacyReadinessStatuses.ManagedByPlatform),
        new("STAFF_PERMISSIONS", "Privacy_Responsibility_StaffPermissions", OrganizationPrivacyReadinessStatuses.ManagedByBusiness),
        new("CUSTOMER_INFO", "Privacy_Responsibility_CustomerInfo", OrganizationPrivacyReadinessStatuses.ManagedByBusiness)
    ];
}
