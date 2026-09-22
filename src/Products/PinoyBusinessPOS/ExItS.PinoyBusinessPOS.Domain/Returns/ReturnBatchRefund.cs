using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

public sealed class ReturnBatchRefund
{
    public const int ReferenceMaxLength = 64;
    public const int NoteMaxLength = 512;
    public const int ClientRefundIdMaxLength = 64;

    public ReturnBatchRefundId Id { get; }
    public ReturnBatchId ReturnBatchId { get; }
    public decimal Amount { get; }
    public SalePaymentMethod Method { get; }
    public string? Reference { get; }
    public string? Note { get; }
    public string? ClientRefundId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }

    private ReturnBatchRefund(
        ReturnBatchRefundId id,
        ReturnBatchId returnBatchId,
        decimal amount,
        SalePaymentMethod method,
        string? reference,
        string? note,
        string? clientRefundId,
        Guid createdBy,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ReturnBatchId = returnBatchId;
        Amount = amount;
        Method = method;
        Reference = reference;
        Note = note;
        ClientRefundId = clientRefundId;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
    }

    public static ReturnBatchRefund Create(
        ReturnBatchId returnBatchId,
        decimal amount,
        SalePaymentMethod method,
        string? reference,
        string? note,
        Guid createdBy,
        DateTimeOffset createdAtUtc,
        string? clientRefundId = null,
        ReturnBatchRefundId? id = null) =>
        new(
            id ?? ReturnBatchRefundId.New(),
            returnBatchId,
            SaleMoney.RoundMoney(Math.Max(0m, amount)),
            method,
            Normalize(reference, ReferenceMaxLength),
            Normalize(note, NoteMaxLength),
            Normalize(clientRefundId, ClientRefundIdMaxLength),
            createdBy,
            createdAtUtc);

    public static ReturnBatchRefund Rehydrate(
        ReturnBatchRefundId id,
        ReturnBatchId returnBatchId,
        decimal amount,
        SalePaymentMethod method,
        string? reference,
        string? note,
        string? clientRefundId,
        Guid createdBy,
        DateTimeOffset createdAtUtc) =>
        new(id, returnBatchId, amount, method, reference, note, clientRefundId, createdBy, createdAtUtc);

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
