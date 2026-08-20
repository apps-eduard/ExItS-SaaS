import type { MessageKey } from "@/lib/i18n/messages";

export type OrganizationSubscriptionStatusTone = "success" | "warning" | "danger" | "neutral";

const STATUS_LABEL_KEYS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Suspended: "dashboard.status.Suspended",
  Trialing: "dashboard.status.Trialing",
  PastDue: "dashboard.status.PastDue",
  GracePeriod: "dashboard.status.GracePeriod",
  Cancelled: "dashboard.status.Cancelled",
  Expired: "dashboard.status.Expired",
};

/** Defensive alias for legacy payloads; canonical backend spelling is Cancelled. */
const STATUS_ALIASES: Record<string, string> = {
  Canceled: "Cancelled",
};

export function normalizeOrganizationSubscriptionStatus(status: string): string {
  return STATUS_ALIASES[status] ?? status;
}

export function organizationSubscriptionStatusTone(
  status: string,
): OrganizationSubscriptionStatusTone {
  const normalized = normalizeOrganizationSubscriptionStatus(status);
  if (normalized === "Active") {
    return "success";
  }
  if (
    normalized === "Trialing" ||
    normalized === "GracePeriod" ||
    normalized === "PastDue" ||
    normalized === "Suspended"
  ) {
    return "warning";
  }
  if (normalized === "Cancelled" || normalized === "Expired") {
    return "danger";
  }
  return "neutral";
}

export function organizationSubscriptionStatusLabelKey(status: string): MessageKey | null {
  const normalized = normalizeOrganizationSubscriptionStatus(status);
  return STATUS_LABEL_KEYS[normalized] ?? null;
}

export function organizationSubscriptionStatusLabel(
  status: string,
  t: (key: MessageKey) => string,
): string {
  const labelKey = organizationSubscriptionStatusLabelKey(status);
  return labelKey ? t(labelKey) : status;
}
