using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Customers;

public sealed class LinkedCustomerAuthorizationUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlatformCustomer = Guid.Parse("cccccccc-cccc-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid PersonalUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkedCustomer = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Correlated_pos_customer_authorizes_with_expected_context()
    {
        var repo = new InMemoryCustomerRepository();
        var created = POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Rosa Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer);
        await repo.AddAsync(created);
        var platform = FakePlatform.Authorized();
        var useCase = new AuthorizeLinkedCustomerStatementAccess(platform, repo);

        var result = await useCase.ExecuteAsync(OrgA, PlatformCustomer, created.Id.Value);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(PersonalUser, result.Value!.PersonalUserId);
        Assert.Equal(OrgA, result.Value.OrganizationId);
        Assert.Equal(PlatformCustomer, result.Value.PlatformBusinessCustomerId);
        Assert.Equal(LinkedCustomer, result.Value.LinkedCustomerAppUserId);
        Assert.Equal(created.Id.Value, result.Value.PosCustomerId);
        Assert.Equal(
            new[]
            {
                nameof(result.Value.LinkedCustomerAppUserId),
                nameof(result.Value.OrganizationId),
                nameof(result.Value.PersonalUserId),
                nameof(result.Value.PlatformBusinessCustomerId),
                nameof(result.Value.PosCustomerId)
            },
            result.Value.GetType().GetProperties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Platform_not_found_is_mapped_to_safe_not_found()
    {
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.NotFound(), new InMemoryCustomerRepository());
        var result = await useCase.ExecuteAsync(OrgA, PlatformCustomer);
        AssertNotFound(result);
    }

    [Fact]
    public async Task Platform_denied_is_mapped_to_denied()
    {
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Denied(), new InMemoryCustomerRepository());
        var result = await useCase.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerDenied, result.ErrorCode);
        Assert.Equal("Linked customer access is denied.", result.ErrorMessage);
    }

    /// <summary>
    /// Personal Business Utang statement visibility still requires an Active Platform link.
    /// Non-linked connection states are represented here as Platform NotFound / Denied outcomes.
    /// </summary>
    [Theory]
    [InlineData("NotLinked")]
    [InlineData("Pending")]
    [InlineData("Declined")]
    [InlineData("Revoked")]
    [InlineData("Blocked")]
    [InlineData("Unavailable")]
    public async Task Personal_statement_visibility_denied_without_active_link(string connectionState)
    {
        _ = connectionState;
        var repo = new InMemoryCustomerRepository();
        await repo.AddAsync(POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Rosa Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer));
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.NotFound(), repo);

        var result = await useCase.ExecuteAsync(OrgA, PlatformCustomer);

        AssertNotFound(result);
    }

    [Fact]
    public async Task Personal_statement_visibility_allowed_when_platform_link_is_active()
    {
        var repo = new InMemoryCustomerRepository();
        var created = POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Rosa Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer);
        await repo.AddAsync(created);
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), repo);

        var result = await useCase.ExecuteAsync(OrgA, PlatformCustomer);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(created.Id.Value, result.Value!.PosCustomerId);
    }

    [Fact]
    public async Task Missing_pos_customer_is_not_found()
    {
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), new InMemoryCustomerRepository());
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer));
    }

    [Fact]
    public async Task Legacy_null_correlation_is_not_found()
    {
        var repo = new InMemoryCustomerRepository();
        await repo.AddAsync(POSCustomer.Create(PosOrganizationId.From(OrgA), "Legacy Customer", T0));
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), repo);
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer));
    }

    [Fact]
    public async Task Mismatched_platform_business_customer_id_is_not_found()
    {
        var repo = new InMemoryCustomerRepository();
        await repo.AddAsync(POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Other Customer",
            T0,
            platformBusinessCustomerId: Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")));
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), repo);
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer));
    }

    [Fact]
    public async Task Pos_customer_in_another_organization_is_not_found()
    {
        var repo = new InMemoryCustomerRepository();
        await repo.AddAsync(POSCustomer.Create(
            PosOrganizationId.From(OrgB),
            "Other Org Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer));
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), repo);
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer));
    }

    [Fact]
    public async Task Optional_pos_customer_id_mismatch_is_not_found()
    {
        var repo = new InMemoryCustomerRepository();
        var created = POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Rosa Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer);
        await repo.AddAsync(created);
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), repo);
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer, Guid.NewGuid()));
    }

    [Fact]
    public async Task Duplicate_correlation_in_same_org_fails_closed()
    {
        var repo = new InMemoryCustomerRepository();
        await repo.AddAsync(POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "First Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer));
        await repo.AddAsync(POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Second Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer));
        var useCase = new AuthorizeLinkedCustomerStatementAccess(FakePlatform.Authorized(), repo);
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer));
    }

    [Fact]
    public async Task Platform_proof_org_mismatch_is_not_found()
    {
        var repo = new InMemoryCustomerRepository();
        await repo.AddAsync(POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Rosa Customer",
            T0,
            platformBusinessCustomerId: PlatformCustomer));
        var platform = new FakePlatform
        {
            Result = new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    PersonalUser,
                    OrgB,
                    PlatformCustomer,
                    LinkedCustomer))
        };
        var useCase = new AuthorizeLinkedCustomerStatementAccess(platform, repo);
        AssertNotFound(await useCase.ExecuteAsync(OrgA, PlatformCustomer));
    }

    [Fact]
    public async Task Create_still_rejects_duplicate_correlation()
    {
        var repo = new InMemoryCustomerRepository();
        var create = new CreatePOSCustomer(repo, new ImmediateUnitOfWork(), new FixedClock(T0));
        Assert.True((await create.ExecuteAsync(OrgA, "One", null, null, null, platformBusinessCustomerId: PlatformCustomer)).IsSuccess);
        var second = await create.ExecuteAsync(OrgA, "Two", null, null, null, platformBusinessCustomerId: PlatformCustomer);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict, second.ErrorCode);
    }

    private static void AssertNotFound(ApplicationResult<AuthorizedLinkedCustomerContext> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
        Assert.Equal("Linked customer was not found.", result.ErrorMessage);
    }

    private sealed class FakePlatform : ILinkedCustomerPlatformAuthorization
    {
        public LinkedCustomerPlatformAuthorizationResult Result { get; set; } =
            new(LinkedCustomerPlatformAuthorizationOutcome.NotFound, null);

        public static FakePlatform Authorized() => new()
        {
            Result = new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    PersonalUser,
                    OrgA,
                    PlatformCustomer,
                    LinkedCustomer))
        };

        public static FakePlatform Denied() => new()
        {
            Result = new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Denied,
                null)
        };

        public static FakePlatform NotFound() => new();

        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class InMemoryCustomerRepository : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public Task<POSCustomer?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.Status == CustomerStatus.Active
                && c.NormalizedMobile == normalizedMobile));

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<POSCustomer?> FindByLinkedPersonalPublicUserIdAsync(
            PosOrganizationId organizationId,
            string linkedPersonalPublicUserId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);

        public Task<POSCustomer?> FindByLinkedBuyerOrganizationIdAsync(
            PosOrganizationId organizationId,
            Guid linkedBuyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<POSCustomer?>(null);



        public Task<int> CountByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(c =>
                c.OrganizationId == organizationId
                && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            CustomerStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _items.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId,
            DateTimeOffset? sinceUtc,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<POSCustomerId> customerIds,
            CancellationToken cancellationToken = default)
        {
            var ids = customerIds.Select(c => c.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id.Value)).ToList());
        }

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(c => c.Id == customer.Id);
            if (index >= 0)
            {
                _items[index] = customer;
            }

            return Task.CompletedTask;
        }
    }
}
