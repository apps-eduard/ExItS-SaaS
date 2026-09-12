/** UX working experience — presentation only; never a security role mutation. */
export type WorkingExperience = "manage_business" | "operations" | "start_selling";

/** Nav presentation workspace derived from WorkingExperience (not RBAC). */
export type OperationsNavWorkspace = "manager" | "cashier";

export function workingExperienceRoute(experience: WorkingExperience): string {
  switch (experience) {
    case "manage_business":
      return "/org";
    case "operations":
      return "/role/manager";
    case "start_selling":
      return "/sell";
    default:
      return "/workspace";
  }
}

export function isBranchRequiredExperience(experience: WorkingExperience): boolean {
  return experience === "operations" || experience === "start_selling";
}

/** Cashier-focused shell/home when bound experience is Start selling. */
export function isCashierWorkingExperience(
  experience: WorkingExperience | null | undefined,
): boolean {
  return experience === "start_selling";
}

export function resolveOperationsNavWorkspace(
  experience: WorkingExperience | null | undefined,
): OperationsNavWorkspace {
  return isCashierWorkingExperience(experience) ? "cashier" : "manager";
}
