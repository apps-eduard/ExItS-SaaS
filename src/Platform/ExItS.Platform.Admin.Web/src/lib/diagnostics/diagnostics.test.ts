import { describe, expect, it } from "vitest";
import { PlatformApiError, PlatformNetworkError } from "@/api/platform-http";
import { classifyHttpDiagnosticCategory } from "@/lib/diagnostics/classify-http-error";
import { formatDiagnosticForClipboard } from "@/lib/diagnostics/build-diagnostic-report";
import {
  createErrorReference,
  safePathname,
  sanitizeApiPath,
  stripSensitiveQueryFromUrl,
} from "@/lib/diagnostics/diagnostic-redaction";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

const environment = {
  pathname: "/admin/reset-password",
  buildSha: "19119089",
  environment: "Local Validation",
  frontendMode: "production",
  localValidationEnabled: true,
  apiMode: "same-origin",
  networkOnline: true,
  now: () => "2026-08-22T08:30:00.000Z",
  createReference: () => "ERR-8F32A1",
};

function sampleRecord(overrides: Partial<DiagnosticRecord> = {}): DiagnosticRecord {
  return {
    application: "Platform Admin React",
    errorReference: "ERR-8F32A1",
    timestampUtc: "2026-08-22T08:30:00.000Z",
    buildSha: "19119089",
    environment: "Local Validation",
    frontendMode: "production",
    localValidationEnabled: true,
    apiMode: "same-origin",
    pagePath: "/admin/reset-password",
    operation: "Reset password",
    category: "SERVER_ERROR",
    userMessage: "Unable to load organizations.",
    httpMethod: "POST",
    apiPath: "/api/v1/platform/auth/reset-password",
    httpStatus: 500,
    httpStatusLabel: "500",
    errorCode: "platform.unhandled_error",
    traceId: "00-server-trace",
    correlationId: "7f9c2f2e-1111-1111-1111-111111111111",
    networkOnline: true,
    retryable: true,
    errorType: "PlatformApiError",
    ...overrides,
  };
}

const SECRETS = [
  "SUPER_SECRET_PASSWORD_123",
  "BEARER_TOKEN_SHOULD_NEVER_COPY",
  "SESSION_TOKEN_SHOULD_NEVER_COPY",
  "RESET_TOKEN_SECRET",
  "ACTIVATION_TOKEN_SECRET",
  "DEVICE_REG_TOKEN_SECRET",
  "access_token=SECRET",
  "refresh_token=SECRET",
];

describe("createErrorReference", () => {
  it("generates ERR- with six hex characters", () => {
    expect(createErrorReference()).toMatch(/^ERR-[0-9A-F]{6}$/);
  });
});

describe("redaction helpers", () => {
  it("removes token query parameters from paths", () => {
    expect(safePathname("/admin/reset-password?token=RESET_TOKEN_SECRET")).toBe(
      "/admin/reset-password",
    );
    expect(stripSensitiveQueryFromUrl("/admin/reset-password?token=RESET_TOKEN_SECRET")).toBe(
      "/admin/reset-password",
    );
  });

  it("keeps safe API paths", () => {
    expect(sanitizeApiPath("/api/v1/platform/auth/reset-password")).toBe(
      "/api/v1/platform/auth/reset-password",
    );
  });
});

describe("classifyHttpDiagnosticCategory", () => {
  it.each([
    [400, "VALIDATION_ERROR"],
    [401, "AUTHENTICATION_REQUIRED"],
    [403, "FORBIDDEN"],
    [404, "NOT_FOUND"],
    [409, "CONFLICT"],
    [419, "SECURITY_REQUEST_ERROR"],
    [429, "RATE_LIMITED"],
    [502, "SERVICE_UNAVAILABLE"],
    [503, "SERVICE_UNAVAILABLE"],
    [504, "SERVICE_UNAVAILABLE"],
    [500, "SERVER_ERROR"],
  ] as const)("maps HTTP %i to %s", (status, expected) => {
    expect(classifyHttpDiagnosticCategory(status)).toBe(expected);
  });

  it("maps domain error codes on 401 to DOMAIN_ERROR", () => {
    expect(
      classifyHttpDiagnosticCategory(401, "application.auth.credential_token_expired"),
    ).toBe("DOMAIN_ERROR");
  });
});

