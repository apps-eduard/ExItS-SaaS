using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Registers;

internal static class RegisterEntityMapper
{
    public static Register ToDomain(RegisterRecord record) =>
        Register.Rehydrate(
            RegisterId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.RegisterCode,
            record.Name,
            record.NormalizedName,
            record.Description,
            Enum.Parse<RegisterStatus>(record.Status, ignoreCase: true),
            record.CreatedAtUtc,
            record.CreatedBy,
            record.UpdatedAtUtc,
            record.UpdatedBy);

    public static RegisterRecord ToRecord(Register register) =>
        new()
        {
            Id = register.Id.Value,
            OrganizationId = register.OrganizationId.Value,
            RegisterCode = register.RegisterCode,
            Name = register.Name,
            NormalizedName = register.NormalizedName,
            Description = register.Description,
            Status = register.Status.ToString(),
            CreatedAtUtc = register.CreatedAtUtc,
            CreatedBy = register.CreatedBy,
            UpdatedAtUtc = register.UpdatedAtUtc,
            UpdatedBy = register.UpdatedBy
        };

    public static void ApplyToRecord(Register register, RegisterRecord record)
    {
        record.Name = register.Name;
        record.NormalizedName = register.NormalizedName;
        record.Description = register.Description;
        record.Status = register.Status.ToString();
        record.UpdatedAtUtc = register.UpdatedAtUtc;
        record.UpdatedBy = register.UpdatedBy;
    }
}
