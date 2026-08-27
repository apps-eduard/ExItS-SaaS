using ExItS.PinoyBuyNowPayLater.Domain.Financing;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Financing;

internal static class BnplFinancingEntityMapper
{
    public static BnplFinancingApplication ToDomain(BnplFinancingApplicationRecord record)
    {
        var offers = record.Offers
            .OrderBy(o => o.Version)
            .Select(o => BnplFinancingOffer.Reconstitute(
                BnplFinancingOfferId.From(o.Id),
                o.Version,
                o.PurchaseAmount,
                o.DownPaymentAmount,
                o.FinancedPrincipal,
                o.CreatedByActorId,
                o.CreatedAtUtc,
                o.ExpiresAtUtc,
                o.IsSuperseded,
                o.AcceptedAtUtc,
                o.AcceptedByActorId))
            .ToList();

        var decisions = record.Decisions
            .OrderBy(d => d.DecidedAtUtc)
            .Select(d => new BnplFinancingDecision(
                d.Id,
                Enum.Parse<BnplFinancingDecisionStage>(d.Stage, ignoreCase: true),
                Enum.Parse<BnplFinancingDecisionOutcome>(d.Outcome, ignoreCase: true),
                d.ActorId,
                d.DecidedAtUtc,
                d.Note,
                d.OfferId))
            .ToList();

        var plans = record.InstallmentPlans
            .OrderBy(p => p.Version)
            .Select(p => BnplInstallmentPlan.Reconstitute(
                BnplInstallmentPlanId.From(p.Id),
                p.OfferId,
                p.Version,
                p.CreatedByActorId,
                p.CreatedAtUtc,
                p.IsSuperseded,
                p.IsLocked,
                p.Items
                    .OrderBy(i => i.SequenceNumber)
                    .Select(i => new BnplInstallmentPlanItem(
                        BnplInstallmentPlanItemId.From(i.Id),
                        i.SequenceNumber,
                        i.PrincipalAmount,
                        i.DueDate))
                    .ToList()))
            .ToList();

        return BnplFinancingApplication.Reconstitute(
            BnplFinancingApplicationId.From(record.Id),
            record.OrganizationId,
            record.BranchId,
            record.CustomerId,
            Enum.Parse<BnplFinancingApplicationStatus>(record.Status, ignoreCase: true),
            record.PurchaseAmount,
            record.DownPaymentAmount,
            record.RequestedFinanceAmount,
            record.PurchaseDescription,
            record.MerchantProductReference,
            record.AggregateVersion,
            record.EligibilityApproved,
            record.EligibilityDecidedAtUtc,
            record.EligibilityDecidedByActorId,
            record.EligibilityNote,
            record.CurrentOfferId,
            record.AcceptedOfferId,
            record.CreatedByActorId,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            offers,
            decisions,
            plans);
    }

    public static BnplFinancingApplicationRecord ToRecord(BnplFinancingApplication application)
    {
        var record = new BnplFinancingApplicationRecord
        {
            Id = application.Id.Value,
            OrganizationId = application.OrganizationId,
            BranchId = application.BranchId,
            CustomerId = application.CustomerId,
            Status = application.Status.ToString(),
            PurchaseAmount = application.PurchaseAmount,
            DownPaymentAmount = application.DownPaymentAmount,
            RequestedFinanceAmount = application.RequestedFinanceAmount,
            PurchaseDescription = application.PurchaseDescription,
            MerchantProductReference = application.MerchantProductReference,
            AggregateVersion = application.AggregateVersion,
            EligibilityApproved = application.EligibilityApproved,
            EligibilityDecidedAtUtc = application.EligibilityDecidedAtUtc,
            EligibilityDecidedByActorId = application.EligibilityDecidedByActorId,
            EligibilityNote = application.EligibilityNote,
            CurrentOfferId = application.CurrentOfferId,
            AcceptedOfferId = application.AcceptedOfferId,
            CreatedByActorId = application.CreatedByActorId,
            CreatedAtUtc = application.CreatedAtUtc,
            UpdatedAtUtc = application.UpdatedAtUtc
        };

        SyncChildren(application, record);
        return record;
    }

