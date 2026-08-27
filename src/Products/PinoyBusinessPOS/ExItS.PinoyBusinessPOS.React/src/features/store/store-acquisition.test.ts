import { describe, expect, it } from "vitest";
import {
  buildBusinessQrAcquisitionPayload,
  buildPublicStoreAbsoluteUrl,
  buildPublicStorePath,
  isLegacyOrganizationQrPayload,
  normalizePublicOrganizationId,
} from "@/features/store/business-qr-url";
import {
  clearStoreAcquisitionIntent,
  isSafeAuthContinuePath,
  rememberStoreAcquisitionIntent,
  resolveAuthContinuePath,
  STORE_ACQUISITION_STORAGE_KEY,
} from "@/features/store/store-acquisition";

describe("business-qr-url", () => {
  it("builds HTTPS store path from PublicOrganizationId", () => {
    expect(normalizePublicOrganizationId("org123456")).toBe("ORG123456");
    expect(buildPublicStorePath("ORG123456")).toBe("/store/ORG123456");
    expect(buildPublicStoreAbsoluteUrl("ORG123456", "https://exits.example")).toBe(
      "https://exits.example/store/ORG123456",
    );
    expect(buildBusinessQrAcquisitionPayload("ORG123456", "https://app.exits")).toBe(
      "https://app.exits/store/ORG123456",
    );
  });

  it("rejects unsafe ids and recognizes legacy envelopes", () => {
    expect(normalizePublicOrganizationId("EX-1111-2222")).toBeNull();
    expect(normalizePublicOrganizationId("not-an-org")).toBeNull();
    expect(isLegacyOrganizationQrPayload("exits://qr/v1/organization/ORG123456")).toBe(true);
    expect(isLegacyOrganizationQrPayload("https://app/store/ORG123456")).toBe(false);
  });

  it("QR acquisition URL contains no secrets", () => {
    const url = buildBusinessQrAcquisitionPayload("ORG654321", "https://exits.app");
    expect(url).not.toMatch(/@/);
    expect(url).not.toMatch(/token/i);
    expect(url).not.toMatch(/email/i);
    expect(url).not.toMatch(/phone/i);
    expect(url).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i,
    );
    expect(url).toContain("/store/ORG654321");
  });
});

describe("store-acquisition continue safety", () => {
  it("allows only internal store/shop continue paths", () => {
    expect(isSafeAuthContinuePath("/store/ORG123456")).toBe(true);
    expect(isSafeAuthContinuePath("/personal/linked-merchants")).toBe(true);
    expect(
      isSafeAuthContinuePath(
        "/personal/linked-merchants/11111111-1111-4111-8111-111111111111/shop",
      ),
    ).toBe(true);
    expect(isSafeAuthContinuePath("https://evil.com")).toBe(false);
    expect(isSafeAuthContinuePath("//evil.com")).toBe(false);
    expect(isSafeAuthContinuePath("/\\evil.com")).toBe(false);
    expect(isSafeAuthContinuePath("javascript:alert(1)")).toBe(false);
    expect(isSafeAuthContinuePath("/store/EX-1111-2222")).toBe(false);
  });

  it("remembers acquisition intent and resolves continue", () => {
    sessionStorage.clear();
    rememberStoreAcquisitionIntent("ORG999888");
    expect(sessionStorage.getItem(STORE_ACQUISITION_STORAGE_KEY)).toContain("ORG999888");
    expect(resolveAuthContinuePath(null)).toBe("/store/ORG999888");
    expect(resolveAuthContinuePath("/store/ORG123456")).toBe("/store/ORG123456");
    expect(resolveAuthContinuePath("https://evil.com")).toBe("/store/ORG999888");
    clearStoreAcquisitionIntent();
    expect(resolveAuthContinuePath(null)).toBeNull();
  });
});
