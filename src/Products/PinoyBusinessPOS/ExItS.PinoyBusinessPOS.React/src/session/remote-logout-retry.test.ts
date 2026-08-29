import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { AUTH_LOGOUT_PATH } from "@/api/platform/browser-session";
import {
  clearPendingRemoteLogout,
  hasPendingRemoteLogout,
  markPendingRemoteLogout,
} from "@/session/pending-remote-logout";
import { completePendingRemoteLogoutIfNeeded } from "@/session/remote-logout-retry";

describe("remote logout retry", () => {
  beforeEach(() => {
    window.localStorage.clear();
    clearPendingRemoteLogout();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("completes pending remote logout when Platform logout succeeds", async () => {
    markPendingRemoteLogout();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-logout" });
        }
        if (url.endsWith(AUTH_LOGOUT_PATH)) {
          expect(init?.method).toBe("POST");
          return jsonResponse(204, null);
        }
        throw new Error(`Unexpected fetch: ${url}`);
      }),
    );

    const cleared = await completePendingRemoteLogoutIfNeeded();
    expect(cleared).toBe(true);
    expect(hasPendingRemoteLogout()).toBe(false);
  });

  it("retains pending marker when Platform logout fails", async () => {
    markPendingRemoteLogout();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-logout" });
        }
        if (url.endsWith(AUTH_LOGOUT_PATH)) {
          return jsonResponse(503, { detail: "logout unavailable" });
        }
        throw new Error(`Unexpected fetch: ${url}`);
      }),
    );

    const cleared = await completePendingRemoteLogoutIfNeeded();
    expect(cleared).toBe(false);
    expect(hasPendingRemoteLogout()).toBe(true);
  });
});
