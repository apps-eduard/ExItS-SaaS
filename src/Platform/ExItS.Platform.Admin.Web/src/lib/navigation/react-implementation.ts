import type { NavigationItemDefinition } from "@/lib/navigation/navigation-types";

export type ReactImplementationStatus = "IMPLEMENTED" | "UNDER_DEVELOPMENT";

const IMPLEMENTED_NAV_IDS = new Set([
  "PWEB-NAV-OVERVIEW",
  "PWEB-NAV-ALL-ORGANIZATIONS",
  "PWEB-NAV-BY-PRODUCT",
  "PWEB-NAV-ALL-ACCOUNTS",
  "PWEB-NAV-PLATFORM-STAFF",
  "PWEB-NAV-ORG-ACCOUNTS",
  "PWEB-NAV-PERSONAL-ACCOUNTS",
  "PWEB-NAV-NEEDS-REVIEW",
  "PWEB-NAV-PRODUCTS",
  "PWEB-NAV-PLANS",
  "PWEB-NAV-AUDIT-LOG",
  "PWEB-NAV-PRIVACY-COMPLIANCE",
  "PWEB-NAV-PLATFORM-HEALTH",
]);

export function reactImplementationStatus(
  item: NavigationItemDefinition,
): ReactImplementationStatus {
  return IMPLEMENTED_NAV_IDS.has(item.id) ? "IMPLEMENTED" : "UNDER_DEVELOPMENT";
}
