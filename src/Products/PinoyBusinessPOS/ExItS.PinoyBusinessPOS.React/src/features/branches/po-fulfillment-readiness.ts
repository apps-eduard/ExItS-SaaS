/**
 * Purchase Order fulfillment readiness projection for Branch → Fulfillment.
 * Reuses getBranchFulfillmentReadiness fields — does not invent readiness rules.
 */

import type { BranchFulfillmentReadinessDto } from "@/api/platform/branch-fulfillment-client";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import type { MessageKey } from "@/i18n/messages";

export type PoFulfillmentChecklistItemId =
  | "enableMethod"
  | "branchInfo"
  | "pickupProgress"
  | "deliveryProgress";

export type PoFulfillmentChecklistItem = {
  id: PoFulfillmentChecklistItemId;
  done: boolean;
  labelKey: MessageKey;
  /** Optional progress like "0/2" shown beside the label. */
  progressLabel?: string;
};

export type PoFulfillmentMethodLine = {
  channel: "pickup" | "delivery";
  enabled: boolean;
  ready: boolean;
  complete: number;
  total: number;
};

export type PoFulfillmentReadinessView = {
  ready: boolean;
  statusKey: MessageKey;
  ledeKey: MessageKey;
  checklist: PoFulfillmentChecklistItem[];
  methods: PoFulfillmentMethodLine[];
  showConfigureAction: boolean;
};

export type SupplierReadinessSummaryItem = {
  key: "fulfillment" | "catalog" | "payments" | "contact";
  ok: boolean;
  labelKey: MessageKey;
  href: string;
};

function progressText(complete: number, total: number): string {
  return `${complete}/${total}`;
}

/**
 * Ready when at least one enabled fulfillment method is fully ready.
 * Disabled methods never contribute missing-detail checklist rows.
 */
export function buildPoFulfillmentReadinessView(
  readiness: Pick<
    BranchFulfillmentReadinessDto,
    | "pickupEnabled"
    | "deliveryEnabled"
    | "pickupReady"
    | "deliveryReady"
    | "branchDetailsComplete"
    | "pickupSectionsComplete"
    | "pickupSectionsTotal"
    | "deliverySectionsComplete"
    | "deliverySectionsTotal"
  >,
): PoFulfillmentReadinessView {
  const pickupEnabled = readiness.pickupEnabled;
  const deliveryEnabled = readiness.deliveryEnabled;
  const noMethod = !pickupEnabled && !deliveryEnabled;
  const pickupReady = pickupEnabled && readiness.pickupReady;
  const deliveryReady = deliveryEnabled && readiness.deliveryReady;
  const ready = pickupReady || deliveryReady;

  const checklist: PoFulfillmentChecklistItem[] = [];

  if (noMethod) {
    checklist.push({
      id: "enableMethod",
      done: false,
      labelKey: "branches.poFulfillment.checklist.enableMethod",
    });
  }

  if (!readiness.branchDetailsComplete) {
    checklist.push({
      id: "branchInfo",
      done: false,
      labelKey: "branches.poFulfillment.checklist.branchInfo",
    });
  } else if (ready) {
    checklist.push({
      id: "branchInfo",
      done: true,
      labelKey: "branches.poFulfillment.checklist.branchInfoComplete",
    });
  }

  if (pickupEnabled) {
    checklist.push({
      id: "pickupProgress",
      done: readiness.pickupReady,
      labelKey: readiness.pickupReady
        ? "branches.poFulfillment.checklist.pickupReady"
        : "branches.poFulfillment.checklist.pickupIncomplete",
      progressLabel: progressText(
        readiness.pickupSectionsComplete,
        readiness.pickupSectionsTotal,
      ),
    });
  }

  if (deliveryEnabled) {
    checklist.push({
      id: "deliveryProgress",
      done: readiness.deliveryReady,
      labelKey: readiness.deliveryReady
        ? "branches.poFulfillment.checklist.deliveryReady"
        : "branches.poFulfillment.checklist.deliveryIncomplete",
      progressLabel: progressText(
        readiness.deliverySectionsComplete,
        readiness.deliverySectionsTotal,
      ),
    });
  }

  const methods: PoFulfillmentMethodLine[] = [
    {
      channel: "pickup",
      enabled: pickupEnabled,
      ready: readiness.pickupReady,
      complete: readiness.pickupSectionsComplete,
      total: readiness.pickupSectionsTotal,
    },
    {
      channel: "delivery",
      enabled: deliveryEnabled,
      ready: readiness.deliveryReady,
      complete: readiness.deliverySectionsComplete,
      total: readiness.deliverySectionsTotal,
    },
  ];

  return {
    ready,
    statusKey: ready
      ? "branches.poFulfillment.status.ready"
      : "branches.poFulfillment.status.setupRequired",
    ledeKey: ready
      ? "branches.poFulfillment.lede.ready"
      : "branches.poFulfillment.lede.setupRequired",
    checklist,
    methods,
    showConfigureAction: !ready,
  };
}

export function buildSupplierReadinessSummary(input: {
  branchId: string;
  fulfillmentReady: boolean;
  catalogOk: boolean | null;
  paymentsOk: boolean | null;
  contactOk: boolean | null;
}): SupplierReadinessSummaryItem[] {
  return [
    {
      key: "fulfillment",
      ok: input.fulfillmentReady,
      labelKey: "branches.poFulfillment.summary.fulfillment",
      href: branchFulfillmentEditPath(input.branchId, "overview"),
    },
    {
      key: "catalog",
      ok: input.catalogOk === true,
      labelKey: "branches.poFulfillment.summary.catalog",
      href: "/customers?kind=businesses",
    },
    {
      key: "payments",
      ok: input.paymentsOk === true,
      labelKey: "branches.poFulfillment.summary.payments",
      href: "/org/payment-methods",
    },
    {
      key: "contact",
      ok: input.contactOk === true,
      labelKey: "branches.poFulfillment.summary.contact",
      href: "/customers?kind=businesses",
    },
  ];
}
