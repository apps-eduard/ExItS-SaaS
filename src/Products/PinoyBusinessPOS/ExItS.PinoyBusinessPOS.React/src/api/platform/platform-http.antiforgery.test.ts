import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { AUTH_LOGIN_PATH } from "@/api/platform/browser-session";
import {
  clearPlatformAntiforgeryToken,
  createCorrelationId,
  isPlatformAntiforgeryValidationError,
  PlatformApiError,
  platformRequest,
  prefetchPlatformAntiforgeryToken,
  refreshPlatformAntiforgeryToken,
} from "@/api/platform/platform-http";

describe("platformRequest antiforgery", () => {
  function jsonResponse(
    body: unknown,
    init: { ok?: boolean; status?: number } = {},
  ): Response {
    const text = body === undefined ? "" : JSON.stringify(body);
    return {
      ok: init.ok ?? true,
      status: init.status ?? 200,
      text: async () => text,
      json: async () => (text ? JSON.parse(text) : null),
    } as Response;
  }

  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("bootstraps and attaches antiforgery header for mutations only", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        expect(init?.credentials).toBe("include");
        return jsonResponse({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.endsWith("/api/v1/platform/auth/logout")) {
        expect(init?.method).toBe("POST");
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
        return jsonResponse(undefined, { status: 204 });
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
      return jsonResponse({ sessionId: "s1", username: "cashier" });
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
        return jsonResponse({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      return jsonResponse(undefined, { status: 204 });
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

  it("fails closed when antiforgery bootstrap returns 403", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse(
          {
            status: 403,
            errorCode: "application.auth.account_scope_denied",
            detail: "Account class 'Organization' is not allowed.",
          },
          { ok: false, status: 403 },
        );
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      platformRequest({
        method: "PUT",
        path: "/api/v1/platform/auth/organization-context",
        body: { organizationId: "org-1" },
      }),
    ).rejects.toMatchObject({ status: 403 });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("fails closed when antiforgery bootstrap returns 404", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse({ status: 404, title: "Not Found" }, { ok: false, status: 404 });
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      platformRequest<void>({ method: "POST", path: "/api/v1/platform/auth/logout" }),
    ).rejects.toMatchObject({ status: 404 });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("fails closed when antiforgery bootstrap returns 500", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse({ status: 500, detail: "bootstrap failed" }, { ok: false, status: 500 });
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      platformRequest<void>({ method: "POST", path: "/api/v1/platform/auth/logout" }),
    ).rejects.toMatchObject({ status: 500 });
  });

  it("retries once after stale antiforgery token then succeeds", async () => {
    let orgContextAttempts = 0;
    let tokenBootstrapCount = 0;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        tokenBootstrapCount += 1;
        return jsonResponse({
          headerName: "X-XSRF-TOKEN",
          token: `csrf-${tokenBootstrapCount}`,
        });
      }
      if (url.endsWith("/api/v1/platform/auth/organization-context")) {
        orgContextAttempts += 1;
        if (orgContextAttempts === 1) {
          return jsonResponse(
            {
              status: 400,
              errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
              detail: "A valid browser antiforgery token is required for this request.",
            },
            { ok: false, status: 400 },
          );
        }
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-2");
        return jsonResponse({ selectedOrganizationId: "org-1" });
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformRequest({
      method: "PUT",
      path: "/api/v1/platform/auth/organization-context",
      body: { organizationId: "org-1" },
    });

    expect(orgContextAttempts).toBe(2);
    expect(tokenBootstrapCount).toBe(2);
  });

  it("does not retry unrelated 403 permission denials", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.endsWith("/api/v1/platform/organizations/org-1/customer-link-requests")) {
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
        return jsonResponse(
          {
            status: 403,
            errorCode: "application.auth.account_scope_denied",
            detail: "Forbidden",
          },
          { ok: false, status: 403 },
        );
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(
      platformRequest({
        method: "POST",
        path: "/api/v1/platform/organizations/org-1/customer-link-requests",
        body: { businessCustomerId: "bc-1" },
      }),
    ).rejects.toMatchObject({ status: 403 });

    expect(
      fetchMock.mock.calls.filter(([url]) =>
        String(url).endsWith(PlatformAntiforgeryDefaults.tokenPath),
      ).length,
    ).toBe(1);
  });

  it("prefetch returns false when bootstrap is denied", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonResponse(
          {
            status: 403,
            errorCode: "application.auth.account_scope_denied",
            detail: "Account class 'Organization' is not allowed.",
          },
          { ok: false, status: 403 },
        );
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(prefetchPlatformAntiforgeryToken()).resolves.toBe(false);
  });

  it("refreshPlatformAntiforgeryToken re-bootstraps with credentials include", async () => {
    let bootstrapCount = 0;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        bootstrapCount += 1;
        expect(init?.credentials).toBe("include");
        return jsonResponse({
          headerName: "X-XSRF-TOKEN",
          token: `csrf-${bootstrapCount}`,
        });
      }
      throw new Error(`Unexpected fetch: ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    await prefetchPlatformAntiforgeryToken();
    await refreshPlatformAntiforgeryToken();
    expect(bootstrapCount).toBe(2);
  });

  it("identifies canonical antiforgery validation errors", () => {
    const error = new PlatformApiError(400, {
      status: 400,
      errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
      detail: "A valid browser antiforgery token is required for this request.",
    });
    expect(isPlatformAntiforgeryValidationError(error)).toBe(true);
  });

  it("treats 200 responses with empty bodies as undefined", async () => {
    const fetchMock = vi.fn(async () => jsonResponse(undefined, { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    const result = await platformRequest<null>({
      path: "/api/v1/platform/organizations/org-1/ownership-transfer/pending",
    });

    expect(result).toBeUndefined();
  });

  it("createCorrelationId falls back to getRandomValues when randomUUID is missing", () => {
    vi.stubGlobal("crypto", {
      getRandomValues: (bytes: Uint8Array) => {
        bytes.set(Array.from({ length: bytes.length }, (_, index) => index + 1));
        return bytes;
      },
    });

    const correlationId = createCorrelationId();
    expect(correlationId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i,
    );
  });
});
