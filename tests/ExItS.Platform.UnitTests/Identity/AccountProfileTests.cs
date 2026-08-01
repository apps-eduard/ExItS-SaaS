using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class AccountProfileTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_personal_profile_is_active()
    {
        var profile = AccountProfile.Create(PlatformUserId.New(), AccountClass.Personal, T0);
        Assert.Equal(AccountClass.Personal, profile.AccountClass);
        Assert.True(profile.IsActive);
        Assert.Equal(AllowedScope.Personal, AccountClassScope.ToScope(profile.AccountClass));
    }

    [Theory]
    [InlineData(AccountClass.Platform, AllowedScope.Platform)]
    [InlineData(AccountClass.Personal, AllowedScope.Personal)]
    [InlineData(AccountClass.Organization, AllowedScope.Organization)]
    public void Account_class_maps_1_to_1_to_allowed_scope(AccountClass accountClass, AllowedScope scope)
    {
        Assert.Equal(scope, AccountClassScope.ToScope(accountClass));
        Assert.True(AccountClassScope.Matches(accountClass, scope));
    }
}
