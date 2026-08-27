import { describe, expect, it } from "vitest";
import {
  assertOrganizationWebAllowsOfflineQueueing,
  organizationWebAllowsOfflineBusinessMutations,
  organizationWebAllowsOfflineBusinessReads,
  organizationWebAllowsOfflineQueueing,
  organizationWebAllowsOfflineSession,
  organizationWebRuntimePolicy,
  OrganizationWebOnlineOnlyError,
  guardOrganizationWebOfflineEnqueue,
} from "@/runtime/organization-web-runtime-policy";

describe("organizationWebRuntimePolicy", () => {
  it("declares Organization Web/PWA as online-only", () => {
    expect(organizationWebRuntimePolicy).toEqual({
      requiresOnlineSession: true,
      offlineSession: false,
      offlineBusinessReads: false,
      offlineBusinessMutations: false,
      offlineTransactions: false,
      offlineQueueing: false,
      offlineBackgroundSync: false,
    });
    expect(organizationWebAllowsOfflineSession()).toBe(false);
    expect(organizationWebAllowsOfflineQueueing()).toBe(false);
    expect(organizationWebAllowsOfflineBusinessReads()).toBe(false);
    expect(organizationWebAllowsOfflineBusinessMutations()).toBe(false);
  });

  it("rejects Organization Web offline enqueue without engine opt-in", () => {
    expect(() => assertOrganizationWebAllowsOfflineQueueing()).toThrow(
      OrganizationWebOnlineOnlyError,
    );
    expect(() => guardOrganizationWebOfflineEnqueue()).toThrow(OrganizationWebOnlineOnlyError);
  });

  it("allows engine opt-in for preserved offline tests / future Capacitor", () => {
    expect(() => guardOrganizationWebOfflineEnqueue({ allowOfflineEngine: true })).not.toThrow();
  });
});
