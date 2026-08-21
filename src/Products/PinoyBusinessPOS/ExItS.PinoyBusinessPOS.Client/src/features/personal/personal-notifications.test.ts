import { describe, expect, it } from "vitest";
import {
  countUnreadPersonalNotifications,
  formatUnreadNotificationBadge,
} from "@/features/personal/personal-notifications";
import type { PersonalInAppNotificationDto } from "@/api/platform/personal-social-client";

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
