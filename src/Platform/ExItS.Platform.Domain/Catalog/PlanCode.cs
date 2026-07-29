using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Catalog;

public sealed partial class PlanCode : IEquatable<PlanCode>
{
    private static readonly Regex ValidPattern = CreateValidPattern();

    public string Value { get; }

    private PlanCode(string value) => Value = value;

    public static PlanCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlanCode, "PlanCode cannot be blank.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!ValidPattern.IsMatch(normalized) || normalized.Length is < 2 or > 64)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanCode,
                "PlanCode must be 2–64 lowercase alphanumeric segments separated by single hyphens.");
        }

        return new PlanCode(normalized);
    }

    public bool Equals(PlanCode? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is PlanCode other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
    public static bool operator ==(PlanCode? left, PlanCode? right) => Equals(left, right);
    public static bool operator !=(PlanCode? left, PlanCode? right) => !Equals(left, right);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
