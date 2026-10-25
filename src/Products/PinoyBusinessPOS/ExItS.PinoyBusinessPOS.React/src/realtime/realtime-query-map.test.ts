import { QueryClient } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { RealtimeEventTypes, type RealtimeEventEnvelope } from "@/realtime/realtime-event-types";
import { applyRealtimeEventToQueries } from "@/realtime/realtime-query-map";

function envelope(partial: Partial<RealtimeEventEnvelope> & Pick<RealtimeEventEnvelope, "eventType">): RealtimeEventEnvelope {
  return {
    eventId: partial.eventId ?? crypto.randomUUID(),
    eventType: partial.eventType,
    schemaVersion: 1,
    occurredAtUtc: new Date().toISOString(),
    resourceType: partial.resourceType ?? "organization",
    resourceId: partial.resourceId ?? "org",
    scopeType: partial.scopeType ?? "organization",
    scopeId: partial.scopeId ?? "org",
    data: partial.data ?? null,
  };
}

describe("applyRealtimeEventToQueries", () => {
  it("dedupes the same eventId within the window", () => {
    const client = new QueryClient();
    const spy = vi.spyOn(client, "invalidateQueries");
    const e = envelope({
      eventId: "same-id",
      eventType: RealtimeEventTypes.PersonalSettingsChanged,
    });
    applyRealtimeEventToQueries(client, e);
    applyRealtimeEventToQueries(client, e);
    expect(spy).toHaveBeenCalledTimes(2); // profile + account-settings once
  });

  it("invalidates storefront queries for storefront availability", () => {
    const client = new QueryClient();
    const spy = vi.spyOn(client, "invalidateQueries");
    applyRealtimeEventToQueries(
      client,
      envelope({
        eventType: RealtimeEventTypes.StorefrontAvailabilityChanged,
        data: { organizationId: "seller-1" },
      }),
    );
    expect(spy.mock.calls.length).toBeGreaterThan(0);
  });

  it("invalidates personal utang queries for ledger entry events", () => {
    const client = new QueryClient();
    const spy = vi.spyOn(client, "invalidateQueries");
    applyRealtimeEventToQueries(
      client,
      envelope({
        eventType: RealtimeEventTypes.PersonalUtangEntryChanged,
        resourceType: "personal-utang",
        resourceId: "entry-1",
        scopeType: "personal-user",
        scopeId: "user-1",
        data: { kind: "entry-recorded", relationshipId: "rel-1", shared: "1" },
      }),
    );
    expect(spy).toHaveBeenCalledWith({ queryKey: ["personal", "utang"] });
  });
});
