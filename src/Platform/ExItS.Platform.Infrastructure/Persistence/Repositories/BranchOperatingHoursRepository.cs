using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class BranchOperatingHoursRepository : IBranchOperatingHoursRepository
{
    private readonly PlatformDbContext _db;

    public BranchOperatingHoursRepository(PlatformDbContext db) => _db = db;

    public async Task<BranchOperatingHoursSchedule?> GetByBranchIdAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.BranchOperatingHours.AsNoTracking()
            .Where(h => h.BranchId == branchId.Value)
            .OrderBy(h => h.DayOfWeek)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return null;
        }

        var days = records.Select(ToDomain).ToList();
        return BranchOperatingHoursSchedule.Rehydrate(branchId, days);
    }

    public async Task UpsertAsync(
        BranchOperatingHoursSchedule schedule,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.BranchOperatingHours
            .Where(h => h.BranchId == schedule.BranchId.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing.Count > 0)
        {
            _db.BranchOperatingHours.RemoveRange(existing);
        }

        foreach (var day in schedule.Days)
        {
            _db.BranchOperatingHours.Add(new BranchOperatingHoursRecord
            {
                BranchId = schedule.BranchId.Value,
                OrganizationId = organizationId.Value,
                DayOfWeek = (int)day.DayOfWeek,
                IsClosed = day.IsClosed,
                IsOpen24Hours = day.IsOpen24Hours,
                OpenTime = day.OpenTime,
                CloseTime = day.CloseTime
            });
        }
    }

    private static BranchDayOperatingHours ToDomain(BranchOperatingHoursRecord record)
    {
        var day = (DayOfWeek)record.DayOfWeek;
        if (record.IsClosed)
        {
            return BranchDayOperatingHours.Closed(day);
        }

        if (record.IsOpen24Hours)
        {
            return BranchDayOperatingHours.Open24Hours(day);
        }

        return BranchDayOperatingHours.Interval(day, record.OpenTime!.Value, record.CloseTime!.Value);
    }
}
