import { describe, expect, it } from "vitest";
import { PlatformApiError } from "@/api/platform-http";
import { buildDiagnosticReport } from "@/lib/diagnostics/build-diagnostic-report";
import {
  createErrorReference,
  isAbortError,
  safePathname,
} from "@/lib/diagnostics/diagnostic-redaction";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";

const SENTINELS = [
  "SUPER_SECRET_PASSWORD_123",
  "BEARER_TOKEN_SHOULD_NEVER_COPY",
  "SESSION_TOKEN_SHOULD_NEVER_COPY",
  "PIN_654321",
  "SECRET_QUERY_VALUE",
  "olivia@example.test",
];

const environment = {
  pathname: "/admin/organizations",
  locale: "en",
  theme: "system",
  density: "balanced",
  browserPlatform: "Win32; en-US",
  now: () => "2026-08-19T12:00:00.000Z",
  createReference: () => "ERR-A7F3",
};

function sampleRecord(overrides: Partial<DiagnosticRecord> = {}): DiagnosticRecord {
  return {
    application: "ExItS Platform Admin Web",
    errorReference: "ERR-A7F3",
    timestamp: "2026-08-19T12:00:00.000Z",
    category: "API",
    message: "Unable to load organizations.",
    route: "/admin/organizations",
    operation: "Load organizations",
    errorType: "PlatformApiError",
    httpStatus: 500,
    errorCode: "application.organization.load_failed",
    requestCorrelationId: "7f9c2f2e-1111-1111-1111-111111111111",
    serverTraceId: "00-server-trace",
    locale: "en",
    theme: "system",
    density: "balanced",
    browserPlatform: "Win32; en-US",
    ...overrides,
  };
}

describe("createErrorReference", () => {
  it("generates a short ERR- reference", () => {
    expect(createErrorReference()).toMatch(/^ERR-[0-9A-F]{4}$/);
  });
});

describe("safePathname", () => {
  it("uses pathname only and drops query or hash values", () => {
    expect(safePathname("/admin/organizations?token=SECRET_QUERY_VALUE#hash")).toBe(
      "/admin/organizations",
    );
  });
});

describe("normalizeDiagnosticError", () => {
  it("normalizes PlatformApiError with request correlation and server trace", () => {
    const error = new PlatformApiError(
      500,
      {
        title: "Unable to load organizations.",
        detail: "Unable to load organizations.",
        errorCode: "application.organization.load_failed",
        traceId: "00-server-trace",
      },
      "7f9c2f2e-1111-1111-1111-111111111111",
    );
    const record = normalizeDiagnosticError({
      error,
      operation: "Load organizations",
      environment,
    });
    expect(record.category).toBe("API");
    expect(record.errorType).toBe("PlatformApiError");
    expect(record.httpStatus).toBe(500);
    expect(record.errorCode).toBe("application.organization.load_failed");
    expect(record.requestCorrelationId).toBe("7f9c2f2e-1111-1111-1111-111111111111");
    expect(record.serverTraceId).toBe("00-server-trace");
    expect(record.route).toBe("/admin/organizations");
    expect(record.errorReference).toBe("ERR-A7F3");
  });

  it("normalizes network failures without fabricating HTTP fields", () => {
    const record = normalizeDiagnosticError({
      error: new TypeError("Failed to fetch SUPER_SECRET_PASSWORD_123"),
      operation: "Sign in",
      environment,
    });
    expect(record.category).toBe("NETWORK");
    expect(record.httpStatus).toBeUndefined();
    expect(record.errorCode).toBeUndefined();
    expect(record.message).toBe("Unable to complete this operation.");
    expect(record.message).not.toContain("SUPER_SECRET_PASSWORD_123");
  });

  it("normalizes render failures without copying the thrown message", () => {
    const record = normalizeDiagnosticError({
      error: new Error("SUPER_SECRET_PASSWORD_123"),
      category: "RENDER",
      componentStack: "at Boom (boom.tsx)",
      environment,
    });
    expect(record.category).toBe("RENDER");
    expect(record.message).toBe("The application could not continue.");
    expect(record.componentStack).toContain("at Boom");
    expect(record.message).not.toContain("SUPER_SECRET_PASSWORD_123");
  });
});

describe("buildDiagnosticReport", () => {
  it("produces one deterministic structured block and omits empty fields", () => {
    const report = buildDiagnosticReport(
      sampleRecord({
        errorCode: undefined,
        componentStack: undefined,
      }),
    );
    expect(report).toBe(`EXITS ERROR DIAGNOSTICS

Application:
ExItS Platform Admin Web

Route:
/admin/organizations

Operation:
Load organizations

Error Reference:
ERR-A7F3

Error Type:
PlatformApiError

Category:
API

HTTP Status:
500

Request Correlation ID:
7f9c2f2e-1111-1111-1111-111111111111

Server Trace ID:
00-server-trace

Timestamp:
2026-08-19T12:00:00.000Z

Locale:
en

Theme:
system

Density:
balanced

Browser/Platform:
Win32; en-US

Message:
Unable to load organizations.

SECURITY:
Sensitive credentials and protected request/response payloads excluded.`);
    expect(report).not.toContain("undefined");
    expect(report).not.toContain("null");
    expect(report).not.toContain("N/A");
    expect(report).not.toContain("Error Code:");
  });

  it("does not copy secrets, identity, or query values from unapproved sources", () => {
    window.history.replaceState({}, "", "/admin?token=SECRET_QUERY_VALUE");
    const noisy = Object.assign(new Error("SUPER_SECRET_PASSWORD_123"), {
      password: "SUPER_SECRET_PASSWORD_123",
      authorization: "BEARER_TOKEN_SHOULD_NEVER_COPY",
      cookie: "SESSION_TOKEN_SHOULD_NEVER_COPY",
      pin: "PIN_654321",
      email: "olivia@example.test",
      body: { password: "SUPER_SECRET_PASSWORD_123" },
    });
    const report = buildDiagnosticReport(
      normalizeDiagnosticError({
        error: noisy,
        category: "RUNTIME",
        environment: {
          ...environment,
          pathname: safePathname(`${window.location.pathname}${window.location.search}`),
        },
      }),
    );
    for (const sentinel of SENTINELS) {
      expect(report).not.toContain(sentinel);
    }
    expect(report).not.toContain("token=");
  });
});

describe("isAbortError", () => {
  it("recognizes abort errors", () => {
    expect(isAbortError(new DOMException("Aborted", "AbortError"))).toBe(true);
    expect(isAbortError(new Error("nope"))).toBe(false);
  });
});
