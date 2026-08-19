using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Governance;

public sealed class GovernanceStepUpGrantId : IEquatable<GovernanceStepUpGrantId>
{
    public Guid Value { get; }

    private GovernanceStepUpGrantId(Guid value) => Value = value;

    public static GovernanceStepUpGrantId New() => new(Guid.NewGuid());

    public static GovernanceStepUpGrantId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "GovernanceStepUpGrantId cannot be an empty GUID.")
            : new GovernanceStepUpGrantId(value);

    public bool Equals(GovernanceStepUpGrantId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is GovernanceStepUpGrantId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(GovernanceStepUpGrantId? left, GovernanceStepUpGrantId? right) => Equals(left, right);
    public static bool operator !=(GovernanceStepUpGrantId? left, GovernanceStepUpGrantId? right) => !Equals(left, right);
}
