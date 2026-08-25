using System.Net.Mail;
using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Platform User aggregate — global identity only.
/// Does not store passwords, tokens, MFA secrets, or product-local profiles.
/// </summary>
public sealed class PlatformUser
{
    private static readonly Regex DisplayNamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-]{0,98}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UsernamePattern = new(
        @"^[a-z0-9][a-z0-9._-]{1,62}[a-z0-9]$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public const int StaffNumberPrefixLength = 4;
    public const int StaffNumberSequenceDigits = 6;
    public const string StaffNumberPrefix = "STF-";

    public PlatformUserId Id { get; }
    public string Username { get; private set; }
    public string NormalizedUsername { get; private set; }
    public string DisplayName { get; private set; }
    /// <summary>Unique login key (personal email or org-scoped staff login such as maria@org001842).</summary>
    public string NormalizedEmail { get; private set; }
    /// <summary>Real contact/invitation/recovery email for staff identities. Not an authorization key.</summary>
    public string? NormalizedContactEmail { get; private set; }
    /// <summary>When set, this identity is permanently scoped to one organization (org staff login).</summary>
    public PlatformOrganizationId? HomeOrganizationId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Phone { get; private set; }
    public string? EmployeeCode { get; private set; }
    public string? StaffNumber { get; private set; }
    public string? PublicUserId { get; private set; }
    public PlatformUserId? CreatedByUserId { get; private set; }
    /// <summary>
    /// Formal same-human correlation to a Personal PlatformUser. Identity correlation only — not authorization.
    /// Null for standalone organization staff (no Personal principal, or legacy unlinked staff).
    /// </summary>
    public PlatformUserId? LinkedPersonalUserId { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SuspendedAtUtc { get; private set; }
    public string? SuspensionReason { get; private set; }

    public bool IsOrganizationScopedStaff => HomeOrganizationId is not null;

    private PlatformUser(
        PlatformUserId id,
        string username,
        string normalizedUsername,
        string displayName,
        string normalizedEmail,
        string? normalizedContactEmail,
        PlatformOrganizationId? homeOrganizationId,
        string? firstName,
        string? lastName,
        string? phone,
        string? employeeCode,
        string? staffNumber,
        string? publicUserId,
        PlatformUserId? createdByUserId,
        PlatformUserId? linkedPersonalUserId,
        AccountStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        string? suspensionReason)
    {
        if (linkedPersonalUserId is not null)
        {
            if (homeOrganizationId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.PersonLinkStaffRequired,
                    "A formal person link can only be set on an organization-scoped staff principal.");
            }

            if (linkedPersonalUserId.Equals(id))
            {
                throw new DomainException(
                    DomainErrorCodes.PersonLinkSelfDenied,
                    "A staff principal cannot be linked to itself.");
            }
        }
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        DisplayName = displayName;
        NormalizedEmail = normalizedEmail;
        NormalizedContactEmail = normalizedContactEmail;
        HomeOrganizationId = homeOrganizationId;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        EmployeeCode = employeeCode;
        StaffNumber = staffNumber;
        PublicUserId = publicUserId;
        CreatedByUserId = createdByUserId;
        LinkedPersonalUserId = linkedPersonalUserId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        SuspendedAtUtc = suspendedAtUtc;
        SuspensionReason = suspensionReason;
    }

    public static PlatformUser Create(
        string username,
        string displayName,
        string email,
        DateTimeOffset utcNow,
        PlatformUserId? id = null)
    {
        EnsureUtc(utcNow);
        var (displayUsername, normalizedUsername) = NormalizeUsername(username);
        var normalizedName = NormalizeDisplayName(displayName);
        var normalizedEmail = NormalizeEmail(email);

        return new PlatformUser(
            id ?? PlatformUserId.New(),
            displayUsername,
            normalizedUsername,
            normalizedName,
            normalizedEmail,
            normalizedContactEmail: null,
            homeOrganizationId: null,
            firstName: null,
            lastName: null,
            phone: null,
            employeeCode: null,
            staffNumber: null,
            publicUserId: null,
            createdByUserId: null,
            linkedPersonalUserId: null,
            AccountStatus.Active,
            utcNow,
            utcNow,
            null,
            null);
    }

    /// <summary>Public Personal signup — identity exists but login remains blocked until email verification and password activation.</summary>
    public static PlatformUser CreatePendingVerification(
        string username,
        string displayName,
        string email,
        DateTimeOffset utcNow,
        PlatformUserId? id = null)
    {
        EnsureUtc(utcNow);
        var (displayUsername, normalizedUsername) = NormalizeUsername(username);
        var normalizedName = NormalizeDisplayName(displayName);
        var normalizedEmail = NormalizeEmail(email);

        return new PlatformUser(
            id ?? PlatformUserId.New(),
            displayUsername,
            normalizedUsername,
            normalizedName,
            normalizedEmail,
            normalizedContactEmail: null,
            homeOrganizationId: null,
            firstName: null,
            lastName: null,
            phone: null,
            employeeCode: null,
            staffNumber: null,
            publicUserId: null,
            createdByUserId: null,
            linkedPersonalUserId: null,
            AccountStatus.PendingVerification,
            utcNow,
            utcNow,
            null,
            null);
    }

    /// <summary>Platform Administrator staff provisioning — assigns immutable StaffNumber.</summary>
    public static PlatformUser CreatePlatformStaff(
        string username,
        string firstName,
        string lastName,
        string displayName,
        string email,
        string staffNumber,
        DateTimeOffset utcNow,
        string? phone = null,
        string? employeeCode = null,
        PlatformUserId? createdByUserId = null,
        bool requireEmailVerification = false,
        PlatformUserId? id = null)
    {
        EnsureUtc(utcNow);
        var (displayUsername, normalizedUsername) = NormalizeUsername(username);
        var normalizedFirstName = NormalizeOptionalName(firstName, nameof(firstName));
        var normalizedLastName = NormalizeOptionalName(lastName, nameof(lastName));
        var normalizedName = NormalizeDisplayName(displayName);
        var normalizedEmail = NormalizeEmail(email);
        var normalizedStaffNumber = NormalizeStaffNumber(staffNumber, expectAssigned: true);
        var normalizedPhone = NormalizeOptionalPhone(phone);
        var normalizedEmployeeCode = NormalizeOptionalEmployeeCode(employeeCode);

        return new PlatformUser(
            id ?? PlatformUserId.New(),
            displayUsername,
            normalizedUsername,
            normalizedName,
            normalizedEmail,
            normalizedContactEmail: null,
            homeOrganizationId: null,
            normalizedFirstName,
            normalizedLastName,
            normalizedPhone,
            normalizedEmployeeCode,
            normalizedStaffNumber,
            publicUserId: null,
            createdByUserId,
            linkedPersonalUserId: null,
            requireEmailVerification ? AccountStatus.PendingVerification : AccountStatus.Active,
            utcNow,
            utcNow,
            null,
            null);
    }

    /// <summary>
    /// Organization-scoped staff identity. Login is a system name (local@ORG######);
    /// contact email is for invitation/recovery only and is not unique.
    /// </summary>
    public static PlatformUser CreateOrganizationStaff(
        string username,
        string staffLogin,
        string contactEmail,
        PlatformOrganizationId homeOrganizationId,
        string displayName,
        DateTimeOffset utcNow,
        string? firstName = null,
        string? lastName = null,
        string? phone = null,
        string? employeeCode = null,
        PlatformUserId? createdByUserId = null,
        PlatformUserId? linkedPersonalUserId = null,
        PlatformUserId? id = null)
    {
        EnsureUtc(utcNow);
        ArgumentNullException.ThrowIfNull(homeOrganizationId);
        var (displayUsername, normalizedUsername) = NormalizeUsername(username);
        var login = NormalizeEmail(staffLogin);
        var contact = NormalizeEmail(contactEmail);
        var normalizedName = NormalizeDisplayName(displayName);
        var assignedId = id ?? PlatformUserId.New();

        return new PlatformUser(
            assignedId,
            displayUsername,
            normalizedUsername,
            normalizedName,
            login,
            contact,
            homeOrganizationId,
            NormalizeOptionalName(firstName, nameof(firstName)),
            NormalizeOptionalName(lastName, nameof(lastName)),
            NormalizeOptionalPhone(phone),
            NormalizeOptionalEmployeeCode(employeeCode),
            staffNumber: null,
            publicUserId: null,
            createdByUserId,
            linkedPersonalUserId,
            AccountStatus.Active,
            utcNow,
            utcNow,
            null,
            null);
    }

    /// <summary>Rehydrate from persistence.</summary>
    public static PlatformUser Rehydrate(
        PlatformUserId id,
        string username,
        string normalizedUsername,
        string displayName,
        string normalizedEmail,
        string? normalizedContactEmail,
        PlatformOrganizationId? homeOrganizationId,
        string? firstName,
        string? lastName,
        string? phone,
        string? employeeCode,
        string? staffNumber,
        string? publicUserId,
        PlatformUserId? createdByUserId,
        AccountStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        string? suspensionReason,
        PlatformUserId? linkedPersonalUserId = null) =>
        new(
            id,
            username,
            normalizedUsername,
            displayName,
            normalizedEmail,
            normalizedContactEmail,
            homeOrganizationId,
            firstName,
            lastName,
            phone,
            employeeCode,
            staffNumber,
            publicUserId,
            createdByUserId,
            linkedPersonalUserId,
            status,
            createdAtUtc,
            updatedAtUtc,
            suspendedAtUtc,
            suspensionReason);

    /// <summary>Assigns the immutable public ExItS ID once. Never changes after assignment.</summary>
    public void AssignPublicUserId(string publicUserId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        var normalized = PublicUserIdRules.Normalize(publicUserId);
        if (PublicUserId is not null)
        {
            if (!string.Equals(PublicUserId, normalized, StringComparison.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.PublicUserIdImmutable,
                    "ExItS ID cannot be changed once assigned.");
            }

            return;
        }

        PublicUserId = normalized;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Records an explicit same-human correlation to a Personal principal.
    /// Does not grant membership, product role, or session authority.
    /// </summary>
    public void LinkToPersonalPrincipal(PlatformUserId personalUserId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        ArgumentNullException.ThrowIfNull(personalUserId);
        if (!IsOrganizationScopedStaff)
        {
            throw new DomainException(
                DomainErrorCodes.PersonLinkStaffRequired,
                "A formal person link can only be set on an organization-scoped staff principal.");
        }

        if (personalUserId.Equals(Id))
        {
            throw new DomainException(
                DomainErrorCodes.PersonLinkSelfDenied,
                "A staff principal cannot be linked to itself.");
        }

        if (LinkedPersonalUserId is not null)
        {
            if (!LinkedPersonalUserId.Equals(personalUserId))
            {
                throw new DomainException(
                    DomainErrorCodes.PersonLinkImmutable,
                    "A formal person link cannot be changed once recorded.");
            }

            return;
        }

        LinkedPersonalUserId = personalUserId;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateProfile(string displayName, string email, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotDeactivated();

        DisplayName = NormalizeDisplayName(displayName);
        NormalizedEmail = NormalizeEmail(email);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Updates Platform Staff identity fields. StaffNumber is immutable once assigned.
    /// </summary>
    public void UpdateStaffProfile(
        string? firstName,
        string? lastName,
        string displayName,
        string email,
        DateTimeOffset utcNow,
        string? phone = null,
        string? employeeCode = null,
        string? attemptedStaffNumber = null)
    {
        EnsureUtc(utcNow);
        EnsureStaffProfileEditable();

        if (attemptedStaffNumber is not null
            && !string.Equals(NormalizeStaffNumber(attemptedStaffNumber, expectAssigned: false), StaffNumber, StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.StaffNumberImmutable,
                "Staff Number cannot be changed once assigned.");
        }

        FirstName = NormalizeOptionalName(firstName, nameof(firstName));
        LastName = NormalizeOptionalName(lastName, nameof(lastName));
        DisplayName = NormalizeDisplayName(displayName);
        NormalizedEmail = NormalizeEmail(email);
        Phone = NormalizeOptionalPhone(phone);
        EmployeeCode = NormalizeOptionalEmployeeCode(employeeCode);
        UpdatedAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(AccountStatus.Suspended, utcNow);
        SuspendedAtUtc = utcNow;
        SuspensionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>
    /// Pending Verification → Active after email verification and password setup.
    /// </summary>
    public void ActivateFromPendingVerification(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != AccountStatus.PendingVerification)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                $"Cannot activate Platform User from {Status} (Pending Verification required).");
        }

        TransitionTo(AccountStatus.Active, utcNow);
        SuspendedAtUtc = null;
        SuspensionReason = null;
    }

    /// <summary>
    /// Restores Active from Suspended (confirmation only) or from Deactivated
    /// (caller must enforce step-up: acting administrator password + MFA when enabled + reason).
    /// </summary>
    public void Reactivate(DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(AccountStatus.Active, utcNow);
        SuspendedAtUtc = null;
        SuspensionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>
    /// Deactivated is a reversible retained state (not deletion). Login remains blocked until reactivation.
    /// </summary>
    public void Deactivate(DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "A reason is required to deactivate a Platform User.");
        }

        TransitionTo(AccountStatus.Deactivated, utcNow);
        SuspendedAtUtc = null;
        SuspensionReason = reason.Trim();
    }

    /// <summary>
    /// Deactivated → Suspended. Login remains blocked; does not restore access.
    /// </summary>
    public void MoveToSuspended(DateTimeOffset utcNow, string reason)
    {
        EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "A reason is required to move a deactivated Platform User to Suspended.");
        }

        if (Status != AccountStatus.Deactivated)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                $"Cannot move Platform User from {Status} to Suspended via Move to Suspended (Deactivated only).");
        }

        TransitionTo(AccountStatus.Suspended, utcNow);
        SuspendedAtUtc = utcNow;
        SuspensionReason = reason.Trim();
    }

    private void TransitionTo(AccountStatus target, DateTimeOffset utcNow)
    {
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            AccountStatus.PendingVerification => target is AccountStatus.Active or AccountStatus.Deactivated,
            AccountStatus.Active => target is AccountStatus.Suspended or AccountStatus.Deactivated,
            AccountStatus.Suspended => target is AccountStatus.Active or AccountStatus.Deactivated,
            AccountStatus.Deactivated => target is AccountStatus.Active or AccountStatus.Suspended,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                $"Cannot transition Platform User from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureNotDeactivated()
    {
        if (Status is AccountStatus.Deactivated or AccountStatus.PendingVerification)
        {
            throw new DomainException(
                DomainErrorCodes.UserNotActive,
                "A deactivated or pending-verification Platform User cannot be updated.");
        }
    }

    private void EnsureStaffProfileEditable()
    {
        if (Status == AccountStatus.Deactivated)
        {
            throw new DomainException(
                DomainErrorCodes.UserNotActive,
                "A deactivated Platform User cannot be updated.");
        }
    }

    public static (string Display, string Normalized) NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUsername,
                "Username cannot be blank.");
        }

        var trimmed = username.Trim();
        var normalized = trimmed.ToLowerInvariant();
        if (normalized.Length is < 3 or > 64 || !UsernamePattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUsername,
                "Username must be 3–64 characters: lowercase letters, numbers, dots, underscores, or hyphens.");
        }

        return (trimmed, normalized);
    }

    internal static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name cannot be blank.");
        }

        var trimmed = Regex.Replace(displayName.Trim(), @"\s+", " ");
        if (trimmed.Length is < 2 or > 100 || !DisplayNamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name must be 2–100 characters and use letters, numbers, spaces, apostrophes, periods, or hyphens.");
        }

        return trimmed;
    }

    internal static string? NormalizeOptionalName(string? value, string fieldName)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = Regex.Replace(value.Trim(), @"\s+", " ");
        if (trimmed.Length == 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                $"{fieldName} cannot be blank when provided.");
        }

        if (trimmed.Length > 100 || !DisplayNamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                $"{fieldName} must be 1–100 characters and use letters, numbers, spaces, apostrophes, periods, or hyphens.");
        }

        return trimmed;
    }

    internal static string? NormalizeOptionalPhone(string? phone)
    {
        if (phone is null)
        {
            return null;
        }

        var trimmed = phone.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > 32)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPhone,
                "Phone must be 32 characters or fewer.");
        }

        return trimmed;
    }

    internal static string? NormalizeOptionalEmployeeCode(string? employeeCode)
    {
        if (employeeCode is null)
        {
            return null;
        }

        var trimmed = employeeCode.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > 64)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmployeeCode,
                "Employee code must be 64 characters or fewer.");
        }

        return trimmed;
    }

    internal static string? NormalizeStaffNumber(string? staffNumber, bool expectAssigned)
    {
        if (staffNumber is null)
        {
            if (expectAssigned)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidStaffNumber,
                    "Staff Number is required for Platform Staff.");
            }

            return null;
        }

        var trimmed = staffNumber.Trim();
        if (trimmed.Length != StaffNumberPrefixLength + StaffNumberSequenceDigits
            || !trimmed.StartsWith(StaffNumberPrefix, StringComparison.Ordinal)
            || !int.TryParse(trimmed.AsSpan(StaffNumberPrefixLength), out var sequence)
            || sequence < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStaffNumber,
                $"Staff Number must match format {StaffNumberPrefix}000001.");
        }

        return trimmed;
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Email cannot be blank.");
        }

        var trimmed = email.Trim();
        if (trimmed.Contains(' ', StringComparison.Ordinal)
            || trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Email must be a plain address without display name.");
        }

        try
        {
            var address = new MailAddress(trimmed);
            return address.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Email format is invalid.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
