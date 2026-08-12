using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public interface IPersonalRewardBalanceRepository
{
    Task<PersonalRewardBalance?> GetByUserAsync(
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalRewardBalance balance, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalRewardBalance balance, int expectedVersion, CancellationToken cancellationToken = default);
}

public interface IPersonalRewardTransactionRepository
{
    Task AddAsync(PersonalRewardTransaction transaction, CancellationToken cancellationToken = default);

    Task<PersonalRewardTransaction?> FindByIdempotencyKeyAsync(
        PlatformUserId personalUserId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PersonalRewardTransaction> Items, int TotalCount)> ListByUserDescendingAsync(
        PlatformUserId personalUserId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
