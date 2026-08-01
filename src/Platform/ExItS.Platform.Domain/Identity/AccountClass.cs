namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Isolated account profile class. Each session is bound to exactly one class (ADR-016 / ADR-017).
/// </summary>
public enum AccountClass
{
    Platform = 1,
    Personal = 2,
    Organization = 3
}

/// <summary>
/// Allowed API scope for a session. Matches <see cref="AccountClass"/> 1:1 in Phase 16.
/// </summary>
public enum AllowedScope
{
    Platform = 1,
    Personal = 2,
    Organization = 3
}

public static class AccountClassScope
{
    public static AllowedScope ToScope(AccountClass accountClass) => accountClass switch
    {
        AccountClass.Platform => AllowedScope.Platform,
        AccountClass.Personal => AllowedScope.Personal,
        AccountClass.Organization => AllowedScope.Organization,
        _ => throw new ArgumentOutOfRangeException(nameof(accountClass), accountClass, null)
    };

    public static bool Matches(AccountClass accountClass, AllowedScope scope) =>
        ToScope(accountClass) == scope;
}
