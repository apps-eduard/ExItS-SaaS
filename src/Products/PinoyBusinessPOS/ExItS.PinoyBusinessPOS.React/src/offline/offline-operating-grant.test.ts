import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  clearAllOfflineOperatingGrants,
  buildColdStartSessionGrantFacts,
  evaluateColdStartOfflineGrant,
  isOrganizationOfflineGrant,
  OFFLINE_OPERATING_GRANT_STORE_KEY,
  persistServerSignedGrant,
  synthesizeSessionFromGrant,
  type StoredOfflineOperatingGrant,
} from "@/offline/offline-operating-grant";
import {
  canonicalizeOfflineOperatingGrant,
  scopeKindToNumeric,
  signOfflineOperatingGrantForTests,
} from "@/offline/server-signed-offline-grant";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

const DEV_PRIVATE_KEY_PEM = `-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgJuN+Pa6hk6BZUISu
lodghNrUkSR+VQsjrIW49hJ21dihRANCAASSV3pYY5NEuiiPYCs/ZRXZL6dNW0DJ
8VhI3X4k2jMfgEoBV/n9zUzAIZMsJ6XfzAHR+cz3/VxgoQYquH3GV0Lt
-----END PRIVATE KEY-----`;

const USER = "248935e9-e462-425f-88f5-a9255bf12748";
const ORG = "ca023f5b-925e-4aa5-a843-d48c4c06fa14";
const BRANCH = "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5";
const POS_DEVICE = "11111111-1111-1111-1111-111111111111";
const INSTALLATION = "22222222-2222-4222-8222-222222222222";

function seedInstallation(id: string = INSTALLATION): void {
  window.localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, id);
}

async function buildSignedGrant(
  overrides: Partial<StoredOfflineOperatingGrant> = {},
): Promise<StoredOfflineOperatingGrant> {
  const grantId = overrides.grantId ?? "33333333-3333-4333-8333-333333333333";
  const base: StoredOfflineOperatingGrant = {
    grantId,
    schemaVersion: 4,
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
    issuedAtUtc: "2026-08-01T12:00:00.000Z",
    lastOnlineValidatedAtUtc: "2026-08-01T12:00:00.000Z",
    expiresAtUtc: "2030-08-01T12:00:00.000Z",
    signature: "",
    ...overrides,
  };
  const canonical = canonicalizeOfflineOperatingGrant({
    grantId: base.grantId,
    schemaVersion: base.schemaVersion,
    userId: base.userId,
    scopeKind: scopeKindToNumeric(base.scopeKind),
    organizationId: base.organizationId,
    organizationDisplayName: base.organizationDisplayName,
    branchId: base.branchId,
    branchName: base.branchName,
    installationDeviceId: base.installationDeviceId,
    posDeviceId: base.posDeviceId,
    roleCode: base.roleCode,
    displayName: base.displayName,
    username: base.username,
    issuedAtUtc: base.issuedAtUtc,
    lastOnlineValidatedAtUtc: base.lastOnlineValidatedAtUtc,
    expiresAtUtc: base.expiresAtUtc,
  });
  base.signature = await signOfflineOperatingGrantForTests(canonical, DEV_PRIVATE_KEY_PEM);
  return base;
}

