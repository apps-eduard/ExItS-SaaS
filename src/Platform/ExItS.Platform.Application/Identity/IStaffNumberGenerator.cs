namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Generates unique Platform Staff numbers (format STF-000001).
/// </summary>
public interface IStaffNumberGenerator
{
    Task<string> GenerateNextAsync(CancellationToken cancellationToken = default);
}
