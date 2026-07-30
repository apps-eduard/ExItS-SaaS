namespace ExItS.PinoyBusinessPOS.Api.Common;

internal static class PosIdempotencyRequestHeaders
{
    public const string IdempotencyKey = "Idempotency-Key";
    public const string PayloadHash = "X-Pos-Payload-Hash";
    public const string OperationId = "X-Pos-Operation-Id";
    public const string OperationType = "X-Pos-Operation-Type";

    public static bool TryRead(
        HttpRequest request,
        out PosIdempotencyHeaderValues values,
        out IResult? validationProblem)
    {
        values = default!;
        validationProblem = null;

        var hasKey = request.Headers.TryGetValue(IdempotencyKey, out var keyValues)
                     && !string.IsNullOrWhiteSpace(keyValues.FirstOrDefault());
        var hasHash = request.Headers.TryGetValue(PayloadHash, out var hashValues)
                      && !string.IsNullOrWhiteSpace(hashValues.FirstOrDefault());

        if (!hasKey && !hasHash)
        {
            values = new PosIdempotencyHeaderValues(false, null, null, null, null);
            return true;
        }

        if (!hasKey || !hasHash)
        {
            validationProblem = Results.Problem(
                title: "validation_failed",
                detail: "Idempotency-Key and X-Pos-Payload-Hash must both be present for idempotent mutations.",
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        Guid? operationId = null;
        if (request.Headers.TryGetValue(OperationId, out var opValues)
            && !string.IsNullOrWhiteSpace(opValues.FirstOrDefault()))
        {
            if (!Guid.TryParse(opValues.FirstOrDefault(), out var parsed) || parsed == Guid.Empty)
            {
                validationProblem = Results.Problem(
                    title: "validation_failed",
                    detail: "X-Pos-Operation-Id must be a non-empty GUID when provided.",
                    statusCode: StatusCodes.Status400BadRequest);
                return false;
            }

            operationId = parsed;
        }

        string? operationType = null;
        if (request.Headers.TryGetValue(OperationType, out var typeValues)
            && !string.IsNullOrWhiteSpace(typeValues.FirstOrDefault()))
        {
            operationType = typeValues.FirstOrDefault()!.Trim();
        }

        values = new PosIdempotencyHeaderValues(
            true,
            keyValues.FirstOrDefault()!.Trim(),
            hashValues.FirstOrDefault()!.Trim(),
            operationId,
            operationType);
        return true;
    }
}

internal readonly record struct PosIdempotencyHeaderValues(
    bool IsPresent,
    string? IdempotencyKey,
    string? PayloadHash,
    Guid? OperationId,
    string? OperationType);
