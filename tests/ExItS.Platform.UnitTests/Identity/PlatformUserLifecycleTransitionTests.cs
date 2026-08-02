using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformUserLifecycleTransitionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Platform_account_transition_matrix_is_enforced()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        var t = T0;

        t = t.AddMinutes(1);
        user.Suspend(t, "temporary");
        Assert.Equal(AccountStatus.Suspended, user.Status);

        t = t.AddMinutes(1);
        user.Reactivate(t);
        Assert.Equal(AccountStatus.Active, user.Status);

        t = t.AddMinutes(1);
        user.Deactivate(t, "left company");
        Assert.Equal(AccountStatus.Deactivated, user.Status);
        Assert.Equal("left company", user.SuspensionReason);

        t = t.AddMinutes(1);
        user.MoveToSuspended(t, "under review");
        Assert.Equal(AccountStatus.Suspended, user.Status);
        Assert.Equal("under review", user.SuspensionReason);

        t = t.AddMinutes(1);
        user.Deactivate(t, "confirmed exit");
        Assert.Equal(AccountStatus.Deactivated, user.Status);

        t = t.AddMinutes(1);
        user.Reactivate(t, "return to work");
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Null(user.SuspendedAtUtc);
    }

    [Fact]
    public void Move_to_suspended_rejects_non_deactivated_source()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        var ex = Assert.Throws<DomainException>(() => user.MoveToSuspended(T0.AddMinutes(1), "nope"));
        Assert.Equal(DomainErrorCodes.InvalidAccountStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Deactivate_and_move_to_suspended_require_reason()
    {
        var user = PlatformUser.Create("ada", "Ada Lovelace", "ada@example.com", T0);
        Assert.Throws<DomainException>(() => user.Deactivate(T0.AddMinutes(1), " "));
        user.Deactivate(T0.AddMinutes(2), "exit");
        Assert.Throws<DomainException>(() => user.MoveToSuspended(T0.AddMinutes(3), ""));
    }

    [Fact]
    public void Pending_verification_activates_or_deactivates_only()
    {
        var user = PlatformUser.CreatePendingVerification("pending.user", "Pending User", "pending@example.com", T0);
        Assert.Equal(AccountStatus.PendingVerification, user.Status);

        var suspendEx = Assert.Throws<DomainException>(() => user.Suspend(T0.AddMinutes(1), "nope"));
        Assert.Equal(DomainErrorCodes.InvalidAccountStatusTransition, suspendEx.ErrorCode);

        user.ActivateFromPendingVerification(T0.AddMinutes(2));
        Assert.Equal(AccountStatus.Active, user.Status);

        var again = PlatformUser.CreatePendingVerification("pending.two", "Pending Two", "pending2@example.com", T0);
        again.Deactivate(T0.AddMinutes(1), "abandoned signup");
        Assert.Equal(AccountStatus.Deactivated, again.Status);
    }
}
