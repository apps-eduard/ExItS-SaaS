import type {
  CreatePersonalTodoRequest,
  PersonalTodoDto,
} from "@/api/platform/personal-todo-client";
import { assertOfflineScope, type OfflineDb } from "@/offline/db";
import { enqueueEncryptedOperation } from "@/offline/outbox";
import {
  applyLocalPersonalTodoChange,
  cacheLocalPersonalTodo,
  type CachedPersonalTodo,
} from "@/offline/personal-todo-cache";
import {
  localRefToken,
  QUEUED_REQUEST_PAYLOAD_VERSION,
  serializeQueuedRequest,
} from "@/offline/queued-request";
import { PERSONAL_TODO_OPERATION_TYPES } from "@/offline/server-dedupe-policy";
import type { OfflineOperationRecord } from "@/offline/types";
import {
  guardPersonalWebOfflineEnqueue,
  type PersonalOfflineEnqueueRuntimeOptions,
} from "@/runtime/personal-web-runtime-policy";

/**
 * Offline Personal To-do engine (RMAP-21G).
 *
 * Personal Web/PWA (PERS-WEB-ONLINE-ONLY-01) does not activate this path for new user actions —
 * enqueue wrappers call `guardPersonalWebOfflineEnqueue`. Engine unit tests and future Capacitor
 * may pass `{ allowOfflineEngine: true }`.
 *
 * Engine-capable operations (preserved for native):
 *   - personal.todo.create    (POST /todos)
 *   - personal.todo.update    (PUT /todos/{id})
 *   - personal.todo.complete  (POST /todos/{id}/complete)
 *   - personal.todo.reopen    (POST /todos/{id}/reopen)
 *   - personal.todo.cancel    (POST /todos/{id}/cancel)
 *
 * The four transitions address an existing row by its own id and assign a target state, so a
 * replay converges rather than duplicating — see `serverDedupeMode`. Only `create` can duplicate,
 * because the server mints the To-do id.
 *
 * Not offline: sharing or assigning a To-do to anybody else (the API has no such route to prove
 * safe) and raising a reminder. A device must never invent a notification the server did not send.
 *
 * The platform To-do API has no hard delete. Cancel is the delete-equivalent the server offers, so
 * that is what "delete offline" queues.
 */

export const PERSONAL_TODO_PRODUCT_DOMAIN = "personal.todo";

const TODOS_PATH = "/api/v1/personal/todos";

export type OfflinePersonalTodoRejectionCode =
  | "offline.personal.todo.title_required"
  | "offline.personal.todo.not_cached"
  | "offline.personal.todo.share_not_supported"
  | "offline.personal.todo.id_required";

export class OfflinePersonalTodoRejectedError extends Error {
  readonly code: OfflinePersonalTodoRejectionCode;

  constructor(code: OfflinePersonalTodoRejectionCode, message: string) {
    super(message);
    this.name = "OfflinePersonalTodoRejectedError";
    this.code = code;
  }
}

export type PersonalTodoOfflineScope = {
  db: OfflineDb;
  scopeBinding: string;
  userId: string;
};

async function todoScopeFields(scope: PersonalTodoOfflineScope) {
  // A private To-do must never be written into an Organization outbox.
  await assertOfflineScope(scope.db, "Personal");
  return {
    db: scope.db,
    scopeKind: "Personal" as const,
    scopeBinding: scope.scopeBinding,
    userId: scope.userId,
    organizationId: null,
    branchId: null,
    installationDeviceId: null,
    posDeviceId: null,
    productDomain: PERSONAL_TODO_PRODUCT_DOMAIN,
    payloadVersion: QUEUED_REQUEST_PAYLOAD_VERSION,
  };
}

function localIdempotencyKey(localId: string): string {
  return localId.replace(/-/g, "").toLowerCase();
}

/** A To-do created offline is addressed by a placeholder until its server id is known. */
function todoRef(todoId: string, isLocal: boolean | undefined): string {
  return isLocal ? localRefToken(todoId) : todoId;
}

export type EnqueuePersonalTodoCreateInput = PersonalTodoOfflineScope & {
  /** Local id, replaced by the server id once the queued To-do posts. */
  todoId: string;
  todo: CreatePersonalTodoRequest;
  ownerUserIdentityId: string;
  /** Present only so an offline attempt to share is refused instead of dropped. */
  shareWithUserIdentityId?: string | null;
};

export type EnqueuedPersonalTodo = {
  operation: OfflineOperationRecord;
  todo: PersonalTodoDto;
};

