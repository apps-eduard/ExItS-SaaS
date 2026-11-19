/**
 * One-off visual capture for Premium Contact Stacked Sheets.
 * Not part of the permanent e2e suite.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");

const CONTACT_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const REL_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

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
    if (url.includes("/api/v1/personal/utang/contacts") && route.request().method() === "GET") {
      return json(route, [
        {
          id: CONTACT_ID,
          displayName: "Peter Paul",
          phone: "09171234567",
          email: "peter@example.com",
          resolvedUserIdentityId: null,
          resolvedPublicUserId: null,
          linkedUserIdentityId: null,
          status: "Active",
          createdAtUtc: "2026-08-25T00:00:00.000Z",
        },
      ]);
    }
    if (url.includes("/api/v1/personal/utang/relationships/lent")) {
      return json(route, [
        {
          id: REL_ID,
          perspective: "Lent",
          creditorUserIdentityId: null,
          creditorContactId: null,
          debtorUserIdentityId: null,
          debtorContactId: CONTACT_ID,
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
    if (
      url.includes("/api/v1/personal/connections") ||
      url.includes("/api/v1/personal/connection-requests")
    ) {
      return json(route, []);
    }
    if (url.includes("/api/v1/personal/dashboard")) {
      return json(route, {
        userIdentityId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
        accountProfileId: "11111111-1111-4111-8111-111111111111",
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 1,
        activeRelationshipCount: 0,
        totalLentBalance: 0,
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

async function openContactSheet(page: Page, width: number) {
  await page.goto("/personal/people");
  await expect(page).toHaveURL(/\/personal\/people/);
  await expect(page.getByTestId("people-list-section")).toBeVisible({ timeout: 20000 });
  await expect(page.getByText("No people yet")).toBeHidden({ timeout: 15000 });
  await page.waitForTimeout(200);

  const open = page.getByTestId(`people-row-${CONTACT_ID}`);
  await expect(open).toBeVisible({ timeout: 15000 });
  await open.click();

  await expect(page.getByTestId(`personal-contact-sheet-${CONTACT_ID}`)).toBeVisible({
    timeout: 10000,
  });
  await expect(page.getByTestId(`personal-contact-sheet-${CONTACT_ID}-dialog`)).toHaveAttribute(
    "data-state",
    "open",
    { timeout: 5000 },
  );
  await expect(page.getByTestId(`personal-contact-sheet-name-${CONTACT_ID}`)).toHaveText(
    "Peter Paul",
  );
  await expect(page.getByTestId(`personal-contact-sheet-actions-${CONTACT_ID}`)).toBeVisible({
    timeout: 5000,
  });
}

test("capture Premium Contact stacked sheets", async ({ page }) => {
  test.setTimeout(90_000);
  await mockPersonalSession(page);
  await installPeopleMocks(page);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });

  const viewports: Array<[number, number]> = [
    [390, 844],
    [430, 932],
    [1440, 900],
  ];

  for (const [width, height] of viewports) {
    await page.setViewportSize({ width, height });
    await page.evaluate(() => {
      document.documentElement.setAttribute("data-theme", "dark");
      document.documentElement.setAttribute("data-primary", "blue");
      document.documentElement.setAttribute("data-accent", "blue");
    });

    await openContactSheet(page, width);
    const dialog = page.getByTestId(`personal-contact-sheet-${CONTACT_ID}-dialog`);
    await page.waitForTimeout(280);
    await page.screenshot({
      path: path.join(OUT_DIR, `contact-stacked-qa-${width}.png`),
      fullPage: false,
    });
    await dialog.screenshot({
      path: path.join(OUT_DIR, `contact-stacked-qa-dialog-${width}.png`),
    });

    await page.getByTestId(`personal-contact-sheet-qa-edit-${CONTACT_ID}`).click();
    await expect(page.getByTestId(`personal-contact-sheet-edit-${CONTACT_ID}`)).toBeVisible({
      timeout: 5000,
    });
    await page.waitForTimeout(320);
    await page.screenshot({
      path: path.join(OUT_DIR, `contact-stacked-edit-${width}.png`),
      fullPage: false,
    });
    await dialog.screenshot({
      path: path.join(OUT_DIR, `contact-stacked-edit-dialog-${width}.png`),
    });

    await page.getByTestId(`personal-contact-sheet-${CONTACT_ID}-close`).click({ force: true });
    await expect(page.getByTestId(`personal-contact-sheet-${CONTACT_ID}`)).toBeHidden({
      timeout: 5000,
    });
  }
});
