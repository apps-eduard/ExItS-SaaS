import { stripLocalValidationRunStamp } from "@/lib/local-validation-run-stamp";

/**
 * Personal commerce shows store names, not Local Validation run stamps.
 * Compact datetime suffixes stay in stored data.
 */

export function stripPersonalRunStamp(value: string): string {
  return stripLocalValidationRunStamp(value);
}

export function personalStoreDisplayName(
  organizationDisplayName: string | null | undefined,
): string {
  const raw = organizationDisplayName?.trim() ?? "";
  if (!raw) {
    return "";
  }
  return stripPersonalRunStamp(raw) || raw;
}

function looksLikeGeneratedCustomerLabel(name: string): boolean {
  if (/\d{8,}/.test(name)) {
    return true;
  }
  if (/\blinked(\s+\d+)?$/i.test(name)) {
    return true;
  }
  if (/\s+\d{5,}$/.test(name)) {
    return true;
  }
  return false;
}

/** POS customer name for Personal UI, or null when it is a generated seed or the viewer's own name. */
export function personalCustomerRelationshipLabel(
  customerDisplayName: string | null | undefined,
  viewerDisplayName?: string | null,
): string | null {
  const raw = customerDisplayName?.trim() ?? "";
  if (!raw) {
    return null;
  }
  const cleaned = stripPersonalRunStamp(raw) || raw;
  if (looksLikeGeneratedCustomerLabel(cleaned)) {
    return null;
  }
  const viewer = viewerDisplayName?.trim() ?? "";
  if (viewer && cleaned.localeCompare(viewer, undefined, { sensitivity: "accent" }) === 0) {
    return null;
  }
  return cleaned;
}

export function personalStoreSearchText(
  organizationDisplayName: string,
  customerDisplayName: string,
): string {
  const display = personalStoreDisplayName(organizationDisplayName);
  const relationship = personalCustomerRelationshipLabel(customerDisplayName);
  return [organizationDisplayName, display, customerDisplayName, relationship ?? ""]
    .join(" ")
    .toLowerCase();
}
