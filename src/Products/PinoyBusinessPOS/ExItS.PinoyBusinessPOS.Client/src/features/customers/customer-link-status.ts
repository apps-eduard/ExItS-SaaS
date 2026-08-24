import type { MessageKey } from "@/i18n/messages";

/**
 * UI-facing customer-link status. Derived only from Platform
 * GET .../customers/{platformBusinessCustomerId}/link-status (or from absence
 * of PlatformBusinessCustomerId / fetch failure) — never from POS
 * linkedPersonalPublicUserId alone.
 */
export type CustomerLinkUiStatus =
  | "NotLinked"
  | "Pending"
  | "Linked"
  | "Declined"
  | "Expired"
  | "Revoked"
  | "Unavailable";

export function mapPlatformCustomerLinkStatus(raw: string | null | undefined): CustomerLinkUiStatus {
  const status = raw?.trim();
  if (!status) {
    return "Unavailable";
  }
  switch (status.toLowerCase()) {
    case "notlinked":
      return "NotLinked";
    case "pending":
      return "Pending";
    case "linked":
    case "active":
      return "Linked";
    case "declined":
      return "Declined";
    case "expired":
      return "Expired";
    case "revoked":
      return "Revoked";
    default:
      return "Unavailable";
  }
}

export function customerLinkStatusLabelKey(status: CustomerLinkUiStatus): MessageKey {
  switch (status) {
    case "NotLinked":
      return "customers.linkStatus.notLinked";
    case "Pending":
      return "customers.linkStatus.pending";
    case "Linked":
      return "customers.linkStatus.linked";
    case "Declined":
      return "customers.linkStatus.declined";
    case "Expired":
      return "customers.linkStatus.expired";
    case "Revoked":
      return "customers.linkStatus.revoked";
    case "Unavailable":
      return "customers.linkStatus.unavailable";
  }
}

export function customerLinkStatusTone(
  status: CustomerLinkUiStatus,
): "success" | "info" | "warning" | "danger" {
  switch (status) {
    case "Linked":
      return "success";
    case "Pending":
      return "info";
    case "Declined":
    case "Expired":
    case "Revoked":
      return "warning";
    case "Unavailable":
      return "danger";
    case "NotLinked":
    default:
      return "info";
  }
}

/**
 * MAUI parity: customer create stores `exits-id:EX-####-####` in notes.
 * Surface the Personal ExItS ID separately from Platform link status.
 */
export function extractPersonalExItsIdFromNotes(
  notes: string | null | undefined,
): { exItsId: string | null; notesWithoutExItsTag: string } {
  if (!notes?.trim()) {
    return { exItsId: null, notesWithoutExItsTag: "" };
  }
  const lines = notes.split(/\r?\n/);
  let exItsId: string | null = null;
  const kept: string[] = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.toLowerCase().startsWith("exits-id:")) {
      const id = trimmed.slice("exits-id:".length).trim();
      if (id) {
        exItsId = id;
      }
      continue;
    }
    kept.push(line);
  }
  return {
    exItsId,
    notesWithoutExItsTag: kept.join("\n").trim(),
  };
}

/**
 * Prefer the POS-local public user id field when present; otherwise notes tag.
 * Presence of an EX-ID does NOT prove Platform link status is Linked.
 */
export function resolveDisplayedPersonalExItsId(input: {
  linkedPersonalPublicUserId?: string | null;
  notes?: string | null;
}): string | null {
  const fromField = input.linkedPersonalPublicUserId?.trim();
  if (fromField) {
    return fromField;
  }
  return extractPersonalExItsIdFromNotes(input.notes).exItsId;
}
