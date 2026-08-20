using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Permissions;

public sealed record PosRoleDto(string Role, string DisplayName);

public sealed record PosRoleAssignmentDto(
    Guid AssignmentId,
    Guid OrganizationId,
    Guid ActorId,
    string Role,
    string RoleDisplayName,
    string Status,
    DateTimeOffset AssignedAtUtc,
    Guid AssignedBy,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedBy,
    string? RevocationReason,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosEffectivePermissionsDto(
    Guid OrganizationId,
    Guid ActorId,
    string? Role,
    string? RoleDisplayName,
    string Status,
    IReadOnlyList<string> AllowedCapabilities,
    IReadOnlyList<string> AllowedFeatureCodes,
    bool CanManageAssignments,
    bool IsBootstrapEligible);

public sealed record AssignPosRoleRequest(Guid ActorId, string Role, Guid? AssignmentId = null);

public sealed record RevokePosRoleRequest(string? Reason = null);

public sealed record PosRoleAssignmentListDto(
    IReadOnlyList<PosRoleAssignmentDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public static class PosRoleAssignmentMapping
{
    public static PosRoleAssignmentDto Map(PosRoleAssignment a) =>
        new(
            a.Id.Value,
            a.OrganizationId.Value,
            a.ActorId,
            PosRoleCodes.ToCode(a.Role),
            PosRoleCodes.ToDisplayName(a.Role),
            PosRoleAssignmentStatusCodes.ToCode(a.Status),
            a.AssignedAtUtc,
            a.AssignedBy,
            a.RevokedAtUtc,
            a.RevokedBy,
            a.RevocationReason,
            a.UpdatedAtUtc);

    public static IReadOnlyList<string> FeatureCodesForRole(PosRole role)
    {
        var caps = PosRoleMatrix.CapabilitiesFor(role);
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cap in caps)
        {
            foreach (var code in FeatureCodesForCapability(cap))
            {
                codes.Add(code);
            }
        }

        return codes.OrderBy(c => c, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> FeatureCodesForCapability(UtangCapability capability) => capability switch
    {
        UtangCapability.ViewCatalog => [PosFeatureCodes.StoreCatalogView],
        UtangCapability.ManageCatalog => [PosFeatureCodes.StoreCatalogManage],
        UtangCapability.ViewSales => [PosFeatureCodes.StoreSalesView],
        UtangCapability.CreateSale => [PosFeatureCodes.StoreSalesCreate],
        UtangCapability.VoidSale => [PosFeatureCodes.StoreSalesVoid],
        UtangCapability.ApplyCommercialDiscount => [PosFeatureCodes.StoreSalesApplyCommercialDiscount],
        UtangCapability.ViewInventory => [PosFeatureCodes.StoreInventoryView],
        UtangCapability.ManageInventory => [PosFeatureCodes.StoreInventoryManage],
        UtangCapability.ViewExpenses => [PosFeatureCodes.StoreExpensesView],
        UtangCapability.ManageExpenses => [PosFeatureCodes.StoreExpensesManage],
        UtangCapability.ViewDashboard => [PosFeatureCodes.StoreDashboardView],
        UtangCapability.ViewReports => [PosFeatureCodes.StoreReportsView],
        UtangCapability.ViewSuppliers => [PosFeatureCodes.StoreSuppliersView],
        UtangCapability.ManageSuppliers => [PosFeatureCodes.StoreSuppliersManage],
        UtangCapability.ViewPurchasing => [PosFeatureCodes.StorePurchasingView],
        UtangCapability.ManagePurchasing => [PosFeatureCodes.StorePurchasingManage],
        UtangCapability.ViewShifts => [PosFeatureCodes.StoreShiftsView],
        UtangCapability.ManageShifts => [PosFeatureCodes.StoreShiftsManage],
        UtangCapability.ViewReturns => [PosFeatureCodes.StoreReturnsView],
        UtangCapability.ProcessReturn => [PosFeatureCodes.StoreReturnsManage],
        UtangCapability.ViewPermissions => [PosFeatureCodes.StorePermissionsView],
        UtangCapability.ManagePermissions => [PosFeatureCodes.StorePermissionsManage],
        UtangCapability.ViewRegisters => [PosFeatureCodes.StoreRegistersView],
        UtangCapability.ManageRegisters => [PosFeatureCodes.StoreRegistersManage],
        UtangCapability.ViewCustomerOrders => [PosFeatureCodes.StoreCustomerOrdering],
        UtangCapability.ManageCustomerOrders =>
            [PosFeatureCodes.StoreCustomerOrdering, PosFeatureCodes.StoreDeliveryOrders],
        UtangCapability.PlaceCustomerOrders => [PosFeatureCodes.StoreCustomerOrdering],
        UtangCapability.ViewCustomersAndHistory
            or UtangCapability.ViewGenerateStatement
            or UtangCapability.ViewGenerateReceipt
            or UtangCapability.ReverseCredit => [PosFeatureCodes.CustomerCreditView],
        UtangCapability.RecordRepayment or UtangCapability.ReverseRepayment => [PosFeatureCodes.CustomerCreditRepay],
        UtangCapability.CreateCustomer
            or UtangCapability.EditCustomer
            or UtangCapability.CreateCredit
            or UtangCapability.MutateDueDate => [PosFeatureCodes.CustomerCreditCreate],
        _ => []
    };
}
