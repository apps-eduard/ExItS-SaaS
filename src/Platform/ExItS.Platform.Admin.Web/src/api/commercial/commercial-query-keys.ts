/**
 * Stable TanStack Query key roots. Must match existing read-query keys so mutations
 * can invalidate without duplicating list/detail tuple builders.
 */
export const commercialQueryKeyRoots = {
  catalogPlans: ["catalog-plans"] as const,
  platformCatalogPlans: ["platform-catalog-plans"] as const,
  catalogProducts: ["catalog-products"] as const,
  platformCatalogProducts: ["platform-catalog-products"] as const,
  organizationCommercialSummary: ["organizations", "commercial-summary"] as const,
  organizationSubscriptions: ["organizations", "subscriptions"] as const,
  organizationEntitlements: ["organizations", "entitlement-snapshots"] as const,
  organizationLatestEntitlement: ["organizations", "latest-entitlement"] as const,
  organizationFeatureOverrides: ["organizations", "feature-overrides"] as const,
  organizationBilling: ["organizations", "payments"] as const,
  organizationActivity: ["organizations", "audit"] as const,
  dashboardSubscriptions: ["dashboard", "subscriptions"] as const,
} as const;

export type CommercialInvalidationScope = {
  organizationId?: string;
  productCode?: string;
  productId?: string;
  planId?: string;
};

export type QueryInvalidator = {
  invalidateQueries: (filters: { queryKey: readonly unknown[] }) => Promise<unknown> | unknown;
};

export async function invalidateCommercialQueries(
  queryClient: QueryInvalidator,
  scope: CommercialInvalidationScope = {},
): Promise<void> {
  const tasks: Array<Promise<unknown> | unknown> = [];

  const enqueue = (queryKey: readonly unknown[]) => {
    tasks.push(queryClient.invalidateQueries({ queryKey }));
  };

  if (scope.planId || scope.productCode || scope.productId) {
    enqueue(commercialQueryKeyRoots.catalogPlans);
    enqueue(commercialQueryKeyRoots.platformCatalogPlans);
    enqueue(commercialQueryKeyRoots.catalogProducts);
    enqueue(commercialQueryKeyRoots.platformCatalogProducts);
    if (scope.planId) {
      enqueue(["catalog-plans", "detail", scope.planId]);
    }
    if (scope.productCode) {
      enqueue(["catalog-products", "plans", scope.productCode]);
    }
    if (scope.productId) {
      enqueue(["catalog-products", "detail", scope.productId]);
    }
  }

  if (scope.organizationId) {
    enqueue([...commercialQueryKeyRoots.organizationCommercialSummary, scope.organizationId]);
    enqueue([...commercialQueryKeyRoots.organizationSubscriptions, scope.organizationId]);
    enqueue([...commercialQueryKeyRoots.organizationEntitlements, scope.organizationId]);
    enqueue([...commercialQueryKeyRoots.organizationLatestEntitlement, scope.organizationId]);
    enqueue([...commercialQueryKeyRoots.organizationFeatureOverrides, scope.organizationId]);
    enqueue([...commercialQueryKeyRoots.organizationBilling, scope.organizationId]);
    enqueue([...commercialQueryKeyRoots.organizationActivity, scope.organizationId]);
    enqueue(commercialQueryKeyRoots.dashboardSubscriptions);
  }
  await Promise.all(tasks);
}

export function catalogPlanInvalidationScope(input: {
  planId?: string;
  productCode?: string;
}): CommercialInvalidationScope {
  return { planId: input.planId, productCode: input.productCode };
}

export function organizationCommercialInvalidationScope(
  organizationId: string,
): CommercialInvalidationScope {
  return { organizationId };
}
