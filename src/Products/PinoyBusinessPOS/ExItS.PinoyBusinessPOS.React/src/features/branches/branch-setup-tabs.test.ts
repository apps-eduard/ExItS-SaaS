import { describe, expect, it } from "vitest";
import {
  branchFulfillmentBackPath,
  branchFulfillmentEditPath,
  branchSetupTabComplete,
  parseBranchSetupTab,
} from "@/features/branches/branch-setup-tabs";

const summary = {
  branchDetailsComplete: true,
  operatingHoursComplete: false,
  deliveryLocationComplete: true,
  deliveryPolicyComplete: false,
  deliveryAreasComplete: true,
  pickupSectionsComplete: 1,
  pickupSectionsTotal: 2,
  deliverySectionsComplete: 2,
  deliverySectionsTotal: 4,
};

describe("branch-setup-tabs", () => {
  it("parses known tab keys and falls back to overview", () => {
    expect(parseBranchSetupTab("location")).toBe("location");
    expect(parseBranchSetupTab("unknown")).toBe("overview");
    expect(parseBranchSetupTab(null)).toBe("overview");
  });

  it("builds edit paths with optional tab query", () => {
    expect(branchFulfillmentEditPath("abc")).toBe("/org/branches/abc/fulfillment");
    expect(branchFulfillmentEditPath("abc", "hours")).toBe("/org/branches/abc/fulfillment?tab=hours");
  });

  it("routes fulfillment back to management detail or list", () => {
    expect(branchFulfillmentBackPath("abc")).toBe("/org/branches/abc");
    expect(branchFulfillmentBackPath()).toBe("/org/branches");
    expect(branchFulfillmentBackPath(null)).toBe("/org/branches");
  });

  it("maps setup completion flags per tab", () => {
    expect(branchSetupTabComplete("overview", summary)).toBeNull();
    expect(branchSetupTabComplete("details", summary)).toBe(true);
    expect(branchSetupTabComplete("hours", summary)).toBe(false);
    expect(branchSetupTabComplete("location", summary)).toBe(true);
  });
});
