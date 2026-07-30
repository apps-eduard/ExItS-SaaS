using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Audit;

/// <summary>Strongly typed identifier for an append-only Platform audit record.</summary>
public sealed class AuditRecordId : IEquatable<AuditRecordId>
{
    public Guid Value { get; }

    private AuditRecordId(Guid value) => Value = value;

    public static AuditRecordId New() => new(Guid.NewGuid());

    public static AuditRecordId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAuditRecordId,
                "AuditRecordId cannot be an empty GUID.");
        }

        return new AuditRecordId(value);
    }

    public bool Equals(AuditRecordId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is AuditRecordId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(AuditRecordId? left, AuditRecordId? right) =>
        Equals(left, right);

    public static bool operator !=(AuditRecordId? left, AuditRecordId? right) =>
        !Equals(left, right);
}
