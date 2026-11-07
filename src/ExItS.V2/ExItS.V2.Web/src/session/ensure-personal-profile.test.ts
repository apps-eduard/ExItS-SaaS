import { afterEach, describe, expect, it, vi } from "vitest";
import type { BrowserSessionSnapshot } from "@/api/auth";

const listAccountProfiles = vi.fn();
const ensureAccountProfile = vi.fn();
const selectAccountProfile = vi.fn();

vi.mock("@/api/auth", async () => {
  const actual = await vi.importActual<typeof import("@/api/auth")>("@/api/auth");
  return {
    ...actual,
    listAccountProfiles: (...args: unknown[]) => listAccountProfiles(...args),
    ensureAccountProfile: (...args: unknown[]) => ensureAccountProfile(...args),
    selectAccountProfile: (...args: unknown[]) => selectAccountProfile(...args),
  };
});

afterEach(() => {
  vi.clearAllMocks();
  vi.resetModules();
});

describe("ensurePersonalSessionProfile", () => {
  it("returns the session when already Personal", async () => {
    const { ensurePersonalSessionProfile } = await import(
      "./ensure-personal-profile"
    );
    const session: BrowserSessionSnapshot = {
      userId: "u1",
      accountClass: "Personal",
      organizationContextLocked: false,
    };
    const refreshSession = vi.fn();
    const result = await ensurePersonalSessionProfile({ session, refreshSession });
    expect(result.ok).toBe(true);
    expect(listAccountProfiles).not.toHaveBeenCalled();
    expect(refreshSession).not.toHaveBeenCalled();
  });

  it("refuses staff-locked Organization sessions", async () => {
    const { ensurePersonalSessionProfile, shouldEnsurePersonalHomeProfile } =
      await import("./ensure-personal-profile");
    const session: BrowserSessionSnapshot = {
      userId: "u2",
      accountClass: "Organization",
      organizationContextLocked: true,
      homeOrganizationId: "33333333-3333-3333-3333-333333333333",
    };
    expect(shouldEnsurePersonalHomeProfile(session)).toBe(false);
    const result = await ensurePersonalSessionProfile({
      session,
      refreshSession: vi.fn(),
    });
    expect(result.ok).toBe(false);
    expect(listAccountProfiles).not.toHaveBeenCalled();
  });

  it("selects an active Personal profile for unlocked Organization owners", async () => {
    const { ensurePersonalSessionProfile, shouldEnsurePersonalHomeProfile } =
      await import("./ensure-personal-profile");
    const organizationSession: BrowserSessionSnapshot = {
      userId: "u3",
      accountClass: "Organization",
      organizationContextLocked: false,
      selectedOrganizationId: "11111111-1111-1111-1111-111111111111",
    };
    const personalSession: BrowserSessionSnapshot = {
      userId: "u3",
      accountClass: "Personal",
      organizationContextLocked: false,
      selectedOrganizationId: null,
    };
    expect(shouldEnsurePersonalHomeProfile(organizationSession)).toBe(true);

    listAccountProfiles.mockResolvedValue({
      ok: true,
      profiles: [
        {
          id: "profile-personal",
          accountClass: "Personal",
          status: "Active",
        },
        {
          id: "profile-org",
          accountClass: "Organization",
          status: "Active",
        },
      ],
    });
    selectAccountProfile.mockResolvedValue({
      ok: true,
      session: personalSession,
    });
    const refreshSession = vi.fn().mockResolvedValue("authenticated");

    const result = await ensurePersonalSessionProfile({
      session: organizationSession,
      refreshSession,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.session.accountClass).toBe("Personal");
    }
    expect(selectAccountProfile).toHaveBeenCalledWith(
      "profile-personal",
      undefined,
    );
    expect(ensureAccountProfile).not.toHaveBeenCalled();
    expect(refreshSession).toHaveBeenCalled();
  });
});
