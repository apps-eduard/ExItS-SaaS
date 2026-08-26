import { afterEach, describe, expect, it, vi } from "vitest";
import { platformApiJson } from "@/api/platform-auth/browser-session";
import {
  clearPlatformAntiforgeryToken,
  peekPlatformAntiforgeryTokenForTests,
  PlatformAntiforgeryDefaults,
} from "@/api/platform-auth/platform-antiforgery";
import { logoutSession, setOrganizationContext } from "@/api/platform-auth/platform-auth-client";
import { jsonResponse } from "@/test/render";

describe("platform antiforgery (PWEB-20)", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    window.localStorage.clear();
    window.sessionStorage.clear();
  });

  it("bootstraps and attaches X-XSRF-TOKEN for cookie mutations only", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        expect(init?.method ?? "GET").toBe("GET");
        expect(init?.credentials).toBe("include");
        return jsonResponse(200, {
          headerName: PlatformAntiforgeryDefaults.headerName,
          token: "csrf-token",
        });
      }
      if (url.endsWith("/api/v1/platform/auth/organization-context")) {
        expect(init?.method).toBe("PUT");
        expect(new Headers(init?.headers).get(PlatformAntiforgeryDefaults.headerName)).toBe(
          "csrf-token",
        );
        return jsonResponse(200, null);
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(setOrganizationContext("11111111-1111-4111-8111-111111111111")).resolves.toEqual({
      ok: true,
    });

    expect(
      fetchMock.mock.calls.some(([url]) =>
        String(url).endsWith(`/platform-api${PlatformAntiforgeryDefaults.tokenPath}`),
      ),
    ).toBe(true);
    expect(peekPlatformAntiforgeryTokenForTests()).toBe("csrf-token");
    expect(window.localStorage.length).toBe(0);
    expect(window.sessionStorage.length).toBe(0);
  });

  it("does not bootstrap antiforgery for exempt login mutations", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      expect(url).toBe("/platform-api/api/v1/platform/auth/login");
      expect(new Headers(init?.headers).get(PlatformAntiforgeryDefaults.headerName)).toBeNull();
      return jsonResponse(200, { username: "olivia", sessionToken: "must-not-escape" });
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformApiJson("/api/v1/platform/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ usernameOrEmail: "olivia", password: "secret" }),
    });

    expect(fetchMock.mock.calls.some(([url]) => String(url).includes("antiforgery"))).toBe(false);
  });

  it("does not attach antiforgery on safe GET requests", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      expect(String(input)).toBe("/platform-api/api/v1/platform/auth/me");
      expect(new Headers(init?.headers).get(PlatformAntiforgeryDefaults.headerName)).toBeNull();
      return jsonResponse(200, { username: "olivia" });
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformApiJson("/api/v1/platform/auth/me");
    expect(fetchMock.mock.calls.some(([url]) => String(url).includes("antiforgery"))).toBe(false);
  });

  it("reuses in-memory token and clears it after logout", async () => {
    let tokenBootstraps = 0;
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        tokenBootstraps += 1;
        return jsonResponse(200, {
          headerName: PlatformAntiforgeryDefaults.headerName,
          token: "csrf-token",
        });
      }
      if (url.endsWith("/api/v1/platform/auth/logout")) {
        expect(new Headers(init?.headers).get(PlatformAntiforgeryDefaults.headerName)).toBe(
          "csrf-token",
        );
        return jsonResponse(204, null);
      }
      if (url.endsWith("/api/v1/platform/auth/organization-context")) {
        expect(new Headers(init?.headers).get(PlatformAntiforgeryDefaults.headerName)).toBe(
          "csrf-token",
        );
        return jsonResponse(200, null);
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await setOrganizationContext("11111111-1111-4111-8111-111111111111");
    await logoutSession();
    expect(peekPlatformAntiforgeryTokenForTests()).toBeNull();

    await setOrganizationContext("11111111-1111-4111-8111-111111111111");
    expect(tokenBootstraps).toBe(2);
  });

  it("never persists antiforgery token in web storage", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse(200, {
          headerName: PlatformAntiforgeryDefaults.headerName,
          token: "csrf-must-not-persist",
        });
      }
      return jsonResponse(204, null);
    });
    vi.stubGlobal("fetch", fetchMock);

    await logoutSession();
    expect(JSON.stringify({ ...window.localStorage })).not.toMatch(/csrf|xsrf|antiforgery/i);
    expect(JSON.stringify({ ...window.sessionStorage })).not.toMatch(/csrf|xsrf|antiforgery/i);
  });
});
