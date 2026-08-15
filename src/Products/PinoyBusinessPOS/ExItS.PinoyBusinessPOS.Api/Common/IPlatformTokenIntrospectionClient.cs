namespace ExItS.PinoyBusinessPOS.Api.Common;

public sealed record PlatformTokenIntrospectionResult(
    bool Active,
    Guid? UserId,
    Guid? OrganizationId,
    string? ProductCode,
    bool? ProductAccessAllowed,
    string? SubscriptionStatus,
    IReadOnlyList<string>? EnabledFeatureCodes,
    string? ProductLocalRoleCode = null,
    string? MappedPosRoleCode = null,
    string? MembershipRole = null,
    bool OrganizationManagementAuthority = false);

public interface IPlatformTokenIntrospectionClient
{
    Task<PlatformTokenIntrospectionResult> IntrospectAsync(string accessToken, CancellationToken cancellationToken = default);
}
