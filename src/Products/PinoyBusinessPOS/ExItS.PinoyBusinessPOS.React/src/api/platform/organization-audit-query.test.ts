import { describe, expect, it } from "vitest";
import {
  formatAuditActor,
  formatAuditSummary,
  formatAuditTarget,
  formatGovernanceAuditAction,
  organizationAuditRequestPath,
} from "@/api/platform/organization-audit-query";

const ORG = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

describe("organization audit query", () => {
  it("builds server filters without inventing another organization id", () => {
    const path = organizationAuditRequestPath(ORG, {
      fromDate: "2026-10-01",
      toDate: "2026-10-02",
      actor: "olivia",
      action: "platform.organization.updated",
      targetType: "Organization",
      outcome: "Succeeded",
      branchId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      page: 2,
    });

    expect(path.startsWith(`/api/v1/platform/organizations/${ORG}/audit?`)).toBe(true);
    expect(path).toContain("fromUtc=2026-10-01T00%3A00%3A00.000Z");
    expect(path).toContain("actor=olivia");
    expect(path).toContain("action=platform.organization.updated");
    expect(path).toContain("targetType=Organization");
    expect(path).toContain("outcome=Succeeded");
    expect(path).toContain("branchId=bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    expect(path).toContain("page=2");
    expect(path).toContain("pageSize=20");
    expect(path).not.toContain("organizationId=");
  });

  it("turns access checks into plain language without account or organization ids", () => {
    expect(formatGovernanceAuditAction("platform.access.checked")).toBe("Access checked");
    expect(formatAuditActor("platform-user:c2af4f42-d726-426d-ab0d-99f929443e9e", "Maria Santos")).toBe(
      "Maria Santos",
    );
    expect(formatAuditActor("platform-user:c2af4f42-d726-426d-ab0d-99f929443e9e")).toBe("—");
    expect(formatAuditActor("olivia.mendoza@exits.local")).toBe("olivia.mendoza@exits.local");
    expect(formatAuditTarget("AuditRecord", "query")).toBe("Audit search");
    expect(
      formatAuditSummary(
        "Actor 'platform-user:c2af4f42-d726-426d-ab0d-99f929443e9e' does not hold permission 'platform.permission.manage_memberships' for organization '6eef2827-ec56-43fc-b71b-d8b52ff3b5c5'.",
        null,
      ),
    ).toBe("This account cannot manage staff.");
  });
});
