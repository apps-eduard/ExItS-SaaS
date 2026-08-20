import { describe, expect, it } from "vitest";
import {
  canCreateSale,
  canEnterSellFloor,
  resolveEffectivePosRoleCode,
  resolveRoleHomeRoute,
} from "@/access/pos-capabilities";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

function grant(partial: Partial<SessionGrantResponse>): SessionGrantResponse {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    ...partial,
  };
}

describe("pos-capabilities", () => {
  it("denies sell floor for owner membership without POS role", () => {
    const ownerOnly = grant({
      productAccessAllowed: true,
      organizationManagementAuthority: true,
      membershipRole: "OrganizationOwner",
      mappedPosRoleCode: null,
      productLocalRoleCode: null,
    });

    expect(canEnterSellFloor(ownerOnly)).toBe(false);
    expect(canCreateSale(ownerOnly)).toBe(false);
    expect(resolveRoleHomeRoute(ownerOnly)).toBe("/org");
  });

  it("allows sell floor for cashier grant", () => {
    const cashier = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
    });

    expect(canEnterSellFloor(cashier)).toBe(true);
    expect(canCreateSale(cashier)).toBe(true);
    expect(resolveRoleHomeRoute(cashier)).toBe("/role/cashier");
  });

  it("maps manager product-local role to store manager home", () => {
    const manager = grant({
      productLocalRoleCode: "Manager",
    });

    expect(resolveEffectivePosRoleCode(manager)).toBe("StoreManager");
    expect(resolveRoleHomeRoute(manager)).toBe("/role/manager");
    expect(canEnterSellFloor(manager)).toBe(true);
  });

  it("denies sell floor when product access is not allowed", () => {
    const denied = grant({
      productAccessAllowed: false,
      mappedPosRoleCode: "Cashier",
    });

    expect(canEnterSellFloor(denied)).toBe(false);
    expect(resolveRoleHomeRoute(denied)).toBe("/");
  });
});
