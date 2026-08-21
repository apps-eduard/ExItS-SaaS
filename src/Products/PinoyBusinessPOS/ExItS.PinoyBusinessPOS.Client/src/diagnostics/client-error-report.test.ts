import { describe, expect, it } from "vitest";
import { formatClientErrorReport } from "@/diagnostics/client-error-report";

describe("formatClientErrorReport", () => {
  it("builds a Cursor-pasteable report with key fields", () => {
    const report = formatClientErrorReport({
      source: "react-error-boundary",
      error: new Error("TileGrid is not defined"),
      componentStack: "\n    at ManagerRoleHomePage\n    at RoleHomeShell",
      url: "http://127.0.0.1:5177/role/manager",
      pathname: "/role/manager",
      mode: "development",
      occurredAt: "2026-08-21T18:00:00.000Z",
    });

    expect(report).toContain("## ExItS POS React — client error report");
    expect(report).toContain("Paste this whole block into Cursor chat");
    expect(report).toContain("Source: react-error-boundary");
    expect(report).toContain("Pathname: /role/manager");
    expect(report).toContain("Error message: TileGrid is not defined");
    expect(report).toContain("### React component stack");
    expect(report).toContain("ManagerRoleHomePage");
    expect(report).toContain("feat/pos-react-client");
  });
});
