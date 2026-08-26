import { describe, expect, it } from "vitest";
import { ApiClientError } from "@/api/http";
import { buildDiagnosticReport } from "@/lib/diagnostics/build-diagnostic-report";
import {
  assertNoForbiddenDiagnostics,
  createErrorReference,
  safePathname,
} from "@/lib/diagnostics/diagnostic-redaction";
import { GENERIC_API_MESSAGE, GENERIC_RUNTIME_MESSAGE } from "@/lib/diagnostics/diagnostic-types";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

const ENVIRONMENT = {
  locale: "en",
  theme: "system",
  pathname: "/appearance",
  now: () => "2026-08-19T12:00:00.000Z",
  createReference: () => "ERR-A7F3",
  browserPlatform: "Win32; en-US",
  appVersion: "0.0.1-impl-01",
};

const SAFE_CORRELATION = "7f9c2f2e-1111-1111-1111-111111111111";

function reportForRuntime(message: string): string {
  return buildDiagnosticReport(normalizeDiagnosticError(new Error(message), ENVIRONMENT));
}

function reportForApi(fields: { title?: string; detail?: string; errorCode?: string }): string {
  const error = new ApiClientError(
    "platform",
    500,
    {
      title: fields.title,
      detail: fields.detail,
      errorCode: fields.errorCode ?? "application.sample",
    },
    SAFE_CORRELATION,
  );
  return buildDiagnosticReport(normalizeDiagnosticError(error, ENVIRONMENT));
}

describe("diagnostics allowlist", () => {
  it("creates a short ERR- reference", () => {
    expect(createErrorReference()).toMatch(/^ERR-[0-9A-F]{4}$/);
  });

  it("strips query and hash from pathname", () => {
    expect(safePathname("/appearance?token=SECRET_QUERY_VALUE#hash")).toBe("/appearance");
    const report = buildDiagnosticReport(
      normalizeDiagnosticError(new Error("x"), {
        ...ENVIRONMENT,
        pathname: "/appearance?token=SECRET_QUERY_VALUE#PIN_654321",
      }),
    );
    expect(report).toContain("/appearance");
    expect(report).not.toContain("SECRET_QUERY_VALUE");
    expect(report).not.toContain("PIN_654321");
    expect(report).not.toContain("?");
    expect(report).not.toContain("#");
  });

  it("copies a generic runtime message, not the original arbitrary message", () => {
    const report = reportForRuntime("Simulated foundation runtime error");
    expect(report).toContain(GENERIC_RUNTIME_MESSAGE);
    expect(report).not.toContain("Simulated foundation runtime error");
  });

  it("keeps safe errorCode, HTTP status, and correlation ID", () => {
    const report = reportForApi({ errorCode: "auth.session_invalid" });
    expect(report).toContain("auth.session_invalid");
    expect(report).toContain("500");
    expect(report).toContain(SAFE_CORRELATION);
    expect(report).toContain(GENERIC_API_MESSAGE);
  });

  it("drops errorCode that is not a namespaced allowlisted token", () => {
    const report = reportForApi({ errorCode: "PIN_654321" });
    expect(report).not.toContain("PIN_654321");
  });

  it("does not copy API problem title or detail", () => {
    const report = reportForApi({
      title: "Customer Olivia Santos",
      detail: "olivia@example.test",
      errorCode: "application.sample",
    });
    expect(report).not.toContain("Customer Olivia Santos");
    expect(report).not.toContain("olivia@example.test");
    expect(report).toContain(GENERIC_API_MESSAGE);
  });

  it.each([
    ["olivia@example.test"],
    ["+639171234567"],
    ["Customer Olivia Santos"],
    ["GCash reference ABC123456"],
    ["PHP 12345.67"],
    ["PIN_654321"],
    ["BEARER_TOKEN_ONLY"],
    ["SESSION_SECRET_ONLY"],
    ["raw-stack-dump"],
  ] as const)("excludes sentinel when it is the only runtime message: %s", (sentinel) => {
    const report = reportForRuntime(sentinel);
    expect(report).not.toContain(sentinel);
    assertNoForbiddenDiagnostics(report, [sentinel]);
    expect(report).toContain(GENERIC_RUNTIME_MESSAGE);
  });

  it.each([
    ["olivia@example.test"],
    ["+639171234567"],
    ["Customer Olivia Santos"],
    ["GCash reference ABC123456"],
    ["PHP 12345.67"],
    ["PIN_654321"],
    ["BEARER_TOKEN_ONLY"],
    ["SESSION_SECRET_ONLY"],
    ["raw-stack-dump"],
  ] as const)("excludes sentinel when it is the only API problem detail: %s", (sentinel) => {
    const report = reportForApi({
      detail: sentinel,
      title: sentinel,
      errorCode: "application.sample",
    });
    expect(report).not.toContain(sentinel);
    assertNoForbiddenDiagnostics(report, [sentinel]);
  });
});
