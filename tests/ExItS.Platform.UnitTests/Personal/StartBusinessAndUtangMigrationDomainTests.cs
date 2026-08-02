using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class StartBusinessAndUtangMigrationDomainTests
{
    private static readonly DateTimeOffset Utc = new(2026, 8, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Personal_contact_and_relationship_support_archive_and_transfer()
    {
        var owner = PlatformUserId.New();
        var contact = PersonalContact.Create(owner, "Ana", "+639170000001", null, Utc);
        contact.Archive(Utc);
        Assert.Equal(PersonalContactStatus.Archived, contact.Status);

        var relationship = PersonalDebtRelationship.Create(
            owner,
            creditorUserIdentityId: owner,
            creditorContactId: null,
            debtorUserIdentityId: null,
            debtorContactId: contact.Id,
            "PHP",
            Utc);
        relationship.Archive(Utc);
        Assert.Equal(PersonalDebtRelationshipStatus.Archived, relationship.Status);

        var creditCustomerId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        relationship.MarkTransferred(orgId, creditCustomerId, batchId, Utc);
        Assert.Equal(PersonalDebtRelationshipStatus.Transferred, relationship.Status);
        Assert.Equal(orgId, relationship.DestinationOrganizationId);
        Assert.Equal(creditCustomerId, relationship.DestinationCreditCustomerId);
        Assert.Equal(batchId, relationship.MigrationBatchId);
    }

    [Fact]
    public void Migration_batch_requires_include_option()
    {
        var ex = Assert.Throws<DomainException>(() =>
            PersonalUtangMigrationBatch.CreatePreview(
                PlatformUserId.New(),
                PlatformOrganizationId.New(),
                "pinoy-business-pos",
                Utc,
                includeContact: false,
                includeOpeningBalance: false,
                includeSelectedHistory: false,
                includeDueDatesAndNotes: false,
                PersonalUtangSourceDisposition.Archive,
                linkedParticipantConsentAcknowledged: false,
                Utc));
        Assert.Equal(DomainErrorCodes.PersonalUtangMigrationSelectionRequired, ex.ErrorCode);
    }

    [Fact]
    public void Product_local_role_grant_separates_pos_owner_from_org_owner()
    {
        var org = PlatformOrganizationId.New();
        var user = PlatformUserId.New();
        var grant = ProductLocalRoleGrant.Create(
            org,
            user,
            "pinoy-business-pos",
            ProductLocalRoleGrant.PosOwnerRoleCode,
            user,
            Utc);
        Assert.Equal("Owner", grant.RoleCode);
        Assert.Equal(ProductLocalRoleGrant.StartBusinessSource, grant.Source);
    }

    [Fact]
    public void Opening_balance_requires_provenance()
    {
        var ex = Assert.Throws<DomainException>(() =>
            BusinessCreditOpeningBalance.Create(
                PlatformOrganizationId.New(),
                CreditCustomerId.New(),
                BusinessCustomerId.New(),
                100m,
                "PHP",
                Utc,
                PersonalUtangMigrationSourceType.PersonalDebtRelationship,
                Guid.Empty,
                PersonalUtangMigrationBatchId.New(),
                PlatformUserId.New(),
                Utc,
                "pinoy-business-pos"));
        Assert.Equal(DomainErrorCodes.PersonalUtangMigrationSelectionRequired, ex.ErrorCode);
    }
}
