namespace ExItS.PinoyLoanManager.Domain.Access;

/// <summary>
/// External product identity for Pinoy Loan Manager.
/// Matches the Platform catalog code without referencing Platform assemblies.
/// </summary>
public sealed class PlmProductIdentity : IEquatable<PlmProductIdentity>
{
    public const string PinoyLoanManagerCode = "pinoy-loan-manager";

    public static PlmProductIdentity PinoyLoanManager { get; } = new(PinoyLoanManagerCode);

    public string Code { get; }

    private PlmProductIdentity(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim();
    }

    public static PlmProductIdentity Create(string code) => new(code);

    public bool Equals(PlmProductIdentity? other) =>
        other is not null && string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is PlmProductIdentity other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Code);

    public override string ToString() => Code;

    public static bool IsPinoyLoanManager(string? productCode) =>
        !string.IsNullOrWhiteSpace(productCode)
        && string.Equals(productCode.Trim(), PinoyLoanManagerCode, StringComparison.OrdinalIgnoreCase);
}
