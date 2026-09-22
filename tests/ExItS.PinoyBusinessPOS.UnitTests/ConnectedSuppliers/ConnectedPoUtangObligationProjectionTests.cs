using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoUtangObligationProjectionTests
{
    [Fact]
    public void ResolvePaidAtReceipt_Utang_omitted_PaidNow_is_zero_not_full_receipt()
    {
        var paid = ConnectedPoUtangObligationProjection.ResolvePaidAtReceipt(
            ConnectedPoPaymentTerm.Utang,
            receivedAmount: 643m,
            requestedPaidNow: null);

        Assert.Equal(0m, paid);
        Assert.Equal(
            643m,
            ConnectedPoUtangObligationProjection.ObligationAmount(643m, paid!.Value));
    }

    [Fact]
    public void ResolvePaidAtReceipt_Cash_omitted_PaidNow_stays_null_for_payable_default()
    {
        var paid = ConnectedPoUtangObligationProjection.ResolvePaidAtReceipt(
            ConnectedPoPaymentTerm.Cash,
            receivedAmount: 643m,
            requestedPaidNow: null);

        Assert.Null(paid);
    }

    [Fact]
    public void ObligationAmount_partial_receipt_payment_matches_both_projections()
    {
        Assert.Equal(400m, ConnectedPoUtangObligationProjection.ObligationAmount(643m, 243m));
        Assert.Equal(0m, ConnectedPoUtangObligationProjection.ObligationAmount(643m, 643m));
        Assert.Equal(0m, ConnectedPoUtangObligationProjection.ObligationAmount(100m, 150m));
    }

    [Fact]
    public void Mixed_PO_and_Direct_Utang_reconcile_to_1390_on_30000_limit()
    {
        var poObligation = ConnectedPoUtangObligationProjection.ObligationAmount(643m, 0m);
        var directObligation = ConnectedPoUtangObligationProjection.ObligationAmount(747m, 0m);
        var outstanding = poObligation + directObligation;
        const decimal limit = 30_000m;
        var reserved = 0m;
        var available = limit - outstanding - reserved;

        Assert.Equal(643m, poObligation);
        Assert.Equal(747m, directObligation);
        Assert.Equal(1_390m, outstanding);
        Assert.Equal(28_610m, available);
    }

    [Fact]
    public void GoodsReceipt_remark_round_trips_for_reversal_lookup()
    {
        var grnId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var poId = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var remark = ConnectedPoUtangObligationProjection.BuildGoodsReceiptRemark(
            grnId,
            "PO-100",
            poId);

        Assert.True(
            ConnectedPoUtangObligationProjection.TryParseGoodsReceiptId(remark, out var parsed));
        Assert.Equal(grnId, parsed);
        Assert.Equal(
            "PO-100",
            ConnectedPoUtangObligationProjection.TryFormatSourceLabelFromRemark(remark));
        Assert.False(
            ConnectedPoUtangObligationProjection.TryParseGoodsReceiptId("unrelated", out _));
    }

    [Fact]
    public void DirectPurchase_remark_round_trips_and_formats_source_label()
    {
        var receiptId = Guid.Parse("33333333-3333-4333-8333-333333333333");
        var remark = ConnectedPoUtangObligationProjection.BuildDirectPurchaseRemark(
            receiptId,
            "260916-001");

        Assert.True(
            ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                remark,
                out var parsed));
        Assert.Equal(receiptId, parsed);
        Assert.Equal(
            "Direct purchase 260916-001",
            ConnectedPoUtangObligationProjection.TryFormatSourceLabelFromRemark(remark));
        Assert.False(
            ConnectedPoUtangObligationProjection.TryParseDirectPurchaseReceiptId(
                "grn:11111111-1111-4111-8111-111111111111|Connected PO PO-1 receipt",
                out _));
    }

    [Fact]
    public void TryResolveSource_strips_sale_guid_prefix_for_clean_label()
    {
        var saleId = Guid.Parse("44444444-4444-4444-8444-444444444444");
        var remark = ConnectedPoUtangObligationProjection.BuildSaleRemark(saleId, "260917-001");

        Assert.True(
            ConnectedPoUtangObligationProjection.TryResolveSource(
                remark,
                sourceSaleId: null,
                out var sourceType,
                out var sourceId,
                out var label));
        Assert.Equal("Sale", sourceType);
        Assert.Equal(saleId, sourceId);
        Assert.DoesNotContain("sale:", label ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(saleId.ToString("D"), label ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("260917-001", label ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
