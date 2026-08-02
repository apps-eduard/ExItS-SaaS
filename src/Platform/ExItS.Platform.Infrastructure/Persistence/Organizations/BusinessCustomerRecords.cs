namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class BusinessCustomerRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? NormalizedEmail { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public string? OwningProductCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? LinkedUserIdentityId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class CreditCustomerRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BusinessCustomerId { get; set; }
    public string CurrencyCode { get; set; } = "PHP";
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class CustomerLinkRequestRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BusinessCustomerId { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? DeclinedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class LinkedCustomerAppUserRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BusinessCustomerId { get; set; }
    public Guid UserIdentityId { get; set; }
    public Guid SourceLinkRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset LinkedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class BusinessCreditOpeningBalanceRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CreditCustomerId { get; set; }
    public Guid BusinessCustomerId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "PHP";
    public DateTimeOffset EffectiveDateUtc { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceRecordId { get; set; }
    public Guid MigrationBatchId { get; set; }
    public Guid ImportedByUserId { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public string DestinationProduct { get; set; } = string.Empty;
}

internal sealed class ProductLocalRoleGrantRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserIdentityId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public Guid GrantedByUserIdentityId { get; set; }
    public string Source { get; set; } = string.Empty;
}
