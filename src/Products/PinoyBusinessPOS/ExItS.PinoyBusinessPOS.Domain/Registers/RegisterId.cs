using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Registers;

/// <summary>Strongly typed identifier for a POS register (logical sales station). Not a device identity.</summary>
public sealed class RegisterId : IEquatable<RegisterId>
{
    public Guid Value { get; }

    private RegisterId(Guid value) => Value = value;

    public static RegisterId New() => new(Guid.NewGuid());

    public static RegisterId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterId,
                "RegisterId cannot be an empty GUID.");
        }

        return new RegisterId(value);
    }

    public bool Equals(RegisterId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is RegisterId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(RegisterId? left, RegisterId? right) => Equals(left, right);

    public static bool operator !=(RegisterId? left, RegisterId? right) => !Equals(left, right);
}

public enum RegisterStatus
{
    Active = 0,
    Inactive = 1
}

/// <summary>
/// Organization-scoped human-readable register code: <c>REG-NNNNNN</c>.
/// Allocated server-side per organization; clients never propose a register code.
/// </summary>
public static class RegisterCodes
{
    public const string Prefix = "REG";
    public const int SequenceDigits = 6;
    public const int MaxLength = 16;
    public const long MaxSequence = 999_999L;

    public static string Format(long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterCode,
                $"Register sequence must be between 1 and {MaxSequence}.");
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Prefix}-{sequence.ToString($"D{SequenceDigits}", System.Globalization.CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? registerCode)
    {
        if (string.IsNullOrWhiteSpace(registerCode))
        {
            throw new DomainException(DomainErrorCodes.InvalidRegisterCode, "Register code is required.");
        }

        var trimmed = registerCode.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength
            || !trimmed.StartsWith($"{Prefix}-", StringComparison.Ordinal)
            || trimmed.Length != Prefix.Length + 1 + SequenceDigits)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterCode,
                "Register code must look like REG-NNNNNN.");
        }

        return trimmed;
    }
}
