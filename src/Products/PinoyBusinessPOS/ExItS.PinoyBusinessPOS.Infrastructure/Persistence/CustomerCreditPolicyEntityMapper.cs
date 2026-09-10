using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class CustomerCreditPolicyEntityMapper
{
    public static CustomerCreditPolicy ToDomain(CustomerCreditPolicyRecord record) =>
        CustomerCreditPolicy.Rehydrate(
            CustomerCreditPolicyId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            POSCustomerId.From(record.CustomerId),
            (CustomerCreditPolicyStatus)record.Status,
            record.CreditLimit,
            record.DefaultTermDays,
            record.ConfiguredByUserId,
            record.ConfiguredAtUtc,
            record.ApprovedByUserId,
            record.ApprovedAtUtc,
            record.UpdatedByUserId,
            record.UpdatedAtUtc);

    public static CustomerCreditPolicyRecord ToRecord(CustomerCreditPolicy policy) =>
        new()
        {
            Id = policy.Id.Value,
            OrganizationId = policy.OrganizationId.Value,
            CustomerId = policy.CustomerId.Value,
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

    public static void ApplyToRecord(CustomerCreditPolicy policy, CustomerCreditPolicyRecord record)
    {
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

    public static CustomerCreditPolicyChange ToDomain(CustomerCreditPolicyChangeRecord record) =>
        CustomerCreditPolicyChange.Rehydrate(
            CustomerCreditPolicyChangeId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            POSCustomerId.From(record.CustomerId),
            CustomerCreditPolicyId.From(record.CustomerCreditPolicyId),
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

    public static CustomerCreditPolicyChangeRecord ToRecord(CustomerCreditPolicyChange change) =>
        new()
        {
            Id = change.Id.Value,
            OrganizationId = change.OrganizationId.Value,
            CustomerId = change.CustomerId.Value,
            CustomerCreditPolicyId = change.CustomerCreditPolicyId.Value,
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
