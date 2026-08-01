namespace ExItS.Platform.Domain.Authorization;

public enum PlatformRoleLifecycleStatus
{
    Active = 1,
    Inactive = 2,
    Retired = 3
}

public enum PlatformRoleKind
{
    BuiltIn = 1,
    Custom = 2
}
