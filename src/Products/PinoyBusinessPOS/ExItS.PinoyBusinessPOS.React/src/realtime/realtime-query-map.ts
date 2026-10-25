import type { QueryClient } from "@tanstack/react-query";
import { RealtimeEventTypes, type RealtimeEventEnvelope } from "@/realtime/realtime-event-types";

const recentEventIds = new Map<string, number>();
const DEDUPE_MS = 5_000;

function shouldHandle(eventId: string): boolean {
  if (!eventId) return true;
  const now = Date.now();
  for (const [id, at] of recentEventIds) {
    if (now - at > DEDUPE_MS) recentEventIds.delete(id);
  }
  if (recentEventIds.has(eventId)) return false;
  recentEventIds.set(eventId, now);
  return true;
}

/**
 * Targeted TanStack Query invalidation for Platform realtime envelopes.
 * Does not wipe the whole cache. Callers that hold unsaved form drafts should
 * gate destructive invalidation with useRealtimeDraftGuard.
 */
export function applyRealtimeEventToQueries(
  queryClient: QueryClient,
  envelope: RealtimeEventEnvelope,
  options?: { skipPersonalProfile?: boolean; skipOrgSettings?: boolean },
): void {
  if (!shouldHandle(envelope.eventId)) return;

  const orgId =
    envelope.data?.organizationId
    ?? (envelope.scopeType === "organization" ? envelope.scopeId : null)
    ?? null;

  switch (envelope.eventType) {
    case RealtimeEventTypes.PersonalSettingsChanged:
    case RealtimeEventTypes.PersonalProfilePhotoChanged:
      if (options?.skipPersonalProfile) return;
      void queryClient.invalidateQueries({ queryKey: ["personal", "profile"] });
      void queryClient.invalidateQueries({ queryKey: ["personal", "account-settings"] });
      if (envelope.data?.kind === "shared-utang-preference") {
        void queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
      }
      break;

    case RealtimeEventTypes.PersonalUtangEntryChanged:
    case RealtimeEventTypes.PersonalUtangInvitationChanged:
      void queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
      break;

    case RealtimeEventTypes.PersonalNotificationChanged:
      void queryClient.invalidateQueries({ queryKey: ["personal", "notifications"] });
      void queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
      break;

    case RealtimeEventTypes.OrganizationSettingsChanged:
      if (options?.skipOrgSettings) return;
      void queryClient.invalidateQueries({
        predicate: (q) =>
          Array.isArray(q.queryKey)
          && (q.queryKey.includes("organization") || q.queryKey.includes("org-profile"))
          && (orgId == null || q.queryKey.includes(orgId)),
      });
      break;

    case RealtimeEventTypes.BranchOperatingHoursChanged:
    case RealtimeEventTypes.BranchFulfillmentSettingsChanged:
    case RealtimeEventTypes.BranchSettingsChanged:
    case RealtimeEventTypes.BranchDeliveryPolicyChanged:
    case RealtimeEventTypes.BranchDeliveryAreasChanged:
    case RealtimeEventTypes.StorefrontAvailabilityChanged:
      if (options?.skipOrgSettings) return;
      void queryClient.invalidateQueries({
        predicate: (q) =>
          Array.isArray(q.queryKey)
          && (q.queryKey[0] === "branch-fulfillment"
            || q.queryKey[0] === "branch-hours"
            || q.queryKey[0] === "fulfillment-readiness"
            || (typeof q.queryKey[0] === "string" && q.queryKey[0].includes("fulfillment"))),
      });
      if (orgId) {
        void queryClient.invalidateQueries({
          predicate: (q) =>
            Array.isArray(q.queryKey)
            && q.queryKey[0] === "storefront"
            && q.queryKey.includes(orgId),
        });
        void queryClient.invalidateQueries({
          predicate: (q) =>
            Array.isArray(q.queryKey)
            && q.queryKey[0] === "personal"
            && q.queryKey[1] === "merchant-ordering-probe"
            && q.queryKey.includes(orgId),
        });
      }
      break;

    case RealtimeEventTypes.MembershipAccessChanged:
      void queryClient.invalidateQueries({ queryKey: ["session"] });
      void queryClient.invalidateQueries({ queryKey: ["workspaces"] });
      void queryClient.invalidateQueries({ queryKey: ["pos-capabilities"] });
      break;

    default:
      break;
  }
}