export async function enqueuePersonalTodoCreate(
  input: EnqueuePersonalTodoCreateInput,
  options?: PersonalOfflineEnqueueRuntimeOptions,
): Promise<EnqueuedPersonalTodo> {
  guardPersonalWebOfflineEnqueue(options);
  if (input.shareWithUserIdentityId) {
    throw new OfflinePersonalTodoRejectedError(
      "offline.personal.todo.share_not_supported",
      "Sharing a to-do requires an internet connection.",
    );
  }
  const title = input.todo.title.trim();
  if (!title) {
    throw new OfflinePersonalTodoRejectedError(
      "offline.personal.todo.title_required",
      "A to-do needs a title before it can be saved on this device.",
    );
  }

  const scope = await todoScopeFields(input);
  const body: CreatePersonalTodoRequest = {
    title,
    notes: input.todo.notes ?? null,
    dueAtUtc: input.todo.dueAtUtc ?? null,
    reminderAtUtc: input.todo.reminderAtUtc ?? null,
    priority: input.todo.priority ?? "None",
    relatedEntityType: input.todo.relatedEntityType ?? null,
    relatedEntityId: input.todo.relatedEntityType ? (input.todo.relatedEntityId ?? null) : null,
  };

  const operation = await enqueueEncryptedOperation({
    ...scope,
    operationType: PERSONAL_TODO_OPERATION_TYPES.TodoCreate,
    operationId: input.todoId,
    idempotencyKey: localIdempotencyKey(input.todoId),
    plaintextJson: serializeQueuedRequest({
      api: "platform",
      method: "POST",
      path: TODOS_PATH,
      body,
    }),
    entityLocalId: input.todoId,
  });

  const todo: PersonalTodoDto = {
    id: input.todoId,
    ownerUserIdentityId: input.ownerUserIdentityId,
    title,
    notes: body.notes ?? null,
    dueAtUtc: body.dueAtUtc ?? null,
    reminderAtUtc: body.reminderAtUtc ?? null,
    reminderNotifiedAtUtc: null,
    priority: body.priority ?? "None",
    status: "Open",
    relatedEntityType: body.relatedEntityType ?? null,
    relatedEntityId: body.relatedEntityId ?? null,
    createdAtUtc: operation.createdAt,
    updatedAtUtc: operation.createdAt,
    completedAtUtc: null,
    version: 0,
  };
  await cacheLocalPersonalTodo(input.db, input.scopeBinding, todo);
  return { operation, todo };
}

export type EnqueuePersonalTodoUpdateInput = PersonalTodoOfflineScope & {
  /** Stable id for this edit attempt, so two offline edits do not share one queue row. */
  operationId: string;
  todoId: string;
  todoIsLocal?: boolean;
  dependsOnTodoOperationId?: string | null;
  todo: CreatePersonalTodoRequest;
  /**
   * Version the person was actually looking at. Sent so the server can reject a stale edit rather
   * than let it quietly overwrite a newer one — unlike the status transitions below, an edit
   * replaces content the person may not have seen.
   */
  expectedVersion: number | null;
};

export async function enqueuePersonalTodoUpdate(
  input: EnqueuePersonalTodoUpdateInput,
  options?: PersonalOfflineEnqueueRuntimeOptions,
): Promise<{ operation: OfflineOperationRecord; todo: CachedPersonalTodo }> {
  guardPersonalWebOfflineEnqueue(options);
  const title = input.todo.title.trim();
  if (!title) {
    throw new OfflinePersonalTodoRejectedError(
      "offline.personal.todo.title_required",
      "A to-do needs a title before it can be saved on this device.",
    );
  }

  const scope = await todoScopeFields(input);
  const body = {
    title,
    notes: input.todo.notes ?? null,
    dueAtUtc: input.todo.dueAtUtc ?? null,
    reminderAtUtc: input.todo.reminderAtUtc ?? null,
    priority: input.todo.priority ?? "None",
    relatedEntityType: input.todo.relatedEntityType ?? null,
    relatedEntityId: input.todo.relatedEntityType ? (input.todo.relatedEntityId ?? null) : null,
    expectedVersion: input.expectedVersion,
  };

  // Refuse rather than queue an edit built from fields this device never read.
  const applied = await applyLocalPersonalTodoChange(
    input.db,
    input.scopeBinding,
    input.todoId,
    (current) => ({
      ...current,
      title,
      notes: body.notes,
      dueAtUtc: body.dueAtUtc,
      reminderAtUtc: body.reminderAtUtc,
      reminderNotifiedAtUtc:
        current.reminderAtUtc === body.reminderAtUtc ? current.reminderNotifiedAtUtc : null,
      priority: body.priority ?? current.priority,
      relatedEntityType: body.relatedEntityType,
      relatedEntityId: body.relatedEntityId,
      updatedAtUtc: new Date().toISOString(),
    }),
  );
  if (!applied) {
    throw new OfflinePersonalTodoRejectedError(
      "offline.personal.todo.not_cached",
      "This to-do is not saved on this device.",
    );
  }

  const operation = await enqueueEncryptedOperation({
    ...scope,
    operationType: PERSONAL_TODO_OPERATION_TYPES.TodoUpdate,
    operationId: input.operationId,
    idempotencyKey: localIdempotencyKey(input.operationId),
    plaintextJson: serializeQueuedRequest({
      api: "platform",
      method: "PUT",
      path: `${TODOS_PATH}/${todoRef(input.todoId, input.todoIsLocal)}`,
      body,
    }),
    dependsOnOperationId: input.dependsOnTodoOperationId ?? null,
    entityLocalId: input.todoId,
  });

  return { operation, todo: applied };
}

