using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Bounded exponential backoff with jitter. Max 8 attempts.
/// </summary>
public sealed class OfflineRetryClassifier(TimeProvider? timeProvider = null) : IOfflineRetryClassifier
{
    public const int DefaultMaxAttempts = 8;
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly Random _rng = new();

    public int MaxAttempts => DefaultMaxAttempts;

    public OfflineFailureClass Classify(ApiCallStatus status, int? httpStatusCode = null)
    {
        if (httpStatusCode is >= 500 and <= 599)
        {
            return OfflineFailureClass.Transient;
        }

        if (httpStatusCode is 401 or 403)
        {
            return OfflineFailureClass.AccessBlocked;
        }

        if (httpStatusCode is 409)
        {
            return OfflineFailureClass.Conflict;
        }

        if (httpStatusCode is >= 400 and < 500)
        {
            return OfflineFailureClass.Permanent;
        }

        return status switch
        {
            ApiCallStatus.Success => OfflineFailureClass.None,
            ApiCallStatus.Offline => OfflineFailureClass.Transient,
            ApiCallStatus.Timeout => OfflineFailureClass.Transient,
            ApiCallStatus.Unavailable => OfflineFailureClass.Transient,
            ApiCallStatus.Cancelled => OfflineFailureClass.Transient,
            ApiCallStatus.Unauthorized => OfflineFailureClass.AccessBlocked,
            ApiCallStatus.Forbidden => OfflineFailureClass.AccessBlocked,
            ApiCallStatus.Conflict => OfflineFailureClass.Conflict,
            ApiCallStatus.Validation => OfflineFailureClass.Permanent,
            ApiCallStatus.NotFound => OfflineFailureClass.Permanent,
            ApiCallStatus.Failed => OfflineFailureClass.Permanent,
            _ => OfflineFailureClass.Permanent
        };
    }

    public DateTimeOffset ComputeNextAttemptUtc(int attemptCount, DateTimeOffset nowUtc)
    {
        var exp = Math.Min(attemptCount, 8);
        var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, Math.Max(0, exp - 1)));
        if (baseDelay > MaxBackoff)
        {
            baseDelay = MaxBackoff;
        }

        var jitterMs = _rng.Next(0, (int)Math.Max(1, baseDelay.TotalMilliseconds * 0.25));
        return nowUtc.Add(baseDelay).AddMilliseconds(jitterMs);
    }
}
