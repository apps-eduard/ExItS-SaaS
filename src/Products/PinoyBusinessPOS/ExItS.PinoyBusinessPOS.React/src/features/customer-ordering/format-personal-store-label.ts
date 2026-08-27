/**
 * Personal commerce shows store names, not Local Validation run stamps.
 * Compact datetime suffixes (YYYYMMDDHHmmss, optional milliseconds) stay in stored data.
 */
const COMPACT_RUN_STAMP =
  /\s+(20\d{2})(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])([01]\d|2[0-3])([0-5]\d)([0-5]\d)(\d{3})?$/;

export function stripPersonalRunStamp(value: string): string {
  return value.trim().replace(COMPACT_RUN_STAMP, "").replace(/\s+/g, " ").trim();
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
