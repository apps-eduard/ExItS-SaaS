using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Shared Check metadata + clearing rules for personal and business Utang repayments.
/// </summary>
public static class UtangCheckPayment
{
    public const int CheckNumberMaxLength = 64;
    public const int BankNameMaxLength = 128;
    public const int AccountNameMaxLength = 128;
    public const int ReferenceMaxLength = 128;
    public const int DispositionReasonMaxLength = 512;

    public static bool ReducesOutstanding(
        RepaymentStatus status,
        UtangPaymentMethod paymentMethod,
        UtangCheckClearingStatus checkClearingStatus) =>
        status == RepaymentStatus.Active
        && (paymentMethod != UtangPaymentMethod.Check
            || checkClearingStatus == UtangCheckClearingStatus.Cleared);

    public static bool IsPendingCheck(
        RepaymentStatus status,
        UtangPaymentMethod paymentMethod,
        UtangCheckClearingStatus checkClearingStatus) =>
        status == RepaymentStatus.Active
        && paymentMethod == UtangPaymentMethod.Check
        && checkClearingStatus == UtangCheckClearingStatus.PendingClearing;

    public static string NormalizeRequiredCheckNumber(string? checkNumber)
    {
        if (string.IsNullOrWhiteSpace(checkNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckNumber,
                "Check number is required.");
        }

        var trimmed = checkNumber.Trim();
        if (trimmed.Length > CheckNumberMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckNumber,
                $"Check number must be at most {CheckNumberMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeRequiredBankName(string? bankName)
    {
        if (string.IsNullOrWhiteSpace(bankName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangBankName,
                "Bank name is required.");
        }

        var trimmed = bankName.Trim();
        if (trimmed.Length > BankNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangBankName,
                $"Bank name must be at most {BankNameMaxLength} characters.");
        }

        return trimmed;
    }

    public static DateOnly NormalizeRequiredCheckDate(DateOnly? checkDate)
    {
        if (checkDate is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckDate,
                "Check date is required.");
        }

        return checkDate.Value;
    }

    public static string? NormalizeOptionalAccountName(string? accountName) =>
        NormalizeOptional(accountName, AccountNameMaxLength, DomainErrorCodes.InvalidUtangCheckAccountName);

    public static string? NormalizeOptionalReference(string? reference) =>
        NormalizeOptional(reference, ReferenceMaxLength, DomainErrorCodes.InvalidUtangCheckReference);

    public static string? NormalizeOptionalDispositionReason(string? reason) =>
        NormalizeOptional(reason, DispositionReasonMaxLength, DomainErrorCodes.InvalidUtangCheckDispositionReason);

    public static void EnsureNonCheckHasNoCheckFields(
        UtangPaymentMethod paymentMethod,
        string? checkNumber,
        string? bankName,
        DateOnly? checkDate,
        string? accountName,
        string? reference)
    {
        if (paymentMethod == UtangPaymentMethod.Check)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(checkNumber)
            || !string.IsNullOrWhiteSpace(bankName)
            || checkDate is not null
            || !string.IsNullOrWhiteSpace(accountName)
            || !string.IsNullOrWhiteSpace(reference))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckFieldsForMethod,
                "Check fields are only allowed when payment method is Check.");
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
