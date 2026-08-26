import { beforeEach, describe, expect, it, vi } from "vitest";
import { ensureOrganizationSessionProfile } from "@/session/ensure-organization-profile";

const listAccountProfiles = vi.hoisted(() => vi.fn());
const ensureAccountProfile = vi.hoisted(() => vi.fn());
const selectAccountProfile = vi.hoisted(() => vi.fn());

vi.mock("@/api/platform/platform-auth-client", () => ({
  listAccountProfiles,
  ensureAccountProfile,
  selectAccountProfile,
}));

describe("ensureOrganizationSessionProfile", () => {
  beforeEach(() => {
    listAccountProfiles.mockReset();
    ensureAccountProfile.mockReset();
    selectAccountProfile.mockReset();
  });

  it("no-ops when session is already Organization", async () => {
    const session = { accountClass: "Organization", homeOrganizationId: "org-a" };
    const result = await ensureOrganizationSessionProfile({
      session,
      refreshSession: vi.fn(),
    });
    expect(result.ok).toBe(true);
    expect(listAccountProfiles).not.toHaveBeenCalled();
  });

  it("ensures and selects Organization profile for Personal session", async () => {
    listAccountProfiles.mockResolvedValueOnce({ ok: true, profiles: [] });
    ensureAccountProfile.mockResolvedValueOnce({
      ok: true,
      profile: {
        id: "prof-org",
        userIdentityId: "u1",
        accountClass: "Organization",
        allowedScope: "Organization",
        status: "Active",
      },
    });
    selectAccountProfile.mockResolvedValueOnce({
      ok: true,
      session: { accountClass: "Organization", sessionId: "s2" },
    });
    const refreshSession = vi.fn().mockResolvedValue("authenticated");

    const result = await ensureOrganizationSessionProfile({
      session: { accountClass: "Personal", email: "owner@example.com" },
      refreshSession,
    });

    expect(result.ok).toBe(true);
    expect(ensureAccountProfile).toHaveBeenCalledWith("Organization");
    expect(selectAccountProfile).toHaveBeenCalledWith("prof-org");
    expect(refreshSession).toHaveBeenCalled();
  });

  it("reuses an existing Active Organization profile without ensure", async () => {
    listAccountProfiles.mockResolvedValueOnce({
      ok: true,
      profiles: [
        {
          id: "existing",
          userIdentityId: "u1",
          accountClass: "Organization",
          allowedScope: "Organization",
          status: "Active",
        },
      ],
    });
    selectAccountProfile.mockResolvedValueOnce({
      ok: true,
      session: { accountClass: "Organization" },
    });

    const result = await ensureOrganizationSessionProfile({
      session: { accountClass: "Personal" },
      refreshSession: vi.fn(),
    });

    expect(result.ok).toBe(true);
    expect(ensureAccountProfile).not.toHaveBeenCalled();
    expect(selectAccountProfile).toHaveBeenCalledWith("existing");
  });
});
