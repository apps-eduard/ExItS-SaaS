using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class BusinessCustomerCreditAuthorizationServiceTests
{
    private static readonly PosOrganizationId Seller = PosOrganizationId.From(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-13T08:00:00Z");
    private static readonly DateOnly BusinessDate = new(2026, 9, 13);

    [Fact]
    public async Task Approved_within_limit_succeeds()
    {
        var policies = new InMemoryPolicies();
        var credits = new InMemoryBusinessCredits();
        await SeedApprovedAsync(policies, creditLimit: 1_000m, termDays: 14);
        credits.SeedActive(Seller, Buyer, 200m);

        var service = new BusinessCustomerCreditAuthorizationService(policies, credits);
        var result = await service.AuthorizeNewCreditAsync(Seller, Buyer, 100m, BusinessDate);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(200m, result.Value!.Outstanding);
        Assert.Equal(800m, result.Value.AvailableCredit);
        Assert.Equal(BusinessDate.AddDays(14), result.Value.DefaultDueDate);
    }

    [Fact]
    public async Task Over_limit_fails()
    {
        var policies = new InMemoryPolicies();
        var credits = new InMemoryBusinessCredits();
        await SeedApprovedAsync(policies, creditLimit: 500m, termDays: 7);
        credits.SeedActive(Seller, Buyer, 400m);

        var service = new BusinessCustomerCreditAuthorizationService(policies, credits);
        var result = await service.AuthorizeNewCreditAsync(Seller, Buyer, 150m, BusinessDate);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessCustomerCreditLimitExceeded, result.ErrorCode);
    }

    [Fact]
    public async Task Not_approved_fails()
    {
        var policies = new InMemoryPolicies();
        var credits = new InMemoryBusinessCredits();
        var (policy, _) = BusinessCustomerCreditPolicy.Configure(
            Seller,
            Buyer,
            connectionId: Guid.NewGuid(),
            creditLimit: 1_000m,
            defaultTermDays: 30,
            Actor,
            "cfg",
            Now);
        await policies.AddAsync(policy);

        var service = new BusinessCustomerCreditAuthorizationService(policies, credits);
        var result = await service.AuthorizeNewCreditAsync(Seller, Buyer, 50m, BusinessDate);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessCustomerCreditNotApproved, result.ErrorCode);
    }

    private static async Task SeedApprovedAsync(
        InMemoryPolicies policies,
        decimal creditLimit,
        int termDays)
    {
        var (policy, _) = BusinessCustomerCreditPolicy.Configure(
            Seller,
            Buyer,
            connectionId: Guid.NewGuid(),
            creditLimit,
            termDays,
            Actor,
            "cfg",
            Now);
        policy.Approve(Actor, "approve", Now.AddSeconds(1));
        await policies.AddAsync(policy);
    }

    private sealed class InMemoryPolicies : IBusinessCustomerCreditPolicyRepository
    {
        private readonly List<BusinessCustomerCreditPolicy> _policies = [];

        public Task<BusinessCustomerCreditPolicy?> GetBySellerAndBuyerAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_policies.FirstOrDefault(p =>
                p.SellerOrganizationId == sellerOrganizationId && p.BuyerOrganizationId == buyerOrganizationId));

        public Task<IReadOnlyList<BusinessCustomerCreditPolicy>> ListBySellerAndBuyerIdsAsync(
            PosOrganizationId sellerOrganizationId,
            IReadOnlyCollection<Guid> buyerOrganizationIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BusinessCustomerCreditPolicy>>(
                _policies
                    .Where(p =>
                        p.SellerOrganizationId == sellerOrganizationId
                        && buyerOrganizationIds.Contains(p.BuyerOrganizationId.Value))
                    .ToList());

        public Task AddAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default)
        {
            _policies.Add(policy);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddChangeAsync(
            BusinessCustomerCreditPolicyChange change,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<(IReadOnlyList<BusinessCustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                ((IReadOnlyList<BusinessCustomerCreditPolicyChange>)Array.Empty<BusinessCustomerCreditPolicyChange>(),
                    0));

        public Task AcquireBusinessCustomerCreditLockAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryBusinessCredits : IBusinessCreditEntryRepository
    {
        private readonly List<BusinessCreditEntry> _entries = [];

        public void SeedActive(
            PosOrganizationId seller,
            PosOrganizationId buyer,
            decimal amount) =>
            _entries.Add(BusinessCreditEntry.Create(seller, buyer, amount, "seed", Now));

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

        public Task AcquireBusinessCreditLockAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
