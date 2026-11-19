/**
 * Visual capture for Contact Sheet upper identity profile polish.
 */
import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { mockPersonalSession, signInAsPersonal } from "./mock-bound-session";

test.use({ serviceWorkers: "block" });

const OUT_DIR = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "_agent-screenshots");
const CONTACT_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const SHARED_CONTACT_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const LINKED_USER_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const REL_LENT_ID = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const REL_OWE_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const REL_SHARED_ID = "ffffffff-ffff-4fff-8fff-ffffffffffff";

type Mode = "lent-overdue" | "owe" | "none" | "shared";

function json(route: { fulfill: (r: object) => Promise<void> }, body: unknown) {
  return route.fulfill({
    status: 200,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

function localContactDto(mode: Mode) {
  return {
    id: CONTACT_ID,
    displayName:
      mode === "none" ? "Very Long Contact Name For Wrapping Check" : "Eduard Uytoco",
    phone: "09171234567",
    email: "eduard@example.com",
    resolvedUserIdentityId: null,
    resolvedPublicUserId: null,
    linkedUserIdentityId: null,
    status: "Active",
    createdAtUtc: "2026-08-25T00:00:00.000Z",
  };
}

function sharedContactDto() {
  return {
    id: SHARED_CONTACT_ID,
    displayName: "Shared Friend",
    phone: "09179876543",
    email: "shared@example.com",
    resolvedUserIdentityId: LINKED_USER_ID,
    resolvedPublicUserId: "PUB-SHARED",
    linkedUserIdentityId: LINKED_USER_ID,
    status: "Active",
    createdAtUtc: "2026-08-25T00:00:00.000Z",
  };
}

function lentRelationship() {
  return {
    id: REL_LENT_ID,
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
  };
}

function oweRelationship() {
  return {
    id: REL_OWE_ID,
    perspective: "Borrowed",
    creditorUserIdentityId: null,
    creditorContactId: CONTACT_ID,
    debtorUserIdentityId: null,
    debtorContactId: null,
    currencyCode: "PHP",
    currentBalance: 50000,
    dueDateUtc: null,
    status: "Active",
    version: 1,
    updatedAtUtc: "2026-11-01T00:00:00.000Z",
    isLedgerOwner: true,
    isSharedLedger: false,
  };
}

function sharedLentRelationship() {
  return {
    id: REL_SHARED_ID,
    perspective: "Lent",
    creditorUserIdentityId: null,
    creditorContactId: null,
    debtorUserIdentityId: LINKED_USER_ID,
    debtorContactId: SHARED_CONTACT_ID,
    currencyCode: "PHP",
    currentBalance: 1250,
    dueDateUtc: null,
    status: "Active",
    version: 1,
    updatedAtUtc: "2026-11-01T00:00:00.000Z",
    isLedgerOwner: true,
    isSharedLedger: true,
  };
}

async function installPeopleMocks(page: Page, getMode: () => Mode) {
  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const mode = getMode();

    if (url.includes("/api/v1/personal/utang/contacts") && method === "GET") {
      if (mode === "shared") {
        return json(route, [sharedContactDto()]);
      }
      return json(route, [localContactDto(mode)]);
    }

    if (url.includes("/api/v1/personal/utang/relationships/lent")) {
      if (mode === "lent-overdue") {
        return json(route, [lentRelationship()]);
      }
      if (mode === "shared") {
        return json(route, [sharedLentRelationship()]);
      }
      return json(route, []);
    }

    if (url.includes("/api/v1/personal/utang/relationships/borrowed")) {
      if (mode === "owe") {
        return json(route, [oweRelationship()]);
      }
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
        activeRelationshipCount: mode === "none" ? 0 : 1,
        totalLentBalance: mode === "lent-overdue" || mode === "shared" ? 4000 : 0,
        totalBorrowedBalance: mode === "owe" ? 50000 : 0,
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

async function openSheet(page: Page, width: number, contactId: string) {
  // Full navigation clears React Query cache so mode mocks re-fetch.
  await page.goto("/personal/people");
  await expect(page).toHaveURL(/\/personal\/people/);
  await expect(page.getByTestId("people-list-section")).toBeVisible({ timeout: 20000 });
  await expect(page.getByText("No people yet")).toBeHidden({ timeout: 15000 });

  const open = page.getByTestId(`people-row-${contactId}`);
  await expect(open).toBeVisible({ timeout: 15000 });
  await open.click();
  await expect(page.getByTestId(`personal-contact-sheet-${contactId}`)).toBeVisible({
    timeout: 10000,
  });
}

test("capture contact identity profile polish", async ({ page }) => {
  test.setTimeout(180_000);
  let mode: Mode = "lent-overdue";
  await mockPersonalSession(page);
  await installPeopleMocks(page, () => mode);
  await signInAsPersonal(page);
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });

  const cases: Array<{ mode: Mode; contactId: string }> = [
    { mode: "lent-overdue", contactId: CONTACT_ID },
    { mode: "owe", contactId: CONTACT_ID },
    { mode: "none", contactId: CONTACT_ID },
    { mode: "shared", contactId: SHARED_CONTACT_ID },
  ];

  for (const width of [390, 430] as const) {
    for (const next of cases) {
      mode = next.mode;
      await page.setViewportSize({ width, height: width === 390 ? 844 : 932 });
      await page.evaluate(() => {
        document.documentElement.setAttribute("data-theme", "dark");
        document.documentElement.setAttribute("data-primary", "green");
      });
      await openSheet(page, width, next.contactId);

      if (next.mode === "lent-overdue") {
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${CONTACT_ID}-direction`),
        ).toHaveText("I Lent", { timeout: 5000 });
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${CONTACT_ID}-due`),
        ).toBeVisible();
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${CONTACT_ID}-kind`),
        ).toHaveText("Local Contact");
      } else if (next.mode === "owe") {
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${CONTACT_ID}-direction`),
        ).toHaveText("I Owe", { timeout: 5000 });
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${CONTACT_ID}-kind`),
        ).toHaveText("Local Contact");
      } else if (next.mode === "shared") {
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${SHARED_CONTACT_ID}-kind`),
        ).toHaveText("Shared Contact", { timeout: 5000 });
        await expect(
          page.getByTestId(`personal-contact-sheet-identity-summary-${SHARED_CONTACT_ID}-direction`),
        ).toHaveText("I Lent");
      } else {
        await expect(
          page.getByTestId(`personal-contact-sheet-status-${CONTACT_ID}-kind`),
        ).toHaveText("Local Contact", { timeout: 5000 });
        await expect(page.getByTestId(`personal-contact-sheet-name-${CONTACT_ID}`)).toHaveText(
          "Very Long Contact Name For Wrapping Check",
        );
      }

      const dialog = page.getByTestId(`personal-contact-sheet-${next.contactId}-dialog`);
      await page.waitForTimeout(200);
      await dialog.screenshot({
        path: path.join(OUT_DIR, `contact-identity-${next.mode}-${width}.png`),
      });
      await page.getByTestId(`personal-contact-sheet-${next.contactId}-close`).click({
        force: true,
      });
      await expect(page.getByTestId(`personal-contact-sheet-${next.contactId}`)).toBeHidden({
        timeout: 5000,
      });
    }
  }
});
