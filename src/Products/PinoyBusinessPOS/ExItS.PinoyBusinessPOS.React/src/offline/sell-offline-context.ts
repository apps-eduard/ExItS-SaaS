import {
  useOrganizationOfflineContext,
  type OrganizationOfflineContext,
} from "@/offline/organization-offline-context";

/**
 * Sell view of the organization-scoped offline store (RMAP-21D).
 * The store itself is shared with every other Business surface, so a queued Cash sale and a
 * queued customer edit sit in one outbox and one Connection & Sync count.
 */
export type SellOfflineContext = OrganizationOfflineContext;

export function useSellOfflineContext(): SellOfflineContext | null {
  return useOrganizationOfflineContext();
}
