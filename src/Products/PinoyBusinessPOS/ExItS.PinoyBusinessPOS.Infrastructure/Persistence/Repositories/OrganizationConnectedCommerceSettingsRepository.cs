using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationConnectedCommerceSettingsRepository(PosDbContext db)
    : IOrganizationConnectedCommerceSettingsRepository
{
    public async Task<OrganizationConnectedCommerceSettings?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationConnectedCommerceSettings
            .Include(x => x.CategoryRules)
            .Include(x => x.CategoryReturnRules)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ConnectedSupplierEntityMapper.ToDomain(record);
    }

    public async Task AddAsync(
        OrganizationConnectedCommerceSettings settings,
        CancellationToken cancellationToken = default)
    {
        await db.OrganizationConnectedCommerceSettings
            .AddAsync(ConnectedSupplierEntityMapper.ToRecord(settings), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        OrganizationConnectedCommerceSettings settings,
        CancellationToken cancellationToken = default)
    {
        var record = await db.OrganizationConnectedCommerceSettings
            .Include(x => x.CategoryRules)
            .Include(x => x.CategoryReturnRules)
            .FirstOrDefaultAsync(x => x.Id == settings.SettingId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            db.OrganizationConnectedCommerceSettings.Update(ConnectedSupplierEntityMapper.ToRecord(settings));
            return;
        }

        ConnectedSupplierEntityMapper.Apply(settings, record);
    }
}
