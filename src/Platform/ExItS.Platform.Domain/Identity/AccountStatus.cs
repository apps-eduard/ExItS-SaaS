namespace ExItS.Platform.Domain.Identity;

/// <summary>Controlled Platform User account status. Does not authorize login by itself.</summary>
public enum AccountStatus
{
    Active = 1,
    Suspended = 2,
    Deactivated = 3
}
