import { describe, expect, it } from "vitest";
import {
  DASHBOARD_ATTENTION_PAGE_SIZE,
  DASHBOARD_AUDIT_PAGE_SIZE,
  DASHBOARD_COUNT_PAGE_SIZE,
  DASHBOARD_PAGE,
  assertDashboardPageSize,
  auditListPath,
  organizationsListPath,
  subscriptionsListPath,
  usersListPath,
} from "@/features/overview/dashboard-bounds";
import { parsePagedResult } from "@/api/platform/paged-result";

describe("dashboard bounded queries", () => {
  it("keeps list and count requests inside the dashboard page-size window", () => {
    expect(DASHBOARD_COUNT_PAGE_SIZE).toBe(1);
    expect(DASHBOARD_ATTENTION_PAGE_SIZE).toBeLessThanOrEqual(DASHBOARD_AUDIT_PAGE_SIZE);
    expect(DASHBOARD_AUDIT_PAGE_SIZE).toBeLessThanOrEqual(8);

    expect(organizationsListPath({ pageSize: DASHBOARD_COUNT_PAGE_SIZE })).toBe(
      `/api/v1/platform/organizations?page=${DASHBOARD_PAGE}&pageSize=1`,
    );
    expect(
      organizationsListPath({ status: "Suspended", pageSize: DASHBOARD_ATTENTION_PAGE_SIZE }),
    ).toContain("status=Suspended");
    expect(
      organizationsListPath({ status: "Suspended", pageSize: DASHBOARD_ATTENTION_PAGE_SIZE }),
    ).toContain("pageSize=5");
    expect(
      subscriptionsListPath({ status: "PastDue", pageSize: DASHBOARD_COUNT_PAGE_SIZE }),
    ).toContain("status=PastDue");
    expect(
      usersListPath({ directory: "Unassigned", pageSize: DASHBOARD_ATTENTION_PAGE_SIZE }),
    ).toContain("directory=Unassigned");
    expect(
      usersListPath({ status: "PendingVerification", pageSize: DASHBOARD_COUNT_PAGE_SIZE }),
    ).toContain("status=PendingVerification");
    expect(auditListPath({ pageSize: DASHBOARD_AUDIT_PAGE_SIZE })).toBe(
      `/api/v1/platform/audit?page=${DASHBOARD_PAGE}&pageSize=8`,
    );
  });

  it("rejects unbounded aggregation page sizes", () => {
    expect(() => assertDashboardPageSize(100)).toThrow(/bounded page-size/i);
    expect(() => assertDashboardPageSize(0)).toThrow(/bounded page-size/i);
  });

  it("does not treat a missing totalCount as zero", () => {
    expect(() => parsePagedResult({ items: [] })).toThrow(/Invalid paged result/);
  });
});
