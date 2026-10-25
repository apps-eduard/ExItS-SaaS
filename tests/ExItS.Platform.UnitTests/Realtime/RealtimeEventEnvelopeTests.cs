using ExItS.Platform.Application.Realtime;

namespace ExItS.Platform.UnitTests.Realtime;

public sealed class RealtimeEventEnvelopeTests
{
    [Fact]
    public void Create_sets_schema_version_and_ids()
    {
        var envelope = RealtimeEventEnvelope.Create(
            RealtimeEventTypes.OrganizationSettingsChanged,
            RealtimeResourceTypes.Organization,
            Guid.NewGuid().ToString("D"),
            RealtimeScopeTypes.Organization,
            Guid.NewGuid().ToString("D"));

        Assert.Equal(RealtimeEventEnvelope.CurrentSchemaVersion, envelope.SchemaVersion);
        Assert.NotEqual(Guid.Empty, envelope.EventId);
        Assert.Equal(RealtimeEventTypes.OrganizationSettingsChanged, envelope.EventType);
    }

    [Fact]
    public void Group_names_are_stable_and_prefixed()
    {
        var org = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var user = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var branch = Guid.Parse("99999999-8888-7777-6666-555555555555");

        Assert.Equal($"rt:organization:{org:D}", RealtimeGroupNames.Organization(org));
        Assert.Equal($"rt:personal-user:{user:D}", RealtimeGroupNames.PersonalUser(user));
        Assert.Equal($"rt:branch:{org:D}:{branch:D}", RealtimeGroupNames.Branch(org, branch));
        Assert.StartsWith("rt:storefront-seller:", RealtimeGroupNames.StorefrontSeller(org));
    }
}