describe("offline operating grant", () => {
  beforeEach(() => {
    clearAllOfflineOperatingGrants();
    window.localStorage.clear();
    seedInstallation();
    vi.useRealTimers();
  });

  it("persists a server-signed organization grant", async () => {
    const grant = await buildSignedGrant();
    expect(persistServerSignedGrant(grant)).toBe(true);
    expect(isOrganizationOfflineGrant(grant)).toBe(true);
    expect(grant.signature.length).toBeGreaterThan(10);
  });

  it("cold-start evaluation accepts a valid server grant", async () => {
    const grant = await buildSignedGrant();
    persistServerSignedGrant(grant);

    const evaluation = await evaluateColdStartOfflineGrant();
    expect(evaluation.ok).toBe(true);
    if (evaluation.ok) {
      const session = synthesizeSessionFromGrant(evaluation.grant);
      expect(session.userId).toBe(USER);
      expect(session.accountClass).toBe("Organization");
      expect(session.selectedOrganizationId).toBe(ORG);
    }
  });

  it("rejects tampered grant signature", async () => {
    const grant = await buildSignedGrant();
    persistServerSignedGrant(grant);
    const store = JSON.parse(
      window.localStorage.getItem(OFFLINE_OPERATING_GRANT_STORE_KEY) ?? "{}",
    ) as { grants: Record<string, { branchName: string }> };
    store.grants[USER].branchName = "Tampered Branch";
    window.localStorage.setItem(OFFLINE_OPERATING_GRANT_STORE_KEY, JSON.stringify(store));

    const evaluation = await evaluateColdStartOfflineGrant();
    expect(evaluation.ok).toBe(false);
    if (!evaluation.ok) {
      expect(evaluation.reason).toBe("signature_failed");
    }
  });

  it("rejects expired grant", async () => {
    const grant = await buildSignedGrant({
      issuedAtUtc: "2026-01-01T12:00:00.000Z",
      lastOnlineValidatedAtUtc: "2026-01-01T12:00:00.000Z",
      expiresAtUtc: "2026-01-02T12:00:00.000Z",
    });
    persistServerSignedGrant(grant);

    const evaluation = await evaluateColdStartOfflineGrant({
      now: new Date("2026-02-01T12:00:00.000Z"),
    });
    expect(evaluation.ok).toBe(false);
    if (!evaluation.ok) {
      expect(evaluation.reason).toBe("no_grant");
    }
  });

  it("rejects legacy v3 integrity grants", async () => {
    window.localStorage.setItem(
      OFFLINE_OPERATING_GRANT_STORE_KEY,
      JSON.stringify({
        version: 1,
        grants: {
          [USER]: {
            schemaVersion: 3,
            userId: USER,
            scopeKind: "Organization",
            organizationId: ORG,
            organizationDisplayName: "Kizy Store",
            branchId: BRANCH,
            branchName: "Main Branch",
            installationDeviceId: INSTALLATION,
            posDeviceId: POS_DEVICE,
            roleCode: "Owner",
            displayName: null,
            username: null,
            issuedAtUtc: "2026-01-01T12:00:00.000Z",
            lastOnlineValidatedAtUtc: "2026-01-01T12:00:00.000Z",
            expiresAtUtc: "2026-02-01T12:00:00.000Z",
            integrity: "deadbeef",
          },
        },
      }),
    );

    const evaluation = await evaluateColdStartOfflineGrant();
    expect(evaluation.ok).toBe(false);
    if (!evaluation.ok) {
      expect(evaluation.reason).toBe("unsupported_schema");
    }
  });

  it("does not store auth tokens in grant document", async () => {
    persistServerSignedGrant(await buildSignedGrant());
    const raw = window.localStorage.getItem(OFFLINE_OPERATING_GRANT_STORE_KEY) ?? "";
    expect(raw.toLowerCase()).not.toContain("bearertoken");
    expect(raw.toLowerCase()).not.toContain("accesstoken");
    expect(raw.toLowerCase()).not.toContain("sessiontoken");
  });

  it("builds cold-start capability facts without bearer tokens", async () => {
    const grant = await buildSignedGrant({ roleCode: "Cashier" });
    const facts = buildColdStartSessionGrantFacts(grant);
    expect(facts.accessToken).toBe("");
    expect(facts.productAccessAllowed).toBe(true);
    expect(facts.mappedPosRoleCode).toBe("Cashier");
  });
});

describe("client cannot mint server grants", () => {
  it("has no establishOfflineOperatingGrant export", async () => {
    const mod = await import("@/offline/offline-operating-grant");
    expect("establishOfflineOperatingGrant" in mod).toBe(false);
    expect("computeGrantIntegrity" in mod).toBe(false);
  });
});
