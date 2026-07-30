using System.Security.Cryptography;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// AES-GCM payload protector. Key is generated once and stored only in SecureStorage.
/// </summary>
public sealed class AesGcmLocalPayloadProtector(ISecureTokenStore tokens) : ILocalPayloadProtector
{
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public async Task EnsureKeyAsync(CancellationToken ct = default)
    {
        if (await IsKeyAvailableAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        await tokens.SetAsync(SecureTokenKeys.LocalPayloadEncryptionKey, Convert.ToBase64String(key), ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsKeyAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var text = await tokens.GetAsync(SecureTokenKeys.LocalPayloadEncryptionKey, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var bytes = Convert.FromBase64String(text);
            return bytes.Length == KeySizeBytes;
        }
        catch
        {
            return false;
        }
    }

    public async Task<EncryptedPayload> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        string associatedData,
        CancellationToken ct = default)
    {
        var key = await LoadKeyAsync(ct).ConfigureAwait(false);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];
        var aad = System.Text.Encoding.UTF8.GetBytes(associatedData);

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext.Span, ciphertext, tag, aad);
        return new EncryptedPayload(ciphertext, nonce, tag);
    }

    public async Task<byte[]> DecryptAsync(
        EncryptedPayload encrypted,
        string associatedData,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(encrypted);
        var key = await LoadKeyAsync(ct).ConfigureAwait(false);
        var plaintext = new byte[encrypted.Ciphertext.Length];
        var aad = System.Text.Encoding.UTF8.GetBytes(associatedData);

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(encrypted.Nonce, encrypted.Ciphertext, encrypted.Tag, plaintext, aad);
        return plaintext;
    }

    private async Task<byte[]> LoadKeyAsync(CancellationToken ct)
    {
        var text = await tokens.GetAsync(SecureTokenKeys.LocalPayloadEncryptionKey, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("local_payload_key_unavailable");
        }

        var bytes = Convert.FromBase64String(text);
        if (bytes.Length != KeySizeBytes)
        {
            throw new InvalidOperationException("local_payload_key_invalid");
        }

        return bytes;
    }
}
