import { afterEach, describe, expect, it, vi } from "vitest";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import {
  isAuthenticationLostFailure,
  maybeNotifyAuthenticationLost,
  notifyAuthenticationLost,
  resetAuthenticationLostLatch,
  setAuthenticationLostHandler,
  shouldSuppressAuthenticationLostForPath,
} from "@/api/auth/session-expiry";
import {
  PlatformApiError,
  PlatformNetworkError,
  clearPlatformAntiforgeryToken,
  platformRequest,
} from "@/api/platform-http";
import { isSessionInvalidError } from "@/api/auth/auth-errors";
import { buildLoginPath, resolvePostLoginPath } from "@/lib/auth/safe-return-path";

describe("session expiry detection", () => {
  it("treats 401 and authoritative session codes as authentication lost", () => {
    expect(isAuthenticationLostFailure(401, undefined)).toBe(true);
    expect(isAuthenticationLostFailure(401, AUTH_ERROR_CODES.sessionExpired)).toBe(true);
    expect(isAuthenticationLostFailure(401, AUTH_ERROR_CODES.sessionInvalid)).toBe(true);
  });

  it("does not treat 403, network-class, 5xx, or CSRF as authentication lost", () => {
    expect(isAuthenticationLostFailure(403, undefined)).toBe(false);
    expect(isAuthenticationLostFailure(500, undefined)).toBe(false);
    expect(isAuthenticationLostFailure(419, undefined)).toBe(false);
    expect(isAuthenticationLostFailure(400, "platform.antiforgery.invalid")).toBe(false);
    expect(isAuthenticationLostFailure(401, AUTH_ERROR_CODES.loginFailed)).toBe(false);
  });

  it("suppresses public auth workflow paths", () => {
    expect(shouldSuppressAuthenticationLostForPath("/api/v1/platform/auth/login")).toBe(true);
    expect(shouldSuppressAuthenticationLostForPath("/api/v1/platform/auth/register")).toBe(true);
    expect(shouldSuppressAuthenticationLostForPath("/api/v1/platform/subscriptions")).toBe(false);
  });
});

describe("session expiry latch", () => {
  afterEach(() => {
    setAuthenticationLostHandler(null);
    resetAuthenticationLostLatch();
  });

  it("coalesces simultaneous authentication-lost notifications into one handler call", () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);

    notifyAuthenticationLost();
    notifyAuthenticationLost();
    maybeNotifyAuthenticationLost({
      status: 401,
      errorCode: AUTH_ERROR_CODES.sessionExpired,
      path: "/api/v1/platform/subscriptions",
    });

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it("resets the latch so a later expiry can notify again", () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    notifyAuthenticationLost();
    resetAuthenticationLostLatch();
    notifyAuthenticationLost();
    expect(handler).toHaveBeenCalledTimes(2);
  });
});

describe("platformRequest session expiry", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    setAuthenticationLostHandler(null);
    resetAuthenticationLostLatch();
  });

  it("notifies on protected 401", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ status: 401, errorCode: AUTH_ERROR_CODES.sessionExpired }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/subscriptions" }),
    ).rejects.toBeInstanceOf(PlatformApiError);
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it("notifies on session_invalid", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/payments" }),
    ).rejects.toBeInstanceOf(PlatformApiError);
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it("does not notify on login 401", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ status: 401, errorCode: AUTH_ERROR_CODES.loginFailed }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", {
        method: "POST",
        path: "/api/v1/platform/auth/login",
        body: { usernameOrEmail: "a", password: "b" },
        skipAntiforgery: true,
      }),
    ).rejects.toBeInstanceOf(PlatformApiError);
    expect(handler).not.toHaveBeenCalled();
  });

  it("does not notify on 403", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 403,
        json: async () => ({ status: 403, title: "Forbidden" }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/subscriptions" }),
    ).rejects.toMatchObject({ status: 403 });
    expect(handler).not.toHaveBeenCalled();
  });

  it("does not notify on 500", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 500,
        json: async () => ({ status: 500, title: "Error" }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/subscriptions" }),
    ).rejects.toMatchObject({ status: 500 });
    expect(handler).not.toHaveBeenCalled();
  });

  it("does not notify on network failure", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    await expect(
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/subscriptions" }),
    ).rejects.toBeInstanceOf(PlatformNetworkError);
    expect(handler).not.toHaveBeenCalled();
  });

  it("coalesces simultaneous protected 401s", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ status: 401, errorCode: AUTH_ERROR_CODES.sessionExpired }),
      }),
    );

    const results = await Promise.allSettled([
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/subscriptions" }),
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/payments" }),
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/organizations" }),
    ]);

    expect(results.every((r) => r.status === "rejected")).toBe(true);
    expect(handler).toHaveBeenCalledTimes(1);
  });

  it("respects skipSessionExpiry", async () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({ status: 401, errorCode: AUTH_ERROR_CODES.sessionInvalid }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", {
        path: "/api/v1/platform/auth/me",
        skipSessionExpiry: true,
      }),
    ).rejects.toBeInstanceOf(PlatformApiError);
    expect(handler).not.toHaveBeenCalled();
  });
});

describe("return path after expiry", () => {
  it("preserves safe returnTo via return query and restores after login", () => {
    expect(
      buildLoginPath({
        returnPath: "/admin/subscriptions",
        notice: "session-expired",
      }),
    ).toBe("/admin/login?return=%2Fadmin%2Fsubscriptions&notice=session-expired");
    expect(resolvePostLoginPath("/admin/subscriptions")).toBe("/admin/subscriptions");
  });
});

describe("isSessionInvalidError", () => {
  it("recognizes session codes and plain 401", () => {
    expect(
      isSessionInvalidError(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.sessionExpired }),
      ),
    ).toBe(true);
    expect(
      isSessionInvalidError(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.sessionInvalid }),
      ),
    ).toBe(true);
    expect(isSessionInvalidError(new PlatformApiError(401, {}))).toBe(true);
  });

  it("does not treat login_failed or antiforgery as session invalid", () => {
    expect(
      isSessionInvalidError(new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.loginFailed })),
    ).toBe(false);
    expect(
      isSessionInvalidError(new PlatformApiError(400, { errorCode: "platform.antiforgery.invalid" })),
    ).toBe(false);
  });
});
