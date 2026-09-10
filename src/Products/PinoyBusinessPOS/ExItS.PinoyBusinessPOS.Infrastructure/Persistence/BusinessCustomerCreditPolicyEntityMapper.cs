using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class BusinessCustomerCreditPolicyEntityMapper
{
    public static BusinessCustomerCreditPolicy ToDomain(BusinessCustomerCreditPolicyRecord record) =>
        BusinessCustomerCreditPolicy.Rehydrate(
            BusinessCustomerCreditPolicyId.From(record.Id),
            PosOrganizationId.From(record.SellerOrganizationId),
            PosOrganizationId.From(record.BuyerOrganizationId),
            record.ConnectionId,
            (CustomerCreditPolicyStatus)record.Status,
            record.CreditLimit,
            record.DefaultTermDays,
            record.ConfiguredByUserId,
            record.ConfiguredAtUtc,
            record.ApprovedByUserId,
            record.ApprovedAtUtc,
            record.UpdatedByUserId,
            record.UpdatedAtUtc);

    public static BusinessCustomerCreditPolicyRecord ToRecord(BusinessCustomerCreditPolicy policy) =>
        new()
        {
            Id = policy.Id.Value,
            SellerOrganizationId = policy.SellerOrganizationId.Value,
            BuyerOrganizationId = policy.BuyerOrganizationId.Value,
            ConnectionId = policy.ConnectionId,
            Status = (int)policy.Status,
            CreditLimit = policy.CreditLimit,
            DefaultTermDays = policy.DefaultTermDays,
            ConfiguredByUserId = policy.ConfiguredByUserId,
            ConfiguredAtUtc = policy.ConfiguredAtUtc,
            ApprovedByUserId = policy.ApprovedByUserId,
            ApprovedAtUtc = policy.ApprovedAtUtc,
            UpdatedByUserId = policy.UpdatedByUserId,
            UpdatedAtUtc = policy.UpdatedAtUtc
        };

    public static void ApplyToRecord(BusinessCustomerCreditPolicy policy, BusinessCustomerCreditPolicyRecord record)
    {
        record.ConnectionId = policy.ConnectionId;
        record.Status = (int)policy.Status;
        record.CreditLimit = policy.CreditLimit;
        record.DefaultTermDays = policy.DefaultTermDays;
        record.ConfiguredByUserId = policy.ConfiguredByUserId;
        record.ConfiguredAtUtc = policy.ConfiguredAtUtc;
        record.ApprovedByUserId = policy.ApprovedByUserId;
        record.ApprovedAtUtc = policy.ApprovedAtUtc;
        record.UpdatedByUserId = policy.UpdatedByUserId;
        record.UpdatedAtUtc = policy.UpdatedAtUtc;
    }

    public static BusinessCustomerCreditPolicyChange ToDomain(BusinessCustomerCreditPolicyChangeRecord record) =>
        BusinessCustomerCreditPolicyChange.Rehydrate(
            BusinessCustomerCreditPolicyChangeId.From(record.Id),
            PosOrganizationId.From(record.SellerOrganizationId),
            PosOrganizationId.From(record.BuyerOrganizationId),
            BusinessCustomerCreditPolicyId.From(record.BusinessCustomerCreditPolicyId),
            (CustomerCreditPolicyChangeAction)record.Action,
            record.PreviousStatus is null
                ? null
                : (CustomerCreditPolicyStatus)record.PreviousStatus.Value,
            (CustomerCreditPolicyStatus)record.NewStatus,
            record.PreviousCreditLimit,
            record.NewCreditLimit,
            record.PreviousTermDays,
            record.NewTermDays,
            record.ActorUserId,
            record.Reason,
            record.ChangedAtUtc);

    public static BusinessCustomerCreditPolicyChangeRecord ToRecord(BusinessCustomerCreditPolicyChange change) =>
        new()
        {
            Id = change.Id.Value,
            SellerOrganizationId = change.SellerOrganizationId.Value,
            BuyerOrganizationId = change.BuyerOrganizationId.Value,
            BusinessCustomerCreditPolicyId = change.BusinessCustomerCreditPolicyId.Value,
            Action = (int)change.Action,
            PreviousStatus = change.PreviousStatus is null ? null : (int)change.PreviousStatus.Value,
            NewStatus = (int)change.NewStatus,
            PreviousCreditLimit = change.PreviousCreditLimit,
            NewCreditLimit = change.NewCreditLimit,
            PreviousTermDays = change.PreviousTermDays,
            NewTermDays = change.NewTermDays,
            ActorUserId = change.ActorUserId,
            Reason = change.Reason,
            ChangedAtUtc = change.ChangedAtUtc
        };
}
