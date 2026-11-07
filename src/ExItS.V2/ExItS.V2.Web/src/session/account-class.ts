import type { BrowserSessionSnapshot } from "@/api/auth";

/** Wire values from Platform `AccountClass` — do not infer from email/username. */
export type AccountClassName = "Personal" | "Organization" | "Platform";

/**
 * V2 development gate: Personal Home only.
 * Business / POS Home code stays in the repo but must not activate.
 */
export const V2_PERSONAL_ONLY_HOME = true;

export function normalizeAccountClass(
  value: string | null | undefined,
): AccountClassName | null {
  if (!value) return null;
  const normalized = value.trim();
  if (normalized.localeCompare("Personal", undefined, { sensitivity: "accent" }) === 0) {
    return "Personal";
  }
  if (
    normalized.localeCompare("Organization", undefined, { sensitivity: "accent" }) === 0
  ) {
    return "Organization";
  }
  if (normalized.localeCompare("Platform", undefined, { sensitivity: "accent" }) === 0) {
    return "Platform";
  }
  return null;
}

export function sessionAccountClass(
  session: BrowserSessionSnapshot | null | undefined,
): AccountClassName | null {
  return normalizeAccountClass(session?.accountClass);
}

/** Staff principals are locked to HomeOrganizationId — never switch via Personal. */
export function isOrganizationContextLocked(
  session: BrowserSessionSnapshot | null | undefined,
): boolean {
  return session?.organizationContextLocked === true;
}

/**
 * Active Home surface for the current V2 build.
 * Ownership / membership alone never selects Business Home.
 * When `V2_PERSONAL_ONLY_HOME` is true, Business Home stays deferred.
 */
export function resolveHomeSurface(
  session: BrowserSessionSnapshot | null | undefined,
): "personal" | "business" | "deferred" | "none" {
  if (!session) return "none";

  const accountClass = sessionAccountClass(session);

  if (V2_PERSONAL_ONLY_HOME) {
    if (accountClass === "Personal" && !isOrganizationContextLocked(session)) {
      return "personal";
    }
    return "deferred";
  }

  if (isOrganizationContextLocked(session)) return "business";
  if (accountClass === "Personal") return "personal";
  if (accountClass === "Organization") return "business";
  return "none";
}

/** Organization Platform management APIs require an Organization account class. */
export function canCallOrganizationPlatformApis(
  session: BrowserSessionSnapshot | null | undefined,
): boolean {
  if (V2_PERSONAL_ONLY_HOME) return false;
  return sessionAccountClass(session) === "Organization";
}

/** Whether Business Home loaders may run for this session in the current V2 build. */
export function isBusinessHomeActivationAllowed(
  session: BrowserSessionSnapshot | null | undefined,
): boolean {
  if (V2_PERSONAL_ONLY_HOME) return false;
  return resolveHomeSurface(session) === "business";
}
