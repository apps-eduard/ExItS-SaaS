import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";

/** Wire values from Platform `AccountClass` — do not infer from email/username. */
export type AccountClassName = "Personal" | "Organization" | "Platform";

export function normalizeAccountClass(value: string | null | undefined): AccountClassName | null {
  if (!value) {
    return null;
  }
  const normalized = value.trim();
  if (normalized.localeCompare("Personal", undefined, { sensitivity: "accent" }) === 0) {
    return "Personal";
  }
  if (normalized.localeCompare("Organization", undefined, { sensitivity: "accent" }) === 0) {
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

/** Staff principals are locked to HomeOrganizationId — never switch via LinkedPersonalUserId. */
export function isOrganizationContextLocked(
  session: BrowserSessionSnapshot | null | undefined,
): boolean {
  return session?.organizationContextLocked === true;
}

/**
 * Looks like an org-scoped staff login (`local@ORG######`).
 * Display/hint only — never used for authorization or AccountClass inference.
 */
export function looksLikeOrgScopedStaffLogin(usernameOrEmail: string): boolean {
  const trimmed = usernameOrEmail.trim();
  return /^[^@\s]+@ORG\d{6}$/i.test(trimmed);
}
