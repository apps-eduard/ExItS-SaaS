import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { AUTH_LOGOUT_PATH } from "@/api/platform/browser-session";
import { logoutSession } from "@/api/platform/platform-auth-client";
import { clearPlatformAntiforgeryToken, PlatformApiError } from "@/api/platform/platform-http";
import {
  clearPosAccessToken,
  getPosAccessToken,
  setPosAccessToken,
} from "@/api/platform/pos-access-token";
import {
  clearPosSessionGrant,
  getPosSessionGrant,
  setPosSessionGrant,
} from "@/api/platform/pos-session-grant";

describe("logoutSession", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
    clearPosSessionGrant();
  });

  it("POSTs logout with CSRF header and clears session artifacts", async () => {
    setPosAccessToken("in-memory-bearer");
    setPosSessionGrant({
      accessToken: "in-memory-bearer",
      productAccessAllowed: true,
      mappedPosRoleCode: "Cashier",
    });

    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-logout" }),
          text: async () => "",
        } as Response;
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        expect(init?.method).toBe("POST");
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-logout");
        return {
          ok: true,
          status: 204,
          json: async () => null,
          text: async () => "",
        } as Response;
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(logoutSession()).resolves.toBe("logged_out");
    expect(getPosAccessToken()).toBeNull();
    expect(getPosSessionGrant()).toBeNull();
  });

  it("treats 401 logout as already signed out and clears artifacts", async () => {
    setPosAccessToken("stale-bearer");
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf" }),
          text: async () => "",
        } as Response;
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        return {
          ok: false,
          status: 401,
          json: async () => ({ detail: "session invalid" }),
          text: async () => "",
        } as Response;
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(logoutSession()).resolves.toBe("already_signed_out");
    expect(getPosAccessToken()).toBeNull();
  });

  it("retries once after CSRF mismatch then succeeds", async () => {
    let logoutAttempts = 0;
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: `csrf-${logoutAttempts}` }),
          text: async () => "",
        } as Response;
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        logoutAttempts += 1;
        if (logoutAttempts === 1) {
          return {
            ok: false,
            status: 400,
            json: async () => ({ detail: "antiforgery" }),
            text: async () => "",
          } as Response;
        }
        return {
          ok: true,
          status: 204,
          json: async () => null,
          text: async () => "",
        } as Response;
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(logoutSession()).resolves.toBe("logged_out");
    expect(logoutAttempts).toBe(2);
  });

  it("throws when logout fails with a non-recoverable server error", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf" }),
          text: async () => "",
        } as Response;
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        return {
          ok: false,
          status: 500,
          json: async () => ({ detail: "logout unavailable" }),
          text: async () => "",
        } as Response;
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    setPosAccessToken("keep-on-failure");
    await expect(logoutSession()).rejects.toBeInstanceOf(PlatformApiError);
    expect(getPosAccessToken()).toBe("keep-on-failure");
  });
});
