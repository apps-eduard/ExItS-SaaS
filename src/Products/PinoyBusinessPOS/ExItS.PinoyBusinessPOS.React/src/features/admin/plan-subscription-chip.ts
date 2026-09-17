/** Visual tier for active subscription chips — keyed from catalog planKey, not entitlement. */
export type PlanSubscriptionChipVariant =
  | "starter"
  | "growth"
  | "pro"
  | "pro-plus"
  | "other";

/**
 * Resolve a distinct plan chip variant from catalog planKey / display name.
 * Display-only — never use for feature entitlement checks.
 */
export function resolvePlanSubscriptionChipVariant(
  planKey: string | null | undefined,
  planDisplayName?: string | null,
): PlanSubscriptionChipVariant {
  const candidates = [planKey, planDisplayName]
    .map((value) => (value ?? "").trim().toLowerCase().replace(/[\s_]+/g, "-"))
    .filter((value) => value.length > 0);

  for (const normalized of candidates) {
    if (
      normalized === "pro-plus" ||
      normalized === "pro+" ||
      normalized === "proplus" ||
      normalized.includes("pro-plus") ||
      normalized.includes("pro+")
    ) {
      return "pro-plus";
    }
    if (normalized === "pro") {
      return "pro";
    }
    if (normalized === "growth" || normalized.includes("growth")) {
      return "growth";
    }
    if (normalized === "starter" || normalized.includes("starter")) {
      return "starter";
    }
  }

  return "other";
}
