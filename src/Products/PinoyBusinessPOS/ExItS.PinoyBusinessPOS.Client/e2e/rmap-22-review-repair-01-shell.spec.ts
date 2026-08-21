import { expect, test } from "@playwright/test";
import { assertNoHorizontalOverflow } from "./helpers";
import { mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

const N1 = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const N2 = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

type NoteState = {
  items: Array<{
    id: string;
    title: string;
    preview: string;
    relatedType: string;
    relatedId: string | null;
    isRead: boolean;
    createdAtUtc: string;
    readAtUtc: string | null;
  }>;
};

async function mockPersonalNotifications(
  page: import("@playwright/test").Page,
  initial?: Partial<NoteState>,
) {
  const state: NoteState = {
    items: initial?.items ?? [
      {
        id: N1,
        title: "Utang reminder",
        preview: "A reminder is due.",
        relatedType: "Reminder",
        relatedId: null,
        isRead: false,
        createdAtUtc: "2026-08-21T08:00:00Z",
        readAtUtc: null,
      },
      {
        id: N2,
        title: "Earlier notice",
        preview: "Already seen.",
        relatedType: "System",
        relatedId: null,
        isRead: true,
        createdAtUtc: "2026-08-20T08:00:00Z",
        readAtUtc: "2026-08-20T09:00:00Z",
      },
      {
        id: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        title: "Second unread",
        preview: "Another unread.",
        relatedType: "Reminder",
        relatedId: null,
        isRead: false,
        createdAtUtc: "2026-08-21T07:00:00Z",
        readAtUtc: null,
      },
      {
        id: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        title: "Third unread",
        preview: "Third.",
        relatedType: "Reminder",
        relatedId: null,
        isRead: false,
        createdAtUtc: "2026-08-21T06:00:00Z",
        readAtUtc: null,
      },
    ],
  };

  await page.route("**/platform-api/**/personal/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/personal/notifications") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(state.items),
      });
    }

    if (url.match(/\/personal\/notifications\/[^/]+\/read/) && method === "POST") {
      const id = url.split("/notifications/")[1]?.split("/")[0] ?? "";
      state.items = state.items.map((item) =>
        item.id === id ? { ...item, isRead: true, readAtUtc: "2026-08-21T10:00:00Z" } : item,
      );
      const updated = state.items.find((item) => item.id === id);
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(updated),
      });
    }

    if (url.includes("/personal/dashboard") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          userIdentityId: "11111111-1111-1111-1111-111111111111",
          accountProfileId: "22222222-2222-2222-2222-222222222222",
          accountClass: "Personal",
          utangAvailable: true,
          contactCount: 0,
          activeRelationshipCount: 0,
          totalLentBalance: 0,
          totalBorrowedBalance: 0,
        }),
      });
    }

    if (url.includes("/personal/todos") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: "[]",
      });
    }

    return route.fallback();
  });

  return state;
}

test.describe("RMAP-22 Review Repair 01 Personal shell", () => {
  test.use({ serviceWorkers: "block" });

  test("bell, unread badge, mark read, connection honesty", async ({ page }) => {
    await mockPersonalSession(page);
    await mockPersonalNotifications(page);
    await signInAsPersonal(page);
    await page.goto("/personal");
    await expect(page.getByTestId("personal-shell")).toBeVisible();
    await expect(page.getByTestId("shell-connection-button")).toBeVisible();
    await expect(page.getByTestId("personal-notification-bell")).toBeVisible();
    await expect(page.getByTestId("personal-notification-bell-badge")).toHaveText("3");

    await page.getByTestId("personal-notification-bell").click();
    await expect(page.getByTestId("personal-notifications-page")).toBeVisible();
    await expect(page.getByTestId("notifications-tab-unread")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(page.getByTestId(`notification-row-${N1}`)).toBeVisible();
    await expect(page.getByTestId(`notification-row-${N2}`)).toHaveCount(0);

    await page.getByTestId(`notification-mark-read-${N1}`).click();
    await expect(page.getByTestId("personal-notification-bell-badge")).toHaveText("2");

    await page.getByTestId("notifications-tab-all").click();
    await expect(page.getByTestId(`notification-row-${N1}`)).toBeVisible();
    await expect(page.getByTestId(`notification-row-${N2}`)).toBeVisible();

    await page.getByTestId("shell-connection-button").click();
    await expect(page.getByTestId("shell-connection-button-panel")).toBeVisible();
    await expect(page.getByTestId("shell-connection-button-status")).toContainText(
      /Online|Offline/,
    );
    await expect(page.getByTestId("shell-connection-button-panel")).not.toContainText(
      /All changes synced|pending|Last synced/i,
    );
    if (await page.getByTestId("shell-connection-button-refresh").isVisible()) {
      await page.getByTestId("shell-connection-button-refresh").click();
    }
  });

  for (const viewport of [
    { width: 375, height: 812 },
    { width: 768, height: 1024 },
    { width: 1024, height: 768 },
    { width: 1440, height: 900 },
  ] as const) {
    test(`top bar responsive ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await mockPersonalSession(page);
      await mockPersonalNotifications(page);
      await page.setViewportSize(viewport);
      await signInAsPersonal(page);
      await page.goto("/personal");
      await expect(page.getByTestId("personal-top-bar")).toBeVisible();
      await expect(page.getByTestId("personal-notification-bell")).toBeVisible();
      await expect(page.getByTestId("shell-connection-button")).toBeVisible();
      await assertNoHorizontalOverflow(page);
    });
  }
});
