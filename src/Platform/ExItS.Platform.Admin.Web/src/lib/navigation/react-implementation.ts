import type { NavigationItemDefinition } from "@/lib/navigation/navigation-types";

export type ReactImplementationStatus = "IMPLEMENTED" | "UNDER_DEVELOPMENT";

const IMPLEMENTED_NAV_IDS = new Set(["PWEB-NAV-OVERVIEW"]);

export function reactImplementationStatus(
  item: NavigationItemDefinition,
): ReactImplementationStatus {
  return IMPLEMENTED_NAV_IDS.has(item.id) ? "IMPLEMENTED" : "UNDER_DEVELOPMENT";
}
