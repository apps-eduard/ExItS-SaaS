using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class OrganizationPaymentMethodSettingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 14, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BranchA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BranchB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void AllBranches_scope_is_available_at_any_branch_when_enabled()
    {
        var setting = OrganizationPaymentMethodSetting.Create(
            PosOrganizationId.From(OrgId),
            PaymentMethodCatalog.BankTransfer,
            isEnabled: true,
            displayName: null,
            requireReference: true,
            PaymentMethodBranchScope.AllBranches,
            selectedBranchIds: null,
            instructions: null,
            accountHint: null,
            T0);

        Assert.True(setting.IsAvailableForBranch(BranchA));
        Assert.True(setting.IsAvailableForBranch(BranchB));
    }

    [Fact]
    public void SelectedBranches_scope_filters_checkout_by_branch()
    {
        var setting = OrganizationPaymentMethodSetting.Create(
            PosOrganizationId.From(OrgId),
            PaymentMethodCatalog.BankTransfer,
            isEnabled: true,
            displayName: null,
            requireReference: true,
            PaymentMethodBranchScope.SelectedBranches,
            selectedBranchIds: [BranchA],
            instructions: null,
            accountHint: null,
            T0);

        Assert.True(setting.IsAvailableForBranch(BranchA));
        Assert.False(setting.IsAvailableForBranch(BranchB));

        var allowedA = PaymentMethodAccessGuard.EnsureCheckoutMethodAllowed(
            SalePaymentMethod.BankTransfer,
            BranchA,
            "Active",
            [PosFeatureCodes.StoreBasicPayments, PosFeatureCodes.StorePaymentManagement],
            [setting]);
        Assert.True(allowedA.IsSuccess);

        var deniedB = PaymentMethodAccessGuard.EnsureCheckoutMethodAllowed(
            SalePaymentMethod.BankTransfer,
            BranchB,
            "Active",
            [PosFeatureCodes.StoreBasicPayments, PosFeatureCodes.StorePaymentManagement],
            [setting]);
        Assert.False(deniedB.IsSuccess);
        Assert.Equal(DomainErrorCodes.PaymentMethodNotAvailableForBranch, deniedB.ErrorCode);
    }

    [Fact]
    public void Disabled_method_is_unavailable_even_for_selected_branch()
    {
        var setting = OrganizationPaymentMethodSetting.Create(
            PosOrganizationId.From(OrgId),
            PaymentMethodCatalog.Check,
            isEnabled: false,
            displayName: null,
            requireReference: true,
            PaymentMethodBranchScope.SelectedBranches,
            selectedBranchIds: [BranchA],
            instructions: null,
            accountHint: null,
            T0);

        Assert.False(setting.IsAvailableForBranch(BranchA));
    }
}
