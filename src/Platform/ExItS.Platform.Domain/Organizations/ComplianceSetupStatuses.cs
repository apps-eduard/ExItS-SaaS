namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization BIR / tax-document activation readiness lifecycle for ExItS technical capability.
/// <see cref="Activated"/> means ExItS tax-document capability is technically active when runtime exists;
/// while <see cref="TaxDocumentIssuanceRuntime.ImplementationAvailable"/> is false, evaluators keep
/// <see cref="ActivationBlocked"/> even when other checklist items are complete.
/// </summary>
public static class ComplianceSetupStatuses
{
    public const string NotConfigured = "NotConfigured";
    public const string SetupInProgress = "SetupInProgress";
    public const string ReadyForReview = "ReadyForReview";
    public const string UnderReview = "UnderReview";
    public const string ApprovedForExItsActivation = "ApprovedForExItsActivation";
    public const string NeedsAttention = "NeedsAttention";
    public const string ActivationBlocked = "ActivationBlocked";
    public const string Activated = "Activated";

    public static readonly IReadOnlySet<string> OrganizationAll = new HashSet<string>(StringComparer.Ordinal)
    {
        NotConfigured,
        SetupInProgress,
        ReadyForReview,
        UnderReview,
        ApprovedForExItsActivation,
        NeedsAttention,
        ActivationBlocked,
        Activated
    };

    /// <summary>Branch-scoped subset — branches do not carry org-level activation states.</summary>
    public static readonly IReadOnlySet<string> BranchAll = new HashSet<string>(StringComparer.Ordinal)
    {
        NotConfigured,
        SetupInProgress,
        ReadyForReview,
        NeedsAttention
    };

    public static bool IsKnownOrganizationStatus(string? value) =>
        !string.IsNullOrWhiteSpace(value) && OrganizationAll.Contains(value);

    public static bool IsKnownBranchStatus(string? value) =>
        !string.IsNullOrWhiteSpace(value) && BranchAll.Contains(value);
}

public static class ComplianceRegistrationTypes
{
    public const string PosPermitToUse = "PosPermitToUse";
    public const string CasRegistration = "CasRegistration";
    public const string EisCertification = "EisCertification";
    public const string EisPermitToTransmit = "EisPermitToTransmit";
    public const string Other = "Other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        PosPermitToUse,
        CasRegistration,
        EisCertification,
        EisPermitToTransmit,
        Other
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

public static class ComplianceRegistrationStatuses
{
    public const string NotProvided = "NotProvided";
    public const string Provided = "Provided";
    public const string UnderReview = "UnderReview";
    public const string AcceptedForReadiness = "AcceptedForReadiness";
    public const string RejectedForReadiness = "RejectedForReadiness";
    public const string Expired = "Expired";
    public const string Revoked = "Revoked";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        NotProvided,
        Provided,
        UnderReview,
        AcceptedForReadiness,
        RejectedForReadiness,
        Expired,
        Revoked
    };

    /// <summary>Statuses an organization Owner/Manager may set without Platform review authority.</summary>
    public static readonly IReadOnlySet<string> OwnerMutable = new HashSet<string>(StringComparer.Ordinal)
    {
        NotProvided,
        Provided,
        UnderReview,
        Expired,
        Revoked
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);

    public static bool IsOwnerMutable(string? value) =>
        !string.IsNullOrWhiteSpace(value) && OwnerMutable.Contains(value);
}

/// <summary>
/// TIN helpers for BIR taxpayer identifier (9 digits, no checksum). Never expose full TIN on public DTOs.
/// </summary>
public static class TinMask
{
    public const int RequiredLength = 9;

    /// <summary>Strip non-digits. Empty/whitespace clears to null. Non-empty must be exactly 9 digits.</summary>
    public static string? NormalizeOrThrow(string? tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
        {
            return null;
        }

        var digits = new string(tin.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        if (digits.Length != RequiredLength)
        {
            throw new Common.DomainException(
                Common.DomainErrorCodes.InvalidTaxpayerTin,
                $"TIN must be exactly {RequiredLength} digits when set.");
        }

        return digits;
    }

    /// <summary>Mask as <c>***-***-123</c> using the last three digits. Null/invalid yields null.</summary>
    public static string? Mask(string? tinNormalized)
    {
        if (string.IsNullOrWhiteSpace(tinNormalized) || tinNormalized.Length < 3)
        {
            return null;
        }

        var last3 = tinNormalized[^3..];
        return $"***-***-{last3}";
    }
}
