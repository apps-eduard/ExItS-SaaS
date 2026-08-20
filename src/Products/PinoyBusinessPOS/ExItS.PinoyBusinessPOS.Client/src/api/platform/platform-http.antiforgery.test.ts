import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { AUTH_LOGIN_PATH } from "@/api/platform/browser-session";
import { clearPlatformAntiforgeryToken, platformRequest } from "@/api/platform/platform-http";

describe("platformRequest antiforgery", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("bootstraps and attaches antiforgery header for mutations only", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        } as Response;
      }
      if (url.endsWith("/api/v1/platform/auth/logout")) {
        expect(init?.method).toBe("POST");
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
        return {
          ok: true,
          status: 204,
          json: async () => null,
        } as Response;
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformRequest<void>({
      method: "POST",
      path: "/api/v1/platform/auth/logout",
    });

    expect(
      fetchMock.mock.calls.some(([url]) =>
        String(url).endsWith(PlatformAntiforgeryDefaults.tokenPath),
      ),
    ).toBe(true);
  });

  it("skips antiforgery bootstrap for login", async () => {
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => {
      expect(init?.method).toBe("POST");
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBeNull();
      return {
        ok: true,
        status: 200,
        json: async () => ({ sessionId: "s1", username: "cashier" }),
      } as Response;
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformRequest({
      method: "POST",
      path: AUTH_LOGIN_PATH,
      body: { usernameOrEmail: "cashier", password: "secret" },
      skipAntiforgery: true,
    });

    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("clears in-memory antiforgery token explicitly", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        } as Response;
      }
      return {
        ok: true,
        status: 204,
        json: async () => null,
      } as Response;
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformRequest<void>({ method: "POST", path: "/api/v1/platform/auth/logout" });
    clearPlatformAntiforgeryToken();
    await platformRequest<void>({ method: "POST", path: "/api/v1/platform/auth/logout" });
    expect(
      fetchMock.mock.calls.filter(([url]) =>
        String(url).endsWith(PlatformAntiforgeryDefaults.tokenPath),
      ).length,
    ).toBe(2);
  });
});
