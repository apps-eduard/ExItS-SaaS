using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.OperationalSetup;

public sealed class PosOperationalSetupCashCountTests
{
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly RegisterId Register = RegisterId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_organization_defaults_to_optional_cash_count()
    {
        var setup = PosOperationalSetup.CreateIncomplete(Org, Actor, Now);
        Assert.Equal(CashCountMode.Optional, setup.CashCountMode);
        Assert.Equal(CashCountMode.Optional, setup.OpeningCashCountMode);
        Assert.Equal(CashCountMode.Optional, setup.ClosingCashCountMode);
    }

    [Fact]
    public void Complete_without_mode_keeps_optional_default()
    {
        var setup = PosOperationalSetup.CreateIncomplete(Org, Actor, Now);
        setup.Complete("Sari-Sari", "PHP", TaxPricingMode.TaxExclusive, 12m, null, null, null, null, Register, Actor, Now);
        Assert.Equal(CashCountMode.Optional, setup.CashCountMode);
        Assert.Equal(CashCountMode.Optional, setup.OpeningCashCountMode);
        Assert.Equal(CashCountMode.Optional, setup.ClosingCashCountMode);
    }

    [Fact]
    public void Parse_configurable_rejects_off()
    {
        var ex = Assert.Throws<DomainException>(() => CashCountModes.ParseConfigurable("Off"));
        Assert.Equal(DomainErrorCodes.CashCountModeOffRetired, ex.ErrorCode);
    }

    [Fact]
    public void For_new_shift_treats_legacy_off_as_optional()
    {
        Assert.Equal(CashCountMode.Optional, CashCountModes.ForNewShift(CashCountMode.Off));
        Assert.Equal(CashCountMode.Required, CashCountModes.ForNewShift(CashCountMode.Required));
    }

    [Fact]
    public void Update_can_change_cash_count_mode()
    {
        var setup = PosOperationalSetup.CreateIncomplete(Org, Actor, Now);
        setup.Complete("Sari-Sari", "PHP", TaxPricingMode.TaxExclusive, 0m, null, null, null, null, Register, Actor, Now, CashCountMode.Optional);
        setup.Update("Sari-Sari", "PHP", TaxPricingMode.TaxExclusive, 0m, null, null, null, null, Actor, Now.AddMinutes(1), CashCountMode.Required);
        Assert.Equal(CashCountMode.Required, setup.CashCountMode);
        Assert.Equal(CashCountMode.Required, setup.OpeningCashCountMode);
        Assert.Equal(CashCountMode.Required, setup.ClosingCashCountMode);
    }

    [Fact]
    public void Update_can_set_opening_and_closing_independently()
    {
        var setup = PosOperationalSetup.CreateIncomplete(Org, Actor, Now);
        setup.Complete("Sari-Sari", "PHP", TaxPricingMode.TaxExclusive, 0m, null, null, null, null, Register, Actor, Now);
        setup.Update(
            "Sari-Sari",
            "PHP",
            TaxPricingMode.TaxExclusive,
            0m,
            null,
            null,
            null,
            null,
            Actor,
            Now.AddMinutes(1),
            openingCashCountMode: CashCountMode.Required,
            closingCashCountMode: CashCountMode.Optional);
        Assert.Equal(CashCountMode.Required, setup.OpeningCashCountMode);
        Assert.Equal(CashCountMode.Optional, setup.ClosingCashCountMode);
        Assert.Equal(CashCountMode.Required, setup.CashCountMode);
    }

    [Fact]
    public void Parse_rejects_unknown_cash_count_mode()
    {
        var ex = Assert.Throws<DomainException>(() => CashCountModes.Parse("Always"));
        Assert.Equal(DomainErrorCodes.InvalidCashCountMode, ex.ErrorCode);
    }
}
