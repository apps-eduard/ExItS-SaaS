import { describe, expect, it } from "vitest";
import { PlatformApiError } from "@/api/platform/platform-http";
import { formatPosErrorReport } from "@/diagnostics/pos-error-report";
import { normalizePosError } from "@/diagnostics/normalize-pos-error";
import { redactDiagnosticText } from "@/diagnostics/diagnostic-redaction";

describe("pos error report", () => {
  it("preserves traceId and errorCode without secrets", () => {
    const report = formatPosErrorReport(
      normalizePosError({
        source: "workspace",
        error: new PlatformApiError(403, {
          errorCode: "application.auth.account_scope_denied",
          traceId: "trace-abc-123",
          detail: "Account class 'Organization' is not allowed to call '/api/v1/platform/antiforgery/token'.",
        }),
        operation: "antiforgery bootstrap",
        httpMethod: "GET",
        path: "/api/v1/platform/antiforgery/token",
        screen: "Choose workspace",
        friendlyMessage: "Security check failed.",
      }),
    );

    expect(report).toContain("ErrorCode: application.auth.account_scope_denied");
    expect(report).toContain("TraceId: trace-abc-123");
    expect(report).toContain("Operation: antiforgery bootstrap");
    expect(report).toContain("Path: /api/v1/platform/antiforgery/token");
    expect(report).toContain("ExItS POS Error Report");
  });

  it("redacts Authorization, Cookie, and X-XSRF-TOKEN material", () => {
    const redacted = redactDiagnosticText(
      "Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig Cookie: session=abc123; X-XSRF-TOKEN=secret-token-value",
    );
    expect(redacted).not.toContain("eyJhbGciOiJIUzI1NiJ9");
    expect(redacted).not.toContain("abc123");
    expect(redacted).not.toContain("secret-token-value");
    expect(redacted).toContain("[REDACTED]");
  });

  it("redacts password-like values from copied reports", () => {
    const report = formatPosErrorReport(
      normalizePosError({
        source: "session",
        error: new Error("password=super-secret failed"),
        operation: "login",
        friendlyMessage: "Sign in failed.",
      }),
    );
    expect(report).not.toContain("super-secret");
    expect(report).toContain("[REDACTED]");
  });
});
