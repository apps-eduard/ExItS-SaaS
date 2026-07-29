using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Catalog;

/// <summary>Platform Plan. Belongs to exactly one ProductCode. No invoices or payments.</summary>
public sealed class Plan
{
    public PlanId Id { get; }
    public ProductCode ProductCode { get; }
    public PlanCode Code { get; }
    public string DisplayName { get; private set; }
    public PlanStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Plan(
        PlanId id,
        ProductCode productCode,
        PlanCode code,
        string displayName,
        PlanStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        ProductCode = productCode;
        Code = code;
        DisplayName = displayName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static Plan CreateDraft(
        ProductCode productCode,
        PlanCode code,
        string displayName,
        DateTimeOffset utcNow,
        PlanId? id = null)
    {
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(code);
        DomainTime.EnsureUtc(utcNow);
        return new Plan(
            id ?? PlanId.New(),
            productCode,
            code,
            DomainTime.NormalizeDisplayName(displayName),
            PlanStatus.Draft,
            utcNow,
            utcNow);
    }

    internal static Plan Rehydrate(
        PlanId id,
        ProductCode productCode,
        PlanCode code,
        string displayName,
        PlanStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, productCode, code, displayName, status, createdAtUtc, updatedAtUtc);

    public void Rename(string displayName, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == PlanStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                "A retired Plan cannot be renamed.");
        }

        DisplayName = DomainTime.NormalizeDisplayName(displayName);
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTimeOffset utcNow) => TransitionTo(PlanStatus.Active, utcNow);

    public void Retire(DateTimeOffset utcNow) => TransitionTo(PlanStatus.Retired, utcNow);

    private void TransitionTo(PlanStatus target, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            PlanStatus.Draft => target is PlanStatus.Active or PlanStatus.Retired,
            PlanStatus.Active => target is PlanStatus.Retired,
            PlanStatus.Retired => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlanStatusTransition,
                $"Cannot transition Plan from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }
}
