namespace ExItS.PinoyBusinessPOS.Domain.Parties;

/// <summary>Provenance for a branch visibility grant on an org-owned customer or supplier (MB2-04).</summary>
public enum PartyBranchGrantSource
{
    ExplicitAssign,
    CreateAtBranch,
    Transaction,
    SetupCopy,
    MigrationBackfill,
}

public static class PartyBranchGrantSources
{
    public const int CodeMaxLength = 32;

    public static string ToCode(PartyBranchGrantSource source) => source switch
    {
        PartyBranchGrantSource.ExplicitAssign => nameof(PartyBranchGrantSource.ExplicitAssign),
        PartyBranchGrantSource.CreateAtBranch => nameof(PartyBranchGrantSource.CreateAtBranch),
        PartyBranchGrantSource.Transaction => nameof(PartyBranchGrantSource.Transaction),
        PartyBranchGrantSource.SetupCopy => nameof(PartyBranchGrantSource.SetupCopy),
        PartyBranchGrantSource.MigrationBackfill => nameof(PartyBranchGrantSource.MigrationBackfill),
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    public static bool TryParse(string? code, out PartyBranchGrantSource source)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            source = default;
            return false;
        }

        return Enum.TryParse(code, ignoreCase: true, out source);
    }
}
