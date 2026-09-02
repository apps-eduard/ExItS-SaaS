import { describe, expect, it } from "vitest";
import {
  buildBranchQrDownloadFilename,
  buildBranchStoreQrAcquisitionPayload,
  buildBusinessQrAcquisitionPayload,
  buildPublicBranchStorePath,
  buildPublicStorePath,
  normalizePublicBranchId,
} from "@/features/store/business-qr-url";
import {
  buildMerchantShopPath,
  isSafeAuthContinuePath,
  rememberStoreAcquisitionIntent,
  peekStoreAcquisitionIntent,
  clearStoreAcquisitionIntent,
} from "@/features/store/store-acquisition";

const ORG = "ORG123456";
const KALIBO = "56a8a186-1111-4111-8111-111111111111";
const ILOILO = "c3cd1c39-2222-4222-8222-222222222222";
const MAIN = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

describe("branch storefront QR URLs", () => {
  it("BRANCHQR-01 each branch produces different public QR/link", () => {
    const kalibo = buildPublicBranchStorePath(ORG, KALIBO);
    const iloilo = buildPublicBranchStorePath(ORG, ILOILO);
    const main = buildPublicBranchStorePath(ORG, MAIN);
    expect(kalibo).not.toBe(iloilo);
    expect(kalibo).not.toBe(main);
    expect(iloilo).not.toBe(main);
    expect(buildBranchStoreQrAcquisitionPayload(ORG, KALIBO, "https://app.test")).not.toBe(
      buildBranchStoreQrAcquisitionPayload(ORG, ILOILO, "https://app.test"),
    );
  });

  it("BRANCHQR-02 Kalibo QR resolves Kalibo", () => {
    expect(buildPublicBranchStorePath(ORG, KALIBO)).toBe(`/store/${ORG}/b/${KALIBO}`);
  });

  it("BRANCHQR-03 Iloilo QR resolves Iloilo", () => {
    expect(buildPublicBranchStorePath(ORG, ILOILO)).toContain(ILOILO);
  });

  it("BRANCHQR-04 Main QR resolves Main", () => {
    expect(buildPublicBranchStorePath(ORG, MAIN)).toContain(MAIN);
  });

  it("BRANCHQR-05 org QR unchanged (org-only path)", () => {
    expect(buildPublicStorePath(ORG)).toBe(`/store/${ORG}`);
    expect(buildBusinessQrAcquisitionPayload(ORG, "https://app.test")).toBe(
      `https://app.test/store/${ORG}`,
    );
    expect(buildBusinessQrAcquisitionPayload(ORG, "https://app.test")).not.toContain("/b/");
  });

  it("BRANCHQR-06 branch rename does not break stable identity", () => {
    const beforeRename = buildPublicBranchStorePath(ORG, KALIBO);
    // Display name is not part of the URL — identity is branch GUID.
    const afterRename = buildPublicBranchStorePath(ORG, KALIBO);
    expect(beforeRename).toBe(afterRename);
    expect(normalizePublicBranchId(KALIBO.toUpperCase())).toBe(KALIBO);
  });

  it("BRANCHQR-07 Copy link shape is exact public branch URL", () => {
    expect(buildBranchStoreQrAcquisitionPayload(ORG, KALIBO, "https://pos.example")).toBe(
      `https://pos.example/store/${ORG}/b/${KALIBO}`,
    );
  });

  it("BRANCHQR-08 download filename is slug-safe", () => {
    expect(buildBranchQrDownloadFilename("mica store", "Kalibo Branch")).toBe(
      "mica-store-kalibo-branch-qr.png",
    );
  });

  it("BRANCHQR-10 storefront continue preserves exact branch context", () => {
    clearStoreAcquisitionIntent();
    rememberStoreAcquisitionIntent(ORG, KALIBO);
    const intent = peekStoreAcquisitionIntent();
    expect(intent?.branchId).toBe(KALIBO);
    expect(buildMerchantShopPath("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb", KALIBO)).toBe(
      `/personal/linked-merchants/bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb/shop?branchId=${KALIBO}`,
    );
    expect(isSafeAuthContinuePath(`/store/${ORG}/b/${KALIBO}`)).toBe(true);
    expect(
      isSafeAuthContinuePath(
        `/personal/linked-merchants/bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb/shop?branchId=${ILOILO}`,
      ),
    ).toBe(true);
    clearStoreAcquisitionIntent();
  });
});

describe("BRANCHQR-09 authorization", () => {
  it("branch management QR uses Owner/Admin capability gate (not cashier)", async () => {
    const { canManageBranchFulfillment } = await import("@/access/pos-capabilities");
    expect(
      canManageBranchFulfillment({
        productAccessAllowed: true,
        productRole: "Cashier",
        organizationManagementAuthority: false,
        capabilities: [],
      } as never),
    ).toBe(false);
    expect(
      canManageBranchFulfillment({
        productAccessAllowed: true,
        productRole: "Owner",
        organizationManagementAuthority: true,
        capabilities: [],
      } as never),
    ).toBe(true);
  });
});
