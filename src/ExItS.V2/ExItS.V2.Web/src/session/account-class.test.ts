import { describe, expect, it } from "vitest";
import type { BrowserSessionSnapshot } from "@/api/auth";
import {
  canCallOrganizationPlatformApis,
  isBusinessHomeActivationAllowed,
  resolveHomeSurface,
  sessionAccountClass,
  V2_PERSONAL_ONLY_HOME,
} from "./account-class";

function session(
  partial: Partial<BrowserSessionSnapshot>,
): BrowserSessionSnapshot {
  return partial;
}

describe("V2 Personal-only Home routing", () => {
  it("keeps Personal-only gate enabled", () => {
    expect(V2_PERSONAL_ONLY_HOME).toBe(true);
  });

  it("opens Personal Home for a Personal-only account", () => {
    const snap = session({
      userId: "u1",
      accountClass: "Personal",
      organizationContextLocked: false,
      selectedOrganizationId: null,
    });
    expect(sessionAccountClass(snap)).toBe("Personal");
    expect(resolveHomeSurface(snap)).toBe("personal");
    expect(isBusinessHomeActivationAllowed(snap)).toBe(false);
    expect(canCallOrganizationPlatformApis(snap)).toBe(false);
  });

  it("keeps Personal Home when the Personal user owns businesses", () => {
    const snap = session({
      userId: "u2",
      accountClass: "Personal",
      organizationContextLocked: false,
      // Membership / selected org hint must not flip Home to Business.
      selectedOrganizationId: "11111111-1111-1111-1111-111111111111",
      selectedOrganizationDisplayName: "Demo Store",
    });
    expect(resolveHomeSurface(snap)).toBe("personal");
    expect(isBusinessHomeActivationAllowed(snap)).toBe(false);
    expect(canCallOrganizationPlatformApis(snap)).toBe(false);
  });

  it("defers Business Home until Personal profile is selected for unlocked Organization", () => {
    const snap = session({
      userId: "u3",
      accountClass: "Organization",
      selectedOrganizationId: "22222222-2222-2222-2222-222222222222",
      organizationContextLocked: false,
    });
    // Home surface stays deferred; SessionProvider switches via ensurePersonalSessionProfile.
    expect(resolveHomeSurface(snap)).toBe("deferred");
    expect(isBusinessHomeActivationAllowed(snap)).toBe(false);
    expect(canCallOrganizationPlatformApis(snap)).toBe(false);
  });

  it("defers Business Home for locked organization staff", () => {
    const snap = session({
      userId: "u4",
      accountClass: "Organization",
      homeOrganizationId: "33333333-3333-3333-3333-333333333333",
      organizationContextLocked: true,
    });
    expect(resolveHomeSurface(snap)).toBe("deferred");
    expect(isBusinessHomeActivationAllowed(snap)).toBe(false);
  });
});
