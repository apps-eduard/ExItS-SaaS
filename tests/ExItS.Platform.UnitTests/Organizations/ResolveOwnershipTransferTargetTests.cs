using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class ResolveOwnershipTransferTargetTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Allows_personal_ex_id_and_personal_qr()
    {
        var users = new InMemoryPlatformUserRepository();
        var personal = PlatformUser.Create("aliceowner", "Alice Owner", "alice@example.com", T0);
        personal.AssignPublicUserId("EX-4827-1936", T0);
        await users.AddAsync(personal);

        var useCase = new ResolveOwnershipTransferTarget(users);
        var byId = await useCase.ExecuteAsync("EX-4827-1936");
        Assert.True(byId.IsSuccess);
        Assert.Equal("EX-4827-1936", byId.Value!.PublicUserId);
        Assert.Equal("Alice Owner", byId.Value.DisplayName);

        var byQr = await useCase.ExecuteAsync("exits://qr/v1/personal/EX-4827-1936");
        Assert.True(byQr.IsSuccess);
        Assert.Equal("EX-4827-1936", byQr.Value!.PublicUserId);
    }

    [Fact]
    public async Task Rejects_business_and_device_qr()
    {
        var useCase = new ResolveOwnershipTransferTarget(new InMemoryPlatformUserRepository());

        var business = await useCase.ExecuteAsync("exits://qr/v1/organization/ORG001842");
        Assert.False(business.IsSuccess);
        Assert.Equal(DomainErrorCodes.OwnershipTransferQrPurposeRejected, business.ErrorCode);
        Assert.Contains("Business QR", business.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Personal QR", business.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var device = await useCase.ExecuteAsync("exits://qr/v1/pos-device-registration/opaque-token-value");
        Assert.False(device.IsSuccess);
        Assert.Equal(DomainErrorCodes.OwnershipTransferQrPurposeRejected, device.ErrorCode);
        Assert.Contains("POS device", device.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_organization_scoped_staff_identity()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgId = PlatformOrganizationId.New();
        var staff = PlatformUser.CreateOrganizationStaff(
            "maria",
            "maria@org001842",
            "maria@example.com",
            orgId,
            "Maria Staff",
            T0);
        staff.AssignPublicUserId("EX-1111-2222", T0);
        await users.AddAsync(staff);

        var useCase = new ResolveOwnershipTransferTarget(users);
        var result = await useCase.ExecuteAsync("EX-1111-2222");
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.OwnershipTransferTargetInvalid, result.ErrorCode);
    }
}
