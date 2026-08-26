import type { FeatureOverride } from "@/api/organizations/entitlement-list-query";
import type { MessageKey } from "@/lib/i18n/messages";

export function overrideEffectiveStatus(
  item: FeatureOverride,
  now = Date.now(),
): "Active" | "Revoked" | "Expired" {
  if (item.status === "Revoked") {
    return "Revoked";
  }
  if (item.expiresAtUtc) {
    const expires = Date.parse(item.expiresAtUtc);
    if (!Number.isNaN(expires) && expires <= now) {
      return "Expired";
    }
  }
  return "Active";
}

export function grantSourceLabel(source: string | undefined, t: (key: MessageKey) => string): string {
  if (source === "Plan") {
    return t("organization.entitlements.grant.source.plan");
  }
  if (source === "Trial") {
    return t("organization.entitlements.grant.source.trial");
  }
  if (source === "Override") {
    return t("organization.entitlements.grant.source.override");
  }
  return source ?? "—";
}
