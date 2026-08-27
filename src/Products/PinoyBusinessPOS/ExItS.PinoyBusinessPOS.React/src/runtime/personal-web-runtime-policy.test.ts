import { describe, expect, it } from "vitest";
import {
  assertPersonalWebAllowsOfflineQueueing,
  guardPersonalWebOfflineEnqueue,
  personalWebAllowsOfflineBusinessMutations,
  personalWebAllowsOfflineBusinessReads,
  personalWebAllowsOfflineQueueing,
  personalWebAllowsOfflineSession,
  personalWebRuntimePolicy,
  PersonalWebOnlineOnlyError,
} from "@/runtime/personal-web-runtime-policy";
import { PERSONAL_WEB_LEGACY_PENDING_OUTBOX_POLICY } from "@/runtime/personal-web-legacy-outbox-policy";

describe("personalWebRuntimePolicy", () => {
  it("declares Personal Web/PWA as online-only", () => {
    expect(personalWebRuntimePolicy).toEqual({
      requiresOnlineSession: true,
      offlineSession: false,
      offlineBusinessReads: false,
      offlineBusinessMutations: false,
      offlineQueueing: false,
      offlineBackgroundSync: false,
    });
    expect(personalWebAllowsOfflineSession()).toBe(false);
    expect(personalWebAllowsOfflineQueueing()).toBe(false);
    expect(personalWebAllowsOfflineBusinessReads()).toBe(false);
    expect(personalWebAllowsOfflineBusinessMutations()).toBe(false);
  });

  it("rejects Personal Web offline enqueue without engine opt-in", () => {
    expect(() => assertPersonalWebAllowsOfflineQueueing()).toThrow(PersonalWebOnlineOnlyError);
    expect(() => guardPersonalWebOfflineEnqueue()).toThrow(PersonalWebOnlineOnlyError);
  });

  it("allows engine opt-in for preserved offline tests / future Capacitor", () => {
    expect(() => guardPersonalWebOfflineEnqueue({ allowOfflineEngine: true })).not.toThrow();
  });

  it("preserves legacy pending outbox drain policy", () => {
    expect(PERSONAL_WEB_LEGACY_PENDING_OUTBOX_POLICY).toBe("preserve-and-drain-when-online");
  });
});
