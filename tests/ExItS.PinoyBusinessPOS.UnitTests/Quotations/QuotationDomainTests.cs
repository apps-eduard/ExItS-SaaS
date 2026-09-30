using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Quotations;

namespace ExItS.PinoyBusinessPOS.UnitTests.Quotations;

public sealed class QuotationDomainTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid CustomerA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid BranchA = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ActorA = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Quotation_numbers_format_and_normalize()
    {
        var date = new DateOnly(2026, 9, 14);
        Assert.Equal("QUO-260914-001", QuotationNumbers.Format(date, 1));
        Assert.Equal("260914-001", QuotationNumbers.Normalize(" 260914-001 "));
    }

    [Fact]
    public void Issue_freezes_snapshots_and_allocates_sent_state()
    {
        var quotation = CreateDraft();
        Assert.Equal(QuotationStatus.Draft, quotation.Status);
        Assert.Null(quotation.QuotationNumber);
        Assert.Null(quotation.IssuedAtUtc);

        quotation.Issue(
            "QUO-260914-007",
            [
                new QuotationLineSnapshotInput(
                    CatalogProductId.From(ProductA),
                    "Bigas Premium",
                    UnitOfMeasure.Kilogram,
                    2m,
                    55.5m,
                    "SKU-A",
                    DiscountAmount: 1m)
            ],
            Now);

        Assert.Equal(QuotationStatus.Sent, quotation.Status);
        Assert.Equal("QUO-260914-007", quotation.QuotationNumber);
        Assert.Equal(Now, quotation.IssuedAtUtc);
        var line = Assert.Single(quotation.Lines);
        Assert.Equal("Bigas Premium", line.NameSnapshot);
        Assert.Equal("SKU-A", line.SkuSnapshot);
        Assert.Equal(UnitOfMeasure.Kilogram, line.UomSnapshot);
        Assert.Equal(1m, line.DiscountAmount);
        Assert.Equal(110m, line.LineTotal); // RoundMoney(55.5*2 - 1) = 111 - 1 = 110
        Assert.False(quotation.IsCustomerVisible);

        var editAfterIssue = Assert.Throws<DomainException>(() =>
            quotation.UpdateDraft(
                POSCustomerId.From(CustomerA),
                "Customer",
                PosBranchId.From(BranchA),
                [new QuotationLineDraft(CatalogProductId.From(ProductA), 1m, 10m)],
                Now));
        Assert.Equal(DomainErrorCodes.InvalidQuotationStatusTransition, editAfterIssue.ErrorCode);
    }

    [Fact]
    public void MarkConverted_is_idempotent_for_same_sale_and_rejects_different_sale()
    {
        var quotation = CreateIssued();
        var saleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        quotation.MarkConverted(saleId, Now);
        Assert.Equal(QuotationStatus.Converted, quotation.Status);
        Assert.Equal(saleId, quotation.ConvertedSaleId);

        quotation.MarkConverted(saleId, Now.AddMinutes(1));
        Assert.Equal(QuotationStatus.Converted, quotation.Status);
        Assert.Equal(saleId, quotation.ConvertedSaleId);

        var other = Assert.Throws<DomainException>(() =>
            quotation.MarkConverted(Guid.Parse("22222222-2222-2222-2222-222222222222"), Now));
        Assert.Equal(DomainErrorCodes.InvalidQuotationStatusTransition, other.ErrorCode);
    }

    [Fact]
    public void Quotation_domain_has_no_stock_or_payment_surface()
    {
        var quotationType = typeof(Quotation);
        var methodNames = quotationType.GetMethods()
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Deduct", methodNames);
        Assert.DoesNotContain("Reserve", methodNames);
        Assert.DoesNotContain("Checkout", methodNames);
        Assert.DoesNotContain("CreateSale", methodNames);
        Assert.DoesNotContain("ApplyPayment", methodNames);
        Assert.Contains("Issue", methodNames);
        Assert.Contains("MarkConverted", methodNames);

        // MarkConverted only stores the sale id — it does not construct a Sale.
        var issued = CreateIssued();
        issued.MarkConverted(Guid.Parse("33333333-3333-3333-3333-333333333333"), Now);
        Assert.Equal(QuotationStatus.Converted, issued.Status);
        Assert.NotNull(issued.ConvertedSaleId);
    }

    [Fact]
    public void Draft_rejects_duplicate_products()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Quotation.CreateDraft(
                PosOrganizationId.From(OrgA),
                POSCustomerId.From(CustomerA),
                "Customer A",
                PosBranchId.From(BranchA),
                ActorA,
                [
                    new QuotationLineDraft(CatalogProductId.From(ProductA), 1m, 10m),
                    new QuotationLineDraft(CatalogProductId.From(ProductA), 2m, 10m)
                ],
                Now));
        Assert.Equal(DomainErrorCodes.QuotationDuplicateProduct, ex.ErrorCode);
    }

    private static Quotation CreateDraft() =>
        Quotation.CreateDraft(
            PosOrganizationId.From(OrgA),
            POSCustomerId.From(CustomerA),
            "Customer A",
            PosBranchId.From(BranchA),
            ActorA,
            [new QuotationLineDraft(CatalogProductId.From(ProductA), 2m, 55.5m, DiscountAmount: 1m)],
            Now,
            customerMobileNumber: "09171234567",
            customerAddress: "Manila",
            notes: "Seller note");

    private static Quotation CreateIssued()
    {
        var quotation = CreateDraft();
        quotation.Issue(
            "QUO-260914-099",
            [
                new QuotationLineSnapshotInput(
                    CatalogProductId.From(ProductA),
                    "Item",
                    UnitOfMeasure.Piece,
                    2m,
                    55.5m,
                    DiscountAmount: 1m)
            ],
            Now);
        return quotation;
    }
}
