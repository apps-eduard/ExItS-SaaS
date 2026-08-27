import type { LinkedMerchantDto } from "@/api/platform/linked-merchants-client";

function linkedAtMs(value: string): number {
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : Number.NEGATIVE_INFINITY;
}

/**
 * Personal "Your stores" is one store per organization.
 * Duplicate customer-links from the same merchant are collapsed to the newest link.
 */
export function selectCanonicalLinkedMerchantPerStore<T extends Pick<LinkedMerchantDto, "organizationId" | "linkedAtUtc">>(
  items: readonly T[],
): T[] {
  const byOrganization = new Map<string, T>();
  for (const item of items) {
    const organizationId = item.organizationId?.trim();
    if (!organizationId) {
      continue;
    }
    const existing = byOrganization.get(organizationId);
    if (!existing || linkedAtMs(item.linkedAtUtc) >= linkedAtMs(existing.linkedAtUtc)) {
      byOrganization.set(organizationId, item);
    }
  }
  return [...byOrganization.values()];
}
