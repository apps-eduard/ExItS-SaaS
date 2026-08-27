import { beforeEach, describe, expect, it, vi } from "vitest";
import { evaluateColdStartOfflineGrant } from "@/offline/offline-operating-grant";
import { OFFLINE_OPERATING_GRANT_STORE_KEY } from "@/offline/offline-operating-grant";

vi.mock("@/workspace/browser-installation-identity", () => ({
  getDurableInstallationDeviceId: () => ({ ok: true, installationDeviceId: "install-personal-web" }),
  peekDurableInstallationDeviceId: () => "install-personal-web",
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

describe("Personal Web cold-start offline session", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("does not enter Personal offline operating session from a stored personal grant", async () => {
    const now = new Date("2026-08-27T12:00:00.000Z");
    const grant = {
      grantId: "grant-personal-1",
      schemaVersion: 4,
      userId: "user-personal-1",
      scopeKind: "Personal",
      organizationId: null,
      organizationDisplayName: "",
      branchId: null,
      branchName: null,
      installationDeviceId: "install-personal-web",
      posDeviceId: null,
      displayName: "Ana",
      username: "ana@example.com",
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
      installationDeviceId: "install-personal-web",
      now,
    });

    expect(result.ok).toBe(false);
  });

  it("allows Personal cold-start when engine opt-in is set", async () => {
    const now = new Date("2026-08-27T12:00:00.000Z");
    const grant = {
      grantId: "grant-personal-2",
      schemaVersion: 4,
      userId: "user-personal-2",
      scopeKind: "Personal",
      organizationId: null,
      organizationDisplayName: "",
      branchId: null,
      branchName: null,
      installationDeviceId: "install-personal-web",
      posDeviceId: null,
      roleCode: null,
      displayName: "Ana",
      username: "ana@example.com",
      issuedAtUtc: "2026-08-27T10:00:00.000Z",
      expiresAtUtc: "2026-08-28T12:00:00.000Z",
      lastOnlineValidatedAtUtc: "2026-08-27T11:00:00.000Z",
      signature: "sig",
    };

    localStorage.setItem(
      OFFLINE_OPERATING_GRANT_STORE_KEY,
      JSON.stringify({ version: 1, grants: { [grant.grantId]: grant } }),
    );

    const result = await evaluateColdStartOfflineGrant({
      installationDeviceId: "install-personal-web",
      now,
      allowPersonalOfflineEngine: true,
    });

    expect(result.ok).toBe(true);
  });
});
