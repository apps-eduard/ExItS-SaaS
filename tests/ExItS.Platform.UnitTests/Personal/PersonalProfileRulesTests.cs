using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalProfileRulesTests
{
    [Fact]
    public void Base_profile_allows_empty_address_and_optional_fields()
    {
        var facts = CompleteBase();
        Assert.Empty(PersonalProfileRules.MissingForBase(facts));
        Assert.Equal(100, PersonalProfileRules.BaseCompletionPercent(facts));
        Assert.Contains(PersonalProfileField.AddressLine1, PersonalProfileRules.MissingForStaff(facts));
        Assert.Contains(PersonalProfileField.CityMunicipality, PersonalProfileRules.MissingForCustomer(facts));
    }

    [Fact]
    public void Staff_acceptance_requires_address_fields_customer_does_not_require_street()
    {
        var facts = CompleteBase() with
        {
            AddressLine1 = "12 Rizal",
            Barangay = "Poblacion",
            CityMunicipality = "Kalibo",
            ProvinceState = "Aklan",
        };
        Assert.Empty(PersonalProfileRules.MissingForStaff(facts));
        Assert.Empty(PersonalProfileRules.MissingForCustomer(facts));

        var withoutStreet = facts with { AddressLine1 = null, Barangay = null };
        Assert.Contains(PersonalProfileField.AddressLine1, PersonalProfileRules.MissingForStaff(withoutStreet));
        Assert.Empty(PersonalProfileRules.MissingForCustomer(withoutStreet));
    }

    [Fact]
    public void Connection_view_hides_residential_address_and_respects_visibility()
    {
        var user = PlatformUser.Create("ana", "Ana Reyes", "ana@example.com", DateTimeOffset.UnixEpoch);
        user.UpdatePersonalIdentity("Ana", "Reyes", "Ana Reyes", "09170000000", DateTimeOffset.UnixEpoch);
        var profile = PersonalUserProfile.Create(user.Id, DateTimeOffset.UnixEpoch);
        profile.Update(
            new PersonalUserProfileDraft(
                null,
                new DateOnly(1990, 1, 1),
                null,
                "Filipino",
                "https://example.test/ana.jpg",
                null,
                PersonalFieldVisibility.Connections,
                PersonalFieldVisibility.Connections,
                PersonalFieldVisibility.Public,
                PersonalFieldVisibility.Private,
                PersonalFieldVisibility.Private),
            DateTimeOffset.UnixEpoch);
        var home = PhilippinesAddress(user.Id, PersonalAddressType.Home, isPrimary: true);

        var connection = PersonalProfileProjection.ForAudience(
            user, profile, PersonalProfileAudience.Connection, [home]);
        Assert.Equal("Ana Reyes", connection.DisplayName);
        Assert.Equal("https://example.test/ana.jpg", connection.ProfilePhotoUrl);
        Assert.Equal("Kalibo", connection.CityMunicipality);
        Assert.Null(connection.MobileNumber);
        Assert.Null(connection.Email);
        Assert.DoesNotContain("12 Rizal", $"{connection.DisplayName} {connection.CityMunicipality}");

        var publicView = PersonalProfileProjection.ForAudience(
            user, profile, PersonalProfileAudience.Public, [home]);
        Assert.Null(publicView.DisplayName);
        Assert.Null(publicView.ProfilePhotoUrl);
        Assert.Equal("Kalibo", publicView.CityMunicipality);
        Assert.Null(publicView.MobileNumber);
        Assert.Null(publicView.Email);
        var visibleNames = connection.GetType().GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain("AddressLine1", visibleNames);
        Assert.DoesNotContain("AddressLine2", visibleNames);
        Assert.DoesNotContain("Barangay", visibleNames);
        Assert.DoesNotContain("ProvinceState", visibleNames);
        Assert.DoesNotContain("PostalCode", visibleNames);
        Assert.DoesNotContain("DateOfBirth", visibleNames);
        Assert.DoesNotContain("Region", typeof(PersonalProfileField).GetFields().Select(field => field.Name));
    }

    [Fact]
    public void Relationship_views_do_not_share_each_others_fields()
    {
        var user = PlatformUser.Create("ana", "Ana Reyes", "ana@example.com", DateTimeOffset.UnixEpoch);
        user.UpdatePersonalIdentity("Ana", "Reyes", "Ana Reyes", "09170000000", DateTimeOffset.UnixEpoch);
        var home = PhilippinesAddress(user.Id, PersonalAddressType.Home, isPrimary: true);
        var staff = PersonalProfileProjection.ForStaffRelationship(user, home);
        var customer = PersonalProfileProjection.ForCustomerRelationship(user, home);
        Assert.Equal("12 Rizal", staff.AddressLine1);
        Assert.Equal("Poblacion", staff.Barangay);
        Assert.Equal("Kalibo", customer.CityMunicipality);
        Assert.Equal("09170000000", customer.MobileNumber);
        Assert.Equal("Ana Reyes", customer.DisplayName);
        Assert.Equal("12 Rizal", customer.AddressLine1);
        Assert.Equal("Poblacion", customer.Barangay);
        Assert.Equal("5600", customer.PostalCode);
        Assert.Null(customer.AddressLine2);
        Assert.Null(customer.ProfilePhotoUrl);
        Assert.Equal(
            "https://lh3.googleusercontent.com/a/photo",
            PersonalProfileProjection.ForCustomerRelationship(user, home, "https://lh3.googleusercontent.com/a/photo").ProfilePhotoUrl);
        var customerShape = customer.GetType().GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain("EmployeeCode", staff.GetType().GetProperties().Select(property => property.Name));
        Assert.DoesNotContain("CreditLimit", customerShape);
    }

    [Fact]
    public async Task Gate_blocks_incomplete_staff_and_customer_acceptance()
    {
        var user = PlatformUser.Create("ana", "Ana Reyes", "ana@example.com", DateTimeOffset.UnixEpoch);
        var gate = new PersonalProfileAcceptanceGate(new FixedAddressRepository([]));
        var staff = await gate.MissingStaffFieldsAsync(user, CancellationToken.None);
        var customer = await gate.MissingCustomerFieldsAsync(user, CancellationToken.None);
        Assert.StartsWith(PersonalProfileAcceptanceGate.IncompleteMessagePrefix, staff);
        Assert.Contains(PersonalProfileField.AddressLine1, staff);
        Assert.Contains(PersonalProfileField.CityMunicipality, customer);
        Assert.DoesNotContain(PersonalProfileField.AddressLine1, customer);

        user.UpdatePersonalIdentity("Ana", "Reyes", "Ana Reyes", "09170000000", DateTimeOffset.UnixEpoch);
        var openGate = new PersonalProfileAcceptanceGate(
            new FixedAddressRepository([PhilippinesAddress(user.Id, PersonalAddressType.Home, isPrimary: true)]));
        Assert.Null(await openGate.MissingStaffFieldsAsync(user, CancellationToken.None));
        Assert.Null(await openGate.MissingCustomerFieldsAsync(user, CancellationToken.None));
    }

    [Fact]
    public void Non_philippines_address_does_not_require_barangay_and_countries_can_differ()
    {
        var user = PlatformUser.Create("ana", "Ana Reyes", "ana@example.com", DateTimeOffset.UnixEpoch);
        user.UpdatePersonalIdentity("Ana", "Reyes", "Ana Reyes", "09170000000", DateTimeOffset.UnixEpoch);
        var home = PhilippinesAddress(user.Id, PersonalAddressType.Home, isPrimary: true);
        var office = PersonalAddress.Create(
            user.Id,
            new PersonalAddressDraft(
                PersonalAddressType.Office,
                "Saudi Arabia",
                "King Fahd Road",
                "Barangay should be dropped",
                "Poblacion",
                "Riyadh",
                "Riyadh Province",
                "11564"),
            isPrimary: false,
            DateTimeOffset.UnixEpoch);
        var other = PersonalAddress.Create(
            user.Id,
            new PersonalAddressDraft(PersonalAddressType.Other, "Japan", "1 Chome", null, null, "Tokyo", "Tokyo", "100-0001"),
            isPrimary: false,
            DateTimeOffset.UnixEpoch);

        Assert.Equal("Aklan", home.ProvinceState);
        Assert.Equal("Poblacion", home.Barangay);
        Assert.Null(office.Barangay);
        Assert.Equal("Riyadh Province", office.ProvinceState);
        Assert.Equal("Saudi Arabia", office.Country);
        Assert.NotEqual(home.Country, office.Country);
        Assert.True(other.IsComplete());
        Assert.Null(other.Barangay);

        var facts = PersonalProfileMapper.Facts(user, [home, office]);
        Assert.Empty(PersonalProfileRules.MissingForStaff(facts));
        Assert.Equal("Philippines", facts.Country);

        PersonalAddress.EnforceSinglePrimary([home, office], office.Id, DateTimeOffset.UnixEpoch);
        Assert.False(home.IsPrimary);
        Assert.True(office.IsPrimary);
        Assert.Throws<DomainException>(() => PersonalAddress.Create(
            user.Id,
            new PersonalAddressDraft(PersonalAddressType.Home, "Philippines", "12 Rizal", null, null, "Kalibo", "Aklan", null),
            false,
            DateTimeOffset.UnixEpoch));
    }

    private static PersonalProfileFacts CompleteBase() =>
        new("Ana", "Reyes", "Ana Reyes", "09170000000", "ana@example.com", "Philippines", null, null, null, null);

    private static PersonalAddress PhilippinesAddress(PlatformUserId userId, PersonalAddressType type, bool isPrimary) =>
        PersonalAddress.Create(
            userId,
            new PersonalAddressDraft(type, "Philippines", "12 Rizal", null, "Poblacion", "Kalibo", "Aklan", "5600"),
            isPrimary,
            DateTimeOffset.UnixEpoch);

    private sealed class FixedAddressRepository(IReadOnlyList<PersonalAddress> addresses) : IPersonalAddressRepository
    {
        public Task<IReadOnlyList<PersonalAddress>> ListByUserAsync(PlatformUserId userIdentityId, CancellationToken cancellationToken = default) =>
            Task.FromResult(addresses);

        public Task<PersonalAddress?> GetByIdForUserAsync(PersonalAddressId id, PlatformUserId userIdentityId, CancellationToken cancellationToken = default) =>
            Task.FromResult(addresses.FirstOrDefault(address => address.Id == id));

        public Task AddAsync(PersonalAddress address, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateAsync(PersonalAddress address, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveEnsuringSinglePrimaryAsync(PersonalAddress address, bool makePrimary, DateTimeOffset utcNow, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> DeleteAsync(PersonalAddressId id, PlatformUserId userIdentityId, DateTimeOffset utcNow, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
