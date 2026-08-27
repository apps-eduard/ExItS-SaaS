import { beforeEach, describe, expect, it, vi } from "vitest";
import { evaluateColdStartOfflineGrant } from "@/offline/offline-operating-grant";
import { OFFLINE_OPERATING_GRANT_STORE_KEY } from "@/offline/offline-operating-grant";

vi.mock("@/workspace/browser-installation-identity", () => ({
  getDurableInstallationDeviceId: () => ({ ok: true, installationDeviceId: "install-org-web" }),
  peekDurableInstallationDeviceId: () => "install-org-web",
}));

vi.mock("@/offline/server-signed-offline-grant", async () => {
  const actual = await vi.importActual<typeof import("@/offline/server-signed-offline-grant")>(
    "@/offline/server-signed-offline-grant",
  );
  return {
    ...actual,
    verifyOfflineOperatingGrantSignature: vi.fn(async () => true),
  };
});

describe("Organization Web cold-start offline session", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("does not enter Organization offline operating session from a stored org grant", async () => {
    const now = new Date("2026-08-27T12:00:00.000Z");
    const grant = {
      grantId: "grant-org-1",
      schemaVersion: 4,
      userId: "user-org-1",
      scopeKind: "Organization",
      organizationId: "org-1",
      organizationDisplayName: "Test Org",
      branchId: "branch-1",
      branchName: "Main",
      installationDeviceId: "install-org-web",
      posDeviceId: "pos-1",
      displayName: "Cashier",
      username: "cashier",
      issuedAtUtc: "2026-08-27T10:00:00.000Z",
      expiresAtUtc: "2026-08-28T12:00:00.000Z",
      lastOnlineValidatedAtUtc: "2026-08-27T11:00:00.000Z",
      signature: "sig",
      signingKeyId: "key-1",
    };

    localStorage.setItem(
      OFFLINE_OPERATING_GRANT_STORE_KEY,
      JSON.stringify({ version: 1, grants: { [grant.grantId]: grant } }),
    );

    const result = await evaluateColdStartOfflineGrant({
      installationDeviceId: "install-org-web",
      now,
    });

    expect(result.ok).toBe(false);
  });
});
