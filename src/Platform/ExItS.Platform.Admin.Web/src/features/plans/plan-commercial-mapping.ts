import type { UpdatePlanCommercialBody } from "@/api/catalog/plan-mutations-client";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import type { PlanCommercialValues } from "@/features/plans/plan-commercial-schema";

export function planToCommercialValues(plan: CatalogPlan): PlanCommercialValues {
  return {
    displayName: plan.displayName,
    description: plan.description ?? "",
    monthlyPrice: plan.monthlyPrice ?? 0,
    annualPrice: plan.annualPrice ?? 0,
    currencyCode: plan.currencyCode ?? "PHP",
    maxBranches: plan.maxBranches ?? 0,
    maxActiveStaff: plan.maxActiveStaff ?? 0,
    maxActivePosDevices: plan.maxActivePosDevices ?? 0,
    maxActiveBusinessTypes: plan.maxActiveBusinessTypes ?? 0,
    customerCreditEnabled: plan.customerCreditEnabled ?? false,
    advancedReportsEnabled: plan.advancedReportsEnabled ?? false,
    exportEnabled: plan.exportEnabled ?? false,
    trialAllowed: plan.trialAllowed ?? false,
    defaultTrialDays: plan.defaultTrialDays ?? 0,
    sortOrder: plan.sortOrder ?? 0,
  };
}

export function commercialValuesToBody(
  values: PlanCommercialValues,
  expectedUpdatedAtUtc?: string,
): UpdatePlanCommercialBody {
  return {
    displayName: values.displayName,
    description: values.description?.trim() ? values.description.trim() : null,
    monthlyPrice: values.monthlyPrice,
    annualPrice: values.annualPrice,
    currencyCode: values.currencyCode.trim().toUpperCase(),
    maxBranches: values.maxBranches,
    maxActiveStaff: values.maxActiveStaff,
    maxActivePosDevices: values.maxActivePosDevices,
    maxActiveBusinessTypes: values.maxActiveBusinessTypes,
    customerCreditEnabled: values.customerCreditEnabled,
    advancedReportsEnabled: values.advancedReportsEnabled,
    exportEnabled: values.exportEnabled,
    trialAllowed: values.trialAllowed,
    defaultTrialDays: values.trialAllowed ? values.defaultTrialDays : 0,
    sortOrder: values.sortOrder,
    expectedUpdatedAtUtc: expectedUpdatedAtUtc ?? null,
  };
}

export function planLifecycleActions(status: string): {
  canActivate: boolean;
  canDeactivate: boolean;
  canRetire: boolean;
} {
  if (status === "Retired") {
    return { canActivate: false, canDeactivate: false, canRetire: false };
  }
  if (status === "Active") {
    return { canActivate: false, canDeactivate: true, canRetire: true };
  }
  return { canActivate: true, canDeactivate: false, canRetire: true };
}

export function nextDraftVersionNumber(
  versions: Array<{ versionNumber: number }> | undefined,
): number {
  if (!versions || versions.length === 0) {
    return 1;
  }
  return Math.max(...versions.map((item) => item.versionNumber)) + 1;
}

/** Version-grant feature codes surfaced for ordering/delivery truth in the editor. */
export const ORDERING_DELIVERY_FEATURE_CODES = [
  "store-customer-ordering",
  "store-delivery-orders",
] as const;
