import { beforeEach, describe, expect, it, vi } from "vitest";
import { ensurePersonalSessionProfile } from "@/session/ensure-personal-profile";

const listAccountProfiles = vi.hoisted(() => vi.fn());
const ensureAccountProfile = vi.hoisted(() => vi.fn());
const selectAccountProfile = vi.hoisted(() => vi.fn());

vi.mock("@/api/platform/platform-auth-client", () => ({
  listAccountProfiles,
  ensureAccountProfile,
  selectAccountProfile,
}));

describe("ensurePersonalSessionProfile", () => {
  beforeEach(() => {
    listAccountProfiles.mockReset();
    ensureAccountProfile.mockReset();
    selectAccountProfile.mockReset();
  });

  it("blocks organization-context-locked staff", async () => {
    const result = await ensurePersonalSessionProfile({
      session: {
        accountClass: "Organization",
        organizationContextLocked: true,
        homeOrganizationId: "org-1",
      },
      refreshSession: vi.fn(),
    });
    expect(result.ok).toBe(false);
    expect(listAccountProfiles).not.toHaveBeenCalled();
  });

  it("no-ops when already Personal", async () => {
    const session = { accountClass: "Personal" };
    const result = await ensurePersonalSessionProfile({
      session,
      refreshSession: vi.fn(),
    });
    expect(result.ok).toBe(true);
    expect(listAccountProfiles).not.toHaveBeenCalled();
  });

  it("selects Personal profile for Organization owners", async () => {
    listAccountProfiles.mockResolvedValueOnce({
      ok: true,
      profiles: [
        {
          id: "prof-personal",
          userIdentityId: "u1",
          accountClass: "Personal",
          allowedScope: "Personal",
          status: "Active",
        },
      ],
    });
    selectAccountProfile.mockResolvedValueOnce({
      ok: true,
      session: { accountClass: "Personal", sessionId: "s2" },
    });
    const refreshSession = vi.fn().mockResolvedValue("authenticated");

    const result = await ensurePersonalSessionProfile({
      session: {
        accountClass: "Organization",
        organizationContextLocked: false,
      },
      refreshSession,
    });

    expect(result.ok).toBe(true);
    expect(selectAccountProfile).toHaveBeenCalledWith("prof-personal");
    expect(refreshSession).toHaveBeenCalled();
  });
});
