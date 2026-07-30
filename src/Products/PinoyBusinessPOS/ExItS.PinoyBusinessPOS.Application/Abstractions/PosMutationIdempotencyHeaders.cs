namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Headers sent with idempotent POS mutation requests from the offline queue.</summary>
public sealed record PosMutationIdempotencyHeaders(
    string IdempotencyKey,
    string PayloadHash,
    Guid? OperationId,
    string OperationType);
