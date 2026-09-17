using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationPaymentMethodSettingRepository(PosDbContext db)
    : IOrganizationPaymentMethodSettingRepository
{
    public async Task<IReadOnlyList<OrganizationPaymentMethodSetting>> ListByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.OrganizationPaymentMethodSettings.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value)
            .OrderBy(r => r.MethodCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(ToDomain).ToArray();
    }

    public async Task<OrganizationPaymentMethodSetting?> GetAsync(
        PosOrganizationId organizationId,
        string methodCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = methodCode.Trim();
        var record = await db.OrganizationPaymentMethodSettings
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.MethodCode == normalized,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task AddAsync(OrganizationPaymentMethodSetting setting, CancellationToken cancellationToken = default)
    {
        await db.OrganizationPaymentMethodSettings.AddAsync(ToRecord(setting), cancellationToken)
            .ConfigureAwait(false);
    }

    public Task UpdateAsync(OrganizationPaymentMethodSetting setting, CancellationToken cancellationToken = default)
    {
        var record = db.OrganizationPaymentMethodSettings.Local
            .FirstOrDefault(r => r.Id == setting.SettingId);
        if (record is null)
        {
            record = ToRecord(setting);
            db.OrganizationPaymentMethodSettings.Update(record);
        }
        else
        {
            record.IsEnabled = setting.IsEnabled;
            record.DisplayName = setting.DisplayName;
            record.RequireReference = setting.RequireReference;
            record.BranchScope = setting.BranchScope.ToString();
            record.SelectedBranchIds = setting.SelectedBranchIds.ToArray();
            record.Instructions = setting.Instructions;
            record.AccountHint = setting.AccountHint;
            record.UpdatedAtUtc = setting.UpdatedAtUtc;
        }

        return Task.CompletedTask;
    }

    private static OrganizationPaymentMethodSetting ToDomain(OrganizationPaymentMethodSettingRecord record)
    {
        _ = Enum.TryParse<PaymentMethodBranchScope>(record.BranchScope, ignoreCase: true, out var scope);
        return OrganizationPaymentMethodSetting.Rehydrate(
            record.Id,
            PosOrganizationId.From(record.OrganizationId),
            record.MethodCode,
            record.IsEnabled,
            record.DisplayName,
            record.RequireReference,
            scope,
            record.SelectedBranchIds ?? [],
            record.Instructions,
            record.AccountHint,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);
    }

    private static OrganizationPaymentMethodSettingRecord ToRecord(OrganizationPaymentMethodSetting setting) =>
        new()
        {
            Id = setting.SettingId,
            OrganizationId = setting.OrganizationId.Value,
            MethodCode = setting.MethodCode,
            IsEnabled = setting.IsEnabled,
            DisplayName = setting.DisplayName,
            RequireReference = setting.RequireReference,
            BranchScope = setting.BranchScope.ToString(),
            SelectedBranchIds = setting.SelectedBranchIds.ToArray(),
            Instructions = setting.Instructions,
            AccountHint = setting.AccountHint,
            CreatedAtUtc = setting.CreatedAtUtc,
            UpdatedAtUtc = setting.UpdatedAtUtc
        };
}
