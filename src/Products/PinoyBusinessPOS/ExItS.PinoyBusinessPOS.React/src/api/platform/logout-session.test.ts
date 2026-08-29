import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
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
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-logout" });
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        expect(init?.method).toBe("POST");
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-logout");
        return jsonResponse(204, null);
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
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        return jsonResponse(401, { detail: "session invalid" });
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
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: `csrf-${logoutAttempts + 1}` });
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        logoutAttempts += 1;
        if (logoutAttempts === 1) {
          return jsonResponse(400, {
              errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
              detail: "antiforgery",
            });
        }
        return jsonResponse(204, null);
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(logoutSession()).resolves.toBe("logged_out");
    expect(logoutAttempts).toBe(2);
  });

  it("does not treat antiforgery-related 403 as already signed out", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        return jsonResponse(403, {
            errorCode: "application.auth.account_scope_denied",
            detail: "Forbidden",
          });
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    setPosAccessToken("keep-on-antiforgery-403");
    await expect(logoutSession()).rejects.toBeInstanceOf(PlatformApiError);
    expect(getPosAccessToken()).toBe("keep-on-antiforgery-403");
  });

  it("throws when logout fails with a non-recoverable server error", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      if (url.endsWith(AUTH_LOGOUT_PATH)) {
        return jsonResponse(500, { detail: "logout unavailable" });
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    setPosAccessToken("keep-on-failure");
    await expect(logoutSession()).rejects.toBeInstanceOf(PlatformApiError);
    expect(getPosAccessToken()).toBe("keep-on-failure");
  });
});
