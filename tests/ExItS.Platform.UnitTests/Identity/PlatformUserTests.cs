using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformUserTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddMinutes(5);

    [Fact]
    public void Create_valid_user_normalizes_username_and_email()
    {
        var user = PlatformUser.Create("Ada.Lovelace", "Ada Lovelace", "Ada.Lovelace@Example.COM", T0);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Equal("Ada.Lovelace", user.Username);
        Assert.Equal("ada.lovelace", user.NormalizedUsername);
        Assert.Equal("Ada Lovelace", user.DisplayName);
        Assert.Equal("ada.lovelace@example.com", user.NormalizedEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("Ada Lovelace")]
    public void Create_rejects_invalid_username(string username)
    {
        var ex = Assert.Throws<DomainException>(() =>
            PlatformUser.Create(username, "Ada Lovelace", "a@b.co", T0));
        Assert.Equal(DomainErrorCodes.InvalidUsername, ex.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Create_rejects_invalid_display_name(string name)
    {
        var ex = Assert.Throws<DomainException>(() => PlatformUser.Create("ada", name, "a@b.co", T0));
        Assert.Equal(DomainErrorCodes.InvalidDisplayName, ex.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("Ada <ada@example.com>")]
    public void Create_rejects_invalid_email(string email)
    {
        var ex = Assert.Throws<DomainException>(() => PlatformUser.Create("ada", "Ada Lovelace", email, T0));
        Assert.Equal(DomainErrorCodes.InvalidEmail, ex.ErrorCode);
    }

    [Fact]
    public void Suspend_reactivate_and_deactivate_follow_transitions()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        user.Suspend(T1, "policy");
        Assert.Equal(AccountStatus.Suspended, user.Status);
        Assert.Equal(T1, user.SuspendedAtUtc);
        Assert.Equal("policy", user.SuspensionReason);

        var t2 = T1.AddMinutes(1);
        user.Reactivate(t2);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Null(user.SuspendedAtUtc);

        var t3 = t2.AddMinutes(1);
        user.Deactivate(t3);
        Assert.Equal(AccountStatus.Deactivated, user.Status);
    }

    [Fact]
    public void Deactivated_user_cannot_reactivate_or_update()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        user.Deactivate(T1);

        var reactivate = Assert.Throws<DomainException>(() => user.Reactivate(T1.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InvalidAccountStatusTransition, reactivate.ErrorCode);

        var update = Assert.Throws<DomainException>(() =>
            user.UpdateProfile("Ada", "ada2@example.com", T1.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.UserNotActive, update.ErrorCode);
    }

    [Fact]
    public void Update_profile_updates_timestamp()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        user.UpdateProfile("Augusta Lovelace", "augusta@example.com", T1);
        Assert.Equal("Augusta Lovelace", user.DisplayName);
        Assert.Equal("augusta@example.com", user.NormalizedEmail);
        Assert.Equal(T1, user.UpdatedAtUtc);
    }

    [Fact]
    public void Rejects_non_utc_timestamp()
    {
        var local = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.FromHours(3));
        var ex = Assert.Throws<DomainException>(() => PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", local));
        Assert.Equal(DomainErrorCodes.InvalidUtcTimestamp, ex.ErrorCode);
    }
}
