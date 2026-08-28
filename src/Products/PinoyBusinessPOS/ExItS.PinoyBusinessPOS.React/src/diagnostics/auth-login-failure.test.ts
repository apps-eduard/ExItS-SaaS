import { describe, expect, it } from "vitest";
import { AUTH_LOGIN_PATH } from "@/api/platform/browser-session";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  AUTH_LOGIN_FAILURE_STAGE,
  authLoginFailureToPosErrorReport,
  buildAuthLoginFailure,
  isHandledSignInFailure,
  isInvalidCredentialsFailure,
  resolveAuthLoginFailurePresentation,
} from "@/diagnostics/auth-login-failure";
import { formatPosErrorReport } from "@/diagnostics/pos-error-report";

const t = (key: string) => key;

describe("auth login failure diagnostics", () => {
  it("LOGIN_401_PRESERVES_STATUS", () => {
    const failure = buildAuthLoginFailure(
      new PlatformApiError(401, {
        errorCode: "application.auth.login_failed",
        detail: "Invalid username or password.",
        traceId: "trace-login-401",
      }, "corr-login-401"),
    );
    expect(failure.status).toBe(401);
    expect(failure.failureStage).toBe(AUTH_LOGIN_FAILURE_STAGE);
    expect(failure.path).toBe(AUTH_LOGIN_PATH);
  });

  it("LOGIN_ERROR_CODE_PRESERVED", () => {
    const failure = buildAuthLoginFailure(
      new PlatformApiError(403, {
        errorCode: "application.auth.account_scope_denied",
        detail: "Account scope denied.",
      }),
    );
    expect(failure.errorCode).toBe("application.auth.account_scope_denied");
  });

  it("LOGIN_TRACE_ID_PRESERVED", () => {
    const failure = buildAuthLoginFailure(
      new PlatformApiError(500, {
        errorCode: "application.internal_error",
        traceId: "trace-server-500",
      }),
    );
    expect(failure.traceId).toBe("trace-server-500");
  });

  it("LOGIN_CORRELATION_ID_PRESERVED", () => {
    const failure = buildAuthLoginFailure(
      new PlatformApiError(502, {
        errorCode: "application.upstream_error",
      }, "corr-upstream-502"),
    );
    expect(failure.requestCorrelationId).toBe("corr-upstream-502");
  });

  it("LOGIN_DETAIL_SANITIZED", () => {
    const failure = buildAuthLoginFailure(
      new PlatformApiError(401, {
        errorCode: "application.auth.login_failed",
        detail: "password=secret-token Authorization Bearer eyJabc.def.ghi failed",
      }),
    );
    expect(failure.detail).toContain("[REDACTED]");
    expect(failure.detail).not.toContain("secret-token");
    expect(failure.detail).not.toContain("eyJabc");
  });

  it("PASSWORD_REDACTED", () => {
    const report = formatPosErrorReport(
      authLoginFailureToPosErrorReport(
        buildAuthLoginFailure(new Error("password=super-secret failed")),
        "Sign in failed.",
      ),
    );
    expect(report).not.toContain("super-secret");
    expect(report).toContain("[REDACTED]");
  });

  it("COOKIES_REDACTED", () => {
    const report = formatPosErrorReport(
      authLoginFailureToPosErrorReport(
        buildAuthLoginFailure(
          new PlatformApiError(500, {
            detail: "Set-Cookie: .ExItS.Platform.Auth=abc123; X-XSRF-TOKEN=csrf-value",
          }),
        ),
        "Sign in failed.",
      ),
    );
    expect(report).not.toContain("abc123");
    expect(report).not.toContain("csrf-value");
    expect(report).toContain("[REDACTED]");
  });

  it("TOKENS_REDACTED", () => {
    const report = formatPosErrorReport(
      authLoginFailureToPosErrorReport(
        buildAuthLoginFailure(
          new PlatformApiError(500, {
            detail: "Authorization Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig",
          }),
        ),
        "Sign in failed.",
      ),
    );
    expect(report).not.toContain("eyJhbGciOiJIUzI1NiJ9");
    expect(report).toContain("[REDACTED]");
  });

  it("COPY_ERROR_DETAILS", () => {
    const report = formatPosErrorReport(
      authLoginFailureToPosErrorReport(
        buildAuthLoginFailure(
          new PlatformApiError(401, {
            errorCode: "application.auth.login_failed",
            traceId: "trace-copy-001",
          }, "corr-copy-001"),
        ),
        "Sign in failed.",
      ),
    );
    expect(report).toContain("ErrorCode: application.auth.login_failed");
    expect(report).toContain("trace-copy-001");
    expect(report).toContain("platform.auth.login");
    expect(report).toContain("ExItS POS Error Report");
  });

  it("treats credential failures as handled sign-in feedback", () => {
    const userMissing = buildAuthLoginFailure(
      new PlatformApiError(404, { errorCode: "application.user.not_found" }),
    );
    const wrongPassword = buildAuthLoginFailure(
      new PlatformApiError(401, { errorCode: "application.credential.password_invalid" }),
    );
    const upstream = buildAuthLoginFailure(
      new PlatformApiError(502, { errorCode: "application.upstream_error" }),
    );
    const network = buildAuthLoginFailure(new TypeError("Failed to fetch"));

    expect(isHandledSignInFailure(userMissing)).toBe(true);
    expect(isHandledSignInFailure(wrongPassword)).toBe(true);
    expect(isHandledSignInFailure(upstream)).toBe(false);
    expect(isHandledSignInFailure(network)).toBe(false);
    expect(resolveAuthLoginFailurePresentation(userMissing, t).title).toBe("signIn.userNotFound");
    expect(resolveAuthLoginFailurePresentation(wrongPassword, t).title).toBe("signIn.passwordIncorrect");
  });

  it("does not classify client runtime errors as network failures", () => {
    const failure = buildAuthLoginFailure(
      new TypeError("crypto.randomUUID is not a function"),
    );
    const presentation = resolveAuthLoginFailurePresentation(failure, t);
    expect(presentation.title).toBe("signIn.failed");
    expect(presentation.detail).toContain("crypto.randomUUID");
    expect(presentation.title).not.toBe("signIn.networkError");
  });

  it("GENERIC_FALSE_ONLY_LOGIN_FAILURE removed from presentation", () => {
    const failure = buildAuthLoginFailure(
      new PlatformApiError(502, {
        errorCode: "application.upstream_error",
        detail: "Platform API gateway timeout.",
      }),
    );
    const presentation = resolveAuthLoginFailurePresentation(failure, t);
    expect(presentation.title).toBe("signIn.failed");
    expect(presentation.detail).toContain("Platform API gateway timeout.");
    expect(presentation.detail).not.toContain("Check your credentials");
    expect(isInvalidCredentialsFailure(failure)).toBe(false);
  });
});
