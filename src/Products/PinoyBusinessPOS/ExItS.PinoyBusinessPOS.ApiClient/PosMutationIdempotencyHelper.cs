using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

internal static class PosMutationIdempotencyHelper
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string PayloadHashHeader = "X-Pos-Payload-Hash";
    public const string OperationIdHeader = "X-Pos-Operation-Id";
    public const string OperationTypeHeader = "X-Pos-Operation-Type";

    public static string Sha256Hex(string json) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();

    public static IReadOnlyDictionary<string, string> BuildHeaders(
        Guid entityId,
        string payloadJson,
        string operationType)
    {
        var hash = Sha256Hex(payloadJson);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [IdempotencyKeyHeader] = entityId.ToString("N"),
            [PayloadHashHeader] = hash,
            [OperationIdHeader] = entityId.ToString("D"),
            [OperationTypeHeader] = operationType
        };
    }

    public static IReadOnlyDictionary<string, string>? BuildHeaders(PosMutationIdempotencyHeaders? idempotency)
    {
        if (idempotency is null)
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [IdempotencyKeyHeader] = idempotency.IdempotencyKey,
            [PayloadHashHeader] = idempotency.PayloadHash,
            [OperationTypeHeader] = idempotency.OperationType
        };

        if (idempotency.OperationId is Guid operationId && operationId != Guid.Empty)
        {
            headers[OperationIdHeader] = operationId.ToString("D");
        }

        return headers;
    }
}
