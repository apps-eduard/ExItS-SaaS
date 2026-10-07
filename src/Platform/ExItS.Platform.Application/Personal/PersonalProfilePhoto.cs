namespace ExItS.Platform.Application.Personal;

public interface IPersonalProfilePhotoStore
{
    Task SaveAsync(Guid userId, string contentType, byte[] content, CancellationToken cancellationToken = default);

    Task<PersonalProfilePhotoFile?> ReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveForKeyAsync(string key, string contentType, byte[] content, CancellationToken cancellationToken = default);

    Task<PersonalProfilePhotoFile?> ReadForKeyAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record PersonalProfilePhotoFile(string ContentType, byte[] Content);

public interface IPersonalProfilePhotoNotifier
{
    Task PhotoUpdatedAsync(Guid userId, string profilePhotoUrl, CancellationToken cancellationToken = default);
}

public static class PersonalProfilePhotoRules
{
    public const int MaxBytes = 2 * 1024 * 1024;

    public static bool TryDetectContentType(ReadOnlySpan<byte> content, out string contentType)
    {
        contentType = "";
        if (content.Length is 0 or > MaxBytes)
        {
            return false;
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        if (content.Length >= 8
            && content[0] == 0x89
            && content[1] == 0x50
            && content[2] == 0x4E
            && content[3] == 0x47
            && content[4] == 0x0D
            && content[5] == 0x0A
            && content[6] == 0x1A
            && content[7] == 0x0A)
        {
            contentType = "image/png";
            return true;
        }

        if (content.Length >= 12
            && content[0] == (byte)'R'
            && content[1] == (byte)'I'
            && content[2] == (byte)'F'
            && content[3] == (byte)'F'
            && content[8] == (byte)'W'
            && content[9] == (byte)'E'
            && content[10] == (byte)'B'
            && content[11] == (byte)'P')
        {
            contentType = "image/webp";
            return true;
        }

        return false;
    }
}
