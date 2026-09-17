using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization membership — Platform-level participation for one user in one organization.
/// Does not grant product-local roles (Doctor, Cashier, etc.).
/// Organization-specific business contact fields live here; Personal identity remains on <see cref="PlatformUser"/>.
/// </summary>
public sealed class OrganizationMembership
{
    private static readonly Regex PhonePattern = new(
        @"^\+?[0-9][0-9 .\-()]{6,30}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public OrganizationMembershipId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId UserId { get; }
    public MembershipStatus Status { get; private set; }
    public OrganizationRole Role { get; private set; }
    /// <summary>
    /// Ordinary-member branch scope. Owner/Administrator access does not depend on this value.
    /// </summary>
    public BranchAccessScope BranchAccessScope { get; private set; }
    public string? Department { get; private set; }
    public string? JobTitle { get; private set; }
    public string? WorkPhone { get; private set; }
    public string? WorkEmail { get; private set; }
    /// <summary>
    /// When true, Active members may appear in Connected B2B organization-contact pickers.
    /// Owner defaults to true on create; other roles default to false.
    /// </summary>
    public bool IsBusinessContact { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SuspendedAtUtc { get; private set; }
    public DateTimeOffset? RemovedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public string? ActorReference { get; private set; }

    private OrganizationMembership(
        OrganizationMembershipId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        MembershipStatus status,
        OrganizationRole role,
        BranchAccessScope branchAccessScope,
        string? department,
        string? jobTitle,
        string? workPhone,
        string? workEmail,
        bool isBusinessContact,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        DateTimeOffset? removedAtUtc,
        string? reason,
        string? actorReference)
    {
        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        Status = status;
        Role = role;
        BranchAccessScope = branchAccessScope;
        Department = department;
        JobTitle = jobTitle;
        WorkPhone = workPhone;
        WorkEmail = workEmail;
        IsBusinessContact = isBusinessContact;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        SuspendedAtUtc = suspendedAtUtc;
        RemovedAtUtc = removedAtUtc;
        Reason = reason;
        ActorReference = actorReference;
    }

    public static OrganizationMembership Create(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRole role,
        DateTimeOffset utcNow,
        OrganizationMembershipId? id = null,
        string? actorReference = null,
        BranchAccessScope branchAccessScope = BranchAccessScope.Explicit,
        string? department = null,
        string? jobTitle = null,
        string? workPhone = null,
        string? workEmail = null,
        bool? isBusinessContact = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        EnsureDefinedRole(role);
        EnsureDefinedBranchAccessScope(branchAccessScope);

        var defaultBusinessContact = role == OrganizationRole.OrganizationOwner;
        return new OrganizationMembership(
            id ?? OrganizationMembershipId.New(),
            organizationId,
            userId,
            MembershipStatus.Active,
            role,
            branchAccessScope,
            NormalizeOptionalText(department, 100),
            NormalizeOptionalText(jobTitle, 100),
            NormalizePhone(workPhone),
            NormalizeWorkEmail(workEmail),
            isBusinessContact ?? defaultBusinessContact,
            utcNow,
            utcNow,
            null,
            null,
            null,
            NormalizeOptional(actorReference));
    }

    public static OrganizationMembership Rehydrate(
        OrganizationMembershipId id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        MembershipStatus status,
        OrganizationRole role,
        BranchAccessScope branchAccessScope,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        DateTimeOffset? removedAtUtc,
        string? reason,
        string? actorReference,
        string? department = null,
        string? jobTitle = null,
        string? workPhone = null,
        string? workEmail = null,
        bool isBusinessContact = false) =>
        new(
            id,
            organizationId,
            userId,
            status,
            role,
            branchAccessScope,
            department,
            jobTitle,
            workPhone,
            workEmail,
            isBusinessContact,
            createdAtUtc,
            updatedAtUtc,
            suspendedAtUtc,
            removedAtUtc,
            reason,
            actorReference);

    public void ChangeRole(OrganizationRole role, DateTimeOffset utcNow, string? actorReference = null)
    {
        EnsureUtc(utcNow);
        EnsureDefinedRole(role);
        if (Status == MembershipStatus.Removed)
        {
            throw new DomainException(
                DomainErrorCodes.MembershipNotActive,
                "A removed membership cannot change role.");
        }

        Role = role;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
        UpdatedAtUtc = utcNow;
    }

    public void SetBranchAccessScope(
        BranchAccessScope scope,
        DateTimeOffset utcNow,
        string? actorReference = null)
    {
        EnsureUtc(utcNow);
        EnsureDefinedBranchAccessScope(scope);
        if (Status == MembershipStatus.Removed)
        {
            throw new DomainException(
                DomainErrorCodes.MembershipNotActive,
                "A removed membership cannot change branch access scope.");
        }

        BranchAccessScope = scope;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Member self-service: work phone/email and business-contact visibility only.
    /// Cannot change department or job title.
    /// </summary>
    public void UpdateOwnBusinessContact(
        string? workPhone,
        string? workEmail,
        bool isBusinessContact,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActiveForProfileEdit();
        WorkPhone = NormalizePhone(workPhone);
        WorkEmail = NormalizeWorkEmail(workEmail);
        IsBusinessContact = isBusinessContact;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Owner/Admin (staff-management) may set full organization-specific business profile.
    /// Does not change Role or permissions.
    /// </summary>
    public void UpdateManagedBusinessProfile(
        string? department,
        string? jobTitle,
        string? workPhone,
        string? workEmail,
        bool isBusinessContact,
        DateTimeOffset utcNow,
        string? actorReference = null)
    {
        EnsureUtc(utcNow);
        EnsureActiveForProfileEdit();
        Department = NormalizeOptionalText(department, 100);
        JobTitle = NormalizeOptionalText(jobTitle, 100);
        WorkPhone = NormalizePhone(workPhone);
        WorkEmail = NormalizeWorkEmail(workEmail);
        IsBusinessContact = isBusinessContact;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
        UpdatedAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow, string? reason = null, string? actorReference = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(MembershipStatus.Suspended, utcNow);
        SuspendedAtUtc = utcNow;
        RemovedAtUtc = null;
        Reason = NormalizeOptional(reason) ?? Reason;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
    }

    public void Reactivate(DateTimeOffset utcNow, string? actorReference = null, string? reason = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(MembershipStatus.Active, utcNow);
        SuspendedAtUtc = null;
        RemovedAtUtc = null;
        Reason = NormalizeOptional(reason) ?? Reason;
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
    }

    /// <summary>
    /// Deactivate Membership — retained reversible state (persisted as <see cref="MembershipStatus.Removed"/>).
    /// </summary>
    public void Deactivate(DateTimeOffset utcNow, string? reason = null, string? actorReference = null) =>
        Remove(utcNow, reason, actorReference);

    public void Remove(DateTimeOffset utcNow, string? reason = null, string? actorReference = null)
    {
        EnsureUtc(utcNow);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                "A reason is required to deactivate a membership.");
        }

        TransitionTo(MembershipStatus.Removed, utcNow);
        RemovedAtUtc = utcNow;
        Reason = reason.Trim();
        ActorReference = NormalizeOptional(actorReference) ?? ActorReference;
    }

    private void EnsureActiveForProfileEdit()
    {
        if (Status != MembershipStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.MembershipNotActive,
                "Only an Active membership can update business profile fields.");
        }
    }

    private void TransitionTo(MembershipStatus target, DateTimeOffset utcNow)
    {
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            MembershipStatus.Active => target is MembershipStatus.Suspended or MembershipStatus.Removed,
            MembershipStatus.Suspended => target is MembershipStatus.Active or MembershipStatus.Removed,
            MembershipStatus.Removed => target is MembershipStatus.Active or MembershipStatus.Suspended,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                $"Cannot transition membership from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = Regex.Replace(value.Trim(), @"\s+", " ");
        if (trimmed.Length > maxLength
            || trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipBusinessProfile,
                $"Text field must be at most {maxLength} characters without markup.");
        }

        return trimmed;
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        if (!PhonePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidMembershipBusinessProfile,
                "Work phone format is invalid.");
        }

        return trimmed;
    }

    private static string? NormalizeWorkEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return PlatformUser.NormalizeEmail(email);
    }

    private static void EnsureDefinedRole(OrganizationRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization role is not defined.");
        }
    }

    private static void EnsureDefinedBranchAccessScope(BranchAccessScope scope)
    {
        if (!Enum.IsDefined(scope))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRole,
                "Branch access scope is not defined.");
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
