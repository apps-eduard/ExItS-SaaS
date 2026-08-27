import { describe, expect, it, vi } from "vitest";
import { enqueueOfflineCashSale } from "@/offline/cash-sale-offline";
import { OrganizationWebOnlineOnlyError } from "@/runtime/organization-web-runtime-policy";

describe("Organization Web offline cash enqueue", () => {
  it("cannot enqueue a sale from the Web runtime path", async () => {
    await expect(
      enqueueOfflineCashSale({
        db: {} as never,
        scopeBinding: "scope",
        userId: "user",
        organizationId: "org",
        branchId: "branch",
        installationDeviceId: "install",
        posDeviceId: "pos",
        saleId: "11111111-1111-4111-8111-111111111111",
        shiftId: "22222222-2222-4222-8222-222222222222",
        lines: [],
        amountTendered: 0,
      }),
    ).rejects.toBeInstanceOf(OrganizationWebOnlineOnlyError);
  });
});

describe("service worker Organization mutation policy", () => {
  it("documents NetworkOnly API caching (no SW mutation replay)", async () => {
    const fs = await import("node:fs/promises");
    const path = await import("node:path");
    const configPath = path.resolve(process.cwd(), "vite.config.ts");
    const source = await fs.readFile(configPath, "utf8");
    expect(source).toContain('handler: "NetworkOnly"');
    expect(source).toMatch(/\/api\//);
    expect(source).not.toMatch(/BackgroundSyncPlugin/);
    expect(source).not.toMatch(/workbox-background-sync/);
  });
});

describe("401/403 vs offline classification", () => {
  it("does not treat HTTP auth failures as network loss", async () => {
    const { isLikelyNetworkFailure } = await import("@/connectivity/network-failure");
    expect(isLikelyNetworkFailure(new Error("Unauthorized"))).toBe(false);
    expect(isLikelyNetworkFailure(Object.assign(new Error("Forbidden"), { name: "Error" }))).toBe(
      false,
    );
    expect(isLikelyNetworkFailure(new TypeError("Failed to fetch"))).toBe(true);
  });
});

vi.mock("@/offline/outbox", () => ({
  enqueueEncryptedOperation: vi.fn(),
}));
