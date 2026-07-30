using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.DesignSystem.Components.Primitives;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

internal static class OfflineCustomerCreditUiHelpers
{
    internal static bool IsLocalContextReady(ILocalContextManager contextManager) =>
        contextManager.ActiveContext?.Status == LocalContextInitStatus.Ready;

    internal static PosCustomerListItemDto ToListItem(LocalCustomerProjection customer) =>
        new(
            customer.CustomerId,
            customer.OrganizationId,
            customer.DisplayName,
            customer.MobileNumber,
            customer.Address,
            customer.Notes,
            customer.Status,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);

    internal static PosCustomerDetailDto ToDetail(LocalCustomerProjection customer) =>
        new(
            customer.CustomerId,
            customer.OrganizationId,
            customer.DisplayName,
            customer.MobileNumber,
            customer.Address,
            customer.Notes,
            customer.Status,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc);

    internal static PosCreditEntryDto ToCreditEntry(LocalCreditProjection credit) =>
        new(
            credit.CreditEntryId,
            credit.OrganizationId,
            credit.CustomerId,
            credit.Amount,
            credit.Remarks,
            credit.Status,
            credit.CreatedAtUtc,
            ReversedAtUtc: null,
            ReversalReason: null,
            CurrentDueDate: null);

    internal static (BadgeTone Tone, string LabelKey)? EntityBadge(LocalEntitySyncState state) =>
        state switch
        {
            LocalEntitySyncState.ServerConfirmed => null,
            LocalEntitySyncState.PendingCreate or LocalEntitySyncState.PendingUpdate or LocalEntitySyncState.Syncing
                => (BadgeTone.Warning, "Offline_EntityPending"),
            LocalEntitySyncState.Conflict => (BadgeTone.Danger, "Offline_EntityConflict"),
            LocalEntitySyncState.Rejected => (BadgeTone.Danger, "Offline_EntityRejected"),
            _ => (BadgeTone.Neutral, "Offline_EntityPending"),
        };

    internal static bool HasAtMostTwoDecimalPlaces(decimal amount) =>
        amount == decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