describe("normalizeDiagnosticError", () => {
  it("normalizes PlatformApiError with method, path, trace, and correlation", () => {
    const error = new PlatformApiError(
      500,
      {
        title: "Server error",
        detail: "Server error",
        errorCode: "platform.unhandled_error",
        traceId: "00-server-trace",
      },
      {
        requestCorrelationId: "7f9c2f2e-1111-1111-1111-111111111111",
        method: "POST",
        path: "/api/v1/platform/auth/reset-password",
      },
    );
    const record = normalizeDiagnosticError({
      error,
      operation: "Reset password",
      environment,
    });
    expect(record.category).toBe("SERVER_ERROR");
    expect(record.httpMethod).toBe("POST");
    expect(record.apiPath).toBe("/api/v1/platform/auth/reset-password");
    expect(record.correlationId).toBe("7f9c2f2e-1111-1111-1111-111111111111");
    expect(record.traceId).toBe("00-server-trace");
    expect(record.pagePath).toBe("/admin/reset-password");
  });

  it("normalizes PlatformNetworkError as NETWORK_ERROR with not-received status", () => {
    const record = normalizeDiagnosticError({
      error: new PlatformNetworkError({
        method: "POST",
        path: "/api/v1/platform/auth/reset-password",
        requestCorrelationId: "client-correlation-id",
      }),
      operation: "Reset password",
      environment,
    });
    expect(record.category).toBe("NETWORK_ERROR");
    expect(record.httpStatusLabel).toBe("Not received");
    expect(record.httpMethod).toBe("POST");
    expect(record.apiPath).toBe("/api/v1/platform/auth/reset-password");
    expect(record.correlationId).toBe("client-correlation-id");
    expect(record.errorCode).toBe("NETWORK_UNAVAILABLE");
    expect(record.retryable).toBe(true);
  });

  it("records offline browser state without claiming API health", () => {
    const record = normalizeDiagnosticError({
      error: new PlatformNetworkError({
        method: "GET",
        path: "/api/v1/platform/auth/me",
        requestCorrelationId: "offline-correlation",
      }),
      operation: "Restore session",
      environment: { ...environment, networkOnline: false },
    });
    expect(record.networkOnline).toBe(false);
    expect(record.category).toBe("NETWORK_ERROR");
  });

  it("does not copy thrown secrets into user messages", () => {
    const record = normalizeDiagnosticError({
      error: new TypeError("Failed to fetch SUPER_SECRET_PASSWORD_123"),
      operation: "Sign in",
      environment,
    });
    expect(record.userMessage).not.toContain("SUPER_SECRET_PASSWORD_123");
    expect(record.category).toBe("NETWORK_ERROR");
  });
});

describe("formatDiagnosticForClipboard", () => {
  it("produces deterministic EXITS PLATFORM ERROR REPORT output", () => {
    const report = formatDiagnosticForClipboard(sampleRecord());
    expect(report).toContain("EXITS PLATFORM ERROR REPORT");
    expect(report).toContain("Error Reference: ERR-8F32A1");
    expect(report).toContain("Build: 19119089");
    expect(report).toContain("Category:\nSERVER_ERROR");
    expect(report).toContain("HTTP Method:\nPOST");
    expect(report).toContain("API Path:\n/api/v1/platform/auth/reset-password");
    expect(report).toContain("Trace ID:\n00-server-trace");
    expect(report).toContain("Safe to paste into Cursor:\nYES");
  });

  it("never copies secrets, tokens, or query values", () => {
    window.history.replaceState({}, "", "/admin/reset-password?token=RESET_TOKEN_SECRET");
    const report = formatDiagnosticForClipboard(
      normalizeDiagnosticError({
        error: Object.assign(new Error("SUPER_SECRET_PASSWORD_123"), {
          password: "SUPER_SECRET_PASSWORD_123",
          authorization: "BEARER_TOKEN_SHOULD_NEVER_COPY",
          cookie: "SESSION_TOKEN_SHOULD_NEVER_COPY",
        }),
        operation: "Reset password",
        environment: {
          ...environment,
          pathname: safePathname(`${window.location.pathname}${window.location.search}`),
        },
      }),
    );
    for (const secret of SECRETS) {
      expect(report).not.toContain(secret);
    }
    expect(report).not.toContain("token=");
    expect(report).toContain("Page:\n/admin/reset-password");
  });
});
