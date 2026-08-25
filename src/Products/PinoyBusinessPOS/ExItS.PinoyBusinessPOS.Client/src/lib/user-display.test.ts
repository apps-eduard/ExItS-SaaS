import { describe, expect, it } from "vitest";
import {
  deriveUserInitials,
  resolveFriendlyPosRole,
  resolveUserDisplayName,
  resolveUserSecondaryIdentity,
} from "@/lib/user-display";

describe("user display helpers", () => {
  it("derives initials from a two-part display name", () => {
    expect(deriveUserInitials({ displayName: "Olivia Mendoza" })).toBe("OM");
  });

  it("falls back to username when display name is missing", () => {
    expect(deriveUserInitials({ username: "cashier" })).toBe("CA");
    expect(resolveUserDisplayName({ username: "cashier" })).toBe("cashier");
  });

  it("returns a secondary identity that differs from the display name", () => {
    expect(
      resolveUserSecondaryIdentity({
        displayName: "Olivia Mendoza",
        username: "olivia",
      }),
    ).toBe("olivia");
    expect(
      resolveUserSecondaryIdentity({
        displayName: "cashier",
        username: "cashier",
      }),
    ).toBeNull();
  });

  it("maps known POS roles and hides unknown codes", () => {
    expect(resolveFriendlyPosRole("Cashier")).toBe("cashier");
    expect(resolveFriendlyPosRole("StoreManager")).toBe("manager");
    expect(resolveFriendlyPosRole("OrganizationOwner")).toBeNull();
  });
});
