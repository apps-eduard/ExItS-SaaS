using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Suppliers;

/// <summary>Strongly typed identifier for a POS supplier. Not a Platform identity.</summary>
public sealed class SupplierId : IEquatable<SupplierId>
{
    public Guid Value { get; }

    private SupplierId(Guid value) => Value = value;

    public static SupplierId New() => new(Guid.NewGuid());

    public static SupplierId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierId,
                "SupplierId cannot be an empty GUID.");
        }

        return new SupplierId(value);
    }

    public bool Equals(SupplierId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is SupplierId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SupplierId? left, SupplierId? right) => Equals(left, right);

    public static bool operator !=(SupplierId? left, SupplierId? right) => !Equals(left, right);
}

public enum SupplierStatus
{
    Active = 0,
    Inactive = 1
}

/// <summary>
/// Organization-scoped human-readable supplier code: <c>SUP-NNNNNN</c>.
/// Allocated server-side per organization; clients never propose a supplier code.
/// </summary>
public static class SupplierCodes
{
    public const string Prefix = "SUP";
    public const int SequenceDigits = 6;
    public const int MaxLength = 16;
    public const long MaxSequence = 999_999L;

    public static string Format(long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierCode,
                $"Supplier sequence must be between 1 and {MaxSequence}.");
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{Prefix}-{sequence.ToString($"D{SequenceDigits}", System.Globalization.CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? supplierCode)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            throw new DomainException(DomainErrorCodes.InvalidSupplierCode, "Supplier code is required.");
        }

        var trimmed = supplierCode.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength
            || !trimmed.StartsWith($"{Prefix}-", StringComparison.Ordinal)
            || trimmed.Length != Prefix.Length + 1 + SequenceDigits)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierCode,
                "Supplier code must look like SUP-NNNNNN.");
        }

        return trimmed;
    }
}
