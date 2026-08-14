using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class PosDeviceRegistrationTokenEntityMapper
{
    public static PosDeviceRegistrationToken ToDomain(PosDeviceRegistrationTokenRecord record) =>
        PosDeviceRegistrationToken.Rehydrate(
            PosDeviceRegistrationTokenId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            record.TokenHash,
            PlatformUserId.From(record.CreatedByUserId),
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.RedeemedAtUtc,
            record.RedeemedByInstallationDeviceId,
            record.RedeemedPosDeviceId is null ? null : PosDeviceId.From(record.RedeemedPosDeviceId.Value),
            Enum.Parse<PosDeviceRegistrationTokenStatus>(record.Status));

    public static PosDeviceRegistrationTokenRecord ToRecord(PosDeviceRegistrationToken token) => new()
    {
        Id = token.Id.Value,
        OrganizationId = token.OrganizationId.Value,
        TokenHash = token.TokenHash,
        CreatedByUserId = token.CreatedByUserId.Value,
        CreatedAtUtc = token.CreatedAtUtc,
        ExpiresAtUtc = token.ExpiresAtUtc,
        RedeemedAtUtc = token.RedeemedAtUtc,
        RedeemedByInstallationDeviceId = token.RedeemedByInstallationDeviceId,
        RedeemedPosDeviceId = token.RedeemedPosDeviceId?.Value,
        Status = token.Status.ToString()
    };

    public static void ApplyToRecord(PosDeviceRegistrationToken token, PosDeviceRegistrationTokenRecord record)
    {
        record.Status = token.Status.ToString();
        record.RedeemedAtUtc = token.RedeemedAtUtc;
        record.RedeemedByInstallationDeviceId = token.RedeemedByInstallationDeviceId;
        record.RedeemedPosDeviceId = token.RedeemedPosDeviceId?.Value;
    }
}
