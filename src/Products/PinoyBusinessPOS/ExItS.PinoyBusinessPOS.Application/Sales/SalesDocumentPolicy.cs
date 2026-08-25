using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

public static class SalesDocumentWording
{
    public const string TransactionSummary = "TransactionSummary";
    public const string DisclaimerEnglish =
        "This document is for business and customer record purposes only. " +
        "It is not a BIR-registered invoice and does not replace any invoice or other document the seller may be legally required to issue. " +
        "ExItS does not determine the seller's legal BIR invoicing obligations.";
}

public sealed class RequestSalesDocument
{
    public ApplicationResult Execute(SalesDocumentKind kind) =>
        kind == SalesDocumentKind.TransactionSummary
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(
                ApplicationErrorCodes.TaxDocumentIssuanceNotAvailable,
                "Tax-document issuance is not available for this organization.");
}
