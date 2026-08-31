import type { BranchFulfillmentSetupSummaryFields } from "@/api/platform/branch-fulfillment-client";
import type { MessageKey } from "@/i18n/messages";

export const BRANCH_SETUP_TABS = [
  "overview",
  "details",
  "hours",
  "location",
  "policy",
  "areas",
] as const;

export type BranchSetupTab = (typeof BRANCH_SETUP_TABS)[number];

export const BRANCH_SETUP_TAB_LABEL_KEYS: Record<BranchSetupTab, MessageKey> = {
  overview: "branches.tab.overview",
  details: "branches.tab.details",
  hours: "branches.tab.hours",
  location: "branches.tab.location",
  policy: "branches.tab.policy",
  areas: "branches.tab.areas",
};

export function parseBranchSetupTab(value: string | null | undefined): BranchSetupTab {
  if (value && BRANCH_SETUP_TABS.includes(value as BranchSetupTab)) {
    return value as BranchSetupTab;
  }
  return "overview";
}

export function branchFulfillmentEditPath(
  branchId: string,
  tab: BranchSetupTab = "overview",
): string {
  const base = `/org/branches/${branchId}`;
  if (tab === "overview") {
    return base;
  }
  return `${base}?tab=${tab}`;
}

/** Single-branch orgs skip the list and land on overview; multi-branch uses the list. */
export function branchFulfillmentBackPath(branchCount: number): string {
  return branchCount === 1 ? "/org" : "/org/branches";
}

export function branchSetupTabComplete(
  tab: BranchSetupTab,
  summary: BranchFulfillmentSetupSummaryFields,
): boolean | null {
  switch (tab) {
    case "overview":
      return null;
    case "details":
      return summary.branchDetailsComplete;
    case "hours":
      return summary.operatingHoursComplete;
    case "location":
      return summary.deliveryLocationComplete;
    case "policy":
      return summary.deliveryPolicyComplete;
    case "areas":
      return summary.deliveryAreasComplete;
  }
}
