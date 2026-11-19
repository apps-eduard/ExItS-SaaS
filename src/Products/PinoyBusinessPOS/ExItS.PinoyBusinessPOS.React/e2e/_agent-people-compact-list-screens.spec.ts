/**
 * Visual capture for V1 Personal People compact contact list.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");

const LOCAL_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const CONNECTED_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const PENDING_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const LINKED_USER_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const PENDING_USER_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown) {
  return route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function installPeopleMocks(page: Page) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();

    if (url.includes("/api/v1/personal/utang/contacts") && method === "GET") {
      return json(route, [
        {
          id: LOCAL_ID,
          displayName: "Eduard Uytoco",
          phone: "09171234567",
          email: "eduard@example.com",
          resolvedUserIdentityId: null,
          resolvedPublicUserId: null,
          linkedUserIdentityId: null,
          status: "Active",
          createdAtUtc: "2026-08-25T00:00:00.000Z",
        },
        {
          id: CONNECTED_ID,
          displayName: "Ana Cruz",
          phone: "09179876543",
          email: "ana@example.com",
          resolvedUserIdentityId: LINKED_USER_ID,
          resolvedPublicUserId: "EX-ANA-0001",
          linkedUserIdentityId: LINKED_USER_ID,
          status: "Active",
          createdAtUtc: "2026-08-25T00:00:00.000Z",
        },
        {
          id: PENDING_ID,
          displayName: "Juan Dela Cruz",
          phone: "09170001111",
          email: "juan@example.com",
          resolvedUserIdentityId: PENDING_USER_ID,
          resolvedPublicUserId: "EX-JUAN-0002",
          linkedUserIdentityId: null,
          status: "Active",
          createdAtUtc: "2026-08-25T00:00:00.000Z",
        },
      ]);
    }

    if (url.includes("/api/v1/personal/utang/relationships/lent")) {
      return json(route, [
        {
          id: "ffffffff-ffff-4fff-8fff-ffffffffffff",
          perspective: "Lent",
          creditorUserIdentityId: null,
          creditorContactId: null,
          debtorUserIdentityId: null,
          debtorContactId: LOCAL_ID,
          currencyCode: "PHP",
          currentBalance: 4000,
          dueDateUtc: "2020-11-11T00:00:00.000Z",
          status: "Active",
          version: 1,
          updatedAtUtc: "2026-11-01T00:00:00.000Z",
          isLedgerOwner: true,
          isSharedLedger: false,
        },
      ]);
    }

    if (url.includes("/api/v1/personal/utang/relationships/borrowed")) {
      return json(route, []);
    }

    if (url.includes("/api/v1/personal/connections")) {
      return json(route, [
        {
          id: "99999999-9999-4999-8999-999999999999",
          requesterUserIdentityId: LINKED_USER_ID,
          targetUserIdentityId: PENDING_USER_ID,
          requesterContactId: PENDING_ID,
          requesterDisplayName: "Me",
          requesterPublicUserId: "EX-ME-0000",
          targetPublicUserId: "EX-JUAN-0002",
          status: "Pending",
          createdAtUtc: "2026-11-01T00:00:00.000Z",
          updatedAtUtc: "2026-11-01T00:00:00.000Z",
          expiresAtUtc: "2026-12-01T00:00:00.000Z",
          direction: "Sent",
        },
      ]);
    }

    if (url.includes("/api/v1/personal/dashboard")) {
      return json(route, {
        userIdentityId: LINKED_USER_ID,
        accountProfileId: "11111111-1111-4111-8111-111111111111",
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 3,
        activeRelationshipCount: 1,
        totalLentBalance: 4000,
        totalBorrowedBalance: 0,
        pendingConfirmationCount: 0,
        sharedWithMeLentBalance: 0,
        sharedWithMeBorrowedBalance: 0,
        sharedWithMeActiveCount: 0,
      });
    }

    if (url.includes("/api/v1/personal/")) {
      return json(route, []);
    }

    return route.fallback();
  });
}

test("capture People compact contact list", async ({ page }) => {
  test.setTimeout(90_000);
  await mockPersonalSession(page);
  await installPeopleMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });

  // Mobile: compact contact rows (no desktop table).
  await page.setViewportSize({ width: 390, height: 844 });
  await page.evaluate(() => {
    document.documentElement.setAttribute("data-theme", "light");
    document.documentElement.setAttribute("data-primary", "blue");
    document.documentElement.setAttribute("data-accent", "blue");
  });
  await page.goto("/personal/people");
  await expect(page).toHaveURL(/\/personal\/people/);
  await expect(page.getByTestId("people-list-section")).toBeVisible({ timeout: 20000 });
  await expect(page.getByTestId("people-list")).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId(`people-row-${LOCAL_ID}`)).toBeVisible();
  await expect(page.getByTestId(`people-row-status-${LOCAL_ID}`)).toHaveText("Local Contact");
  await expect(page.getByTestId(`people-row-status-${CONNECTED_ID}`)).toHaveText("Connected");
  await expect(page.getByTestId(`people-row-status-${PENDING_ID}`)).toHaveText("Pending");
  await expect(page.getByText(/I Lent/i)).toHaveCount(0);
  await expect(page.getByText(/I Owe/i)).toHaveCount(0);
  await expect(page.getByTestId("people-table")).toHaveCount(0);
  await page.screenshot({
    path: path.join(OUT_DIR, "people-compact-list-390.png"),
    fullPage: false,
  });

  // Large screen: compact People table (same non-financial columns).
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto("/personal/people");
  await expect(page.getByTestId("people-list-section")).toBeVisible({ timeout: 20000 });
  await expect(page.getByTestId("people-table")).toBeVisible({ timeout: 15000 });
  await expect(page.getByTestId(`people-table-row-${LOCAL_ID}`)).toBeVisible();
  await expect(page.getByTestId(`people-table-name-${LOCAL_ID}`)).toHaveText("Eduard Uytoco");
  await expect(page.getByTestId(`people-table-status-${LOCAL_ID}`)).toHaveText("Local Contact");
  await expect(page.getByTestId(`people-table-status-${CONNECTED_ID}`)).toHaveText("Connected");
  await expect(page.getByTestId(`people-table-status-${PENDING_ID}`)).toHaveText("Pending");
  await expect(page.getByText(/I Lent/i)).toHaveCount(0);
  await expect(page.getByText(/I Owe/i)).toHaveCount(0);
  await expect(page.getByTestId("people-list")).toBeHidden();
  await page.screenshot({
    path: path.join(OUT_DIR, "people-compact-list-1440.png"),
    fullPage: false,
  });

  // Confirm table row opens the stacked contact sheet (financial detail path).
  await page.getByTestId(`people-table-row-${LOCAL_ID}`).click();
  const sheet = page.getByTestId(`personal-contact-sheet-${LOCAL_ID}`);
  await expect(sheet).toHaveAttribute("data-open", "true", { timeout: 10000 });
  await expect(page.getByTestId(`personal-contact-sheet-name-${LOCAL_ID}`)).toHaveText(
    "Eduard Uytoco",
  );
});
