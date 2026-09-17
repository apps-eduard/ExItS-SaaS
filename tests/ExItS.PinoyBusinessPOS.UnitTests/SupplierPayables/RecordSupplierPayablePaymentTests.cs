using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.SupplierPayables;

public sealed class RecordSupplierPayablePaymentTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-17T08:00:00Z");

    [Fact]
    public async Task Execute_rejects_buyer_manual_self_settlement()
    {
        var payables = new InMemoryPayables();
        var payable = SupplierPayable.Create(
            PosOrganizationId.From(OrgId),
            SupplierId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            SupplierPayableSourceType.GoodsReceipt,
            Guid.NewGuid(),
            originalAmount: 100m,
            createdBy: Actor,
            utcNow: Now,
            paidNow: 0m);
        await payables.AddAsync(payable);

        var useCase = new RecordSupplierPayablePayment(
            payables,
            new FakeAccess(),
            new FixedClock(Now));

        var result = await useCase.ExecuteAsync(
            OrgId,
            payable.Id.Value,
            new RecordSupplierPayablePaymentRequest(25m, "Cash"),
            Actor);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.SupplierPayableBuyerManualSettlementForbidden, result.ErrorCode);
        Assert.Equal(100m, (await payables.GetByIdAsync(PosOrganizationId.From(OrgId), payable.Id))!.Balance);
    }

    [Fact]
    public void ApplyPayment_still_allows_direct_mirror_settlement()
    {
        var payable = SupplierPayable.Create(
            PosOrganizationId.From(OrgId),
            SupplierId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")),
            SupplierPayableSourceType.GoodsReceipt,
            Guid.NewGuid(),
            originalAmount: 100m,
            createdBy: Actor,
            utcNow: Now,
            paidNow: 0m);

        var payment = payable.ApplyPayment(
            40m,
            SupplierPayablePaymentMethod.Cash,
            Actor,
            Now,
            reference: "b2b-sync:seller");

        Assert.Equal(40m, payment.Amount);
        Assert.Equal(60m, payable.Balance);
        Assert.True(payable.HasPostedPayments);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeAccess : IPosCommercialAccessAccessor
    {
        public PosCommercialAccess Current { get; set; } = PosCommercialAccess.DevelopmentDefault;
    }

    private sealed class InMemoryPayables : ISupplierPayableRepository
    {
        private readonly List<SupplierPayable> _items = [];

        public Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default)
        {
            _items.Add(payable);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SupplierPayable?> GetByIdAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(p => p.Id == payableId && p.OrganizationId == organizationId));

        public Task<SupplierPayable?> FindBySourceAsync(
            PosOrganizationId organizationId,
            SupplierPayableSourceType sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SupplierPayable?>(null);

        public Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            SupplierPayableFilter filter,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<SupplierPayable>, int)>(([], 0));

        public Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(
            PosOrganizationId organizationId,
            SupplierPayableId payableId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupplierPayablePayment>>([]);

        public Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(
            PosOrganizationId organizationId,
            SupplierId supplierId,
            DateOnly asOfDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SupplierPayableSummaryTotals(0m, 0m, 0));
    }
}
