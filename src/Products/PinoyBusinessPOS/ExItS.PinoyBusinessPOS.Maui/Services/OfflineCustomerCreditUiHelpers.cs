using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Payments;
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
            PlatformBusinessCustomerId: null,
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
            PlatformBusinessCustomerId: null,
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
            CurrentDueDate: credit.CurrentDueDate,
            SourceSaleId: null);

    internal static PosRepaymentDto ToRepayment(LocalRepaymentProjection repayment) =>
        new(
            repayment.RepaymentId,
            repayment.OrganizationId,
            repayment.CustomerId,
            repayment.Amount,
            repayment.Remarks,
            repayment.Status,
            repayment.RecordedAtUtc,
            RecordedBy: Guid.Empty,
            ReversedAtUtc: null,
            ReversalReason: repayment.PendingReversalReason,
            ReversedBy: null);

    internal static (BadgeTone Tone, string LabelKey)? EntityBadge(LocalEntitySyncState state) =>
        state switch
        {
            LocalEntitySyncState.ServerConfirmed => null,
            LocalEntitySyncState.PendingCreate or LocalEntitySyncState.PendingUpdate or LocalEntitySyncState.Syncing
                => (BadgeTone.Warning, "Offline_EntityPending"),
            LocalEntitySyncState.PendingReversal
                => (BadgeTone.Warning, "Offline_EntityPendingReversal"),
            LocalEntitySyncState.Conflict => (BadgeTone.Danger, "Offline_EntityConflict"),
            LocalEntitySyncState.Rejected => (BadgeTone.Danger, "Offline_EntityRejected"),
            LocalEntitySyncState.BlockedByAccess => (BadgeTone.Danger, "Offline_EntityBlockedByAccess"),
            _ => (BadgeTone.Neutral, "Offline_EntityPending"),
        };

    internal static bool HasAtMostTwoDecimalPlaces(decimal amount) =>
        amount == decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
