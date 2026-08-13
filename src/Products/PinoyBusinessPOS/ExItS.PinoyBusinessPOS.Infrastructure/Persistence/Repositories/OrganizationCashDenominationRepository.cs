using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationCashDenominationRepository : IOrganizationCashDenominationRepository
{
    private readonly PosDbContext _db;

    public OrganizationCashDenominationRepository(PosDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrganizationCashDenomination>> ListAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.OrganizationCashDenominations.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToList();
    }

    public async Task ReplaceAsync(
        PosOrganizationId organizationId,
        IReadOnlyList<OrganizationCashDenomination> denominations,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.OrganizationCashDenominations
            .Where(r => r.OrganizationId == organizationId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var keepIds = denominations.Select(d => d.Id.Value).ToHashSet();
        foreach (var record in existing.Where(r => !keepIds.Contains(r.Id)))
        {
            _db.OrganizationCashDenominations.Remove(record);
        }

        var byId = existing.ToDictionary(r => r.Id);
        foreach (var denomination in denominations)
        {
            if (byId.TryGetValue(denomination.Id.Value, out var record))
            {
                record.Value = denomination.Value;
                record.DisplayLabel = denomination.DisplayLabel;
                record.IsEnabled = denomination.IsEnabled;
                record.SortOrder = denomination.SortOrder;
                record.UpdatedAtUtc = denomination.UpdatedAtUtc;
                continue;
            }

            _db.OrganizationCashDenominations.Add(ToRecord(denomination));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static OrganizationCashDenomination ToDomain(OrganizationCashDenominationRecord record) =>
        OrganizationCashDenomination.Rehydrate(
            OrganizationCashDenominationId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Value,
            record.DisplayLabel,
            record.IsEnabled,
            record.SortOrder,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static OrganizationCashDenominationRecord ToRecord(OrganizationCashDenomination denomination) =>
        new()
        {
            Id = denomination.Id.Value,
            OrganizationId = denomination.OrganizationId.Value,
            Value = denomination.Value,
            DisplayLabel = denomination.DisplayLabel,
            IsEnabled = denomination.IsEnabled,
            SortOrder = denomination.SortOrder,
            CreatedAtUtc = denomination.CreatedAtUtc,
            UpdatedAtUtc = denomination.UpdatedAtUtc
        };
}
