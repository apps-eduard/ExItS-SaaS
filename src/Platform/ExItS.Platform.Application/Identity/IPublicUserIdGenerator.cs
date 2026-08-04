namespace ExItS.Platform.Application.Identity;

/// <summary>Generates unique immutable public ExItS user IDs (format EX-####-####).</summary>
public interface IPublicUserIdGenerator
{
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default);
}
