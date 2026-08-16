namespace ExItS.Deployment;

/// <summary>
/// Portfolio independence tokens for forbidding nested foreign product trees / DB names.
/// Values are assembled without embedding obsolete product spellings as contiguous source text.
/// </summary>
public static class ForeignProductNaming
{
    /// <summary>Legacy nested product directory / DB token that must never re-enter this repository.</summary>
    public static string ForbiddenNestedProductToken { get; } =
        new([
            (char)72, (char)101, (char)97, (char)108, (char)116, (char)104,
            (char)67, (char)97, (char)114, (char)101
        ]);

    public static string ForbiddenNestedProductRootRelativePath => ForbiddenNestedProductToken + "/";

    public static bool ContainsForbiddenToken(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(ForbiddenNestedProductToken, StringComparison.OrdinalIgnoreCase);
}
