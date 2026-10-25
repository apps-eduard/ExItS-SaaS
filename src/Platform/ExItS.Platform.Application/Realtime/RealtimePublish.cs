namespace ExItS.Platform.Application.Realtime;

/// <summary>Helpers that build envelopes and target groups after a successful commit.</summary>
public static class RealtimePublish
{
    public static Task OrganizationSettingsChangedAsync(
        IRealtimeChangePublisher publisher,
        Guid organizationId,
        string? resourceVersion = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = RealtimeEventEnvelope.Create(
            RealtimeEventTypes.OrganizationSettingsChanged,
            RealtimeResourceTypes.Organization,
            organizationId.ToString("D"),
            RealtimeScopeTypes.Organization,
            organizationId.ToString("D"),
            resourceVersion,
            correlationId);
        return publisher.PublishToGroupsAsync(
            [RealtimeGroupNames.Organization(organizationId)],
            envelope,
            cancellationToken);
    }

    public static Task BranchSettingsChangedAsync(
        IRealtimeChangePublisher publisher,
        Guid organizationId,
        Guid branchId,
        string eventType,
        string? resourceVersion = null,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = RealtimeEventEnvelope.Create(
            eventType,
            RealtimeResourceTypes.Branch,
            branchId.ToString("D"),
            RealtimeScopeTypes.Organization,
            organizationId.ToString("D"),
            resourceVersion,
            data: data is null
                ? new Dictionary<string, string> { ["organizationId"] = organizationId.ToString("D") }
                : MergeOrg(data, organizationId));
        return publisher.PublishToGroupsAsync(
            [
                RealtimeGroupNames.Organization(organizationId),
                RealtimeGroupNames.Branch(organizationId, branchId),
                RealtimeGroupNames.StorefrontSeller(organizationId),
                RealtimeGroupNames.LegacyStorefrontSeller(organizationId)
            ],
            envelope,
            cancellationToken);
    }

    public static Task PersonalSettingsChangedAsync(
        IRealtimeChangePublisher publisher,
        Guid userId,
        string eventType,
        string? resourceVersion = null,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = RealtimeEventEnvelope.Create(
            eventType,
            RealtimeResourceTypes.PersonalProfile,
            userId.ToString("D"),
            RealtimeScopeTypes.PersonalUser,
            userId.ToString("D"),
            resourceVersion,
            data: data);
        return publisher.PublishToGroupsAsync(
            [
                RealtimeGroupNames.PersonalUser(userId),
                RealtimeGroupNames.LegacyPersonalProfile(userId)
            ],
            envelope,
            cancellationToken);
    }

    public static Task MembershipAccessChangedAsync(
        IRealtimeChangePublisher publisher,
        Guid organizationId,
        Guid affectedUserId,
        string? resourceVersion = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = RealtimeEventEnvelope.Create(
            RealtimeEventTypes.MembershipAccessChanged,
            RealtimeResourceTypes.Membership,
            affectedUserId.ToString("D"),
            RealtimeScopeTypes.Organization,
            organizationId.ToString("D"),
            resourceVersion,
            data: new Dictionary<string, string>
            {
                ["organizationId"] = organizationId.ToString("D"),
                ["userId"] = affectedUserId.ToString("D")
            });
        return publisher.PublishToGroupsAsync(
            [
                RealtimeGroupNames.Organization(organizationId),
                RealtimeGroupNames.PersonalUser(affectedUserId)
            ],
            envelope,
            cancellationToken);
    }

    /// <summary>
    /// Notify one Personal user. Payload must stay minimal (ids/kind only — never notes, amounts, or previews).
    /// </summary>
    public static Task PersonalUserEventAsync(
        IRealtimeChangePublisher publisher,
        Guid userId,
        string eventType,
        string resourceType,
        string resourceId,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = RealtimeEventEnvelope.Create(
            eventType,
            resourceType,
            resourceId,
            RealtimeScopeTypes.PersonalUser,
            userId.ToString("D"),
            data: data);
        return publisher.PublishToGroupsAsync(
            [
                RealtimeGroupNames.PersonalUser(userId),
                RealtimeGroupNames.LegacyPersonalProfile(userId)
            ],
            envelope,
            cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> MergeOrg(
        IReadOnlyDictionary<string, string> data,
        Guid organizationId)
    {
        var copy = new Dictionary<string, string>(data, StringComparer.Ordinal);
        copy["organizationId"] = organizationId.ToString("D");
        return copy;
    }
}
