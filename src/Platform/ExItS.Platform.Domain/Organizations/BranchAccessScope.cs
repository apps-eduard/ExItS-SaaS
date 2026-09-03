namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Branch access scope for ordinary organization memberships.
/// Owner/Administrator ignore this and always have implicit all-active access.
/// </summary>
public enum BranchAccessScope
{
    /// <summary>Only persisted membership branch assignment rows apply.</summary>
    Explicit = 0,

    /// <summary>Every current and future Active branch in the organization.</summary>
    AllActive = 1,

    /// <summary>
    /// Active branches inside the member's granted Active Areas. Branches with no Area are excluded.
    /// </summary>
    Areas = 2
}
