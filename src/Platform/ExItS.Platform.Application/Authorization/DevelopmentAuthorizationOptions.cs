namespace ExItS.Platform.Application.Authorization;

/// <summary>
/// Development-stage authorization switches. Must never be enabled as a production authentication substitute.
/// </summary>
public sealed class DevelopmentAuthorizationOptions
{
    public const string SectionName = "DevelopmentAuthorization";

    /// <summary>
    /// When true, actors labeled <see cref="Domain.Audit.AuditActorType.DevelopmentOperator"/> receive all
    /// Platform permissions. Intended only for Development/Testing hosts so existing unauthenticated
    /// development workflows continue while the permission model is exercised. Production must leave this false.
    /// </summary>
    public bool GrantDevelopmentOperatorFullAccess { get; set; }
}
