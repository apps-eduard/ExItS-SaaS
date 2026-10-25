namespace ExItS.Platform.Application.Realtime;

/// <summary>Stable Platform event type names (envelope EventType). Keep in sync with docs/architecture/exits-realtime-signalr-architecture.md registry.</summary>
public static class RealtimeEventTypes
{
    public const string OrganizationSettingsChanged = "OrganizationSettingsChanged";
    public const string BranchSettingsChanged = "BranchSettingsChanged";
    public const string BranchOperatingHoursChanged = "BranchOperatingHoursChanged";
    public const string BranchFulfillmentSettingsChanged = "BranchFulfillmentSettingsChanged";
    public const string BranchDeliveryPolicyChanged = "BranchDeliveryPolicyChanged";
    public const string BranchDeliveryAreasChanged = "BranchDeliveryAreasChanged";
    public const string StorefrontAvailabilityChanged = "StorefrontAvailabilityChanged";
    public const string PersonalSettingsChanged = "PersonalSettingsChanged";
    public const string PersonalProfilePhotoChanged = "PersonalProfilePhotoChanged";
    public const string MembershipAccessChanged = "MembershipAccessChanged";
    public const string PersonalUtangEntryChanged = "PersonalUtangEntryChanged";
    public const string PersonalUtangInvitationChanged = "PersonalUtangInvitationChanged";
    public const string PersonalNotificationChanged = "PersonalNotificationChanged";
}

public static class RealtimeScopeTypes
{
    public const string PersonalUser = "personal-user";
    public const string Organization = "organization";
    public const string Branch = "branch";
    public const string StorefrontSeller = "storefront-seller";
}

public static class RealtimeResourceTypes
{
    public const string Organization = "organization";
    public const string Branch = "branch";
    public const string PersonalProfile = "personal-profile";
    public const string Membership = "membership";
    public const string PersonalUtang = "personal-utang";
    public const string PersonalNotification = "personal-notification";
}
