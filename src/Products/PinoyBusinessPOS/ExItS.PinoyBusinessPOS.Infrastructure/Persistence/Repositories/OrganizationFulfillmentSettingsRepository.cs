using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationFulfillmentSettingsRepository(PosDbContext db)
    : IOrganizationFulfillmentSettingsRepository
{
    public async Task<OrganizationFulfillmentSettings?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationFulfillmentSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(
        OrganizationFulfillmentSettings settings,
        CancellationToken cancellationToken = default)
    {
        await db.OrganizationFulfillmentSettings.AddAsync(ToRecord(settings), cancellationToken)
            .ConfigureAwait(false);
    }

    public Task UpdateAsync(
        OrganizationFulfillmentSettings settings,
        CancellationToken cancellationToken = default)
    {
        var record = db.OrganizationFulfillmentSettings.Local
            .FirstOrDefault(r => r.Id == settings.SettingId);
        if (record is null)
        {
            db.OrganizationFulfillmentSettings.Update(ToRecord(settings));
        }
        else
        {
            record.OfferDelivery = settings.OfferDelivery;
            record.UpdatedAtUtc = settings.UpdatedAtUtc;
        }

        return Task.CompletedTask;
    }

    private static OrganizationFulfillmentSettings ToDomain(OrganizationFulfillmentSettingsRecord r) =>
        OrganizationFulfillmentSettings.Rehydrate(
            r.Id,
            PosOrganizationId.From(r.OrganizationId),
            r.OfferDelivery,
            r.CreatedAtUtc,
            r.UpdatedAtUtc);

    private static OrganizationFulfillmentSettingsRecord ToRecord(OrganizationFulfillmentSettings x) =>
        new()
        {
            Id = x.SettingId,
            OrganizationId = x.OrganizationId.Value,
            OfferDelivery = x.OfferDelivery,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };
}
