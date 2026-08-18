using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationBranchAndPosDeviceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 10, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Main_branch_is_active_primary_and_uses_stable_main_code()
    {
        var branch = OrganizationBranch.CreateMainBranch(PlatformOrganizationId.New(), T0);

        Assert.Equal("MAIN", branch.Code);
        Assert.Equal("Main Branch", branch.Name);
        Assert.True(branch.IsPrimary);
        Assert.Equal(OrganizationBranchStatus.Active, branch.Status);
    }

    [Fact]
    public void Additional_branch_normalizes_code_and_can_be_archived()
    {
        var branch = OrganizationBranch.Create(PlatformOrganizationId.New(), " north-1 ", "North Branch", T0);

        Assert.Equal("NORTH-1", branch.Code);
        Assert.False(branch.IsPrimary);
        branch.Archive(T0.AddMinutes(1));
        Assert.Equal(OrganizationBranchStatus.Archived, branch.Status);
        Assert.Throws<DomainException>(() => branch.Activate(T0.AddMinutes(2)));
    }

    [Fact]
    public void Pos_device_registration_normalizes_installation_id_and_revoke_is_idempotent()
    {
        var device = PosDevice.Register(PlatformOrganizationId.New(), OrganizationBranchId.New(), " device-guid ", "Front Counter", T0);
        var user = PlatformUserId.New();

        Assert.Equal("device-guid", device.InstallationDeviceId);
        device.Revoke(user, T0.AddMinutes(1));
        device.Revoke(PlatformUserId.New(), T0.AddMinutes(2));

        Assert.Equal(PosDeviceStatus.Revoked, device.Status);
        Assert.Equal(user, device.RevokedByUserId);
        Assert.Equal(T0.AddMinutes(1), device.RevokedAtUtc);
    }

    [Fact]
    public void Additional_branch_does_not_enable_fulfillment_or_clone_primary()
    {
        var org = PlatformOrganizationId.New();
        var branch = OrganizationBranch.Create(org, "north-1", "North Branch", T0);

        Assert.False(branch.IsPrimary);
        Assert.False(branch.PickupEnabled);
        Assert.False(branch.DeliveryEnabled);
        Assert.False(branch.CustomerOrderingEnabled);
        Assert.Null(typeof(OrganizationBranch).GetProperty("CatalogProductId"));
    }

    [Fact]
    public void Customer_link_is_organization_scoped_not_branch_owned()
    {
        Assert.Null(typeof(CustomerLinkRequest).GetProperty("BranchId"));
        Assert.Null(typeof(LinkedCustomerAppUser).GetProperty("BranchId"));
        Assert.NotNull(typeof(CustomerLinkRequest).GetProperty("OrganizationId"));
        Assert.NotNull(typeof(LinkedCustomerAppUser).GetProperty("OrganizationId"));
    }

    [Fact]
    public void Subscription_upgrade_source_does_not_create_branches_or_enable_fulfillment()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var path = Path.Combine(
            dir!.FullName,
            "src",
            "Platform",
            "ExItS.Platform.Application",
            "Subscriptions",
            "PlanChangeUseCases.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("class UpgradeOrganizationSubscription", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IOrganizationBranchRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateBranch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SetFulfillmentCapabilities", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PickupEnabled = true", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_branch_source_does_not_clone_catalog_customers_staff_or_devices()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var path = Path.Combine(
            dir!.FullName,
            "src",
            "Platform",
            "ExItS.Platform.Application",
            "Organizations",
            "BranchUseCases.cs");
        var source = File.ReadAllText(path);
        var start = source.IndexOf("public sealed class CreateBranch", StringComparison.Ordinal);
        var end = source.IndexOf("public sealed class UpdateBranch", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var create = source[start..end];
        Assert.DoesNotContain("CatalogProduct", create, StringComparison.Ordinal);
        Assert.DoesNotContain("BusinessCustomer", create, StringComparison.Ordinal);
        Assert.DoesNotContain("PosDevice", create, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizationMember", create, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventory", create, StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_primary_business_type_is_immutable()
    {
        var organization = PlatformOrganization.Create("Store", "store", T0);
        var type = BusinessTypeId.New();
        organization.AssignPrimaryBusinessType(type, T0.AddMinutes(1));
        organization.AssignPrimaryBusinessType(type, T0.AddMinutes(2));

        var exception = Assert.Throws<DomainException>(() =>
            organization.AssignPrimaryBusinessType(BusinessTypeId.New(), T0.AddMinutes(3)));

        Assert.Equal(DomainErrorCodes.PrimaryBusinessTypeImmutable, exception.ErrorCode);
    }
}
