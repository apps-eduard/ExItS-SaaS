using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class ExpenseEntityMapper
{
    public static ExpenseCategory ToDomain(ExpenseCategoryRecord record) =>
        ExpenseCategory.Rehydrate(
            ExpenseCategoryId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.Name,
            record.NormalizedName,
            Enum.Parse<ExpenseCategoryStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static ExpenseCategoryRecord ToRecord(ExpenseCategory category) =>
        new()
        {
            Id = category.Id.Value,
            OrganizationId = category.OrganizationId.Value,
            Name = category.Name,
            NormalizedName = category.NormalizedName,
            Status = category.Status.ToString(),
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc
        };

    public static void ApplyToRecord(ExpenseCategory category, ExpenseCategoryRecord record)
    {
        record.Name = category.Name;
        record.NormalizedName = category.NormalizedName;
        record.Status = category.Status.ToString();
        record.UpdatedAtUtc = category.UpdatedAtUtc;
    }

    public static Expense ToDomain(ExpenseRecord record) =>
        Expense.Rehydrate(
            ExpenseId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.ExpenseNumber,
            ExpenseCategoryId.From(record.CategoryId),
            Enum.Parse<ExpenseStatus>(record.Status, ignoreCase: true),
            ExpensePaymentMethods.Parse(record.PaymentMethod),
            record.Amount,
            record.Description,
            record.Payee,
            record.GcashReference,
            record.ExpenseDate,
            record.RecordedAtUtc,
            record.RecordedBy,
            record.VoidedAtUtc,
            record.VoidedBy,
            record.VoidReason,
            record.UpdatedAtUtc,
            record.BranchId is null ? null : PosBranchId.From(record.BranchId.Value));

    public static ExpenseRecord ToRecord(Expense expense) =>
        new()
        {
            Id = expense.Id.Value,
            OrganizationId = expense.OrganizationId.Value,
            BranchId = expense.BranchId?.Value,
            ExpenseNumber = expense.ExpenseNumber,
            CategoryId = expense.CategoryId.Value,
            Status = expense.Status.ToString(),
            PaymentMethod = ExpensePaymentMethods.ToCode(expense.PaymentMethod),
            Amount = expense.Amount,
            Description = expense.Description,
            Payee = expense.Payee,
            GcashReference = expense.GCashReference,
            ExpenseDate = expense.ExpenseDate,
            RecordedAtUtc = expense.RecordedAtUtc,
            RecordedBy = expense.RecordedBy,
            VoidedAtUtc = expense.VoidedAtUtc,
            VoidedBy = expense.VoidedBy,
            VoidReason = expense.VoidReason,
            UpdatedAtUtc = expense.UpdatedAtUtc
        };

    public static void ApplyToRecord(Expense expense, ExpenseRecord record)
    {
        record.Status = expense.Status.ToString();
        record.VoidedAtUtc = expense.VoidedAtUtc;
        record.VoidedBy = expense.VoidedBy;
        record.VoidReason = expense.VoidReason;
        record.UpdatedAtUtc = expense.UpdatedAtUtc;
    }
}
