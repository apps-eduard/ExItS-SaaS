using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class BranchDayOperatingHours
{
    public DayOfWeek DayOfWeek { get; }
    public bool IsClosed { get; }
    public bool IsOpen24Hours { get; }
    public TimeOnly? OpenTime { get; }
    public TimeOnly? CloseTime { get; }

    private BranchDayOperatingHours(
        DayOfWeek dayOfWeek,
        bool isClosed,
        bool isOpen24Hours,
        TimeOnly? openTime,
        TimeOnly? closeTime)
    {
        DayOfWeek = dayOfWeek;
        IsClosed = isClosed;
        IsOpen24Hours = isOpen24Hours;
        OpenTime = openTime;
        CloseTime = closeTime;
    }

    public static BranchDayOperatingHours Closed(DayOfWeek dayOfWeek) =>
        new(dayOfWeek, isClosed: true, isOpen24Hours: false, null, null);

    public static BranchDayOperatingHours Open24Hours(DayOfWeek dayOfWeek) =>
        new(dayOfWeek, isClosed: false, isOpen24Hours: true, null, null);

    public static BranchDayOperatingHours Interval(DayOfWeek dayOfWeek, TimeOnly openTime, TimeOnly closeTime)
    {
        if (openTime == closeTime)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchOperatingHours,
                "Open and close times must differ.");
        }

        return new(dayOfWeek, isClosed: false, isOpen24Hours: false, openTime, closeTime);
    }

    public bool IsValidConfiguration()
    {
        if (IsClosed)
        {
            return !IsOpen24Hours && OpenTime is null && CloseTime is null;
        }

        if (IsOpen24Hours)
        {
            return OpenTime is null && CloseTime is null;
        }

        return OpenTime is not null && CloseTime is not null && OpenTime != CloseTime;
    }
}

public sealed class BranchOperatingHoursSchedule
{
    public OrganizationBranchId BranchId { get; }
    public IReadOnlyList<BranchDayOperatingHours> Days { get; }

    private BranchOperatingHoursSchedule(OrganizationBranchId branchId, IReadOnlyList<BranchDayOperatingHours> days)
    {
        BranchId = branchId;
        Days = days;
    }

    public static BranchOperatingHoursSchedule Create(
        OrganizationBranchId branchId,
        IReadOnlyList<BranchDayOperatingHours> days)
    {
        ArgumentNullException.ThrowIfNull(branchId);
        if (days.Count != 7)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchOperatingHours,
                "Operating hours must include all seven days.");
        }

        var distinct = days.Select(d => d.DayOfWeek).Distinct().Count();
        if (distinct != 7)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchOperatingHours,
                "Operating hours must include each day exactly once.");
        }

        if (days.Any(d => !d.IsValidConfiguration()))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchOperatingHours,
                "One or more operating-hour day entries are invalid.");
        }

        return new(branchId, days.OrderBy(d => d.DayOfWeek).ToList());
    }

    public static BranchOperatingHoursSchedule Rehydrate(
        OrganizationBranchId branchId,
        IReadOnlyList<BranchDayOperatingHours> days) =>
        new(branchId, days.OrderBy(d => d.DayOfWeek).ToList());

    public bool IsConfigured => Days.Any(d => !d.IsClosed || d.IsOpen24Hours);
}
