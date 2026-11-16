import { afterEach, describe, expect, it, vi } from "vitest";
import { clearCachedSecurityToken } from "./platform-client";
import { preferAuthorizedPersonalProfile, switchToAuthorizedOrganizationProfile, switchToAuthorizedPersonalProfile } from "./profile-preference";

afterEach(() => {
  clearCachedSecurityToken();
  vi.unstubAllGlobals();
});

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status });
}

describe("account profile preference", () => {
  it("selects an existing Personal profile and discards the select token", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith("/antiforgery/token")) return json({ token: "csrf-1" });
      if (url.endsWith("/auth/me")) {
        const selected = fetchMock.mock.calls.some((call) => String(call[0]).endsWith("/account-profiles/select"));
        return json(selected
          ? { accountClass: "Personal", displayName: "Mica", organizationContextLocked: false }
          : { accountClass: "Organization", displayName: "Mica", organizationContextLocked: false });
      }
      if (url.endsWith("/account-profiles") && (!init?.method || init.method === "GET")) {
        return json([
          { id: "personal-1", accountClass: "Personal", status: "Active" },
          { id: "org-1", accountClass: "Organization", status: "Active" },
        ]);
      }
      expect(url).toContain("/account-profiles/select");
      expect(JSON.parse(String(init?.body))).toEqual({ accountProfileId: "personal-1" });
      return json({ sessionToken: "discard-me", accountClass: "Personal" });
    });
    vi.stubGlobal("fetch", fetchMock);

    const session = await preferAuthorizedPersonalProfile();
    expect(session?.accountClass).toBe("Personal");
    expect(JSON.stringify(window.localStorage)).not.toContain("discard-me");
  });

  it("leaves an Organization-only session on Organization", async () => {
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith("/auth/me")) return json({ accountClass: "Organization", organizationContextLocked: false });
      if (url.endsWith("/account-profiles")) return json([{ id: "org-1", accountClass: "Organization", status: "Active" }]);
      throw new Error(`unexpected ${url}`);
    }));

    const session = await preferAuthorizedPersonalProfile();
    expect(session?.accountClass).toBe("Organization");
  });

  it("does not move a locked Organization staff session", async () => {
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (!url.endsWith("/auth/me")) throw new Error(`unexpected ${url}`);
      return json({ accountClass: "Organization", organizationContextLocked: true });
    }));

    const session = await preferAuthorizedPersonalProfile();
    expect(session?.organizationContextLocked).toBe(true);
  });

  it("switches to an authorized Personal profile", async () => {
    let selected = false;
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith("/antiforgery/token")) return json({ token: "csrf-1" });
      if (url.endsWith("/auth/me")) {
        return json({ accountClass: selected ? "Personal" : "Organization", organizationContextLocked: false });
      }
      if (url.endsWith("/account-profiles") && (!init?.method || init.method === "GET")) {
        return json([{ id: "personal-1", accountClass: "Personal", status: "Active" }]);
      }
      selected = true;
      expect(JSON.parse(String(init?.body))).toEqual({ accountProfileId: "personal-1" });
      return json({ sessionToken: "discard-me" });
    }));

    await expect(switchToAuthorizedPersonalProfile()).resolves.toBe(true);
  });

  it("switches to an authorized Organization profile", async () => {
    let selected = false;
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith("/antiforgery/token")) return json({ token: "csrf-1" });
      if (url.endsWith("/auth/me")) {
        return json({ accountClass: selected ? "Organization" : "Personal", organizationContextLocked: false });
      }
      if (url.endsWith("/account-profiles") && (!init?.method || init.method === "GET")) {
        return json([{ id: "org-1", accountClass: "Organization", status: "Active" }]);
      }
      selected = true;
      expect(JSON.parse(String(init?.body))).toEqual({ accountProfileId: "org-1" });
      return json({ sessionToken: "discard-me" });
    }));

    await expect(switchToAuthorizedOrganizationProfile()).resolves.toBe(true);
  });
});
