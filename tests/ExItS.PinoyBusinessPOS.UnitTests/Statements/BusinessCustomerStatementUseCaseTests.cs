using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Statements;

public sealed class BusinessCustomerStatementUseCaseTests
{
    private static readonly Guid SellerOrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BuyerOrgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-14T12:00:00Z");

    [Fact]
    public async Task Statement_includes_period_credits_and_outstanding()
    {
        var (relationships, credits, connectionId, access) = await CreateHarnessAsync();
        var seller = PosOrganizationId.From(SellerOrgId);
        var buyer = PosOrganizationId.From(BuyerOrgId);

        var before = BusinessCreditEntry.Create(seller, buyer, 100m, "Before", Now.AddDays(-40), connectionId);
        var inPeriod = BusinessCreditEntry.Create(
            seller,
            buyer,
            542m,
            "Product sale SALE-20260914-000008",
            Now.AddDays(-1),
            connectionId);
        inPeriod.ApplyCurrentDueDate(DateOnly.FromDateTime(Now.AddDays(29).UtcDateTime));
        await credits.AddAsync(before);
        await credits.AddAsync(inPeriod);

        var useCase = new GetBusinessCustomerStatement(
            relationships,
            credits,
            access,
            new FixedClock(Now));

        var result = await useCase.ExecuteAsync(
            SellerOrgId,
            connectionId,
            DateOnly.FromDateTime(Now.AddDays(-30).UtcDateTime),
            DateOnly.FromDateTime(Now.UtcDateTime),
            "Seller Store",
            "PHP",
            "en-PH");

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(100m, dto.OpeningBalance);
        Assert.Equal(542m, dto.PeriodCreditTotal);
        Assert.Equal(0m, dto.PeriodRepaymentTotal);
        Assert.Equal(642m, dto.ClosingBalance);
        Assert.Equal(642m, dto.OutstandingBalance);
        Assert.Equal("Buyer Bakery", dto.CustomerDisplayName);
        Assert.Single(dto.Lines);
        Assert.Equal("Credit", dto.Lines[0].EntryType);
        Assert.Equal(542m, dto.Lines[0].Amount);
        Assert.Contains("SALE-20260914", dto.Lines[0].Remarks);
    }

    [Fact]
    public async Task Statement_rejects_inverted_period()
    {
        var (relationships, credits, connectionId, access) = await CreateHarnessAsync();
        var useCase = new GetBusinessCustomerStatement(
            relationships,
            credits,
            access,
            new FixedClock(Now));

        var result = await useCase.ExecuteAsync(
            SellerOrgId,
            connectionId,
            DateOnly.FromDateTime(Now.UtcDateTime),
            DateOnly.FromDateTime(Now.AddDays(-1).UtcDateTime),
            null,
            "PHP",
            "en-PH");

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.StatementInvalidPeriod, result.ErrorCode);
    }

    private static async Task<(
        InMemoryRelationshipRepository Relationships,
        InMemoryBusinessCredits Credits,
        Guid ConnectionId,
        PosCommercialAccessAccessor Access)> CreateHarnessAsync()
    {
        var seller = PosOrganizationId.From(SellerOrgId);
        var buyer = PosOrganizationId.From(BuyerOrgId);
        var relationship = ConnectedSupplierRelationship.InviteBuyer(
            buyer,
            seller,
            Now.AddDays(-60),
            Actor,
            buyerDisplayName: "Buyer Bakery",
            buyerPublicOrganizationId: "ORG111111",
            supplierDisplayName: "Seller Store",
            supplierPublicOrganizationId: "ORG222222");
        relationship.Approve(Now.AddDays(-59), Actor);
        var relationships = new InMemoryRelationshipRepository();
        await relationships.AddAsync(relationship);
        var access = new PosCommercialAccessAccessor
        {
            Current = PosCommercialAccess.DevelopmentDefault
        };
        return (relationships, new InMemoryBusinessCredits(), relationship.Id.Value, access);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class InMemoryBusinessCredits : IBusinessCreditEntryRepository
    {
        private readonly List<BusinessCreditEntry> _entries = [];

        public Task<BusinessCreditEntry?> GetByIdAsync(
            PosOrganizationId sellerOrganizationId,
            BusinessCreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(e =>
                e.SellerOrganizationId == sellerOrganizationId && e.Id == entryId));

        public Task AddAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _entries
                    .Where(e =>
                        e.SellerOrganizationId == sellerOrganizationId
                        && e.BuyerOrganizationId == buyerOrganizationId
                        && e.Status == CreditEntryStatus.Active)
                    .Sum(e => e.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByBuyerIdsAsync(
            PosOrganizationId sellerOrganizationId,
            IReadOnlyCollection<Guid> buyerOrganizationIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                _entries
                    .Where(e =>
                        e.SellerOrganizationId == sellerOrganizationId
                        && buyerOrganizationIds.Contains(e.BuyerOrganizationId.Value)
                        && e.Status == CreditEntryStatus.Active)
                    .GroupBy(e => e.BuyerOrganizationId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount)));

        public Task<IReadOnlyList<BusinessCreditEntry>> ListChronologicalForBuyerAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BusinessCreditEntry>>(
                _entries
                    .Where(e =>
                        e.SellerOrganizationId == sellerOrganizationId
                        && e.BuyerOrganizationId == buyerOrganizationId)
                    .OrderBy(e => e.CreatedAtUtc)
                    .ThenBy(e => e.Id.Value)
                    .ToList());

        public Task AcquireBusinessCreditLockAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryRelationshipRepository : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<ConnectedSupplierRelationship>)_items);

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
