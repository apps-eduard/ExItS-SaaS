import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import { filterTodosByTab, type PersonalTodoDto } from "@/api/platform/personal-todo-client";
import { openOfflineDatabase, organizationScopeKey, personalScopeKey } from "@/offline/db";
import {
  applyLocalPersonalTodoChange,
  cacheLocalPersonalTodo,
  cachePersonalTodos,
  getCachedPersonalTodo,
  listCachedPersonalTodos,
} from "@/offline/personal-todo-cache";

const ownerUserIdentityId = "99999999-9999-4999-8999-999999999999";

const todo: PersonalTodoDto = {
  id: "11111111-1111-4111-8111-111111111111",
  ownerUserIdentityId,
  title: "Bayaran ang ospital",
  notes: "dalhin ang resibo",
  dueAtUtc: "2026-09-01T00:00:00.000Z",
  reminderAtUtc: "2026-08-31T00:00:00.000Z",
  priority: "High",
  status: "Open",
  relatedEntityType: "PersonalContact",
  relatedEntityId: "22222222-2222-4222-8222-222222222222",
  createdAtUtc: "2026-08-01T00:00:00.000Z",
  updatedAtUtc: "2026-08-02T00:00:00.000Z",
  completedAtUtc: null,
  version: 4,
};

async function openPersonal(userId: string) {
  const scopeBinding = personalScopeKey(userId);
  const db = await openOfflineDatabase("Personal", scopeBinding);
  return { db, scopeBinding };
}

describe("RMAP-21G Personal To-do cache", () => {
  it("round-trips a To-do with its full content", async () => {
    const { db, scopeBinding } = await openPersonal("todo-cache-roundtrip");

    await cachePersonalTodos(db, scopeBinding, [todo]);

    expect(await listCachedPersonalTodos(db, scopeBinding)).toEqual([
      { ...todo, origin: "Server", serverId: todo.id, pendingLocalChange: false },
    ]);
    expect(await getCachedPersonalTodo(db, scopeBinding, todo.id)).toMatchObject({
      title: "Bayaran ang ospital",
      version: 4,
    });
  });

  it("stores the title, notes, times and related pointer only as ciphertext", async () => {
    const { db, scopeBinding } = await openPersonal("todo-cache-private");

    await cachePersonalTodos(db, scopeBinding, [todo]);

    const raw = JSON.stringify(await db.getAll("personalTodos"));
    expect(raw).not.toContain("Bayaran ang ospital");
    expect(raw).not.toContain("dalhin ang resibo");
    expect(raw).not.toContain("2026-09-01");
    expect(raw).not.toContain("2026-08-31");
    expect(raw).not.toContain("High");
    expect(raw).not.toContain("PersonalContact");
    expect(raw).not.toContain("22222222-2222-4222-8222-222222222222");
    // Lifecycle and sync bookkeeping stay readable by design.
    expect(raw).toContain("Open");
  });

  it("cannot be read with another Personal user's scope key", async () => {
    const mine = await openPersonal("todo-cache-owner");
    await cachePersonalTodos(mine.db, mine.scopeBinding, [todo]);

    const otherKey = personalScopeKey("todo-cache-intruder");
    expect(await listCachedPersonalTodos(mine.db, otherKey)).toEqual([]);
    expect(await getCachedPersonalTodo(mine.db, otherKey, todo.id)).toBeNull();
  });

  it("keeps one Personal user's to-dos out of another's database", async () => {
    const first = await openPersonal("todo-cache-user-one");
    const second = await openPersonal("todo-cache-user-two");

    await cachePersonalTodos(first.db, first.scopeBinding, [todo]);

    expect(await listCachedPersonalTodos(second.db, second.scopeBinding)).toEqual([]);
  });

  it("refuses to cache a private To-do in an Organization database", async () => {
    const scopeBinding = organizationScopeKey({
      userId: "org-user",
      organizationId: "33333333-3333-4333-8333-333333333333",
      branchId: "44444444-4444-4444-8444-444444444444",
      installationDeviceId: "55555555-5555-4555-8555-555555555555",
    });
    const db = await openOfflineDatabase("Organization", scopeBinding);

    await expect(cachePersonalTodos(db, scopeBinding, [todo])).rejects.toThrow(/scope mismatch/i);
    await expect(cacheLocalPersonalTodo(db, scopeBinding, todo)).rejects.toThrow(/scope mismatch/i);
    expect(await db.getAll("personalTodos")).toEqual([]);
  });

  it("does not let a server read overwrite a change still waiting in the outbox", async () => {
    const { db, scopeBinding } = await openPersonal("todo-cache-pending");

    await cachePersonalTodos(db, scopeBinding, [todo]);
    await applyLocalPersonalTodoChange(db, scopeBinding, todo.id, (current) => ({
      ...current,
      status: "Completed",
    }));

    // The server row this device just read predates the offline completion.
    await cachePersonalTodos(db, scopeBinding, [todo]);

    const cached = await getCachedPersonalTodo(db, scopeBinding, todo.id);
    expect(cached).toMatchObject({ status: "Completed", pendingLocalChange: true });
  });

  it("refuses to invent a To-do it has never seen", async () => {
    const { db, scopeBinding } = await openPersonal("todo-cache-missing");

    expect(
      await applyLocalPersonalTodoChange(db, scopeBinding, todo.id, (current) => current),
    ).toBeNull();
    expect(await db.getAll("personalTodos")).toEqual([]);
  });

  it("feeds the agenda tabs from the cache without decrypting twice", async () => {
    const { db, scopeBinding } = await openPersonal("todo-cache-tabs");
    const now = new Date("2026-08-15T12:00:00.000Z");

    await cachePersonalTodos(db, scopeBinding, [
      { ...todo, id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", dueAtUtc: "2026-08-01T00:00:00.000Z" },
      { ...todo, id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb", dueAtUtc: "2026-09-01T00:00:00.000Z" },
      {
        ...todo,
        id: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
        status: "Completed",
        completedAtUtc: "2026-08-10T00:00:00.000Z",
      },
    ]);

    const cached = await listCachedPersonalTodos(db, scopeBinding);
    expect(filterTodosByTab(cached, "overdue", now)).toHaveLength(1);
    expect(filterTodosByTab(cached, "upcoming", now)).toHaveLength(1);
    expect(filterTodosByTab(cached, "completed", now)).toHaveLength(1);
    expect(filterTodosByTab(cached, "open", now)).toHaveLength(2);
  });
});
