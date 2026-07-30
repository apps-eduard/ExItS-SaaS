namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Store expense lifecycle. Member names are the stable persistence codes.
/// A recorded expense is never edited; corrections use an explicit void with a reason.
/// </summary>
public enum ExpenseStatus
{
    Recorded = 0,
    Voided = 1
}
