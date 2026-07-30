using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Organization-owned flat expense category. Grouping and lifecycle only — no hierarchy,
/// budgets, GL mapping, or supplier state.
/// </summary>
public sealed class ExpenseCategory
{
    public const int NameMaxLength = 128;

    public ExpenseCategoryId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public ExpenseCategoryStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ExpenseCategory(
        ExpenseCategoryId id,
        PosOrganizationId organizationId,
        string name,
        string normalizedName,
        ExpenseCategoryStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        NormalizedName = normalizedName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ExpenseCategory Create(
        PosOrganizationId organizationId,
        string name,
        DateTimeOffset utcNow,
        ExpenseCategoryId? id = null)
    {
        ExpenseMoney.EnsureUtc(utcNow);
        var display = NormalizeName(name);

        return new ExpenseCategory(
            id ?? ExpenseCategoryId.New(),
            organizationId,
            display,
            Normalize(display),
            ExpenseCategoryStatus.Active,
            utcNow,
            utcNow);
    }

    public static ExpenseCategory Rehydrate(
        ExpenseCategoryId id,
        PosOrganizationId organizationId,
        string name,
        string normalizedName,
        ExpenseCategoryStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, organizationId, name, normalizedName, status, createdAtUtc, updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow)
    {
        ExpenseMoney.EnsureUtc(utcNow);
        if (Status == ExpenseCategoryStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.ExpenseCategoryNotActive,
                "Inactive expense categories cannot be edited. Reactivate first.");
        }

        var display = NormalizeName(name);
        Name = display;
        NormalizedName = Normalize(display);
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        ExpenseMoney.EnsureUtc(utcNow);
        if (Status == ExpenseCategoryStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseCategoryStatusTransition,
                "Expense category is already inactive.");
        }

        Status = ExpenseCategoryStatus.Inactive;
        UpdatedAtUtc = utcNow;
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        ExpenseMoney.EnsureUtc(utcNow);
        if (Status == ExpenseCategoryStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseCategoryStatusTransition,
                "Expense category is already active.");
        }

        Status = ExpenseCategoryStatus.Active;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.InvalidExpenseCategoryName, "Expense category name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseCategoryName,
                $"Expense category name must be 1–{NameMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Uppercase invariant uniqueness key for a trimmed category name.</summary>
    public static string Normalize(string trimmedName) => trimmedName.ToUpperInvariant();

    public static string NormalizeForLookup(string name) => Normalize(NormalizeName(name));
}
