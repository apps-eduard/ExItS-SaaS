import { POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import {
  affiliationCanOpen,
  affiliationNeedsPlan,
  type PersonalProductAffiliationDto,
} from "@/api/platform/product-affiliations-client";

/**
 * UI-only start/open capability. Product identity still comes from the affiliation API.
 * Only PinoyBusinessPOS has a personal onboarding flow and an organization app route.
 */
export type ProductPortfolioCapability = {
  canStartBusiness: boolean;
  canOpenProduct: boolean;
  canManageSubscription: boolean;
  canChoosePlan: boolean;
  startRoute: string | null;
  choosePlanRoute: string | null;
  manageRoute: string | null;
  availability: "ready" | "coming-soon";
};

const POS_START_ROUTE = "/personal/explore-pos";
const POS_MANAGE_ROUTE = "/org/subscription";

export function isProductCommerciallyReady(productCode: string): boolean {
  return productCode === POS_PRODUCT_CODE;
}

export function resolveProductPortfolioCapability(
  row: PersonalProductAffiliationDto,
): ProductPortfolioCapability {
  const ready = isProductCommerciallyReady(row.productCode);
  const hasOrganization = Boolean(row.organizationId);
  const canOpen = ready && hasOrganization && affiliationCanOpen(row.subscriptionStatus);
  const needsPlan = affiliationNeedsPlan(row.subscriptionStatus, hasOrganization);

  return {
    canStartBusiness: ready && !hasOrganization,
    canOpenProduct: canOpen,
    canManageSubscription: ready && Boolean(row.canManageBilling) && canOpen,
    canChoosePlan: ready && Boolean(row.canManageBilling) && needsPlan,
    startRoute: ready ? POS_START_ROUTE : null,
    choosePlanRoute: ready ? POS_START_ROUTE : null,
    manageRoute: ready && hasOrganization ? POS_MANAGE_ROUTE : null,
    availability: ready ? "ready" : "coming-soon",
  };
}

export function businessMetaLine(row: PersonalProductAffiliationDto): string {
  return [row.roleDisplay, row.planDisplayName, row.subscriptionStatus].filter(Boolean).join(" · ");
}

export function affiliatedBusinesses(
  rows: PersonalProductAffiliationDto[],
): PersonalProductAffiliationDto[] {
  const seen = new Set<string>();
  const affiliated: PersonalProductAffiliationDto[] = [];
  for (const row of rows) {
    if (!row.organizationId || seen.has(row.productCode)) {
      continue;
    }
    seen.add(row.productCode);
    affiliated.push(row);
  }
  return affiliated;
}
