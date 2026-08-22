import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  activatePlan,
  createDraftPlanVersion,
  deactivatePlan,
  publishPlanVersion,
  renamePlan,
  retirePlan,
  updatePlanCommercial,
  upsertDraftFeatureGrant,
  type CreateDraftPlanVersionBody,
  type UpdatePlanCommercialBody,
  type UpsertDraftFeatureGrantBody,
} from "@/api/catalog/plan-mutations-client";
import {
  activateSubscriptionFromPayment,
  confirmManualPayment,
  createManualPayment,
  rejectManualPayment,
  simulateLocalValidationPayment,
  upgradeSubscriptionFromPayment,
  voidManualPayment,
  type ActivateSubscriptionFromPaymentBody,
  type ConfirmPaymentBody,
  type CreateManualPaymentBody,
  type RejectPaymentBody,
  type SimulateLocalValidationPaymentBody,
  type UpgradeSubscriptionFromPaymentBody,
  type VoidPaymentBody,
} from "@/api/payments/payment-mutations-client";
import {
  applyPendingPlanChange,
  cancelSubscription,
  convertTrialSubscription,
  createPaidSubscription,
  enterSubscriptionGracePeriod,
  expireSubscription,
  markSubscriptionPastDue,
  reactivateSubscription,
  scheduleSubscriptionDowngrade,
  startTrialSubscription,
  suspendSubscription,
  upgradeOrganizationSubscription,
  type ConvertTrialBody,
  type CreatePaidSubscriptionBody,
  type DowngradeSubscriptionBody,
  type GracePeriodBody,
  type ReactivateSubscriptionBody,
  type StartTrialBody,
  type SubscriptionLifecycleBody,
  type UpgradeSubscriptionBody,
} from "@/api/subscriptions/subscription-mutations-client";
import {
  createFeatureOverride,
  generateEntitlementSnapshot,
  reconcileEntitlementSnapshot,
  revokeFeatureOverride,
  type CreateFeatureOverrideBody,
  type GenerateEntitlementSnapshotBody,
  type ReconcileEntitlementBody,
  type RevokeFeatureOverrideBody,
} from "@/api/entitlements/entitlement-mutations-client";
import {
  invalidateCommercialQueries,
  organizationCommercialInvalidationScope,
} from "@/api/commercial/commercial-query-keys";
import { env } from "@/lib/env";

const noRetry = { retry: false as const };

function planMutationInvalidation(
  queryClient: ReturnType<typeof useQueryClient>,
  plan: { id: string; productCode: string },
) {
  return invalidateCommercialQueries(queryClient, {
    planId: plan.id,
    productCode: plan.productCode,
    invalidatePlanVersions: true,
    invalidateProductFeatures: true,
  });
}

export function useUpdatePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      productCode: string;
      planId: string;
      body: UpdatePlanCommercialBody;
    }) => updatePlanCommercial(env.platformApiBaseUrl, input.productCode, input.planId, input.body),
    onSuccess: (plan) => planMutationInvalidation(queryClient, plan),
  });
}

export function useRenamePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      productCode: string;
      planId: string;
      body: { displayName: string; expectedUpdatedAtUtc?: string | null };
    }) => renamePlan(env.platformApiBaseUrl, input.productCode, input.planId, input.body),
    onSuccess: (plan) => planMutationInvalidation(queryClient, plan),
  });
}

export function useActivatePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string }) =>
      activatePlan(env.platformApiBaseUrl, input.productCode, input.planId),
    onSuccess: (plan) => planMutationInvalidation(queryClient, plan),
  });
}

export function useDeactivatePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string }) =>
      deactivatePlan(env.platformApiBaseUrl, input.productCode, input.planId),
    onSuccess: (plan) => planMutationInvalidation(queryClient, plan),
  });
}

export function useRetirePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string }) =>
      retirePlan(env.platformApiBaseUrl, input.productCode, input.planId),
    onSuccess: (plan) => planMutationInvalidation(queryClient, plan),
  });
}

export function useCreateDraftPlanVersionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      productCode: string;
      planId: string;
      body: CreateDraftPlanVersionBody;
    }) =>
      createDraftPlanVersion(
        env.platformApiBaseUrl,
        input.productCode,
        input.planId,
        input.body,
      ),
    onSuccess: (version) =>
      invalidateCommercialQueries(queryClient, {
        planId: version.planId,
        productCode: version.productCode,
        invalidatePlanVersions: true,
      }),
  });
}

export function usePublishPlanVersionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string; versionNumber: number }) =>
      publishPlanVersion(
        env.platformApiBaseUrl,
        input.productCode,
        input.planId,
        input.versionNumber,
      ),
    onSuccess: (version) =>
      invalidateCommercialQueries(queryClient, {
        planId: version.planId,
        productCode: version.productCode,
        invalidatePlanVersions: true,
      }),
  });
}

export function useUpsertDraftFeatureGrantMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      productCode: string;
      planId: string;
      versionNumber: number;
      body: UpsertDraftFeatureGrantBody;
    }) =>
      upsertDraftFeatureGrant(
        env.platformApiBaseUrl,
        input.productCode,
        input.planId,
        input.versionNumber,
        input.body,
      ),
    onSuccess: (version) =>
      invalidateCommercialQueries(queryClient, {
        planId: version.planId,
        productCode: version.productCode,
        invalidatePlanVersions: true,
      }),
  });
}

