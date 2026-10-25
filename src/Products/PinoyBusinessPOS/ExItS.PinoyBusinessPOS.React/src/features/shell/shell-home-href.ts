import { isWarehouseBranch } from "@/features/branches/branch-type";
import type { BoundWorkspace } from "@/workspace/types";
import { workingExperienceRoute } from "@/workspace/working-experience";

/** Personal home route (Personal shell / unbound). */
export const PERSONAL_HOME_HREF = "/personal";

/**
 * Top-bar Home destination:
 * - no bound workspace → Personal home
 * - warehouse branch → `/warehouse`
 * - otherwise → working-experience home (`/org`, `/role/manager`, `/sell`, …)
 */
export function shellHomeHref(boundWorkspace: BoundWorkspace | null | undefined): string {
  if (!boundWorkspace) {
    return PERSONAL_HOME_HREF;
  }

  if (isWarehouseBranch(boundWorkspace.branchType)) {
    return "/warehouse";
  }

  return workingExperienceRoute(boundWorkspace.experience);
}
