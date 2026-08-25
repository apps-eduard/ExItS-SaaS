/** UX working experience — presentation only; never a security role mutation. */
export type WorkingExperience = "manage_business" | "operations" | "start_selling";

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