    public static void CopyToRecord(BnplFinancingApplication application, BnplFinancingApplicationRecord record)
    {
        record.Status = application.Status.ToString();
        record.PurchaseAmount = application.PurchaseAmount;
        record.DownPaymentAmount = application.DownPaymentAmount;
        record.RequestedFinanceAmount = application.RequestedFinanceAmount;
        record.PurchaseDescription = application.PurchaseDescription;
        record.MerchantProductReference = application.MerchantProductReference;
        record.AggregateVersion = application.AggregateVersion;
        record.EligibilityApproved = application.EligibilityApproved;
        record.EligibilityDecidedAtUtc = application.EligibilityDecidedAtUtc;
        record.EligibilityDecidedByActorId = application.EligibilityDecidedByActorId;
        record.EligibilityNote = application.EligibilityNote;
        record.CurrentOfferId = application.CurrentOfferId;
        record.AcceptedOfferId = application.AcceptedOfferId;
        record.UpdatedAtUtc = application.UpdatedAtUtc;
        SyncChildren(application, record);
    }

    private static void SyncChildren(BnplFinancingApplication application, BnplFinancingApplicationRecord record)
    {
        var offerById = record.Offers.ToDictionary(o => o.Id);
        foreach (var offer in application.Offers)
        {
            if (!offerById.TryGetValue(offer.Id.Value, out var existing))
            {
                record.Offers.Add(new BnplFinancingOfferRecord
                {
                    Id = offer.Id.Value,
                    ApplicationId = application.Id.Value,
                    Version = offer.Version,
                    PurchaseAmount = offer.PurchaseAmount,
                    DownPaymentAmount = offer.DownPaymentAmount,
                    FinancedPrincipal = offer.FinancedPrincipal,
                    CreatedByActorId = offer.CreatedByActorId,
                    CreatedAtUtc = offer.CreatedAtUtc,
                    ExpiresAtUtc = offer.ExpiresAtUtc,
                    IsSuperseded = offer.IsSuperseded,
                    AcceptedAtUtc = offer.AcceptedAtUtc,
                    AcceptedByActorId = offer.AcceptedByActorId
                });
            }
            else
            {
                existing.IsSuperseded = offer.IsSuperseded;
                existing.AcceptedAtUtc = offer.AcceptedAtUtc;
                existing.AcceptedByActorId = offer.AcceptedByActorId;
            }
        }

        var decisionIds = record.Decisions.Select(d => d.Id).ToHashSet();
        foreach (var decision in application.Decisions)
        {
            if (decisionIds.Contains(decision.DecisionId))
            {
                continue;
            }

            record.Decisions.Add(new BnplFinancingDecisionRecord
            {
                Id = decision.DecisionId,
                ApplicationId = application.Id.Value,
                Stage = decision.Stage.ToString(),
                Outcome = decision.Outcome.ToString(),
                ActorId = decision.ActorId,
                DecidedAtUtc = decision.DecidedAtUtc,
                Note = decision.Note,
                OfferId = decision.OfferId
            });
        }

        var planById = record.InstallmentPlans.ToDictionary(p => p.Id);
        foreach (var plan in application.InstallmentPlans)
        {
            if (!planById.TryGetValue(plan.Id.Value, out var existingPlan))
            {
                var planRecord = new BnplInstallmentPlanRecord
                {
                    Id = plan.Id.Value,
                    ApplicationId = application.Id.Value,
                    OfferId = plan.OfferId,
                    Version = plan.Version,
                    CreatedByActorId = plan.CreatedByActorId,
                    CreatedAtUtc = plan.CreatedAtUtc,
                    IsSuperseded = plan.IsSuperseded,
                    IsLocked = plan.IsLocked
                };

                foreach (var item in plan.Items)
                {
                    planRecord.Items.Add(new BnplInstallmentPlanItemRecord
                    {
                        Id = item.Id.Value,
                        PlanId = plan.Id.Value,
                        SequenceNumber = item.SequenceNumber,
                        PrincipalAmount = item.PrincipalAmount,
                        DueDate = item.DueDate
                    });
                }

                record.InstallmentPlans.Add(planRecord);
            }
            else
            {
                existingPlan.IsSuperseded = plan.IsSuperseded;
                existingPlan.IsLocked = plan.IsLocked;
            }
        }
    }
}
