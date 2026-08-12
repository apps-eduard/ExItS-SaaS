using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OnlineLoginProgressControllerTests
{
    [Fact]
    public async Task Soft_prompt_appears_after_delay_without_declaring_offline()
    {
        var controller = new OnlineLoginProgressController
        {
            SoftPromptDelay = TimeSpan.FromMilliseconds(40)
        };
        controller.BeginOnlineAttempt(TimeSpan.FromSeconds(5));
        var softShown = false;

        var run = controller.RunAsync(
            async ct =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
                return "ok";
            },
            onSoftPrompt: () => softShown = true);

        await Task.Delay(80);
        Assert.True(softShown);
        Assert.True(controller.SoftPromptVisible);

        var result = await run;
        Assert.Equal(OnlineLoginProgressOutcome.Completed, result.Outcome);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task Fast_online_success_skips_soft_prompt()
    {
        var controller = new OnlineLoginProgressController
        {
            SoftPromptDelay = TimeSpan.FromMilliseconds(200)
        };
        controller.BeginOnlineAttempt(TimeSpan.FromSeconds(5));
        var softShown = false;

        var result = await controller.RunAsync(
            ct => Task.FromResult("fast"),
            onSoftPrompt: () => softShown = true);

        Assert.False(softShown);
        Assert.Equal(OnlineLoginProgressOutcome.Completed, result.Outcome);
        Assert.Equal("fast", result.Value);
    }

    [Fact]
    public async Task Choosing_pin_cancels_online_and_discards_late_result()
    {
        var controller = new OnlineLoginProgressController
        {
            SoftPromptDelay = TimeSpan.FromSeconds(5)
        };
        controller.BeginOnlineAttempt(TimeSpan.FromSeconds(10));
        var onlineFinished = false;

        var run = controller.RunAsync(async ct =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                onlineFinished = true;
                return "late";
            }
            catch (OperationCanceledException)
            {
                onlineFinished = false;
                throw;
            }
        });

        await Task.Delay(30);
        controller.ChoosePinInstead();
        var result = await run;

        Assert.Equal(OnlineLoginProgressOutcome.PinSelected, result.Outcome);
        Assert.True(controller.PinChosen);
        Assert.False(onlineFinished);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Hard_timeout_returns_timed_out_without_pin_choice()
    {
        var controller = new OnlineLoginProgressController
        {
            SoftPromptDelay = TimeSpan.FromSeconds(5)
        };
        controller.BeginOnlineAttempt(TimeSpan.FromMilliseconds(50));

        var result = await controller.RunAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return "never";
        });

        Assert.Equal(OnlineLoginProgressOutcome.HardTimedOut, result.Outcome);
        Assert.False(controller.PinChosen);
    }

    [Fact]
    public async Task Hard_timeout_when_work_swallows_cancel_is_still_timed_out()
    {
        // Mirrors PosApiClient: OperationCanceledException → Cancelled status, no throw.
        var controller = new OnlineLoginProgressController
        {
            SoftPromptDelay = TimeSpan.FromSeconds(5)
        };
        controller.BeginOnlineAttempt(TimeSpan.FromMilliseconds(40));

        var result = await controller.RunAsync(async ct =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return "never";
            }
            catch (OperationCanceledException)
            {
                return "cancelled-payload";
            }
        });

        Assert.Equal(OnlineLoginProgressOutcome.HardTimedOut, result.Outcome);
        Assert.Null(result.Value);
        Assert.False(controller.PinChosen);
    }

    [Fact]
    public void Default_soft_prompt_delay_is_three_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(3), OnlineLoginProgressController.DefaultSoftPromptDelay);
        Assert.Equal(TimeSpan.FromSeconds(3), new OnlineLoginProgressController().SoftPromptDelay);
    }

    [Fact]
    public void Boot_and_SignIn_wire_progressive_online_login_ux()
    {
        var boot = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Boot.razor"));
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));

        Assert.Contains("OnlineLoginProgressController", boot, StringComparison.Ordinal);
        Assert.Contains("SignIn_StillConnecting", boot, StringComparison.Ordinal);
        Assert.Contains("SignIn_ContinueWaiting", boot, StringComparison.Ordinal);
        Assert.Contains("SignIn_UsePinInstead", boot, StringComparison.Ordinal);
        Assert.Contains("IsConnectedAsync", boot, StringComparison.Ordinal);
        Assert.Contains("/offline-pin", boot, StringComparison.Ordinal);
        Assert.Contains("TimeoutSeconds", boot, StringComparison.Ordinal);

        Assert.Contains("OnlineLoginProgressController", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_StillConnecting", signIn, StringComparison.Ordinal);
        Assert.Contains("ChoosePinInstead", signIn, StringComparison.Ordinal);
        Assert.Contains("AuthFailureReason.Offline", signIn, StringComparison.Ordinal);
        Assert.Contains("AuthFailureReason.Timeout", signIn, StringComparison.Ordinal);
        Assert.Contains("AuthFailureReason.Cancelled", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_ServerUnreachablePinHint", signIn, StringComparison.Ordinal);
        Assert.Contains("Auth_InvalidCredentials", signIn, StringComparison.Ordinal);
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Maui project not found.");
    }
}
