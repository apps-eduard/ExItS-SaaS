import { afterEach, describe, expect, it, vi } from "vitest";
import { clearCachedSecurityToken, loginWithPassword, registerPersonalAccount } from "./platform-client";

afterEach(() => {
  clearCachedSecurityToken();
  vi.unstubAllGlobals();
});

describe("platform session client", () => {
  it("signs in with the cookie and antiforgery header, and does not store the session token", async () => {
    const setItem = vi.spyOn(Storage.prototype, "setItem");
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith("/antiforgery/token")) {
        return new Response(JSON.stringify({ headerName: "X-XSRF-TOKEN", token: "csrf-1" }), { status: 200 });
      }
      expect(init?.credentials).toBe("include");
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-1");
      const body = JSON.parse(String(init?.body));
      expect(body).toEqual({ usernameOrEmail: "ada@example.com", password: "ledger-1947" });
      return new Response(JSON.stringify({ sessionToken: "discard-me", displayName: "Ada" }), { status: 200 });
    });
    vi.stubGlobal("fetch", fetchMock);

    await loginWithPassword("ada@example.com", "ledger-1947");
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(JSON.stringify(window.localStorage)).not.toContain("discard-me");
    expect(setItem).not.toHaveBeenCalled();
    setItem.mockRestore();
  });

  it("returns the server acknowledgement and surfaces a validation problem", async () => {
    vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
      if (String(input).endsWith("/antiforgery/token")) {
        return new Response(JSON.stringify({ token: "csrf-1" }), { status: 200 });
      }
      return new Response(JSON.stringify({ detail: "Display name is required.", errorCode: "Invalid" }), { status: 400 });
    }));

    await expect(registerPersonalAccount("Ada", "ada@example.com")).rejects.toThrow(/display name/i);
  });
});
