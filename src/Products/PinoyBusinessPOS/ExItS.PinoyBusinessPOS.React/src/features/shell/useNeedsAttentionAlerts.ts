import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  canManageBranchFulfillment,
  canManageCustomerCreditPolicy,
  canManagePaymentMethods,
  canViewInventory,
  canViewSuppliers,
  hasOrganizationManagementAuthority,
  isPosOwnerRole,
} from "@/access/pos-capabilities";
import { getBranchFulfillmentReadiness } from "@/api/platform/branch-fulfillment-client";
import {
  getOrganizationFulfillmentSettings,
  getSupplierConnectedSupplierCommerceReadiness,
  listBusinessCustomers,
} from "@/api/pos/pos-connected-suppliers-client";
import { listInventory } from "@/api/pos/pos-inventory-client";
import { listPaymentMethods } from "@/api/pos/pos-payment-methods-client";
import { getManagementOverview } from "@/api/pos/pos-reporting-client";
import {
  buildNeedsAttentionAlerts,
  formatNeedsAttentionBadge,
  groupNeedsAttentionAlerts,
  hasEnabledPoPaymentMethod,
  requirementIsMissing,
  type NeedsAttentionAlert,
  type NeedsAttentionGroup,
} from "@/features/shell/needs-attention";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const COMMERCE_READINESS_CAP = 25;

export function needsAttentionQueryKey(
  organizationId: string | null | undefined,
  branchId: string | null | undefined,
) {
  return ["shell", "needs-attention", organizationId ?? "none", branchId ?? "none"] as const;
}

export type UseNeedsAttentionAlertsResult = {
  alerts: NeedsAttentionAlert[];
  groups: NeedsAttentionGroup[];
  count: number;
  badge: string | null;
  isLoading: boolean;
  isFetching: boolean;
};

/**
 * Loads unresolved actionable conditions for the navbar Needs Attention control.
 * Respects capability gates and bound org/branch scope. Does not invent counts.
 */
