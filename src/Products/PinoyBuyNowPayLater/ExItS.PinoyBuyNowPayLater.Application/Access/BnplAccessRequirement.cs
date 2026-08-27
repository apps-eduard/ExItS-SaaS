namespace ExItS.PinoyBuyNowPayLater.Application.Access;

/// <summary>
/// Optional requirements for a BNPL operational evaluation.
/// Capability and branch are required only when the caller supplies them.
/// </summary>
public sealed class BnplAccessRequirement
{
    public string? RequiredCapability { get; init; }

    public Guid? RequiredBranchId { get; init; }

    public static BnplAccessRequirement None { get; } = new();

    public static BnplAccessRequirement ForCapability(string capability) =>
        new() { RequiredCapability = capability };

    public static BnplAccessRequirement ForBranchAndCapability(Guid branchId, string capability) =>
        new() { RequiredBranchId = branchId, RequiredCapability = capability };
}
