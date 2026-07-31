using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Identity;

/// <summary>
/// Marker type for <see cref="PasswordHasher{TUser}"/> — not an ASP.NET Identity user store.
/// </summary>
public sealed class PlatformPasswordUser;

/// <summary>
/// Wraps ASP.NET Core <see cref="PasswordHasher{TUser}"/> (versioned PBKDF2, random salt,
/// configurable iteration count, constant-time verify, rehash-needed detection).
/// Does not use ASP.NET Identity user/store packages.
/// </summary>
public sealed class AspNetCorePlatformPasswordHasher : IPlatformPasswordHasher
{
    private readonly PasswordHasher<PlatformPasswordUser> _hasher;
    private readonly PlatformPasswordUser _user = new();

    public AspNetCorePlatformPasswordHasher(IOptions<PasswordHasherOptions>? options = null)
    {
        _hasher = options is null
            ? new PasswordHasher<PlatformPasswordUser>()
            : new PasswordHasher<PlatformPasswordUser>(options);
    }

    public string Algorithm => PlatformUserCredential.AspNetCoreIdentityV3;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return _hasher.HashPassword(_user, password);
    }

    public PlatformPasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || providedPassword is null)
        {
            return PlatformPasswordVerificationResult.Failed;
        }

        var result = _hasher.VerifyHashedPassword(_user, hashedPassword, providedPassword);
        return result switch
        {
            PasswordVerificationResult.Success => PlatformPasswordVerificationResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PlatformPasswordVerificationResult.SuccessRehashNeeded,
            _ => PlatformPasswordVerificationResult.Failed
        };
    }
}
