using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class UpdateBranchPartialAddressTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 31, 6, 0, 0, TimeSpan.Zero);
    private static readonly PlatformOrganizationId Org = PlatformOrganizationId.From(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public async Task Coordinate_only_update_preserves_structured_address()
    {
        var branch = OrganizationBranch.Create(Org, "MAIN", "Main Branch", T0);
        branch.UpdateAddress("123 Lacson St", null, "Bacolod City", "Negros Occidental", "6100", "PH", T0);
        branch.UpdateContactPhone("+63 917 111 2222", T0);
        branch.UpdateTimeZone("Asia/Manila", T0);

        var useCase = new UpdateBranch(
            new FakeSingleBranchRepository(branch),
            new FakeEmptyPolicyRepository(),
            new EntitlementQueryService(new InMemoryEntitlementSnapshotRepository()),
            new NoOpUnitOfWork(),
            new FixedClock(T0.AddMinutes(1)));

        var result = await useCase.ExecuteAsync(
            Org,
            branch.Id,
            new UpdateBranchCommand(
                Name: "Main Branch",
                Latitude: 10.6765m,
                Longitude: 122.9509m));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal("123 Lacson St", branch.AddressLine1);
        Assert.Equal("Bacolod City", branch.City);
        Assert.Equal("Negros Occidental", branch.Region);
        Assert.Equal("6100", branch.PostalCode);
        Assert.Equal("PH", branch.CountryCode);
        Assert.Equal(10.6765m, branch.Latitude);
        Assert.Equal(122.9509m, branch.Longitude);
    }

    [Fact]
    public async Task Empty_string_address_fields_clear_existing_values()
    {
        var branch = OrganizationBranch.Create(Org, "MAIN", "Main Branch", T0);
        branch.UpdateAddress("123 Lacson St", "Suite 2", "Bacolod City", "Negros Occidental", "6100", "PH", T0);

        var useCase = new UpdateBranch(
            new FakeSingleBranchRepository(branch),
            new FakeEmptyPolicyRepository(),
            new EntitlementQueryService(new InMemoryEntitlementSnapshotRepository()),
            new NoOpUnitOfWork(),
            new FixedClock(T0.AddMinutes(1)));

        var result = await useCase.ExecuteAsync(
            Org,
            branch.Id,
            new UpdateBranchCommand(
                Name: "Main Branch",
                AddressLine1: "",
                AddressLine2: "",
                City: "",
                Region: "",
                PostalCode: "",
                CountryCode: ""));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Null(branch.AddressLine1);
        Assert.Null(branch.City);
        Assert.Null(branch.CountryCode);
    }

    private sealed class FakeSingleBranchRepository(OrganizationBranch branch) : IOrganizationBranchRepository
    {
        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationBranch?>(branch.Id == id ? branch : null);

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>([branch]);

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationBranch?>(branch);

        public Task AddAsync(OrganizationBranch entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(OrganizationBranch entity, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeEmptyPolicyRepository : IBranchDeliveryPolicyRepository
    {
        public Task<BranchDeliveryPolicy?> GetByBranchIdAsync(
            OrganizationBranchId branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchDeliveryPolicy?>(null);

        public Task<IReadOnlyList<BranchDeliveryPolicy>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDeliveryPolicy>>([]);

        public Task AddAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(BranchDeliveryPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }
}
