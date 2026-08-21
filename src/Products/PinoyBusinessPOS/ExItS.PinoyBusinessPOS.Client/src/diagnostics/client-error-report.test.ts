import { describe, expect, it } from "vitest";
import { formatClientErrorReport } from "@/diagnostics/client-error-report";
import {
  redactDiagnosticText,
  safeDiagnosticError,
  safeDiagnosticLocation,
} from "@/diagnostics/diagnostic-redaction";

describe("diagnostic redaction", () => {
  it("strips query string from invitation URLs", () => {
    const location = safeDiagnosticLocation(
      "https://example.com/personal/invitations/accept?token=abc123",
    );
    expect(location.url).toBe("https://example.com/personal/invitations/accept");
    expect(location.pathname).toBe("/personal/invitations/accept");
    expect(location.url).not.toContain("abc123");
    expect(location.url).not.toContain("?");
  });

  it("strips URL fragments", () => {
    const location = safeDiagnosticLocation("https://example.com/app#/secret-or-token");
    expect(location.url).toBe("https://example.com/app");
    expect(location.url).not.toContain("#");
    expect(location.url).not.toContain("secret-or-token");
  });

  it("does not dump unknown objects with secrets", () => {
    const normalized = safeDiagnosticError({
      accessToken: "secret",
      password: "secret",
      customerName: "Ana",
    });
    expect(normalized.message).toContain("sensitive keys omitted");
    expect(normalized.message).not.toContain("secret");
    expect(normalized.message).not.toContain("Ana");
  });

  it("redacts bearer material in messages", () => {
    const text = redactDiagnosticText(
      "Authorization Bearer eyJhbGciOiJIUzI1NiJ9.payload.sig failed",
    );
    expect(text).toContain("[REDACTED]");
    expect(text).not.toContain("eyJhbGciOiJIUzI1NiJ9");
  });

  it("keeps normal Error useful in report", () => {
    const report = formatClientErrorReport({
      source: "react-error-boundary",
      error: new Error("TileGrid is not defined"),
      componentStack: "\n    at ManagerRoleHomePage",
      url: "http://127.0.0.1:5177/role/manager?token=should-not-appear",
      pathname: "/role/manager",
      mode: "development",
      occurredAt: "2026-08-21T18:00:00.000Z",
    });

    expect(report).toContain("Error message: TileGrid is not defined");
    expect(report).toContain("Pathname: /role/manager");
    expect(report).toContain("http://127.0.0.1:5177/role/manager");
    expect(report).not.toContain("should-not-appear");
    expect(report).not.toContain("token=");
    expect(report).toContain("ManagerRoleHomePage");
  });

  it("omits arbitrary object dumps from report", () => {
    const report = formatClientErrorReport({
      source: "unhandled-rejection",
      error: {
        accessToken: "leak-me",
        password: "also-leak",
        customerName: "ShouldNotAppear",
      },
      url: "https://example.com/personal/utang?invite=xyz",
    });
    expect(report).not.toContain("leak-me");
    expect(report).not.toContain("also-leak");
    expect(report).not.toContain("ShouldNotAppear");
    expect(report).not.toContain("xyz");
    expect(report).toContain("/personal/utang");
  });
});
