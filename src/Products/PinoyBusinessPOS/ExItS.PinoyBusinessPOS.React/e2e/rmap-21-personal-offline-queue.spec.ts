import { expect, test, type Page, type Route } from "@playwright/test";
import { clientNavigate } from "./mock-bound-session";

/**
 * PERS-WEB-ONLINE-ONLY-01 — Personal Web must not enqueue new offline operations.
 *
 * Historical RMAP-21G proved Personal Todo/Utang could queue offline. That engine remains for
 * future Capacitor, but Web/PWA runtime policy blocks new enqueue. This suite asserts the
 * Web channel refuses offline writes (no new outbox rows).
 */

const USER_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

function json(route: Route, body: unknown, status = 200) {
  return route.fulfill({ status, contentType: "application/json", body: JSON.stringify(body) });
}

function session() {
  return {
    sessionId: "22222222-2222-4222-8222-222222222222",
    userId: USER_ID,
    username: "ana@example.com",
    displayName: "Ana Personal",
    email: "ana@example.com",
    accountClass: "Personal",
    homeOrganizationId: null,
    organizationContextLocked: false,
  };
}

async function mockPersonalApi(page: Page) {
  const posts: { path: string }[] = [];
  let loggedIn = false;

  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;

    if (pathname.includes("/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }
    if (pathname.includes("/platform/auth/login") && method === "POST") {
      loggedIn = true;
      return json(route, { ...session(), requiresPasswordChange: false });
    }
    if (pathname.includes("/platform/auth/me")) {
      if (!loggedIn) {
        return json(route, { detail: "unauthorized" }, 401);
      }
      return json(route, session());
    }
    if (pathname.includes("/platform/auth/logout")) {
      loggedIn = false;
      return json(route, {});
    }
    if (pathname.includes("/platform/auth/organizations") && method === "GET") {
      return json(route, []);
    }
    if (pathname.includes("/personal/dashboard")) {
      return json(route, {
        userIdentityId: USER_ID,
        accountProfileId: USER_ID,
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 0,
        activeRelationshipCount: 0,
        totalLentBalance: 0,
        totalBorrowedBalance: 0,
      });
    }
    if (pathname.includes("/personal/me")) {
      return json(route, { userIdentityId: USER_ID, displayName: "Ana Personal", email: "ana@example.com" });
    }
    if (pathname.includes("/personal/notifications/unread-count")) {
      return json(route, { unreadCount: 0 });
    }
    if (pathname.includes("/personal/todos") && method === "GET") {
      return json(route, []);
    }
    if (pathname.includes("/personal/todos") && method === "POST") {
      posts.push({ path: pathname });
      return json(route, { id: "todo-1", title: "x", status: "Open", version: 1 }, 201);
    }
    if (pathname.includes("/utang/contacts") && method === "GET") {
      return json(route, []);
    }
    if (pathname.includes("/utang/contacts") && method === "POST") {
      posts.push({ path: pathname });
      return json(route, { id: "c-1", displayName: "x" }, 201);
    }
    if (pathname.includes("/utang/relationships")) {
      return json(route, []);
    }
    if (pathname.includes("/utang/invitations")) {
      return json(route, []);
    }
    if (pathname.includes("/personal/notifications") && !pathname.includes("unread-count")) {
      return json(route, []);
    }
    if (pathname.includes("/personal/connections")) {
      return json(route, []);
    }
    return json(route, { detail: `unmocked ${pathname}` }, 404);
  });

  return { posts };
}

async function signIn(page: Page) {
  await page.goto("/sign-in");
  await page.getByLabel("Email or staff login").fill("ana@example.com");
  await page.getByLabel("Password").fill("secret");
  await page.getByRole("button", { name: "Sign in" }).click();
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 20000 });
  const dismiss = page.getByRole("button", { name: "Dismiss" });
  if (await dismiss.isVisible().catch(() => false)) {
    await dismiss.click();
  }
}

async function readPersonalOutbox(page: Page) {
  return page.evaluate(async () => {
    const names = await indexedDB.databases();
    const personal = names.find((entry) => entry.name?.startsWith("exits-offline-Personal-"));
    if (!personal?.name) {
      return { types: [] as string[], count: 0 };
    }
    const db = await new Promise<IDBDatabase>((resolve, reject) => {
      const request = indexedDB.open(personal.name as string);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    type Row = { operationType?: string };
    const rows = await new Promise<Row[]>((resolve, reject) => {
      try {
        const request = db.transaction("outbox").objectStore("outbox").getAll();
        request.onsuccess = () => resolve(request.result as Row[]);
        request.onerror = () => reject(request.error);
      } catch {
        resolve([]);
      }
    });
    db.close();
    return {
      types: rows.map((row) => row.operationType ?? ""),
      count: rows.length,
    };
  });
}

test.describe("PERS-WEB-ONLINE-ONLY Personal Web blocks offline enqueue", () => {
  test.use({ serviceWorkers: "block" });

  test("Todo create while offline does not enqueue outbox work", async ({ page }) => {
    const api = await mockPersonalApi(page);
    await signIn(page);

    await clientNavigate(page, "/personal/todo");
    await expect(page.getByTestId("personal-todo-hub")).toBeVisible({ timeout: 15000 });

    await page.getByTestId("todo-create-toggle").click();
    await page.context().setOffline(true);
    await expect(
      page.getByTestId("todo-create-form").getByTestId("todo-offline-notice"),
    ).toBeVisible({ timeout: 15000 });

    await page.getByTestId("todo-create-title").fill("Bayaran ang kuryente");
    // Web online-only: primary write control stays disabled — no outbox enqueue path.
    await expect(page.getByTestId("todo-create-submit")).toBeDisabled();
    expect(api.posts).toHaveLength(0);

    const queued = await readPersonalOutbox(page);
    expect(queued.types).not.toContain("personal.todo.create");
    expect(queued.count).toBe(0);
  });
});
