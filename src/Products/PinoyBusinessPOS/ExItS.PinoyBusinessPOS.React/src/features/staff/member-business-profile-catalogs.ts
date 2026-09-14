/**
 * Starter department labels for organization member business profiles.
 * Custom values are stored on membership rows and reused org-wide (not a separate table).
 */
export const DEFAULT_DEPARTMENTS = [
  "Management",
  "Administration",
  "Finance",
  "Sales",
  "Operations",
  "Purchasing",
  "Inventory / Warehouse",
  "Customer Service",
  "Human Resources",
  "Information Technology",
  "Other",
] as const;

/**
 * Starter position / job-title labels.
 * Distinct from RBAC OrganizationRole (Owner / Admin / Staff).
 */
export const DEFAULT_JOB_TITLES = [
  "Owner / Proprietor",
  "Manager",
  "Supervisor",
  "Staff",
  "Accountant",
  "Sales Staff",
  "Purchasing Staff",
  "Warehouse Staff",
  "Cashier",
  "Customer Service Staff",
  "IT Staff",
  "Other",
] as const;

/** Merge starter defaults with organization-scoped custom values (case-insensitive dedupe). */
export function mergeOrgScopedOptions(
  defaults: readonly string[],
  used: Iterable<string | null | undefined>,
): string[] {
  const seen = new Set<string>();
  const result: string[] = [];

  function add(raw: string | null | undefined) {
    const trimmed = raw?.trim();
    if (!trimmed) {
      return;
    }
    const key = trimmed.toLocaleLowerCase();
    if (seen.has(key)) {
      return;
    }
    seen.add(key);
    result.push(trimmed);
  }

  for (const value of defaults) {
    add(value);
  }
  for (const value of used) {
    add(value);
  }
  return result;
}