export type PersonalTodoTransition = "complete" | "reopen" | "cancel";

export type EnqueuePersonalTodoTransitionInput = PersonalTodoOfflineScope & {
  /** Stable id for this transition attempt. */
  operationId: string;
  todoId: string;
  todoIsLocal?: boolean;
  dependsOnTodoOperationId?: string | null;
  transition: PersonalTodoTransition;
};

const TRANSITION_OPERATION_TYPE: Record<PersonalTodoTransition, string> = {
  complete: PERSONAL_TODO_OPERATION_TYPES.TodoComplete,
  reopen: PERSONAL_TODO_OPERATION_TYPES.TodoReopen,
  cancel: PERSONAL_TODO_OPERATION_TYPES.TodoCancel,
};

const TRANSITION_STATUS: Record<PersonalTodoTransition, string> = {
  complete: "Completed",
  reopen: "Open",
  cancel: "Cancelled",
};

/**
 * Queue a status transition.
 *
 * `expectedVersion` is deliberately omitted here. "I finished this" is a target state, not a change
 * to content the person might not have seen, so pinning a version read hours ago would only reject
 * a true intention. The transition addresses the To-do by its own id, so a replay converges.
 */
export async function enqueuePersonalTodoTransition(
  input: EnqueuePersonalTodoTransitionInput,
  options?: PersonalOfflineEnqueueRuntimeOptions,
): Promise<{ operation: OfflineOperationRecord; todo: CachedPersonalTodo }> {
  guardPersonalWebOfflineEnqueue(options);
  if (!input.todoId.trim()) {
    throw new OfflinePersonalTodoRejectedError(
      "offline.personal.todo.id_required",
      "This action needs the to-do it belongs to.",
    );
  }

  const scope = await todoScopeFields(input);
  const status = TRANSITION_STATUS[input.transition];
  const now = new Date().toISOString();

  const applied = await applyLocalPersonalTodoChange(
    input.db,
    input.scopeBinding,
    input.todoId,
    (current) => ({
      ...current,
      status,
      completedAtUtc: input.transition === "complete" ? now : null,
      updatedAtUtc: now,
    }),
  );
  if (!applied) {
    throw new OfflinePersonalTodoRejectedError(
      "offline.personal.todo.not_cached",
      "This to-do is not saved on this device.",
    );
  }

  const operation = await enqueueEncryptedOperation({
    ...scope,
    operationType: TRANSITION_OPERATION_TYPE[input.transition],
    operationId: input.operationId,
    idempotencyKey: localIdempotencyKey(input.operationId),
    plaintextJson: serializeQueuedRequest({
      api: "platform",
      method: "POST",
      path: `${TODOS_PATH}/${todoRef(input.todoId, input.todoIsLocal)}/${input.transition}`,
      body: { expectedVersion: null },
    }),
    dependsOnOperationId: input.dependsOnTodoOperationId ?? null,
    entityLocalId: input.todoId,
  });

  return { operation, todo: applied };
}

/**
 * Sharing a To-do stays online. Kept as an explicit refusal because the platform API exposes no
 * share or assign route at all, so there is nothing to prove safe and nothing to approximate.
 */
export function rejectOfflineTodoShare(): never {
  throw new OfflinePersonalTodoRejectedError(
    "offline.personal.todo.share_not_supported",
    "Sharing a to-do requires an internet connection.",
  );
}
