import { describe, expect, it } from "vitest";
import { actionCenterItemHref } from "@/api/admin/action-center-client";

describe("action-center-client", () => {
  it("routes payment items to billing workspace", () => {
    expect(
      actionCenterItemHref({
        id: "payment-pending-1",
        category: "payment",
        severity: "warning",
        title: "Pending",
        reason: "Ref",
        paymentId: "00000000-0000-0000-0000-000000000001",
      }),
    ).toBe("/admin/payments/00000000-0000-0000-0000-000000000001");
  });

  it("routes subscription summaries to subscription portfolio filters", () => {
    expect(
      actionCenterItemHref({
        id: "summary-past-due-subscriptions",
        category: "subscription",
        severity: "danger",
        title: "Past-due",
        reason: "1",
      }),
    ).toBe("/admin/subscriptions?status=PastDue");

    expect(
      actionCenterItemHref({
        id: "summary-grace-subscriptions",
        category: "subscription",
        severity: "warning",
        title: "Grace",
        reason: "1",
      }),
    ).toBe("/admin/subscriptions?status=GracePeriod");
  });

  it("routes usage warnings to usage limits", () => {
    expect(
      actionCenterItemHref({
        id: "usage-1",
        category: "usage",
        severity: "warning",
        title: "Usage",
        reason: "80%",
        organizationId: "00000000-0000-0000-0000-000000000002",
        productCode: "pinoy-business-pos",
      }),
    ).toBe(
      "/admin/usage?organizationId=00000000-0000-0000-0000-000000000002&productCode=pinoy-business-pos",
    );
  });

  it("routes account and admin-only categories to matching workspaces", () => {
    expect(
      actionCenterItemHref({
        id: "summary-unassigned-accounts",
        category: "account",
        severity: "warning",
        title: "Accounts",
        reason: "1",
      }),
    ).toBe("/admin/users?directory=Unassigned");

    expect(
      actionCenterItemHref({
        id: "job-failed-1",
        category: "job",
        severity: "danger",
        title: "Job",
        reason: "failed",
        jobId: "00000000-0000-0000-0000-000000000009",
      }),
    ).toBe("/admin/global-catalog/imports/00000000-0000-0000-0000-000000000009");

    expect(
      actionCenterItemHref({
        id: "health-overall",
        category: "health",
        severity: "danger",
        title: "Health",
        reason: "Unhealthy",
      }),
    ).toBe("/admin/system-health");

    expect(
      actionCenterItemHref({
        id: "org-suspended-1",
        category: "organization",
        severity: "warning",
        title: "Suspended organizations",
        reason: "1",
      }),
    ).toBe("/admin/organizations?status=Suspended");
  });
});
