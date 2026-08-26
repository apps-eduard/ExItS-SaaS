import { expect, test, type Page, type Route } from "@playwright/test";
import { clientNavigate } from "./mock-bound-session";

/**
 * RMAP-21G end-to-end: the Personal offline queue a person actually walks.
 *
 * A private To-do and a private Utang contact are the two Personal writes that need no live server
 * state and no second human, so both must survive a dropped connection. This proves the browser
 * path: writing while offline stores the work on the device instead of losing it, and reconnecting
 * replays it to the platform API without the person doing anything.
 *
 * Kept separate from the Organization offline spec because a Personal LocalStore is a different
 * database under a different scope — a Personal note must never land in a store's outbox.
 */

const USER_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const SERVER_TODO_ID = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const SERVER_CONTACT_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

type PersonalPost = { path: string; body: Record<string, unknown>; idempotencyKey?: string };

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
  const posts: PersonalPost[] = [];
  let loggedIn = false;

  await page.route("**/platform-api/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const pathname = new URL(url).pathname;

    if (pathname.includes("/antiforgery/token")) {
      return json(route, { headerName: "X-XSRF-TOKEN", token: "e2e-csrf" });
    }
    if (pathname.includes("/auth/me") && method === "GET") {
      return loggedIn ? json(route, session()) : json(route, {}, 401);
    }
    if (pathname.includes("/auth/login") && method === "POST") {
      loggedIn = true;
      return json(route, { ...session(), sessionToken: "must-not-persist" });
    }
    if (pathname.includes("/auth/organizations") && method === "GET") {
      return json(route, []);
    }
    if (pathname.includes("/auth/token") && method === "POST") {
      return json(route, { accessToken: "e2e-personal-token", productAccessAllowed: false });
    }

    if (pathname.includes("/personal/dashboard") && method === "GET") {
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
    if (pathname.includes("/personal/me") && method === "GET") {
      return json(route, {
        userIdentityId: USER_ID,
        displayName: "Ana Personal",
        email: "ana@example.com",
      });
    }
    if (pathname.includes("/personal/notifications") && method === "GET") {
      return json(route, []);
    }

    if (pathname.endsWith("/personal/todos") && method === "GET") {
      return json(route, []);
    }
    if (pathname.endsWith("/personal/todos") && method === "POST") {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      posts.push({
        path: pathname,
        body,
        idempotencyKey: route.request().headers()["idempotency-key"],
      });
      return json(
        route,
        {
          id: SERVER_TODO_ID,
          ownerUserIdentityId: USER_ID,
          title: body.title ?? "",
          notes: null,
          status: "Open",
          priority: "None",
          dueAtUtc: null,
          reminderAtUtc: null,
          relatedEntityType: null,
          relatedEntityId: null,
          version: 1,
          createdAtUtc: "2026-08-21T00:00:00Z",
          updatedAtUtc: "2026-08-21T00:00:00Z",
          completedAtUtc: null,
        },
        201,
      );
    }

    if (pathname.endsWith("/utang/contacts") && method === "GET") {
      return json(route, []);
    }
    if (pathname.endsWith("/utang/contacts") && method === "POST") {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      posts.push({
        path: pathname,
        body,
        idempotencyKey: route.request().headers()["idempotency-key"],
      });
      return json(
        route,
        {
          id: SERVER_CONTACT_ID,
          displayName: body.displayName ?? "",
          phone: null,
          email: null,
          linkedUserIdentityId: null,
          status: "Active",
          createdAtUtc: "2026-08-21T00:00:00Z",
        },
        201,
      );
    }
    if (pathname.includes("/utang/relationships")) {
      return json(route, []);
    }
    if (pathname.includes("/utang/invitations")) {
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
  await expect(page.getByTestId("personal-shell")).toBeVisible({ timeout: 15000 });
}

/** Reads the Personal LocalStore outbox directly — a Personal store is never the store's store. */
async function readPersonalOutbox(page: Page) {
  return page.evaluate(async () => {
    const names = await indexedDB.databases();
    const personal = names.find((entry) => entry.name?.startsWith("exits-offline-Personal-"));
    if (!personal?.name) {
      return null;
    }
    const db = await new Promise<IDBDatabase>((resolve, reject) => {
      const request = indexedDB.open(personal.name as string);
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error);
    });
    type Row = { operationType?: string; queueState?: string };
    const rows = await new Promise<Row[]>((resolve, reject) => {
      const request = db.transaction("outbox").objectStore("outbox").getAll();
      request.onsuccess = () => resolve(request.result as Row[]);
      request.onerror = () => reject(request.error);
    });
    db.close();
    return {
      types: rows.map((row) => row.operationType ?? ""),
      states: rows.map((row) => row.queueState ?? ""),
      raw: JSON.stringify(rows),
    };
  });
}

test.describe("RMAP-21G Personal offline queue and reconnect sync", () => {
  test.use({ serviceWorkers: "block" });

  test("queues a private To-do written offline and replays it on reconnect", async ({ page }) => {
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
    await page.getByTestId("todo-create-submit").click();

    // The To-do is readable on the device straight away, marked as not yet sent. It carries no due
    // date, so it lives under Open rather than Today.
    await page.getByTestId("todo-tab-open").click();
    await expect(page.getByTestId("todo-waiting-chip").first()).toBeVisible({ timeout: 15000 });
    expect(api.posts).toHaveLength(0);

    const queued = await readPersonalOutbox(page);
    expect(queued?.types).toContain("personal.todo.create");
    // The note itself is ciphertext at rest, even on the person's own device.
    expect(queued?.raw).not.toContain("Bayaran ang kuryente");

    await page.context().setOffline(false);
    await expect.poll(() => api.posts.length, { timeout: 20000 }).toBe(1);
    expect(api.posts[0].path).toContain("/personal/todos");
    expect(api.posts[0].body.title).toBe("Bayaran ang kuryente");

    // Replayed exactly once, and the queue row is settled rather than left to fire again.
    await expect
      .poll(async () => (await readPersonalOutbox(page))?.states ?? [], { timeout: 15000 })
      .toEqual(["Succeeded"]);
    expect(api.posts).toHaveLength(1);
  });

  test("queues a private Utang contact written offline and replays it on reconnect", async ({
    page,
  }) => {
    const api = await mockPersonalApi(page);
    await signIn(page);

    await clientNavigate(page, "/personal/utang/people");
    await expect(page.getByTestId("personal-utang-people")).toBeVisible({ timeout: 15000 });

    await page.context().setOffline(true);
    await page.getByTestId("utang-contact-name").fill("Aling Nena");
    await page.getByTestId("utang-contact-submit").click();

    await expect
      .poll(async () => (await readPersonalOutbox(page))?.types ?? [], { timeout: 15000 })
      .toContain("personal.contact.create");
    expect(api.posts).toHaveLength(0);

    await page.context().setOffline(false);
    await expect.poll(() => api.posts.length, { timeout: 20000 }).toBe(1);
    expect(api.posts[0].path).toContain("/utang/contacts");
    expect(api.posts[0].body.displayName).toBe("Aling Nena");
  });
});
