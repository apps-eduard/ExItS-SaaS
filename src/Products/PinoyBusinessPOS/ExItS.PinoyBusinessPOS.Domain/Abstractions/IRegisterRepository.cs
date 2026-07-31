using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public sealed record RegisterFilter(
    string? RegisterCode = null,
    string? Name = null,
    RegisterStatus? Status = null,
    bool? HasOpenShift = null);

public interface IRegisterRepository
{
    Task<Register?> GetByIdAsync(
        PosOrganizationId organizationId,
        RegisterId registerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Register> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        RegisterFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Register>> ListAvailableForShiftAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<Register?> FindByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenShiftAsync(
        PosOrganizationId organizationId,
        RegisterId registerId,
        CancellationToken cancellationToken = default);

    Task<string> AllocateNextRegisterCodeAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Register register, CancellationToken cancellationToken = default);

    Task UpdateAsync(Register register, CancellationToken cancellationToken = default);
}
