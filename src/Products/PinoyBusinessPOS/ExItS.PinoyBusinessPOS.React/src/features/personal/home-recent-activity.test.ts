import { describe, expect, it } from "vitest";
import {
  buildHomeRecentActivityItems,
  formatHomeActivityWhen,
} from "@/features/personal/home-recent-activity";
import type { MessageKey } from "@/i18n/messages";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";

const t = (key: MessageKey) => key;

function account(partial: Partial<UtangAccountRow>): UtangAccountRow {
  return {
    relationshipId: "rel-1",
    perspective: "lent",
    displayName: "Ada",
    currentBalance: 100,
    dueDateUtc: null,
    updatedAtUtc: "2026-11-01T12:00:00Z",
    isSharedLedger: false,
    isLedgerOwner: true,
    status: "Active",
    dueKind: "none",
    ...partial,
  };
}

describe("home-recent-activity", () => {
  it("formats activity dates", () => {
    expect(formatHomeActivityWhen("not-a-date")).toBe("");
    expect(formatHomeActivityWhen("2026-11-01T12:00:00Z")).toMatch(/Nov/);
  });

  it("prefers notifications over utang fallback", () => {
    const items = buildHomeRecentActivityItems({
      notifications: [
        {
          id: "11111111-1111-1111-1111-111111111111",
          title: "To-do reminder",
          preview: "Buy milk",
          relatedType: "PersonalTodo",
          relatedId: null,
          isRead: false,
          createdAtUtc: "2026-11-10T08:00:00Z",
          readAtUtc: null,
        },
      ],
      accounts: [account({})],
      t,
    });
    expect(items).toHaveLength(1);
    expect(items[0]?.title).toBe("personal.social.notif.todoReminderTitle");
    expect(items[0]?.detail).toBe("Buy milk");
    expect(items[0]?.href).toBe("/personal/notifications");
  });

  it("falls back to recent utang accounts", () => {
    const items = buildHomeRecentActivityItems({
      notifications: [],
      accounts: [
        account({
          relationshipId: "a",
          updatedAtUtc: "2026-11-02T00:00:00Z",
          perspective: "owe",
        }),
        account({
          relationshipId: "b",
          updatedAtUtc: "2026-11-03T00:00:00Z",
          perspective: "lent",
          displayName: "Ben",
        }),
      ],
      t,
    });
    expect(items[0]?.id).toBe("b");
    expect(items[0]?.title).toBe("personal.home.activityMoneyLent");
    expect(items[0]?.href).toBe("/personal/utang/lent");
    expect(items[1]?.href).toBe("/personal/utang/owe");
  });

  it("caps notification items at five", () => {
    const notifications = Array.from({ length: 8 }, (_, i) => ({
      id: `11111111-1111-1111-1111-11111111111${i}`,
      title: `Notice ${i}`,
      preview: `Preview ${i}`,
      relatedType: "PersonalTodo",
      relatedId: null,
      isRead: false,
      createdAtUtc: `2026-11-${String(10 - i).padStart(2, "0")}T08:00:00Z`,
      readAtUtc: null,
    }));
    const items = buildHomeRecentActivityItems({
      notifications,
      accounts: [],
      t,
    });
    expect(items).toHaveLength(5);
  });
});
