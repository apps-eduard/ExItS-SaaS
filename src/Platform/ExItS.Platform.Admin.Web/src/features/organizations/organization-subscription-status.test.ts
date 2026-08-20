import { describe, expect, it } from "vitest";
import {
  normalizeOrganizationSubscriptionStatus,
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusLabelKey,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { translate } from "@/lib/i18n/messages";

describe("organizationSubscriptionStatus", () => {
  it("normalizes legacy Canceled to Cancelled", () => {
    expect(normalizeOrganizationSubscriptionStatus("Canceled")).toBe("Cancelled");
    expect(normalizeOrganizationSubscriptionStatus("Cancelled")).toBe("Cancelled");
  });

  it("maps tones consistently", () => {
    expect(organizationSubscriptionStatusTone("Active")).toBe("success");
    expect(organizationSubscriptionStatusTone("Trialing")).toBe("warning");
    expect(organizationSubscriptionStatusTone("PastDue")).toBe("warning");
    expect(organizationSubscriptionStatusTone("GracePeriod")).toBe("warning");
    expect(organizationSubscriptionStatusTone("Suspended")).toBe("warning");
    expect(organizationSubscriptionStatusTone("Cancelled")).toBe("danger");
    expect(organizationSubscriptionStatusTone("Canceled")).toBe("danger");
    expect(organizationSubscriptionStatusTone("Expired")).toBe("danger");
    expect(organizationSubscriptionStatusTone("UnknownStatus")).toBe("neutral");
  });

  it("provides label keys for known statuses", () => {
    expect(organizationSubscriptionStatusLabelKey("Cancelled")).toBe("dashboard.status.Cancelled");
    expect(organizationSubscriptionStatusLabelKey("Expired")).toBe("dashboard.status.Expired");
    expect(organizationSubscriptionStatusLabelKey("GracePeriod")).toBe(
      "dashboard.status.GracePeriod",
    );
    expect(organizationSubscriptionStatusLabelKey("UnknownStatus")).toBeNull();
  });

  it("localizes Cancelled and Expired in fil-PH", () => {
    const t = (key: Parameters<typeof translate>[1]) => translate("fil-PH", key);
    expect(organizationSubscriptionStatusLabel("Cancelled", t)).toBe("Nakansela");
    expect(organizationSubscriptionStatusLabel("Expired", t)).toBe("Nag-expire");
    expect(organizationSubscriptionStatusLabel("UnknownStatus", t)).toBe("UnknownStatus");
  });
});
