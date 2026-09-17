using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Persisted allocation of a seller BusinessRepayment onto one BusinessCreditEntry (receivable).
/// Amounts are immutable after create; corrections reverse the repayment.
/// </summary>
public sealed class BusinessRepaymentAllocation
{
    public Guid Id { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public BusinessRepaymentId RepaymentId { get; }
    public BusinessCreditEntryId CreditEntryId { get; }
    public decimal Amount { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private BusinessRepaymentAllocation(
        Guid id,
        PosOrganizationId sellerOrganizationId,
        BusinessRepaymentId repaymentId,
        BusinessCreditEntryId creditEntryId,
        decimal amount,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        SellerOrganizationId = sellerOrganizationId;
        RepaymentId = repaymentId;
        CreditEntryId = creditEntryId;
        Amount = amount;
        CreatedAtUtc = createdAtUtc;
    }

    public static BusinessRepaymentAllocation Create(
        PosOrganizationId sellerOrganizationId,
        BusinessRepaymentId repaymentId,
        BusinessCreditEntryId creditEntryId,
        decimal amount,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Allocation timestamp must be UTC.");
        }

        var normalized = SaleMoney.RoundMoney(amount);
        if (normalized <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentAllocationAmount,
                "Allocation amount must be greater than zero.");
        }

        return new BusinessRepaymentAllocation(
            id ?? Guid.NewGuid(),
            sellerOrganizationId,
            repaymentId,
            creditEntryId,
            normalized,
            utcNow);
    }

    public static BusinessRepaymentAllocation Rehydrate(
        Guid id,
        PosOrganizationId sellerOrganizationId,
        BusinessRepaymentId repaymentId,
        BusinessCreditEntryId creditEntryId,
        decimal amount,
        DateTimeOffset createdAtUtc) =>
        new(id, sellerOrganizationId, repaymentId, creditEntryId, amount, createdAtUtc);
}
