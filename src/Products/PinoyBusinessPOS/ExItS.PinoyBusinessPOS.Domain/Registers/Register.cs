using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Registers;

/// <summary>
/// Organization-owned logical POS sales station (P10-WP07). Not a branch, drawer, device, or cash account.
/// Cash authority remains on CashierShift.
/// </summary>
public sealed class Register
{
    public const int NameMaxLength = 128;
    public const int DescriptionMaxLength = 512;

    private static readonly Regex NamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-&/]{0,126}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public RegisterId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string RegisterCode { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
    public string? Description { get; private set; }
    public RegisterStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private Register(
        RegisterId id,
        PosOrganizationId organizationId,
        string registerCode,
        string name,
        string normalizedName,
        string? description,
        RegisterStatus status,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        Id = id;
        OrganizationId = organizationId;
        RegisterCode = registerCode;
        Name = name;
        NormalizedName = normalizedName;
        Description = description;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static Register Create(
        PosOrganizationId organizationId,
        string registerCode,
        string name,
        Guid actorId,
        DateTimeOffset utcNow,
        string? description = null,
        RegisterId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        var code = RegisterCodes.Normalize(registerCode);
        var displayName = NormalizeName(name);
        var desc = NormalizeOptionalDescription(description);

        return new Register(
            id ?? RegisterId.New(),
            organizationId,
            code,
            displayName,
            Normalize(displayName),
            desc,
            RegisterStatus.Active,
            utcNow,
            actorId,
            utcNow,
            actorId);
    }

    public static Register Rehydrate(
        RegisterId id,
        PosOrganizationId organizationId,
        string registerCode,
        string name,
        string normalizedName,
        string? description,
        RegisterStatus status,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(id, organizationId, registerCode, name, normalizedName, description, status, createdAtUtc, createdBy, updatedAtUtc, updatedBy);

    public void Update(string name, string? description, Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        Name = NormalizeName(name);
        NormalizedName = Normalize(Name);
        Description = NormalizeOptionalDescription(description);
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void Deactivate(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        if (Status == RegisterStatus.Inactive)
        {
            return;
        }

        if (Status != RegisterStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterStatusTransition,
                "Only Active registers can be deactivated.");
        }

        Status = RegisterStatus.Inactive;
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void Activate(Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);
        if (Status == RegisterStatus.Active)
        {
            return;
        }

        if (Status != RegisterStatus.Inactive)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterStatusTransition,
                "Only Inactive registers can be activated.");
        }

        Status = RegisterStatus.Active;
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void EnsureActiveForShift()
    {
        if (Status != RegisterStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.RegisterNotActive,
                "An Active register is required to open a shift or create a sale.");
        }
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(DomainErrorCodes.InvalidRegisterName, "Register name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength || !NamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterName,
                "Register name is invalid.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRegisterDescription,
                $"Description cannot exceed {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }

    private static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();
}
