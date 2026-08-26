import { describe, expect, it } from "vitest";
import {
  classifyNotificationDayGroup,
  formatNotificationMonthHeading,
  groupNotificationsByDay,
  groupNotificationsByMonth,
  resolveArchivedNotificationStatusLabel,
} from "@/features/personal/notification-archive";
import type { PersonalInAppNotificationDto } from "@/api/platform/personal-types";

function note(
  partial: Partial<PersonalInAppNotificationDto> & Pick<PersonalInAppNotificationDto, "id" | "createdAtUtc">,
): PersonalInAppNotificationDto {
  return {
    title: "Connection",
    preview: "Mica wants to connect with you.",
    relatedType: "PersonalConnectionRequest",
    relatedId: "req-1",
    isRead: true,
    ...partial,
  };
}

describe("notification-archive", () => {
  const now = new Date("2026-08-26T15:00:00.000Z");

  it("classifies today / yesterday / earlier", () => {
    expect(classifyNotificationDayGroup("2026-08-26T01:00:00.000Z", now)).toBe("today");
    expect(classifyNotificationDayGroup("2026-08-25T23:00:00.000Z", now)).toBe("yesterday");
    expect(classifyNotificationDayGroup("2026-08-20T12:00:00.000Z", now)).toBe("earlier");
  });

  it("groups by day and month without duplicates", () => {
    const items = [
      note({ id: "1", createdAtUtc: "2026-08-26T10:00:00.000Z" }),
      note({ id: "2", createdAtUtc: "2026-08-25T10:00:00.000Z" }),
      note({ id: "3", createdAtUtc: "2026-07-21T10:00:00.000Z" }),
      note({ id: "4", createdAtUtc: "2026-07-14T10:00:00.000Z" }),
    ];
    const days = groupNotificationsByDay(items, now);
    expect(days.map((g) => g.key)).toEqual(["today", "yesterday", "earlier"]);
    const months = groupNotificationsByMonth(items);
    expect(months.map((g) => g.key)).toEqual(["2026-08", "2026-07"]);
    expect(formatNotificationMonthHeading(months[1]!, "en")).toContain("July");
  });

  it("maps archived statuses without action verbs", () => {
    expect(
      resolveArchivedNotificationStatusLabel(
        { relatedType: "PersonalConnectionRequest", preview: "Mica wants to connect", isRead: true },
        "Accepted",
      ),
    ).toBe("connected");
    expect(
      resolveArchivedNotificationStatusLabel(
        { relatedType: "PersonalConnectionRequest", preview: "Mica wants to connect", isRead: true },
        "Declined",
      ),
    ).toBe("declined");
    expect(
      resolveArchivedNotificationStatusLabel(
        { relatedType: "PersonalTodo", preview: "Buy milk", isRead: false },
        null,
      ),
    ).toBe("unread");
  });
});
