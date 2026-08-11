using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Audit;

public sealed class AuditRecordTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_succeeds_with_required_fields_only()
    {
        var record = AuditRecord.Create(
            T0,
            "development-operator:unauthenticated",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            Guid.NewGuid().ToString("D"),
            AuditOutcome.Succeeded);

        Assert.Equal(T0, record.OccurredAtUtc);
        Assert.Equal(AuditOutcome.Succeeded, record.Outcome);
        Assert.Null(record.OrganizationId);
        Assert.Null(record.ProductCode);
        Assert.Null(record.Reason);
        Assert.Null(record.Summary);
    }

    [Fact]
    public void Create_normalizes_optional_organization_product_reason_and_summary()
    {
        var orgId = PlatformOrganizationId.New();
        var productCode = ProductCode.Create("other-product");

        var record = AuditRecord.Create(
            T0,
            "platform-user:11111111-1111-1111-1111-111111111111",
            AuditActorType.PlatformUser,
            PlatformAuditActions.SubscriptionActivated,
            "Subscription",
            Guid.NewGuid().ToString("D"),
            AuditOutcome.Succeeded,
            orgId,
            productCode,
            correlationId: "  corr-1  ",
            reason: "  approved  ",
            summary: "  subscription activated  ");

        Assert.Equal(orgId, record.OrganizationId);
        Assert.Equal(productCode, record.ProductCode);
        Assert.Equal("corr-1", record.CorrelationId);
        Assert.Equal("approved", record.Reason);
        Assert.Equal("subscription activated", record.Summary);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_rejects_blank_actor_identifier(string? actorIdentifier)
    {
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            actorIdentifier!,
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_rejects_blank_action_code(string? actionCode)
    {
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            actionCode!,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_rejects_blank_target_type(string? targetType)
    {
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            targetType!,
            "target",
            AuditOutcome.Succeeded));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_rejects_blank_target_id(string? targetId)
    {
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            targetId!,
            AuditOutcome.Succeeded));
    }

    [Fact]
    public void Create_rejects_actor_identifier_over_max_length()
    {
        var tooLong = new string('a', 257);
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            tooLong,
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded));
    }

    [Fact]
    public void Create_rejects_reason_over_max_length()
    {
        var tooLong = new string('a', 513);
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded,
            reason: tooLong));
    }

    [Fact]
    public void Create_rejects_summary_over_max_length()
    {
        var tooLong = new string('a', 2001);
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded,
            summary: tooLong));
    }

    [Fact]
    public void Create_rejects_undefined_actor_type()
    {
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            (AuditActorType)999,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded));
    }

    [Fact]
    public void Create_rejects_undefined_outcome()
    {
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            (AuditOutcome)999));
    }

    [Fact]
    public void Create_rejects_non_utc_timestamp()
    {
        var nonUtc = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(8));
        Assert.Throws<DomainException>(() => AuditRecord.Create(
            nonUtc,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformUserCreated,
            "PlatformUser",
            "target",
            AuditOutcome.Succeeded));
    }

    [Fact]
    public void Rehydrate_preserves_all_fields()
    {
        var id = AuditRecordId.New();
        var orgId = PlatformOrganizationId.New();
        var productCode = ProductCode.Create("other-product");

        var record = AuditRecord.Rehydrate(
            id,
            T0,
            "dev-admin",
            AuditActorType.DevelopmentOperator,
            PlatformAuditActions.PlatformRoleAssigned,
            "PlatformRoleAssignment",
            "target-1",
            orgId,
            productCode,
            "corr-9",
            AuditOutcome.Denied,
            "no access",
            "denied access to resource");

        Assert.Equal(id, record.Id);
        Assert.Equal(AuditOutcome.Denied, record.Outcome);
        Assert.Equal("no access", record.Reason);
        Assert.Equal("denied access to resource", record.Summary);
        Assert.Equal(orgId, record.OrganizationId);
        Assert.Equal(productCode, record.ProductCode);
    }
}
