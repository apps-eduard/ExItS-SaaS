import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { AUTH_LOGOUT_PATH } from "@/api/platform/browser-session";
import { markPendingRemoteLogout } from "@/session/pending-remote-logout";

const USER = "248935e9-e462-425f-88f5-a9255bf12748";

describe("reconnect after offline logout", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("does not auto-restore server session while pending remote logout remains", async () => {
    markPendingRemoteLogout();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = init?.method ?? "GET";
        if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-logout" });
        }
        if (url.endsWith(AUTH_LOGOUT_PATH) && method === "POST") {
          return jsonResponse(503, { detail: "logout unavailable" });
        }
        if (url.includes("/api/v1/platform/auth/me")) {
          return jsonResponse(200, {
              userId: USER,
              username: "kizy",
              displayName: "Kizy",
              accountClass: "Organization",
            });
        }
        throw new Error(`Unexpected fetch: ${url}`);
      }),
    );

    const { resolveBootstrapSessionForTests } = await import("@/session/SessionProvider");
    const resolved = await resolveBootstrapSessionForTests();
    expect(resolved.status).not.toBe("authenticated");
    expect(resolved.session).toBeNull();
  });
});
