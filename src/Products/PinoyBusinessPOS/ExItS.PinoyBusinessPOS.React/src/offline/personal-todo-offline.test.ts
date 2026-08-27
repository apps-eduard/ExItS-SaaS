import "fake-indexeddb/auto";
import { describe, expect, it } from "vitest";
import type { PersonalTodoDto } from "@/api/platform/personal-todo-client";
import { decryptPayload, deriveScopeKeyFromBinding } from "@/offline/crypto";
import { openOfflineDatabase, organizationScopeKey, personalScopeKey } from "@/offline/db";
import { listOutbox, listSafeOutboxMetadata } from "@/offline/outbox";
import {
  cachePersonalTodos,
  getCachedPersonalTodo,
  listCachedPersonalTodos,
} from "@/offline/personal-todo-cache";
import {
  enqueuePersonalTodoCreate,
  enqueuePersonalTodoTransition,
  enqueuePersonalTodoUpdate,
  OfflinePersonalTodoRejectedError,
  rejectOfflineTodoShare,
} from "@/offline/personal-todo-offline";
import {
  collectLocalRefs,
  parseQueuedRequest,
  resolveLocalRefs,
  type QueuedRequestEnvelope,
} from "@/offline/queued-request";
import { mayAutoRetry, serverDedupeMode } from "@/offline/server-dedupe-policy";
import type { OfflineOperationRecord } from "@/offline/types";

const ownerUserIdentityId = "99999999-9999-4999-8999-999999999999";
const todoId = "11111111-1111-4111-8111-111111111111";
const operationId = "22222222-2222-4222-8222-222222222222";

const serverTodo: PersonalTodoDto = {
  id: todoId,
  ownerUserIdentityId,
  title: "Bayaran ang ospital",
  notes: "dalhin ang resibo",
  dueAtUtc: "2026-09-01T00:00:00.000Z",
  reminderAtUtc: "2026-08-31T00:00:00.000Z",
  reminderNotifiedAtUtc: null,
  priority: "High",
  status: "Open",
  relatedEntityType: null,
  relatedEntityId: null,
  createdAtUtc: "2026-08-01T00:00:00.000Z",
  updatedAtUtc: "2026-08-02T00:00:00.000Z",
  completedAtUtc: null,
  version: 4,
};

async function openPersonal(userId: string) {
  const scopeBinding = personalScopeKey(userId);
  const db = await openOfflineDatabase("Personal", scopeBinding);
  return { db, scopeBinding, userId };
}

async function decryptRequest(
  record: OfflineOperationRecord,
  scopeBinding: string,
): Promise<QueuedRequestEnvelope | null> {
  const key = await deriveScopeKeyFromBinding(scopeBinding);
  const plaintext = await decryptPayload(
    key,
    { ciphertext: record.ciphertext, iv: record.iv },
    `${record.scopeKind}|${record.operationType}|${record.operationId}`,
  );
  return parseQueuedRequest(new TextDecoder().decode(plaintext));
}

