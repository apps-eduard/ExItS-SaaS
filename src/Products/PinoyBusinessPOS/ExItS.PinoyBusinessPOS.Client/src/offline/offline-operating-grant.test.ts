import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  clearAllOfflineOperatingGrants,
  buildColdStartSessionGrantFacts,
  computeGrantIntegrity,
  establishOfflineOperatingGrant,
  evaluateColdStartOfflineGrant,
  isOrganizationOfflineGrant,
  OFFLINE_OPERATING_GRANT_STORE_KEY,
  synthesizeSessionFromGrant,
} from "@/offline/offline-operating-grant";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

const USER = "248935e9-e462-425f-88f5-a9255bf12748";
const ORG = "ca023f5b-925e-4aa5-a843-d48c4c06fa14";
const BRANCH = "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5";
const POS_DEVICE = "11111111-1111-1111-1111-111111111111";
const INSTALLATION = "22222222-2222-4222-8222-222222222222";

function seedInstallation(id: string = INSTALLATION): void {
  window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, id);
}

describe("offline operating grant", () => {
  beforeEach(() => {
    clearAllOfflineOperatingGrants();
    window.localStorage.clear();
    seedInstallation();
    vi.useRealTimers();
  });

  it("establishes an organization grant with integrity", async () => {
    const grant = await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
      roleCode: "Owner",
      displayName: "Kizy Uy",
      username: "kizy",
    });
    expect(grant).not.toBeNull();
    expect(isOrganizationOfflineGrant(grant!)).toBe(true);
    expect(grant!.integrity.length).toBeGreaterThan(10);
  });

  it("cold-start unlock restores grant-bound session when offline", async () => {
    await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
    });

    const evaluation = await evaluateColdStartOfflineGrant();
    expect(evaluation.ok).toBe(true);
    if (evaluation.ok) {
      const session = synthesizeSessionFromGrant(evaluation.grant);
      expect(session.userId).toBe(USER);
      expect(session.accountClass).toBe("Organization");
      expect(session.selectedOrganizationId).toBe(ORG);
    }
  });

  it("rejects tampered grant integrity", async () => {
    const grant = await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
    });
    expect(grant).not.toBeNull();
    const store = JSON.parse(
      window.localStorage.getItem(OFFLINE_OPERATING_GRANT_STORE_KEY) ?? "{}",
    ) as { grants: Record<string, { branchName: string; integrity: string }> };
    store.grants[USER].branchName = "Tampered Branch";
    window.localStorage.setItem(OFFLINE_OPERATING_GRANT_STORE_KEY, JSON.stringify(store));

    const evaluation = await evaluateColdStartOfflineGrant();
    expect(evaluation.ok).toBe(false);
    if (!evaluation.ok) {
      expect(evaluation.reason).toBe("integrity_failed");
    }
  });

  it("rejects expired grant", async () => {
    const now = new Date("2026-01-01T12:00:00.000Z");
    await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
      now,
    });

    const evaluation = await evaluateColdStartOfflineGrant({
      now: new Date("2026-02-01T12:00:00.000Z"),
    });
    expect(evaluation.ok).toBe(false);
    if (!evaluation.ok) {
      expect(evaluation.reason).toBe("no_grant");
    }
  });

  it("isolates grants by installation device", async () => {
    await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
    });

    seedInstallation("33333333-3333-4333-8333-333333333333");
    const evaluation = await evaluateColdStartOfflineGrant();
    expect(evaluation.ok).toBe(false);
  });

  it("does not store auth tokens in grant document", async () => {
    await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
    });
    const raw = window.localStorage.getItem(OFFLINE_OPERATING_GRANT_STORE_KEY) ?? "";
    expect(raw.toLowerCase()).not.toContain("bearertoken");
    expect(raw.toLowerCase()).not.toContain("accesstoken");
    expect(raw.toLowerCase()).not.toContain("sessiontoken");
  });

  it("recomputes integrity deterministically", async () => {
    const grant = await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
    });
    expect(grant).not.toBeNull();
    const { integrity, ...rest } = grant!;
    expect(await computeGrantIntegrity(rest)).toBe(integrity);
  });

  it("builds cold-start capability facts without bearer tokens", async () => {
    const grant = await establishOfflineOperatingGrant({
      userId: USER,
      scopeKind: "Organization",
      organizationId: ORG,
      organizationDisplayName: "Kizy Store",
      branchId: BRANCH,
      branchName: "Main Branch",
      installationDeviceId: INSTALLATION,
      posDeviceId: POS_DEVICE,
      roleCode: "Cashier",
    });
    expect(grant).not.toBeNull();
    const facts = buildColdStartSessionGrantFacts(grant!);
    expect(facts.accessToken).toBe("");
    expect(facts.productAccessAllowed).toBe(true);
    expect(facts.mappedPosRoleCode).toBe("Cashier");
  });
});
