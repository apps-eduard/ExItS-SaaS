using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Personal↔Organization customer-connection block (pair-scoped).
/// Prevents the Organization from inviting/reminding this Personal identity.
/// Does not delete customers, POS correlation, or Business Utang.
/// </summary>
public sealed class PersonalOrganizationConnectionBlock
{
    public PersonalOrganizationConnectionBlockId Id { get; }
    public PlatformUserId PersonalUserIdentityId { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PersonalOrganizationConnectionBlockStatus Status { get; private set; }
    public DateTimeOffset BlockedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? UnblockedAtUtc { get; private set; }
    public CustomerLinkRequestId? SourceCustomerLinkRequestId { get; private set; }

    public bool IsActive => Status == PersonalOrganizationConnectionBlockStatus.Active;

    private PersonalOrganizationConnectionBlock(
        PersonalOrganizationConnectionBlockId id,
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        PersonalOrganizationConnectionBlockStatus status,
        DateTimeOffset blockedAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? unblockedAtUtc,
        CustomerLinkRequestId? sourceCustomerLinkRequestId)
    {
        Id = id;
        PersonalUserIdentityId = personalUserIdentityId;
        OrganizationId = organizationId;
        Status = status;
        BlockedAtUtc = blockedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        UnblockedAtUtc = unblockedAtUtc;
        SourceCustomerLinkRequestId = sourceCustomerLinkRequestId;
    }

    public static PersonalOrganizationConnectionBlock Create(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow,
        CustomerLinkRequestId? sourceCustomerLinkRequestId = null,
        PersonalOrganizationConnectionBlockId? id = null)
    {
        ArgumentNullException.ThrowIfNull(personalUserIdentityId);
        ArgumentNullException.ThrowIfNull(organizationId);
        EnsureUtc(utcNow);

        return new PersonalOrganizationConnectionBlock(
            id ?? PersonalOrganizationConnectionBlockId.New(),
            personalUserIdentityId,
            organizationId,
            PersonalOrganizationConnectionBlockStatus.Active,
            utcNow,
            utcNow,
            unblockedAtUtc: null,
            sourceCustomerLinkRequestId);
    }

    public static PersonalOrganizationConnectionBlock Rehydrate(
        PersonalOrganizationConnectionBlockId id,
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        PersonalOrganizationConnectionBlockStatus status,
        DateTimeOffset blockedAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? unblockedAtUtc,
        CustomerLinkRequestId? sourceCustomerLinkRequestId) =>
        new(
            id,
            personalUserIdentityId,
            organizationId,
            status,
            blockedAtUtc,
            updatedAtUtc,
            unblockedAtUtc,
            sourceCustomerLinkRequestId);

    /// <summary>Idempotent: active block stays active.</summary>
    public void Activate(DateTimeOffset utcNow, CustomerLinkRequestId? sourceCustomerLinkRequestId = null)
    {
        EnsureUtc(utcNow);
        if (Status == PersonalOrganizationConnectionBlockStatus.Active)
        {
            UpdatedAtUtc = utcNow;
            if (sourceCustomerLinkRequestId is not null)
            {
                SourceCustomerLinkRequestId = sourceCustomerLinkRequestId;
            }

            return;
        }

        Status = PersonalOrganizationConnectionBlockStatus.Active;
        BlockedAtUtc = utcNow;
        UnblockedAtUtc = null;
        UpdatedAtUtc = utcNow;
        if (sourceCustomerLinkRequestId is not null)
        {
            SourceCustomerLinkRequestId = sourceCustomerLinkRequestId;
        }
    }

    /// <summary>Idempotent: inactive block stays inactive.</summary>
    public void Unblock(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PersonalOrganizationConnectionBlockStatus.Inactive)
        {
            UpdatedAtUtc = utcNow;
            return;
        }

        Status = PersonalOrganizationConnectionBlockStatus.Inactive;
        UnblockedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public enum PersonalOrganizationConnectionBlockStatus
{
    Active = 0,
    Inactive = 1
}

public sealed class PersonalOrganizationConnectionBlockId : IEquatable<PersonalOrganizationConnectionBlockId>
{
    public Guid Value { get; }

    private PersonalOrganizationConnectionBlockId(Guid value) => Value = value;

    public static PersonalOrganizationConnectionBlockId New() => new(Guid.NewGuid());

    public static PersonalOrganizationConnectionBlockId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalOrganizationConnectionBlockId,
                "Personal organization connection block id cannot be empty.");
        }

        return new PersonalOrganizationConnectionBlockId(value);
    }

    public bool Equals(PersonalOrganizationConnectionBlockId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalOrganizationConnectionBlockId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}