export function useNeedsAttentionAlerts(): UseNeedsAttentionAlertsResult {
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;
  const workspace =
    organizationId && branchId ? { organizationId, branchId } : null;

  const allowInventory = canViewInventory(sessionGrant);
  const allowSuppliers = canViewSuppliers(sessionGrant);
  const allowCredit = canManageCustomerCreditPolicy(sessionGrant);
  /**
   * Detect supplier-commerce payment gaps for eligible sellers even when the
   * `store-payment-management` feature code is absent from the grant.
   * Capability still gates mutation UIs elsewhere; do not hide the alert.
   */
  const allowPaymentDetection =
    canManagePaymentMethods(sessionGrant) ||
    allowSuppliers ||
    hasOrganizationManagementAuthority(sessionGrant) ||
    isPosOwnerRole(sessionGrant);
  /**
   * Branch/fulfillment readiness for connected-supplier setup must surface for
   * supplier-eligible principals — not only Platform org-admin fulfillment editors.
   */
  const allowBranchDetection =
    canManageBranchFulfillment(sessionGrant) ||
    allowSuppliers ||
    hasOrganizationManagementAuthority(sessionGrant) ||
    isPosOwnerRole(sessionGrant);
  const supplierCommerceEligible = allowSuppliers || isPosOwnerRole(sessionGrant);
  const allowCommerce = allowSuppliers || allowCredit || allowPaymentDetection;

  const enabled = Boolean(workspace);

  const overviewQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "overview",
      allowInventory,
    ],
    enabled: enabled && allowInventory,
    staleTime: 60_000,
    queryFn: ({ signal }) => getManagementOverview(workspace!, signal),
  });

  const outOfStockQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "out-of-stock",
      allowInventory,
    ],
    enabled: enabled && allowInventory,
    staleTime: 60_000,
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        { tracked: true, stockStatus: "OutOfStock", page: 1, pageSize: 1 },
        signal,
      ),
  });

  const paymentsQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "payments",
      allowPaymentDetection,
    ],
    enabled: enabled && allowPaymentDetection,
    staleTime: 60_000,
    queryFn: ({ signal }) => listPaymentMethods(workspace!, signal),
  });

  const businessCustomersQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "business-customers",
      allowSuppliers || allowCredit,
    ],
    enabled: enabled && (allowSuppliers || allowCredit),
    staleTime: 60_000,
    queryFn: ({ signal }) => listBusinessCustomers(workspace!, undefined, signal),
  });

  const activeConnectionIds = useMemo(() => {
    const rows = businessCustomersQuery.data ?? [];
    return rows
      .filter(
        (row) =>
          row.relationshipStatus.localeCompare("Active", undefined, {
            sensitivity: "accent",
          }) === 0,
      )
      .map((row) => row.connectionId)
      .slice(0, COMMERCE_READINESS_CAP);
  }, [businessCustomersQuery.data]);

  const commerceReadinessQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "commerce-readiness",
      activeConnectionIds.join(","),
      allowSuppliers || allowCredit,
    ],
    enabled:
      enabled &&
      (allowSuppliers || allowCredit) &&
      activeConnectionIds.length > 0 &&
      businessCustomersQuery.isSuccess,
    staleTime: 60_000,
    queryFn: async ({ signal }) => {
      const results = await Promise.all(
        activeConnectionIds.map(async (connectionId) => {
          const readiness = await getSupplierConnectedSupplierCommerceReadiness(
            workspace!,
            connectionId,
            signal,
          );
          return { connectionId, readiness };
        }),
      );
      return results;
    },
  });

  const branchReadinessQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "branch-fulfillment",
      allowBranchDetection,
    ],
    enabled: enabled && allowBranchDetection && Boolean(organizationId) && Boolean(branchId),
    staleTime: 60_000,
    queryFn: () => getBranchFulfillmentReadiness(organizationId!, branchId!),
  });

  const orgFulfillmentQuery = useQuery({
    queryKey: [
      ...needsAttentionQueryKey(organizationId, branchId),
      "org-fulfillment-settings",
      allowSuppliers || allowBranchDetection,
    ],
    enabled: enabled && Boolean(workspace) && (allowSuppliers || allowBranchDetection),
    staleTime: 60_000,
    queryFn: ({ signal }) => getOrganizationFulfillmentSettings(workspace!, signal),
  });

  const alerts = useMemo(() => {
    if (!workspace) {
      return [];
    }

    const incompleteSupplierConnectionIds: string[] = [];
    const creditIncompleteConnectionIds: string[] = [];

    for (const item of commerceReadinessQuery.data ?? []) {
      if (!item.readiness.isReady && allowSuppliers) {
        incompleteSupplierConnectionIds.push(item.connectionId);
      }
      if (
        allowCredit &&
        requirementIsMissing(item.readiness.requirements, "CreditPolicy")
      ) {
        creditIncompleteConnectionIds.push(item.connectionId);
      }
    }

    const paymentSetupIncomplete =
      allowPaymentDetection &&
      supplierCommerceEligible &&
      paymentsQuery.isSuccess &&
      !hasEnabledPoPaymentMethod(paymentsQuery.data ?? []);

    return buildNeedsAttentionAlerts({
      inventory: allowInventory
        ? {
            lowStockProductCount: overviewQuery.data?.lowStockProductCount ?? null,
            outOfStockProductCount: outOfStockQuery.data?.totalCount ?? null,
            expiredLotCount: overviewQuery.data?.expiredLotCount ?? null,
            nearExpiryLotCount: overviewQuery.data?.nearExpiryLotCount ?? null,
          }
        : null,
      commerce: allowCommerce
        ? {
            incompleteSupplierConnectionIds: allowSuppliers
              ? incompleteSupplierConnectionIds
              : [],
            paymentSetupIncomplete,
            creditIncompleteConnectionIds: allowCredit
              ? creditIncompleteConnectionIds
              : [],
          }
        : null,
      branch:
        allowBranchDetection && branchReadinessQuery.data
          ? {
              branchId,
              deliveryEnabled: branchReadinessQuery.data.deliveryEnabled,
              pickupEnabled: branchReadinessQuery.data.pickupEnabled,
              deliveryReady: branchReadinessQuery.data.deliveryReady,
              pickupReady: branchReadinessQuery.data.pickupReady,
              customerOrderingEnabled: branchReadinessQuery.data.customerOrderingEnabled,
              supplierCommerceEligible,
              orgOfferDelivery: orgFulfillmentQuery.data?.offerDelivery === true,
              branchDetailsComplete: branchReadinessQuery.data.branchDetailsComplete,
              deliveryLocationComplete: branchReadinessQuery.data.deliveryLocationComplete,
              deliveryPolicyComplete: branchReadinessQuery.data.deliveryPolicyComplete,
              deliveryAreasComplete: branchReadinessQuery.data.deliveryAreasComplete,
            }
          : null,
    });
  }, [
    allowBranchDetection,
    allowCommerce,
    allowCredit,
    allowInventory,
    allowPaymentDetection,
    allowSuppliers,
    branchId,
    branchReadinessQuery.data,
    orgFulfillmentQuery.data?.offerDelivery,
    commerceReadinessQuery.data,
    outOfStockQuery.data?.totalCount,
    overviewQuery.data?.expiredLotCount,
    overviewQuery.data?.lowStockProductCount,
    overviewQuery.data?.nearExpiryLotCount,
    paymentsQuery.data,
    paymentsQuery.isSuccess,
    supplierCommerceEligible,
    workspace,
  ]);

  const groups = useMemo(() => groupNeedsAttentionAlerts(alerts), [alerts]);
  const count = alerts.length;
  const badge = formatNeedsAttentionBadge(count);

  const isLoading =
    (allowInventory && (overviewQuery.isLoading || outOfStockQuery.isLoading)) ||
    (allowPaymentDetection && paymentsQuery.isLoading) ||
    ((allowSuppliers || allowCredit) &&
      (businessCustomersQuery.isLoading ||
        (activeConnectionIds.length > 0 && commerceReadinessQuery.isLoading))) ||
    (allowBranchDetection && branchReadinessQuery.isLoading);

  const isFetching =
    overviewQuery.isFetching ||
    outOfStockQuery.isFetching ||
    paymentsQuery.isFetching ||
    businessCustomersQuery.isFetching ||
    commerceReadinessQuery.isFetching ||
    branchReadinessQuery.isFetching;

  return { alerts, groups, count, badge, isLoading, isFetching };
}
