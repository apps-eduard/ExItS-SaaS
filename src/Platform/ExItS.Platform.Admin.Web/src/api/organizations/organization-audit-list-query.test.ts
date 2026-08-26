import { describe, expect, it } from "vitest";
import {
  organizationAuditRequestPath,
  organizationAuditSearchParams,
  parseOrganizationAuditSearchParams,
} from "@/api/organizations/organization-audit-list-query";

describe("organization audit list query", () => {
  it("parses and serializes supported URL filters only", () => {
    const state = parseOrganizationAuditSearchParams(
      new URLSearchParams(
        "fromUtc=2026-01-01T00:00:00Z&toUtc=2026-08-01T00:00:00Z&actor=olivia&action=platform.auth.login_succeeded&targetType=PlatformUser&outcome=Succeeded&branchId=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb&page=2",
      ),
    );
    expect(state).toEqual({
      fromUtc: "2026-01-01T00:00:00Z",
      toUtc: "2026-08-01T00:00:00Z",
      actor: "olivia",
      action: "platform.auth.login_succeeded",
      targetType: "PlatformUser",
      outcome: "Succeeded",
      branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      page: 2,
    });
    expect(organizationAuditSearchParams(state).toString()).toContain("outcome=Succeeded");
    expect(organizationAuditSearchParams(state).get("page")).toBe("2");
  });

  it("ignores invalid outcome and branchId without inventing filters", () => {
    const state = parseOrganizationAuditSearchParams(
      new URLSearchParams("outcome=Maybe&branchId=not-a-guid&fromUtc=not-a-date"),
    );
    expect(state.outcome).toBe("");
    expect(state.branchId).toBe("");
    expect(state.fromUtc).toBe("");
  });

  it("builds the org-scoped audit path with supported server parameters", () => {
    expect(
      organizationAuditRequestPath("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", {
        fromUtc: "2026-01-01T00:00:00Z",
        toUtc: "",
        actor: "olivia",
        action: "",
        targetType: "",
        outcome: "Denied",
        branchId: "",
        page: 1,
      }),
    ).toBe(
      "/api/v1/platform/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/audit?fromUtc=2026-01-01T00%3A00%3A00Z&actor=olivia&outcome=Denied&page=1&pageSize=20",
    );
  });
});
