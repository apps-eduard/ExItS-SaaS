namespace ExItS.ArchitectureTests;

internal static class PortfolioIndependenceTokens
{
    internal static string ForbiddenToken { get; } =
        new([
            (char)72, (char)101, (char)97, (char)108, (char)116, (char)104,
            (char)67, (char)97, (char)114, (char)101
        ]);
}
