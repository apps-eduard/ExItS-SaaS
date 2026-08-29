import { describe, expect, it } from "vitest";
import {
  buildExpiredWasteQuickFlowHref,
  isWasteLossReasonCode,
  parseWasteLossPrefillQuantity,
} from "@/features/inventory/expired-waste-quick-flow";

describe("expired-waste-quick-flow", () => {
  it("builds exact-lot write-off href with Expired reason and source", () => {
    const href = buildExpiredWasteQuickFlowHref({
      productId: "prod-1",
      lotId: "lot-1",
      quantity: 7,
      source: "expiration",
    });
    const url = new URL(href, "https://example.test");
    expect(url.pathname).toBe("/inventory/waste-loss/new");
    expect(url.searchParams.get("productId")).toBe("prod-1");
    expect(url.searchParams.get("lotId")).toBe("lot-1");
    expect(url.searchParams.get("reason")).toBe("Expired");
    expect(url.searchParams.get("source")).toBe("expiration");
    expect(url.searchParams.get("quantity")).toBe("7");
  });

  it("omits non-positive quantity from href", () => {
    const href = buildExpiredWasteQuickFlowHref({
      productId: "prod-1",
      lotId: "lot-1",
      quantity: 0,
    });
    expect(href).not.toContain("quantity=");
  });

  it("accepts only known waste-loss reason codes", () => {
    expect(isWasteLossReasonCode("Expired")).toBe(true);
    expect(isWasteLossReasonCode("Spoiled")).toBe(true);
    expect(isWasteLossReasonCode("NotAReason")).toBe(false);
    expect(isWasteLossReasonCode(null)).toBe(false);
  });

  it("parses positive prefill quantity only", () => {
    expect(parseWasteLossPrefillQuantity("7.5")).toBe(7.5);
    expect(parseWasteLossPrefillQuantity("0")).toBeNull();
    expect(parseWasteLossPrefillQuantity("abc")).toBeNull();
    expect(parseWasteLossPrefillQuantity(null)).toBeNull();
  });
});
