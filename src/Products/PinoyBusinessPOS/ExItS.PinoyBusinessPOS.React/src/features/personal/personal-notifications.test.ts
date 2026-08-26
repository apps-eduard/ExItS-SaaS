import { describe, expect, it } from "vitest";
import {
  countUnreadPersonalNotifications,
  extractCustomerLinkMerchantName,
  formatUnreadNotificationBadge,
  localizePersonalNotification,
  resolveCustomerLinkNotificationState,
} from "@/features/personal/personal-notifications";
import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";
import type { MessageKey } from "@/i18n/messages";
import { catalogs } from "@/i18n/messages";

function note(partial: Partial<PersonalInAppNotificationDto>): PersonalInAppNotificationDto {
  return {
    id: partial.id ?? "n1",
    title: partial.title ?? "Title",
    preview: partial.preview ?? "Preview",
    relatedType: partial.relatedType ?? "Reminder",
    relatedId: partial.relatedId ?? null,
    isRead: partial.isRead ?? false,
    createdAtUtc: partial.createdAtUtc ?? "2026-08-21T01:00:00Z",
    readAtUtc: partial.readAtUtc ?? null,
  };
}

describe("personal notification unread helpers", () => {
  it("counts unread items only", () => {
    expect(
      countUnreadPersonalNotifications([
        note({ id: "1", isRead: false }),
        note({ id: "2", isRead: true }),
        note({ id: "3", isRead: false }),
      ]),
    ).toBe(2);
  });

  it("formats badge as null / exact / 9+", () => {
    expect(formatUnreadNotificationBadge(0)).toBeNull();
    expect(formatUnreadNotificationBadge(3)).toBe("3");
    expect(formatUnreadNotificationBadge(9)).toBe("9");
    expect(formatUnreadNotificationBadge(10)).toBe("9+");
    expect(formatUnreadNotificationBadge(99)).toBe("9+");
  });
});

describe("localizePersonalNotification", () => {
  const tFil = (key: MessageKey) => catalogs["fil-PH"][key];

  it("extracts merchant name from English customer-link preview", () => {
    expect(
      extractCustomerLinkMerchantName(
        "Sari-Sari Ni Ana added you as a customer and wants to link your ExItS account.",
      ),
    ).toBe("Sari-Sari Ni Ana");
    expect(extractCustomerLinkMerchantName("unrelated preview")).toBeNull();
  });

  it("localizes customer-link title and preview", () => {
    const result = localizePersonalNotification(
      note({
        title: "Customer link request",
        preview:
          "Sari-Sari Ni Ana added you as a customer and wants to link your ExItS account.",
        relatedType: "CustomerLinkRequest",
      }),
      tFil,
    );
    expect(result.title).toBe(catalogs["fil-PH"]["personal.social.notif.customerLinkTitle"]);
    expect(result.preview).toContain("Sari-Sari Ni Ana");
    expect(result.preview).not.toContain("added you as a customer");
  });

  it("localizes todo and utang titles but keeps user preview", () => {
    expect(
      localizePersonalNotification(
        note({
          title: "Personal to-do reminder",
          preview: "Bayad kuryente",
          relatedType: "PersonalTodo",
        }),
        tFil,
      ),
    ).toEqual({
      title: catalogs["fil-PH"]["personal.social.notif.todoReminderTitle"],
      preview: "Bayad kuryente",
    });

    expect(
      localizePersonalNotification(
        note({
          title: "Personal Utang reminder",
          preview: "Pay Juan",
          relatedType: "PersonalDebtRelationship",
        }),
        tFil,
      ),
    ).toEqual({
      title: catalogs["fil-PH"]["personal.social.notif.utangReminderTitle"],
      preview: "Pay Juan",
    });
  });

  it("leaves unknown related types unchanged", () => {
    expect(
      localizePersonalNotification(
        note({ title: "Other", preview: "Body", relatedType: "SomethingElse" }),
        tFil,
      ),
    ).toEqual({ title: "Other", preview: "Body" });
  });
});

describe("resolveCustomerLinkNotificationState", () => {
  const preview =
    "mica store added you as a customer and wants to link your ExItS account.";

  it("returns pending when relatedId matches a pending request", () => {
    expect(
      resolveCustomerLinkNotificationState(
        "req-1",
        preview,
        [{ id: "req-1" }],
        [],
      ),
    ).toBe("pending");
  });

  it("returns accepted when store is linked and request is no longer pending", () => {
    expect(
      resolveCustomerLinkNotificationState(
        "req-1",
        preview,
        [],
        [{ organizationDisplayName: "mica store" }],
      ),
    ).toBe("accepted");
  });

  it("returns declined when relatedId exists but request is resolved without a link", () => {
    expect(
      resolveCustomerLinkNotificationState("req-1", preview, [], []),
    ).toBe("declined");
  });

  it("returns unknown without relatedId and without a linked store match", () => {
    expect(resolveCustomerLinkNotificationState(null, preview, [], [])).toBe("unknown");
  });
});