export function useStartTrialMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: StartTrialBody }) =>
      startTrialSubscription(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useSuspendSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { subscriptionId: string; body?: SubscriptionLifecycleBody }) =>
      suspendSubscription(env.platformApiBaseUrl, input.subscriptionId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useReactivateSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { subscriptionId: string; body?: ReactivateSubscriptionBody }) =>
      reactivateSubscription(env.platformApiBaseUrl, input.subscriptionId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useCancelSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { subscriptionId: string; body?: SubscriptionLifecycleBody }) =>
      cancelSubscription(env.platformApiBaseUrl, input.subscriptionId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useUpgradeSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      subscriptionId: string;
      body: UpgradeSubscriptionBody;
    }) =>
      upgradeOrganizationSubscription(
        env.platformApiBaseUrl,
        input.organizationId,
        input.subscriptionId,
        input.body,
      ),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useDowngradeSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      subscriptionId: string;
      body: DowngradeSubscriptionBody;
    }) =>
      scheduleSubscriptionDowngrade(
        env.platformApiBaseUrl,
        input.organizationId,
        input.subscriptionId,
        input.body,
      ),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useApplyPendingPlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; subscriptionId: string }) =>
      applyPendingPlanChange(env.platformApiBaseUrl, input.organizationId, input.subscriptionId),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useEnterGracePeriodMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { subscriptionId: string; body: GracePeriodBody }) =>
      enterSubscriptionGracePeriod(env.platformApiBaseUrl, input.subscriptionId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useMarkPastDueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { subscriptionId: string; body?: SubscriptionLifecycleBody }) =>
      markSubscriptionPastDue(env.platformApiBaseUrl, input.subscriptionId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useExpireSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { subscriptionId: string; body?: SubscriptionLifecycleBody }) =>
      expireSubscription(env.platformApiBaseUrl, input.subscriptionId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useCreateManualPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (body: CreateManualPaymentBody) =>
      createManualPayment(env.platformApiBaseUrl, body),
    onSuccess: (payment) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(payment.organizationId),
      ),
  });
}

export function useConfirmManualPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { paymentId: string; body: ConfirmPaymentBody }) =>
      confirmManualPayment(env.platformApiBaseUrl, input.paymentId, input.body),
    onSuccess: (payment) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(payment.organizationId),
      ),
  });
}

export function useRejectManualPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { paymentId: string; body: RejectPaymentBody }) =>
      rejectManualPayment(env.platformApiBaseUrl, input.paymentId, input.body),
    onSuccess: (payment) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(payment.organizationId),
      ),
  });
}

export function useVoidManualPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { paymentId: string; body: VoidPaymentBody }) =>
      voidManualPayment(env.platformApiBaseUrl, input.paymentId, input.body),
    onSuccess: (payment) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(payment.organizationId),
      ),
  });
}

export function useActivateSubscriptionFromPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { paymentId: string; body: ActivateSubscriptionFromPaymentBody }) =>
      activateSubscriptionFromPayment(env.platformApiBaseUrl, input.paymentId, input.body),
    onSuccess: (result) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(result.subscription.organizationId),
      ),
  });
}

export function useUpgradeSubscriptionFromPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { paymentId: string; body: UpgradeSubscriptionFromPaymentBody }) =>
      upgradeSubscriptionFromPayment(env.platformApiBaseUrl, input.paymentId, input.body),
    onSuccess: (result) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(result.subscription.organizationId),
      ),
  });
}

export function useCreatePaidSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { organizationId: string; body: CreatePaidSubscriptionBody }) =>
      createPaidSubscription(env.platformApiBaseUrl, input.organizationId, input.body),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useConvertTrialSubscriptionMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      subscriptionId: string;
      body: ConvertTrialBody;
    }) =>
      convertTrialSubscription(
        env.platformApiBaseUrl,
        input.organizationId,
        input.subscriptionId,
        input.body,
      ),
    onSuccess: (subscription) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(subscription.organizationId),
      ),
  });
}

export function useSimulateLocalValidationPaymentMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      body: SimulateLocalValidationPaymentBody;
      localValidationToolsEnabled: boolean;
    }) =>
      simulateLocalValidationPayment(env.platformApiBaseUrl, input.body, {
        localValidationToolsEnabled: input.localValidationToolsEnabled,
      }),
    onSuccess: (_result, input) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(input.body.organizationId),
      ),
  });
}

export function useGenerateEntitlementSnapshotMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      productCode: string;
      body?: GenerateEntitlementSnapshotBody;
    }) =>
      generateEntitlementSnapshot(
        env.platformApiBaseUrl,
        input.organizationId,
        input.productCode,
        input.body,
      ),
    onSuccess: (snapshot) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(snapshot.organizationId),
      ),
  });
}

export function useReconcileEntitlementSnapshotMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      productCode: string;
      body?: ReconcileEntitlementBody;
    }) =>
      reconcileEntitlementSnapshot(
        env.platformApiBaseUrl,
        input.organizationId,
        input.productCode,
        input.body,
      ),
    onSuccess: (snapshot) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(snapshot.organizationId),
      ),
  });
}

export function useCreateFeatureOverrideMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      organizationId: string;
      productCode: string;
      body: CreateFeatureOverrideBody;
    }) =>
      createFeatureOverride(
        env.platformApiBaseUrl,
        input.organizationId,
        input.productCode,
        input.body,
      ),
    onSuccess: (featureOverride) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(featureOverride.organizationId),
      ),
  });
}

export function useRevokeFeatureOverrideMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { overrideId: string; body: RevokeFeatureOverrideBody }) =>
      revokeFeatureOverride(env.platformApiBaseUrl, input.overrideId, input.body),
    onSuccess: (featureOverride) =>
      invalidateCommercialQueries(
        queryClient,
        organizationCommercialInvalidationScope(featureOverride.organizationId),
      ),
  });
}
