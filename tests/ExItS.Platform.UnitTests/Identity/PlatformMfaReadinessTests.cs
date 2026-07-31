using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformMfaReadinessTests
{
    [Fact]
    public async Task Readiness_defaults_to_not_enrolled_without_factors_or_flags()
    {
        var service = new PlatformMfaReadinessService(
            new NullPlatformMfaFactorStore(),
            Options.Create(new PlatformMfaOptions()));

        var snapshot = await service.GetForUserAsync(PlatformUserId.New());
        Assert.False(snapshot.MfaEnabled);
        Assert.False(snapshot.EnrollmentAvailable);
        Assert.False(snapshot.EnforcementRequired);
        Assert.False(snapshot.ChallengeRequired);
        Assert.Equal(0, snapshot.RegisteredFactorCount);
        Assert.Equal(PlatformMfaReadinessService.StateNotEnrolled, snapshot.ReadinessState);
    }

    [Fact]
    public async Task Readiness_reports_deferred_when_enrollment_flag_enabled_without_challenge()
    {
        var service = new PlatformMfaReadinessService(
            new NullPlatformMfaFactorStore(),
            Options.Create(new PlatformMfaOptions { EnrollmentEnabled = true }));

        var snapshot = await service.GetForUserAsync(PlatformUserId.New());
        Assert.True(snapshot.EnrollmentAvailable);
        Assert.False(snapshot.ChallengeRequired);
        Assert.Equal(PlatformMfaReadinessService.StateReadyDeferred, snapshot.ReadinessState);
    }

    [Fact]
    public void Access_token_lifetime_is_clamped_to_max()
    {
        var options = new PlatformAccessTokenOptions { LifetimeHours = 100, MaxLifetimeHours = 24 };
        Assert.Equal(24, options.ResolveLifetimeHours());
        options.LifetimeHours = 0;
        Assert.Equal(1, options.ResolveLifetimeHours());
    }
}
