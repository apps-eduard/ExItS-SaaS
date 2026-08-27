namespace ExItS.PinoyPawnManager.Domain.Access;

/// <summary>
/// External product identity for Pinoy Pawn Manager.
/// Matches the Platform catalog code without referencing Platform assemblies.
/// </summary>
public sealed class PpmProductIdentity : IEquatable<PpmProductIdentity>
{
    public const string PinoyPawnManagerCode = "pinoy-pawn-manager";

    public static PpmProductIdentity PinoyPawnManager { get; } = new(PinoyPawnManagerCode);

    public string Code { get; }

    private PpmProductIdentity(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim();
    }

    public static PpmProductIdentity Create(string code) => new(code);

    public bool Equals(PpmProductIdentity? other) =>
        other is not null && string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is PpmProductIdentity other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Code);

    public override string ToString() => Code;

    public static bool IsPinoyPawnManager(string? productCode) =>
        !string.IsNullOrWhiteSpace(productCode)
        && string.Equals(productCode.Trim(), PinoyPawnManagerCode, StringComparison.OrdinalIgnoreCase);
}