describe("RMAP-21G Personal To-do offline queue", () => {
  it("queues a To-do against the Platform API and shows it immediately", async () => {
    const scope = await openPersonal("todo-create");

    const { operation, todo } = await enqueuePersonalTodoCreate({
      ...scope,
      todoId,
      ownerUserIdentityId,
      todo: {
        title: "  Bayaran ang ospital  ",
        notes: "dalhin ang resibo",
        dueAtUtc: "2026-09-01T00:00:00.000Z",
        priority: "High",
      },
    }, { allowOfflineEngine: true });

    expect(operation.scopeKind).toBe("Personal");
    expect(operation.organizationId).toBeNull();
    expect(operation.productDomain).toBe("personal.todo");
    expect(todo.title).toBe("Bayaran ang ospital");
    expect(todo.status).toBe("Open");

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request).toEqual({
      api: "platform",
      method: "POST",
      path: "/api/v1/personal/todos",
      body: {
        title: "Bayaran ang ospital",
        notes: "dalhin ang resibo",
        dueAtUtc: "2026-09-01T00:00:00.000Z",
        reminderAtUtc: null,
        priority: "High",
        relatedEntityType: null,
        relatedEntityId: null,
      },
    });

    const cached = await listCachedPersonalTodos(scope.db, scope.scopeBinding);
    expect(cached).toHaveLength(1);
    expect(cached[0]).toMatchObject({ origin: "Local", pendingLocalChange: true });
    expect(cached[0].serverId).toBeNull();
  });

  it("drops a related-entity id when no related type was chosen", async () => {
    const scope = await openPersonal("todo-related");

    const { operation } = await enqueuePersonalTodoCreate({
      ...scope,
      todoId,
      ownerUserIdentityId,
      todo: {
        title: "Tawagan si Nena",
        relatedEntityType: null,
        relatedEntityId: "33333333-3333-4333-8333-333333333333",
      },
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.body).toMatchObject({ relatedEntityType: null, relatedEntityId: null });
  });

  it("refuses a titleless To-do and refuses sharing offline", async () => {
    const scope = await openPersonal("todo-invalid");

    await expect(
      enqueuePersonalTodoCreate({
        ...scope,
        todoId,
        ownerUserIdentityId,
        todo: { title: "   " },
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.personal.todo.title_required" });

    await expect(
      enqueuePersonalTodoCreate({
        ...scope,
        todoId,
        ownerUserIdentityId,
        todo: { title: "Share this" },
        shareWithUserIdentityId: "44444444-4444-4444-8444-444444444444",
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.personal.todo.share_not_supported" });

    expect(() => rejectOfflineTodoShare()).toThrow(OfflinePersonalTodoRejectedError);
    expect(await listOutbox(scope.db)).toEqual([]);
  });

  it("queues an edit that still carries the version the person saw", async () => {
    const scope = await openPersonal("todo-update");
    await cachePersonalTodos(scope.db, scope.scopeBinding, [serverTodo]);

    const { operation, todo } = await enqueuePersonalTodoUpdate({
      ...scope,
      operationId,
      todoId,
      todo: { title: "Bayaran bukas", notes: null, priority: "Normal" },
      expectedVersion: 4,
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.method).toBe("PUT");
    expect(request?.path).toBe(`/api/v1/personal/todos/${todoId}`);
    expect(request?.body).toMatchObject({
      title: "Bayaran bukas",
      // An edit replaces content the person may not have seen, so a stale edit stays rejectable.
      expectedVersion: 4,
    });

    expect(todo.title).toBe("Bayaran bukas");
    expect(todo.pendingLocalChange).toBe(true);
    // A local change does not advance the server's version.
    const row = await scope.db.get("personalTodos", todoId);
    expect(row?.version).toBe(4);
  });

  it("refuses an edit of a To-do this device never cached", async () => {
    const scope = await openPersonal("todo-update-uncached");

    await expect(
      enqueuePersonalTodoUpdate({
        ...scope,
        operationId,
        todoId,
        todo: { title: "Invented from blank fields" },
        expectedVersion: null,
      }, { allowOfflineEngine: true }),
    ).rejects.toMatchObject({ code: "offline.personal.todo.not_cached" });

    expect(await listOutbox(scope.db)).toEqual([]);
  });

  it("queues complete as a target state without pinning a version", async () => {
    const scope = await openPersonal("todo-complete");
    await cachePersonalTodos(scope.db, scope.scopeBinding, [serverTodo]);

    const { operation, todo } = await enqueuePersonalTodoTransition({
      ...scope,
      operationId,
      todoId,
      transition: "complete",
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.path).toBe(`/api/v1/personal/todos/${todoId}/complete`);
    // "I finished this" is a target state, so a version read hours ago must not reject it.
    expect(request?.body).toEqual({ expectedVersion: null });
    expect(todo.status).toBe("Completed");
    expect(todo.completedAtUtc).not.toBeNull();

    const cached = await getCachedPersonalTodo(scope.db, scope.scopeBinding, todoId);
    expect(cached).toMatchObject({ status: "Completed", pendingLocalChange: true });
  });

  it("queues cancel as the delete-equivalent the API offers", async () => {
    const scope = await openPersonal("todo-cancel");
    await cachePersonalTodos(scope.db, scope.scopeBinding, [serverTodo]);

    const { operation, todo } = await enqueuePersonalTodoTransition({
      ...scope,
      operationId,
      todoId,
      transition: "cancel",
    }, { allowOfflineEngine: true });

    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.path).toBe(`/api/v1/personal/todos/${todoId}/cancel`);
    expect(todo.status).toBe("Cancelled");
    expect(operation.operationType).toBe("personal.todo.cancel");
  });

  it("queues reopen and clears the local completion", async () => {
    const scope = await openPersonal("todo-reopen");
    await cachePersonalTodos(scope.db, scope.scopeBinding, [
      { ...serverTodo, status: "Completed", completedAtUtc: "2026-08-03T00:00:00.000Z" },
    ]);

    const { operation, todo } = await enqueuePersonalTodoTransition({
      ...scope,
      operationId,
      todoId,
      transition: "reopen",
    }, { allowOfflineEngine: true });

    expect(operation.operationType).toBe("personal.todo.reopen");
    expect(todo.status).toBe("Open");
    expect(todo.completedAtUtc).toBeNull();
  });

  it("completes a To-do that was itself created offline through a placeholder", async () => {
    const scope = await openPersonal("todo-local-then-complete");

    await enqueuePersonalTodoCreate({
      ...scope,
      todoId,
      ownerUserIdentityId,
      todo: { title: "Created offline" },
    }, { allowOfflineEngine: true });

    const { operation } = await enqueuePersonalTodoTransition({
      ...scope,
      operationId,
      todoId,
      todoIsLocal: true,
      dependsOnTodoOperationId: todoId,
      transition: "complete",
    }, { allowOfflineEngine: true });

    expect(operation.dependsOnOperationId).toBe(todoId);
    const request = await decryptRequest(operation, scope.scopeBinding);
    expect(request?.path).toBe(`/api/v1/personal/todos/{{local:${todoId}}}/complete`);
    expect(collectLocalRefs(request!)).toEqual([todoId]);

    const serverTodoId = "55555555-5555-4555-8555-555555555555";
    const resolved = resolveLocalRefs(request!, () => serverTodoId);
    expect(resolved.resolved).toBe(true);
    if (resolved.resolved) {
      expect(resolved.envelope.path).toBe(`/api/v1/personal/todos/${serverTodoId}/complete`);
    }
  });

  it("refuses to write a private To-do into an Organization store", async () => {
    const scopeBinding = organizationScopeKey({
      userId: "org-user",
      organizationId: "66666666-6666-4666-8666-666666666666",
      branchId: "77777777-7777-4777-8777-777777777777",
      installationDeviceId: "88888888-8888-4888-8888-888888888888",
    });
    const db = await openOfflineDatabase("Organization", scopeBinding);

    await expect(
      enqueuePersonalTodoCreate({
        db,
        scopeBinding,
        userId: "org-user",
        todoId,
        ownerUserIdentityId,
        todo: { title: "Should never land here" },
      }, { allowOfflineEngine: true }),
    ).rejects.toThrow(/scope mismatch/i);

    expect(await listOutbox(db)).toEqual([]);
  });

  it("keeps To-do content out of the safe sync metadata", async () => {
    const scope = await openPersonal("todo-metadata");

    await enqueuePersonalTodoCreate({
      ...scope,
      todoId,
      ownerUserIdentityId,
      todo: { title: "Bayaran ang ospital", notes: "dalhin ang resibo" },
    }, { allowOfflineEngine: true });

    const serialized = JSON.stringify(await listSafeOutboxMetadata(scope.db));
    expect(serialized).not.toContain("ospital");
    expect(serialized).not.toContain("resibo");
  });
});

describe("RMAP-21G To-do dedupe policy", () => {
  it("treats the four transitions as target-state and the create as duplicable", () => {
    expect(serverDedupeMode("personal.todo.create")).toBe("none");
    expect(serverDedupeMode("personal.todo.update")).toBe("target-state");
    expect(serverDedupeMode("personal.todo.complete")).toBe("target-state");
    expect(serverDedupeMode("personal.todo.reopen")).toBe("target-state");
    expect(serverDedupeMode("personal.todo.cancel")).toBe("target-state");
  });

  it("auto-retries a converging transition but never a create whose outcome is unknown", () => {
    expect(mayAutoRetry("personal.todo.complete", "ambiguous-transport")).toBe(true);
    expect(mayAutoRetry("personal.todo.update", "ambiguous-transport")).toBe(true);
    expect(mayAutoRetry("personal.todo.create", "ambiguous-transport")).toBe(false);
    expect(mayAutoRetry("personal.todo.create", "not-dispatched")).toBe(true);
  });
});
