namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Coordinates an online auth attempt with a soft "still connecting" prompt and optional PIN fallback.
/// Ensures choosing PIN cancels/ignores the pending online attempt so the two flows cannot race.
/// </summary>
public sealed class OnlineLoginProgressController
{
    public static readonly TimeSpan DefaultSoftPromptDelay = TimeSpan.FromSeconds(3);

    private CancellationTokenSource? _onlineCts;
    private TaskCompletionSource<bool>? _pinChosen;
    private int _session;

    /// <summary>Delay before showing "Still connecting..." without declaring offline. Default 3s.</summary>
    public TimeSpan SoftPromptDelay { get; set; } = DefaultSoftPromptDelay;

    public bool SoftPromptVisible { get; private set; }

    public bool PinChosen { get; private set; }

    public CancellationToken OnlineToken =>
        _onlineCts?.Token ?? CancellationToken.None;

    /// <summary>Starts a new online attempt with a hard timeout (typically PosApi TimeoutSeconds, 10–15s).</summary>
    public void BeginOnlineAttempt(TimeSpan hardTimeout)
    {
        CancelOnlineAttempt();
        _session++;
        SoftPromptVisible = false;
        PinChosen = false;
        _onlineCts = new CancellationTokenSource(hardTimeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(15)
            : hardTimeout);
        _pinChosen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void ContinueWaiting()
    {
        // Soft prompt stays visible; hard timeout continues. Does not declare offline.
    }

    public void ChoosePinInstead()
    {
        PinChosen = true;
        _pinChosen?.TrySetResult(true);
        try
        {
            _onlineCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Session already ended.
        }
    }

    public void CancelOnlineAttempt()
    {
        try
        {
            _onlineCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        _onlineCts?.Dispose();
        _onlineCts = null;
        _pinChosen?.TrySetResult(false);
        _pinChosen = null;
    }

    /// <summary>
    /// Runs <paramref name="onlineWork"/> with soft-prompt timing. If PIN is chosen, discards online results.
    /// </summary>
    public async Task<OnlineLoginProgressResult<T>> RunAsync<T>(
        Func<CancellationToken, Task<T>> onlineWork,
        Action? onSoftPrompt = null,
        CancellationToken externalCt = default)
    {
        if (_onlineCts is null || _pinChosen is null)
        {
            throw new InvalidOperationException("BeginOnlineAttempt must be called first.");
        }

        var session = _session;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_onlineCts.Token, externalCt);
        var onlineTask = onlineWork(linked.Token);
        var pinTask = _pinChosen.Task;

        var softDelayTask = Task.Delay(SoftPromptDelay <= TimeSpan.Zero
            ? DefaultSoftPromptDelay
            : SoftPromptDelay, CancellationToken.None);
        var first = await Task.WhenAny(onlineTask, pinTask, softDelayTask).ConfigureAwait(false);

        if (session != _session)
        {
            return OnlineLoginProgressResult<T>.Discarded();
        }

        if (first == softDelayTask && !onlineTask.IsCompleted && !pinTask.IsCompleted)
        {
            SoftPromptVisible = true;
            onSoftPrompt?.Invoke();
        }

        var winner = await Task.WhenAny(onlineTask, pinTask).ConfigureAwait(false);
        if (session != _session)
        {
            return OnlineLoginProgressResult<T>.Discarded();
        }

        if (PinChosen || (pinTask.IsCompletedSuccessfully && pinTask.Result))
        {
            PinChosen = true;
            return OnlineLoginProgressResult<T>.PinSelected();
        }

        try
        {
            var value = await onlineTask.ConfigureAwait(false);
            // PIN may have been chosen in the narrow window after completion started.
            if (PinChosen || (pinTask.IsCompletedSuccessfully && pinTask.Result))
            {
                return OnlineLoginProgressResult<T>.PinSelected();
            }

            // API clients often swallow OperationCanceledException into a Cancelled status
            // instead of rethrowing. When our hard-timeout CTS fired, treat that as unreachable
            // rather than a completed "request cancelled" payload.
            if (_onlineCts.IsCancellationRequested)
            {
                SoftPromptVisible = false;
                return OnlineLoginProgressResult<T>.HardTimedOut();
            }

            SoftPromptVisible = false;
            return OnlineLoginProgressResult<T>.Completed(value);
        }
        catch (OperationCanceledException) when (PinChosen || (pinTask.IsCompletedSuccessfully && pinTask.Result))
        {
            return OnlineLoginProgressResult<T>.PinSelected();
        }
        catch (OperationCanceledException)
        {
            SoftPromptVisible = false;
            return OnlineLoginProgressResult<T>.HardTimedOut();
        }
    }
}

public readonly record struct OnlineLoginProgressResult<T>(
    OnlineLoginProgressOutcome Outcome,
    T? Value)
{
    public static OnlineLoginProgressResult<T> Completed(T value) =>
        new(OnlineLoginProgressOutcome.Completed, value);

    public static OnlineLoginProgressResult<T> PinSelected() =>
        new(OnlineLoginProgressOutcome.PinSelected, default);

    public static OnlineLoginProgressResult<T> HardTimedOut() =>
        new(OnlineLoginProgressOutcome.HardTimedOut, default);

    public static OnlineLoginProgressResult<T> Discarded() =>
        new(OnlineLoginProgressOutcome.Discarded, default);
}

public enum OnlineLoginProgressOutcome
{
    Completed,
    PinSelected,
    HardTimedOut,
    Discarded
}
