import type { PersonalTodoDto } from "@/api/platform/personal-todo-client";
import { decryptPayload, encryptPayload } from "@/offline/crypto";
import {
  getActiveOfflineCryptoKeyForScope,
  OfflineCryptoLockedError,
} from "@/offline/local-store-key";
import { assertOfflineScope, type OfflineDb } from "@/offline/db";
import type { CachedPersonalTodoRecord } from "@/offline/types";

/**
 * Private-by-default Personal To-do cache (RMAP-21G).
 *
 * "Private by default" is the whole design: a To-do can say "pay the hospital on Friday", so the
 * title, notes, due and reminder times, priority and every related-entity pointer are AES-GCM
 * sealed under the Personal scope key. Only the local id, the lifecycle status, the row version and
 * the sync bookkeeping stay readable, because that is all the agenda tabs and the outbox need.
 *
 * Sharing is not part of this store. Nothing here grants anybody else access to a To-do, and no
 * reminder is ever raised from cached data — a reminder time is only a field the server owns.
 */

const TODO_AAD = "personal-todo-cache";

function aad(todoId: string): string {
  return `${TODO_AAD}|${todoId}`;
}

async function seal(scopeBinding: string, todo: PersonalTodoDto) {
  const key = await getActiveOfflineCryptoKeyForScope(scopeBinding);
  return encryptPayload(key, new TextEncoder().encode(JSON.stringify(todo)), aad(todo.id));
}

async function unseal(
  scopeBinding: string,
  row: CachedPersonalTodoRecord,
): Promise<PersonalTodoDto | null> {
  try {
    const key = await getActiveOfflineCryptoKeyForScope(scopeBinding);
    const plaintext = await decryptPayload(
      key,
      { ciphertext: row.ciphertext, iv: row.iv },
      aad(row.todoId),
    );
    return JSON.parse(new TextDecoder().decode(plaintext)) as PersonalTodoDto;
  } catch {
    // Wrong scope key, tampered row, or corrupt envelope — drop the row rather than guess.
    return null;
  }
}

export type CachedPersonalTodo = PersonalTodoDto & {
  origin: "Server" | "Local";
  serverId: string | null;
  /** True while a local create, edit, or transition is still queued. */
  pendingLocalChange: boolean;
};

async function requirePersonalScope(db: OfflineDb): Promise<void> {
  await assertOfflineScope(db, "Personal");
}

async function toRecord(
  scopeBinding: string,
  todo: PersonalTodoDto,
  fields: Pick<CachedPersonalTodoRecord, "origin" | "serverId" | "version" | "pendingLocalChange">,
): Promise<CachedPersonalTodoRecord> {
  const envelope = await seal(scopeBinding, todo);
  return {
    todoId: todo.id,
    serverId: fields.serverId,
    origin: fields.origin,
    status: todo.status,
    version: fields.version,
    pendingLocalChange: fields.pendingLocalChange,
    updatedAtUtc: todo.updatedAtUtc,
    cachedAtUtc: new Date().toISOString(),
    ciphertext: envelope.ciphertext,
    iv: envelope.iv,
  };
}

/**
 * Merge a fetched agenda into the cache.
 *
 * A To-do with a queued local change is left alone: the server row this device just read predates
 * the change the person made offline, so overwriting it would make their edit disappear from the
 * list while it is still waiting in the outbox.
 */
export async function cachePersonalTodos(
  db: OfflineDb,
  scopeBinding: string,
  todos: ReadonlyArray<PersonalTodoDto>,
): Promise<void> {
  await requirePersonalScope(db);
  if (todos.length === 0) {
    return;
  }
  try {
    const records = await Promise.all(
      todos.map(async (todo) => {
        const existing = await db.get("personalTodos", todo.id);
        if (existing?.pendingLocalChange) {
          return null;
        }
        return toRecord(scopeBinding, todo, {
          origin: "Server",
          serverId: todo.id,
          version: todo.version,
          pendingLocalChange: false,
        });
      }),
    );
    const tx = db.transaction("personalTodos", "readwrite");
    for (const record of records) {
      if (record) {
        await tx.store.put(record);
      }
    }
    await tx.done;
  } catch (error) {
    if (error instanceof OfflineCryptoLockedError) {
      return;
    }
    throw error;
  }
}

export async function cachePersonalTodo(
  db: OfflineDb,
  scopeBinding: string,
  todo: PersonalTodoDto,
): Promise<void> {
  await cachePersonalTodos(db, scopeBinding, [todo]);
}

/** Optimistic row for a To-do created on this device — no server id yet. */
export async function cacheLocalPersonalTodo(
  db: OfflineDb,
  scopeBinding: string,
  todo: PersonalTodoDto,
): Promise<void> {
  await requirePersonalScope(db);
  await db.put(
    "personalTodos",
    await toRecord(scopeBinding, todo, {
      origin: "Local",
      serverId: null,
      version: null,
      pendingLocalChange: true,
    }),
  );
}

/**
 * Apply a queued edit or status transition to the cached row.
 *
 * The row keeps its `version`, because the version still belongs to the server: a local change
 * does not advance it. Returns null when there is no readable cached row to change, so a caller
 * can refuse rather than invent one.
 */
export async function applyLocalPersonalTodoChange(
  db: OfflineDb,
  scopeBinding: string,
  todoId: string,
  change: (todo: PersonalTodoDto) => PersonalTodoDto,
): Promise<CachedPersonalTodo | null> {
  await requirePersonalScope(db);
  const existing = await db.get("personalTodos", todoId);
  if (!existing) {
    return null;
  }
  const current = await unseal(scopeBinding, existing);
  if (!current) {
    return null;
  }
  const next = change(current);
  await db.put(
    "personalTodos",
    await toRecord(scopeBinding, next, {
      origin: existing.origin,
      serverId: existing.serverId,
      version: existing.version,
      pendingLocalChange: true,
    }),
  );
  return {
    ...next,
    origin: existing.origin,
    serverId: existing.serverId,
    pendingLocalChange: true,
  };
}

export async function listCachedPersonalTodos(
  db: OfflineDb,
  scopeBinding: string,
): Promise<CachedPersonalTodo[]> {
  let rows: CachedPersonalTodoRecord[];
  try {
    rows = await db.getAll("personalTodos");
  } catch {
    return [];
  }
  const decrypted = await Promise.all(
    rows.map(async (row) => {
      const todo = await unseal(scopeBinding, row);
      return todo
        ? {
            ...todo,
            origin: row.origin,
            serverId: row.serverId,
            pendingLocalChange: row.pendingLocalChange,
          }
        : null;
    }),
  );
  return decrypted
    .filter((todo): todo is CachedPersonalTodo => todo != null)
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc));
}

export async function getCachedPersonalTodo(
  db: OfflineDb,
  scopeBinding: string,
  todoId: string,
): Promise<CachedPersonalTodo | null> {
  try {
    const row = await db.get("personalTodos", todoId);
    if (!row) {
      return null;
    }
    const todo = await unseal(scopeBinding, row);
    return todo
      ? {
          ...todo,
          origin: row.origin,
          serverId: row.serverId,
          pendingLocalChange: row.pendingLocalChange,
        }
      : null;
  } catch {
    return null;
  }
}
