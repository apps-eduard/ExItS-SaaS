import { describe, expect, it, vi } from "vitest";
import {
  isAuthenticationLostFailure,
  maybeNotifyAuthenticationLost,
  notifyAuthenticationLost,
  resetAuthenticationLostLatch,
  setAuthenticationLostHandler,
  shouldSuppressAuthenticationLostForPath,
} from "@/session/session-expiry";
import { SESSION_EXPIRED_ERROR_CODE } from "@/api/platform/browser-session";

describe("session expiry", () => {
  it("treats session_expired and bare 401 as authentication lost", () => {
    expect(isAuthenticationLostFailure(401, SESSION_EXPIRED_ERROR_CODE)).toBe(true);
    expect(isAuthenticationLostFailure(401, "application.auth.session_invalid")).toBe(true);
    expect(isAuthenticationLostFailure(401, undefined)).toBe(true);
  });

  it("does not treat login_failed as authentication lost", () => {
    expect(isAuthenticationLostFailure(401, "application.auth.login_failed")).toBe(false);
  });

  it("treats unauthenticated development-operator 403 as authentication lost", () => {
    expect(
      isAuthenticationLostFailure(
        403,
        "platform.authorization.denied",
        "Actor 'development-operator:unauthenticated' does not hold permission 'platform.permission.manage_memberships'.",
      ),
    ).toBe(true);
  });

  it("does not treat authenticated permission denial 403 as authentication lost", () => {
    expect(
      isAuthenticationLostFailure(
        403,
        "platform.authorization.denied",
        "Actor 'user:abc' does not hold permission 'platform.permission.manage_memberships'.",
      ),
    ).toBe(false);
  });

  it("suppresses auth workflow paths", () => {
    expect(shouldSuppressAuthenticationLostForPath("/api/v1/platform/auth/login")).toBe(true);
    expect(shouldSuppressAuthenticationLostForPath("/api/v1/platform/customers")).toBe(false);
  });

  it("notifies handler once for concurrent auth-lost responses", () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    resetAuthenticationLostLatch();

    maybeNotifyAuthenticationLost({
      status: 401,
      errorCode: SESSION_EXPIRED_ERROR_CODE,
      path: "/api/v1/platform/personal/people",
    });
    maybeNotifyAuthenticationLost({
      status: 401,
      errorCode: SESSION_EXPIRED_ERROR_CODE,
      path: "/api/v1/platform/personal/todos",
    });

    expect(handler).toHaveBeenCalledTimes(1);
    setAuthenticationLostHandler(null);
  });

  it("does not notify for suppressed paths", () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    resetAuthenticationLostLatch();

    maybeNotifyAuthenticationLost({
      status: 401,
      errorCode: SESSION_EXPIRED_ERROR_CODE,
      path: "/api/v1/platform/auth/login",
    });

    expect(handler).not.toHaveBeenCalled();
    setAuthenticationLostHandler(null);
  });

  it("allows manual notify after latch reset", () => {
    const handler = vi.fn();
    setAuthenticationLostHandler(handler);
    resetAuthenticationLostLatch();
    notifyAuthenticationLost();
    resetAuthenticationLostLatch();
    notifyAuthenticationLost();
    expect(handler).toHaveBeenCalledTimes(2);
    setAuthenticationLostHandler(null);
  });
});
