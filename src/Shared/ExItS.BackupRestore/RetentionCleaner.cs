namespace ExItS.BackupRestore;

/// <summary>Provisional retention policy — engineering default, not a business SLA.</summary>
public sealed record RetentionPolicy(
    int DailyRetainDays = 14,
    int WeeklyRetainWeeks = 8,
    int MonthlyRetainMonths = 12)
{
    public static RetentionPolicy Provisional { get; } = new();
}

public sealed record RetentionCandidate(
    string BackupSetId,
    DateTimeOffset CreatedAtUtc,
    string ArtifactPath,
    string ManifestPath,
    bool IsComplete);

public sealed record RetentionDecision(
    string BackupSetId,
    bool Delete,
    string Reason);

public static class RetentionCleaner
{
    /// <summary>
    /// Selects deletable artifacts. Never selects the latest complete backup. Incomplete artifacts may be deleted.
    /// Dry-run callers must not delete; this method only returns decisions.
    /// </summary>
    public static IReadOnlyList<RetentionDecision> Evaluate(
        IReadOnlyList<RetentionCandidate> candidates,
        RetentionPolicy policy,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(policy);

        if (candidates.Count == 0)
        {
            return [];
        }

        var ordered = candidates
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenBy(c => c.BackupSetId, StringComparer.Ordinal)
            .ToList();

        var latestComplete = ordered.FirstOrDefault(c => c.IsComplete);
        var decisions = new List<RetentionDecision>(ordered.Count);

        foreach (var candidate in ordered)
        {
            if (latestComplete is not null
                && string.Equals(candidate.BackupSetId, latestComplete.BackupSetId, StringComparison.Ordinal))
            {
                decisions.Add(new RetentionDecision(candidate.BackupSetId, Delete: false, "Protected: latest complete backup."));
                continue;
            }

            if (!candidate.IsComplete)
            {
                decisions.Add(new RetentionDecision(candidate.BackupSetId, Delete: true, "Incomplete artifact is not promotable."));
                continue;
            }

            if (ShouldRetain(candidate.CreatedAtUtc, nowUtc, policy))
            {
                decisions.Add(new RetentionDecision(candidate.BackupSetId, Delete: false, "Within provisional retention window."));
                continue;
            }

            decisions.Add(new RetentionDecision(candidate.BackupSetId, Delete: true, "Outside provisional retention window."));
        }

        return decisions;
    }

    private static bool ShouldRetain(DateTimeOffset createdAtUtc, DateTimeOffset nowUtc, RetentionPolicy policy)
    {
        var age = nowUtc - createdAtUtc;
        if (age < TimeSpan.FromDays(policy.DailyRetainDays))
        {
            return true;
        }

        // Weekly bucket: keep if within weekly window and created on Sunday UTC (provisional).
        if (age < TimeSpan.FromDays(policy.WeeklyRetainWeeks * 7)
            && createdAtUtc.DayOfWeek == DayOfWeek.Sunday)
        {
            return true;
        }

        // Monthly bucket: keep if within monthly window and created on the 1st UTC.
        if (age < TimeSpan.FromDays(policy.MonthlyRetainMonths * 31)
            && createdAtUtc.Day == 1)
        {
            return true;
        }

        return false;
    }
}
