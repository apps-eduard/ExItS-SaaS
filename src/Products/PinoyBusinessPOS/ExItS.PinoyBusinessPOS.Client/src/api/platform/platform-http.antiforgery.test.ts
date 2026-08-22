import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import { AUTH_LOGIN_PATH } from "@/api/platform/browser-session";
import {
  clearPlatformAntiforgeryToken,
  isPlatformAntiforgeryValidationError,
  PlatformApiError,
  platformRequest,
} from "@/api/platform/platform-http";

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

  it("fails closed when antiforgery bootstrap returns 403", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return {
          ok: false,
          status: 403,
          json: async () => ({
            status: 403,
            errorCode: "application.auth.account_scope_denied",
            detail: "Account class 'Organization' is not allowed.",
          }),
        } as Response;
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
        return {
          ok: false,
          status: 404,
          json: async () => ({ status: 404, title: "Not Found" }),
        } as Response;
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
        return {
          ok: false,
          status: 500,
          json: async () => ({ status: 500, detail: "bootstrap failed" }),
        } as Response;
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
        return {
          ok: true,
          status: 200,
          json: async () => ({
            headerName: "X-XSRF-TOKEN",
            token: `csrf-${tokenBootstrapCount}`,
          }),
        } as Response;
      }
      if (url.endsWith("/api/v1/platform/auth/organization-context")) {
        orgContextAttempts += 1;
        if (orgContextAttempts === 1) {
          return {
            ok: false,
            status: 400,
            json: async () => ({
              status: 400,
              errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
              detail: "A valid browser antiforgery token is required for this request.",
            }),
          } as Response;
        }
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-2");
        return {
          ok: true,
          status: 200,
          json: async () => ({ selectedOrganizationId: "org-1" }),
        } as Response;
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
        return {
          ok: true,
          status: 200,
          json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        } as Response;
      }
      if (url.endsWith("/api/v1/platform/organizations/org-1/customer-link-requests")) {
        expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
        return {
          ok: false,
          status: 403,
          json: async () => ({
            status: 403,
            errorCode: "application.auth.account_scope_denied",
            detail: "Forbidden",
          }),
        } as Response;
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

  it("identifies canonical antiforgery validation errors", () => {
    const error = new PlatformApiError(400, {
      status: 400,
      errorCode: PlatformAntiforgeryDefaults.invalidErrorCode,
      detail: "A valid browser antiforgery token is required for this request.",
    });
    expect(isPlatformAntiforgeryValidationError(error)).toBe(true);
  });
});
