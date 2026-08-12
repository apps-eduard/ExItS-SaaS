using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Statements;

public sealed class LinkedCustomerStatementUseCaseTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PlatformCustomer = Guid.Parse("cccccccc-cccc-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OtherPlatformCustomer = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PersonalUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LinkedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Summary_returns_outstanding_for_authorized_linked_customer()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 100m, "Goods", T0));
        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 40m, "Partial", Actor, T0.AddHours(1)));

        var result = await harness.Summary.ExecuteAsync(OrgA, PlatformCustomer);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(60m, result.Value!.OutstandingBalance);
        Assert.Equal(harness.PosCustomer.Id.Value, result.Value.PosCustomerId);
        Assert.Equal(PlatformCustomer, result.Value.PlatformBusinessCustomerId);
        Assert.Equal(LinkedId, result.Value.LinkedCustomerAppUserId);
        Assert.Equal("Rosa Customer", result.Value.CustomerDisplayName);
        Assert.Equal("PHP", result.Value.Currency);
        Assert.Null(result.Value.MerchantDisplayName);
    }

    [Fact]
    public async Task Summary_zero_balance_when_no_utang()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var result = await harness.Summary.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.OutstandingBalance);
    }

    [Fact]
    public async Task Summary_fully_paid_is_zero()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 50m, "Goods", T0));
        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 50m, "Paid", Actor, T0.AddHours(1)));

        var result = await harness.Summary.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.OutstandingBalance);
    }

    [Fact]
    public async Task Summary_denied_when_platform_denied()
    {
        var harness = await Harness.CreateAsync(FakePlatform.Denied());
        var result = await harness.Summary.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerDenied, result.ErrorCode);
    }

    [Fact]
    public async Task Summary_not_found_when_platform_unreachable()
    {
        var harness = await Harness.CreateAsync(FakePlatform.NotFound());
        var result = await harness.Summary.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Summary_not_found_for_wrong_business_customer()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var result = await harness.Summary.ExecuteAsync(OrgA, OtherPlatformCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Summary_not_found_for_wrong_org()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var result = await harness.Summary.ExecuteAsync(OrgB, PlatformCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task Activity_newest_first_with_partial_payment_and_default_page_size()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        for (var i = 0; i < 12; i++)
        {
            await harness.Credits.AddAsync(CreditEntry.Create(
                PosOrganizationId.From(OrgA),
                harness.PosCustomer.Id,
                10m,
                $"C{i}",
                T0.AddMinutes(i)));
        }

        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            25m,
            "Partial",
            Actor,
            T0.AddMinutes(20)));

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(LinkedCustomerStatementLimits.DefaultPageSize, result.Value!.PageSize);
        Assert.Equal(10, result.Value.Items.Count);
        Assert.True(result.Value.HasMore);
        Assert.Equal("PartialPayment", result.Value.Items[0].Type);
        Assert.Equal(25m, result.Value.Items[0].PaymentAmount);
        Assert.Null(result.Value.Items[0].ChargeAmount);
        Assert.True(result.Value.Items[0].BalanceAfter is > 0m);

        // Ensure no nested receipt/product line collections on DTO
        Assert.Equal(
            new[]
            {
                nameof(LinkedCustomerActivityItemDto.ActivityId),
                nameof(LinkedCustomerActivityItemDto.AdjustmentAmount),
                nameof(LinkedCustomerActivityItemDto.BalanceAfter),
                nameof(LinkedCustomerActivityItemDto.ChargeAmount),
                nameof(LinkedCustomerActivityItemDto.HasDetails),
                nameof(LinkedCustomerActivityItemDto.OccurredAtUtc),
                nameof(LinkedCustomerActivityItemDto.PaymentAmount),
                nameof(LinkedCustomerActivityItemDto.ReferenceNumber),
                nameof(LinkedCustomerActivityItemDto.SourceSaleId),
                nameof(LinkedCustomerActivityItemDto.Status),
                nameof(LinkedCustomerActivityItemDto.Type)
            },
            typeof(LinkedCustomerActivityItemDto).GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray());
    }

    [Fact]
    public async Task Activity_source_sale_id_enables_lazy_receipt_without_lines()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var saleId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            80m,
            "Goods",
            T0,
            sourceSaleId: SaleId.From(saleId)));

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var item = Assert.Single(result.Value!.Items);
        Assert.True(item.HasDetails);
        Assert.Equal(saleId, item.SourceSaleId);
        Assert.Null(typeof(LinkedCustomerActivityItemDto).GetProperty("Lines"));
    }

    [Fact]
    public async Task Activity_repayment_has_no_source_sale_details_flag()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        await harness.Repayments.AddAsync(Repayment.Create(
            PosOrganizationId.From(OrgA),
            harness.PosCustomer.Id,
            10m,
            "Partial",
            Actor,
            T0));

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.False(item.HasDetails);
        Assert.Null(item.SourceSaleId);
    }

    [Fact]
    public async Task Activity_page_size_five_returns_at_most_five()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        for (var i = 0; i < 8; i++)
        {
            await harness.Credits.AddAsync(CreditEntry.Create(
                PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 5m, $"C{i}", T0.AddMinutes(i)));
        }

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer, page: 1, pageSize: 5);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Items.Count);
        Assert.Equal(5, result.Value.PageSize);
        Assert.True(result.Value.HasMore);
    }

    [Fact]
    public async Task Activity_page_size_fifty_is_clamped_to_twenty()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        for (var i = 0; i < 25; i++)
        {
            await harness.Credits.AddAsync(CreditEntry.Create(
                PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 1m, $"C{i}", T0.AddMinutes(i)));
        }

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer, page: 1, pageSize: 50);
        Assert.True(result.IsSuccess);
        Assert.Equal(LinkedCustomerStatementLimits.MaxPageSize, result.Value!.PageSize);
        Assert.Equal(20, result.Value.Items.Count);
        Assert.True(result.Value.HasMore);
    }

    [Fact]
    public async Task Activity_page_two_has_no_duplicate_ids()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        for (var i = 0; i < 15; i++)
        {
            await harness.Credits.AddAsync(CreditEntry.Create(
                PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 1m, $"C{i}", T0.AddMinutes(i)));
        }

        var page1 = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer, page: 1, pageSize: 10);
        var page2 = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer, page: 2, pageSize: 10);
        Assert.True(page1.IsSuccess);
        Assert.True(page2.IsSuccess);
        Assert.Equal(10, page1.Value!.Items.Count);
        Assert.Equal(5, page2.Value!.Items.Count);
        Assert.Empty(page1.Value.Items.Select(i => i.ActivityId).Intersect(page2.Value.Items.Select(i => i.ActivityId)));
    }

    [Fact]
    public async Task Activity_excludes_unrelated_customer_ledger()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var other = POSCustomer.Create(
            PosOrganizationId.From(OrgA),
            "Other",
            T0,
            platformBusinessCustomerId: OtherPlatformCustomer);
        await harness.Customers.AddAsync(other);
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), other.Id, 999m, "Secret", T0));
        await harness.Credits.AddAsync(CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 10m, "Own", T0.AddMinutes(1)));

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(10m, result.Value.Items[0].ChargeAmount);
    }

    [Fact]
    public async Task Activity_reversal_projects_as_reversal_type()
    {
        var harness = await Harness.CreateAuthorizedAsync();
        var credit = CreditEntry.Create(
            PosOrganizationId.From(OrgA), harness.PosCustomer.Id, 30m, "Goods", T0);
        credit.Reverse("mistake", T0.AddMinutes(5));
        await harness.Credits.AddAsync(credit);

        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.True(result.IsSuccess);
        Assert.Equal("UtangChargeReversal", result.Value!.Items[0].Type);
        Assert.Equal("Reversed", result.Value.Items[0].Status);
        Assert.Equal(0m, (await harness.Summary.ExecuteAsync(OrgA, PlatformCustomer)).Value!.OutstandingBalance);
    }

    [Fact]
    public async Task Activity_denied_when_platform_denied()
    {
        var harness = await Harness.CreateAsync(FakePlatform.Denied());
        var result = await harness.Activity.ExecuteAsync(OrgA, PlatformCustomer);
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.LinkedCustomerDenied, result.ErrorCode);
    }

    private sealed class Harness
    {
        public required POSCustomer PosCustomer { get; init; }
        public required InMemoryCustomers Customers { get; init; }
        public required InMemoryCredits Credits { get; init; }
        public required InMemoryRepayments Repayments { get; init; }
        public required GetLinkedCustomerStatementSummary Summary { get; init; }
        public required ListLinkedCustomerRecentActivity Activity { get; init; }

        public static async Task<Harness> CreateAuthorizedAsync() =>
            await CreateAsync(FakePlatform.Authorized());

        public static async Task<Harness> CreateAsync(ILinkedCustomerPlatformAuthorization platform)
        {
            var customers = new InMemoryCustomers();
            var credits = new InMemoryCredits();
            var repayments = new InMemoryRepayments();
            var clock = new FixedClock(T0.AddDays(1));
            var outstanding = new OutstandingBalanceService(credits, repayments, clock);
            var posCustomer = POSCustomer.Create(
                PosOrganizationId.From(OrgA),
                "Rosa Customer",
                T0,
                platformBusinessCustomerId: PlatformCustomer);
            await customers.AddAsync(posCustomer);

            var authorize = new AuthorizeLinkedCustomerStatementAccess(platform, customers);
            var activityQuery = new InMemoryRecentActivity(credits, repayments);
            return new Harness
            {
                PosCustomer = posCustomer,
                Customers = customers,
                Credits = credits,
                Repayments = repayments,
                Summary = new GetLinkedCustomerStatementSummary(authorize, customers, outstanding, clock),
                Activity = new ListLinkedCustomerRecentActivity(authorize, activityQuery, outstanding)
            };
        }
    }

    private sealed class FakePlatform : ILinkedCustomerPlatformAuthorization
    {
        private readonly LinkedCustomerPlatformAuthorizationResult _result;

        private FakePlatform(LinkedCustomerPlatformAuthorizationResult result) => _result = result;

        public static FakePlatform Authorized() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Authorized,
                new LinkedCustomerPlatformAuthorizationProof(
                    PersonalUser, OrgA, PlatformCustomer, LinkedId)));

        public static FakePlatform Denied() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.Denied, null));

        public static FakePlatform NotFound() => new(
            new LinkedCustomerPlatformAuthorizationResult(
                LinkedCustomerPlatformAuthorizationOutcome.NotFound, null));

        public Task<LinkedCustomerPlatformAuthorizationResult> VerifyAsync(
            Guid organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default)
        {
            if (_result.Outcome == LinkedCustomerPlatformAuthorizationOutcome.Authorized
                && (_result.Proof!.OrganizationId != organizationId
                    || _result.Proof.PlatformBusinessCustomerId != platformBusinessCustomerId))
            {
                return Task.FromResult(new LinkedCustomerPlatformAuthorizationResult(
                    LinkedCustomerPlatformAuthorizationOutcome.NotFound, null));
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryRecentActivity(
        InMemoryCredits credits,
        InMemoryRepayments repayments) : ILinkedCustomerRecentActivityQuery
    {
        public Task<IReadOnlyList<LinkedCustomerActivityRawRow>> ListRecentDescendingAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            take = Math.Min(take, LinkedCustomerStatementLimits.MaxPageSize + 1);
            var rows = credits.All
                .Where(c => c.OrganizationId == organizationId && c.CustomerId == customerId)
                .Select(c => new LinkedCustomerActivityRawRow(
                    c.Id.Value,
                    "Credit",
                    c.Amount,
                    c.Status == CreditEntryStatus.Active ? c.Amount : 0m,
                    c.Status.ToString(),
                    c.CreatedAtUtc,
                    c.SourceSaleId?.Value))
                .Concat(repayments.All
                    .Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId)
                    .Select(r => new LinkedCustomerActivityRawRow(
                        r.Id.Value,
                        "Repayment",
                        r.Amount,
                        r.Status == RepaymentStatus.Active ? -r.Amount : 0m,
                        r.Status.ToString(),
                        r.RecordedAtUtc,
                        null)))
                .OrderByDescending(r => r.RecordedAtUtc)
                .ThenByDescending(r => r.EntryId)
                .Skip(Math.Max(skip, 0))
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<LinkedCustomerActivityRawRow>>(rows);
        }
    }

    private sealed class InMemoryCustomers : IPOSCustomerRepository
    {
        private readonly List<POSCustomer> _items = [];

        public Task AddAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            _items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(POSCustomer customer, CancellationToken cancellationToken = default)
        {
            var i = _items.FindIndex(c => c.Id == customer.Id);
            if (i >= 0)
            {
                _items[i] = customer;
            }

            return Task.CompletedTask;
        }

        public Task<POSCustomer?> GetByIdAsync(PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c => c.OrganizationId == organizationId && c.Id == customerId));

        public Task<POSCustomer?> FindActiveByNormalizedMobileAsync(PosOrganizationId organizationId, string normalizedMobile, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.Status == CustomerStatus.Active && c.NormalizedMobile == normalizedMobile));

        public Task<POSCustomer?> FindByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<int> CountByPlatformBusinessCustomerIdAsync(
            PosOrganizationId organizationId,
            Guid platformBusinessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(c =>
                c.OrganizationId == organizationId && c.PlatformBusinessCustomerId == platformBusinessCustomerId));

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, CustomerStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = _items.Where(c => c.OrganizationId == organizationId).ToList();
            return Task.FromResult(((IReadOnlyList<POSCustomer>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<POSCustomer> Items, int TotalCount)> ListUpdatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            ListAsync(organizationId, null, null, skip, take, cancellationToken);

        public Task<IReadOnlyList<POSCustomer>> ListByIdsAsync(
            PosOrganizationId organizationId, IReadOnlyCollection<POSCustomerId> customerIds, CancellationToken cancellationToken = default)
        {
            var ids = customerIds.Select(c => c.Value).ToHashSet();
            return Task.FromResult<IReadOnlyList<POSCustomer>>(
                _items.Where(c => c.OrganizationId == organizationId && ids.Contains(c.Id.Value)).ToList());
        }
    }

    private sealed class InMemoryCredits : ICreditEntryRepository
    {
        public List<CreditEntry> All { get; } = [];

        public Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            All.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default)
        {
            var i = All.FindIndex(e => e.Id == entry.Id);
            if (i >= 0)
            {
                All[i] = entry;
            }

            return Task.CompletedTask;
        }

        public Task<CreditEntry?> GetByIdAsync(
            PosOrganizationId organizationId,
            POSCustomerId customerId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Id == entryId));

        public Task<CreditEntry?> GetByIdForOrganizationAsync(
            PosOrganizationId organizationId,
            CreditEntryId entryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(e => e.OrganizationId == organizationId && e.Id == entryId));

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = All.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId)
                .OrderByDescending(e => e.CreatedAtUtc).ThenByDescending(e => e.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<CreditEntry>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<CreditEntry>)Array.Empty<CreditEntry>(), 0));

        public Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
            PosOrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(
                All.Where(e => e.OrganizationId == organizationId && e.Status == CreditEntryStatus.Active).ToList());

        public Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CreditEntry>>(Array.Empty<CreditEntry>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Where(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active)
                .Sum(e => e.Amount));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Count(e =>
                e.OrganizationId == organizationId && e.CustomerId == customerId && e.Status == CreditEntryStatus.Active));
    }

    private sealed class InMemoryRepayments : IRepaymentRepository
    {
        public List<Repayment> All { get; } = [];

        public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            All.Add(repayment);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default)
        {
            var i = All.FindIndex(r => r.Id == repayment.Id);
            if (i >= 0)
            {
                All[i] = repayment;
            }

            return Task.CompletedTask;
        }

        public Task<Repayment?> GetByIdAsync(PosOrganizationId organizationId, RepaymentId repaymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.FirstOrDefault(r => r.OrganizationId == organizationId && r.Id == repaymentId));

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var list = All.Where(r => r.OrganizationId == organizationId && r.CustomerId == customerId)
                .OrderByDescending(r => r.RecordedAtUtc).ThenByDescending(r => r.Id.Value).ToList();
            return Task.FromResult(((IReadOnlyList<Repayment>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListCreatedSinceAsync(
            PosOrganizationId organizationId, DateTimeOffset? sinceUtc, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Repayment>)Array.Empty<Repayment>(), 0));

        public Task<IReadOnlyList<Repayment>> ListRecordedInRangeAsync(
            PosOrganizationId organizationId, DateOnly fromDateUtc, DateOnly toDateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Repayment>>(Array.Empty<Repayment>());

        public Task<decimal> SumActiveAmountAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Where(r =>
                r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active)
                .Sum(r => r.Amount));

        public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                All.Where(r => r.OrganizationId == organizationId && r.Status == RepaymentStatus.Active)
                    .GroupBy(r => r.CustomerId.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount)));

        public Task<int> CountActiveAsync(
            PosOrganizationId organizationId, POSCustomerId customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(All.Count(r =>
                r.OrganizationId == organizationId && r.CustomerId == customerId && r.Status == RepaymentStatus.Active));
    }
}
