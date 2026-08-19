import { describe, expect, it } from "vitest";
import { ApiClientError } from "@/api/http";
import { buildDiagnosticReport } from "@/lib/diagnostics/build-diagnostic-report";
import {
  assertNoForbiddenDiagnostics,
  createErrorReference,
  safePathname,
} from "@/lib/diagnostics/diagnostic-redaction";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

const SENTINELS = [
  "SUPER_SECRET_PASSWORD_123",
  "BEARER_TOKEN_SHOULD_NEVER_COPY",
  "PIN_654321",
  "SECRET_QUERY_VALUE",
  "olivia@example.test",
  "raw-stack-dump-SHOULD-NOT-APPEAR",
];

describe("diagnostics redaction", () => {
  it("creates a short ERR- reference", () => {
    expect(createErrorReference()).toMatch(/^ERR-[0-9A-F]{4}$/);
  });

  it("keeps pathname only", () => {
    expect(safePathname("/appearance?token=SECRET_QUERY_VALUE#hash")).toBe("/appearance");
  });

  it("builds an allowlisted report without sentinels", () => {
    const error = new ApiClientError(
      "platform",
      500,
      {
        title: "Unable to complete this operation.",
        detail: `Unable to complete. password=SUPER_SECRET_PASSWORD_123 token=BEARER_TOKEN_SHOULD_NEVER_COPY PIN_654321 olivia@example.test`,
        errorCode: "application.sample",
      },
      "7f9c2f2e-1111-1111-1111-111111111111",
    );
    const record = normalizeDiagnosticError(error, {
      locale: "en",
      theme: "system",
      pathname: "/appearance?token=SECRET_QUERY_VALUE",
      now: () => "2026-08-19T12:00:00.000Z",
      createReference: () => "ERR-A7F3",
      browserPlatform: "Win32; en-US",
      appVersion: "0.0.1-impl-01",
    });
    const report = buildDiagnosticReport(record);
    expect(report).toContain("EXITS MOBILE CLIENT DIAGNOSTICS");
    expect(report).toContain("ERR-A7F3");
    expect(report).toContain("/appearance");
    expect(report).not.toContain("SECRET_QUERY_VALUE");
    expect(report).not.toContain("raw-stack-dump-SHOULD-NOT-APPEAR");
    assertNoForbiddenDiagnostics(report, SENTINELS);
  });
});
