import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  activatePlan,
  createDraftPlanVersion,
  deactivatePlan,
  publishPlanVersion,
  retirePlan,
  updatePlanCommercial,
  type CreateDraftPlanVersionBody,
  type UpdatePlanCommercialBody,
} from "@/api/catalog/plan-mutations-client";
import {
  cancelSubscription,
  reactivateSubscription,
  startTrialSubscription,
  suspendSubscription,
  upgradeOrganizationSubscription,
  type ReactivateSubscriptionBody,
  type StartTrialBody,
  type SubscriptionLifecycleBody,
  type UpgradeSubscriptionBody,
} from "@/api/subscriptions/subscription-mutations-client";
import {
  generateEntitlementSnapshot,
  type GenerateEntitlementSnapshotBody,
} from "@/api/entitlements/entitlement-mutations-client";
import {
  invalidateCommercialQueries,
  organizationCommercialInvalidationScope,
} from "@/api/commercial/commercial-query-keys";
import { env } from "@/lib/env";

const noRetry = { retry: false as const };

export function useUpdatePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: {
      productCode: string;
      planId: string;
      body: UpdatePlanCommercialBody;
    }) => updatePlanCommercial(env.platformApiBaseUrl, input.productCode, input.planId, input.body),
    onSuccess: (plan) =>
      invalidateCommercialQueries(queryClient, {
        planId: plan.id,
        productCode: plan.productCode,
      }),
  });
}

export function useActivatePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string }) =>
      activatePlan(env.platformApiBaseUrl, input.productCode, input.planId),
    onSuccess: (plan) =>
      invalidateCommercialQueries(queryClient, {
        planId: plan.id,
        productCode: plan.productCode,
      }),
  });
}

export function useDeactivatePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string }) =>
      deactivatePlan(env.platformApiBaseUrl, input.productCode, input.planId),
    onSuccess: (plan) =>
      invalidateCommercialQueries(queryClient, {
        planId: plan.id,
        productCode: plan.productCode,
      }),
  });
}

export function useRetirePlanMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    ...noRetry,
    mutationFn: (input: { productCode: string; planId: string }) =>
      retirePlan(env.platformApiBaseUrl, input.productCode, input.planId),
    onSuccess: (plan) =>
      invalidateCommercialQueries(queryClient, {
        planId: plan.id,
        productCode: plan.productCode,
      }),
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
