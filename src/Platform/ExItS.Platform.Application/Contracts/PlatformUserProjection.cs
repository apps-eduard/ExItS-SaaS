using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Contracts;

/// <summary>HealthCare-facing Platform User projection. No credentials or clinical data.</summary>
public sealed class PlatformUserProjection
{
    public PlatformUserId PlatformUserId { get; }
    public string DisplayName { get; }
    public string NormalizedEmail { get; }
    public AccountStatus AccountStatus { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public int SourceVersion { get; }

    public PlatformUserProjection(
        PlatformUserId platformUserId,
        string displayName,
        string normalizedEmail,
        AccountStatus accountStatus,
        DateTimeOffset updatedAtUtc,
        int sourceVersion)
    {
        ArgumentNullException.ThrowIfNull(platformUserId);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Display name is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Normalized email is required.");
        }

        if (updatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "UpdatedAt must be UTC.");
        }

        if (sourceVersion < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidSourceVersion, "Source version must be positive.");
        }

        if (!Enum.IsDefined(accountStatus))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Account status is invalid.");
        }

        PlatformUserId = platformUserId;
        DisplayName = displayName.Trim();
        NormalizedEmail = normalizedEmail.Trim().ToLowerInvariant();
        AccountStatus = accountStatus;
        UpdatedAtUtc = updatedAtUtc;
        SourceVersion = sourceVersion;
    }
}
