/**
 * Which queued operations the server can deduplicate, and what the sync processor is therefore
 * allowed to do after a failure it cannot classify (RMAP-21F).
 *
 * POS money routes accept a client-chosen entity id plus `Idempotency-Key`, so replaying one is
 * safe: the second request lands on the same row. The Platform Personal routes have neither — the
 * server mints the id and there is no idempotency store — so a replay after an ambiguous failure
 * would create a second contact, a second debt, or a second payment against a friend.
 *
 * A browser cannot tell "the request never left the device" apart from "the server committed and
 * the response was lost". For an operation with no server-side dedupe, the safe answer is to stop
 * and ask the person rather than to guess, so this policy is what keeps auto-retry honest.
 */

export type ServerDedupeMode =
  /** Server matches a replay on `Idempotency-Key` + client entity id. Auto-retry is safe. */
  | "idempotency-key"
  /**
   * The request assigns a state on a row addressed by its own id, so a replay converges on the
   * same result instead of adding a second row. Auto-retry is safe even without an idempotency
   * store — worst case the server answers "already completed" (RMAP-21G).
   */
  | "target-state"
  /** No server dedupe. A replay may duplicate, so an ambiguous failure needs a human. */
  | "none";

/** Personal Utang operation types (RMAP-21F). */
export const PERSONAL_OPERATION_TYPES = {
  ContactCreate: "personal.contact.create",
  RelationshipCreate: "personal.utang.relationship.create",
  EntryRecord: "personal.utang.entry.record",
} as const;

/** Personal To-do operation types (RMAP-21G). */
export const PERSONAL_TODO_OPERATION_TYPES = {
  TodoCreate: "personal.todo.create",
  TodoUpdate: "personal.todo.update",
  TodoComplete: "personal.todo.complete",
  TodoReopen: "personal.todo.reopen",
  TodoCancel: "personal.todo.cancel",
} as const;

const NO_SERVER_DEDUPE = new Set<string>([
  PERSONAL_OPERATION_TYPES.ContactCreate,
  PERSONAL_OPERATION_TYPES.RelationshipCreate,
  PERSONAL_OPERATION_TYPES.EntryRecord,
  // The server mints the To-do id, so a replayed create makes a second To-do.
  PERSONAL_TODO_OPERATION_TYPES.TodoCreate,
]);

const TARGET_STATE = new Set<string>([
  PERSONAL_TODO_OPERATION_TYPES.TodoUpdate,
  PERSONAL_TODO_OPERATION_TYPES.TodoComplete,
  PERSONAL_TODO_OPERATION_TYPES.TodoReopen,
  PERSONAL_TODO_OPERATION_TYPES.TodoCancel,
]);

export function serverDedupeMode(operationType: string): ServerDedupeMode {
  if (NO_SERVER_DEDUPE.has(operationType)) {
    return "none";
  }
  if (TARGET_STATE.has(operationType)) {
    return "target-state";
  }
  return "idempotency-key";
}

/** Failure the processor observed for one attempt, before deciding the next queue state. */
export type AttemptFailureKind =
  /** The request was never dispatched — still offline, or the fetch was refused up front. */
  | "not-dispatched"
  /** Dispatched, but no response came back. The server may or may not have committed. */
  | "ambiguous-transport"
  /** A response arrived: the server saw the request and answered. */
  | "server-responded";

/**
 * Whether the processor may send this operation again on its own.
 *
 * An `ambiguous-transport` failure is the dangerous one: with no server dedupe, retrying could
 * double a debt, so the operation is parked for the person to confirm instead.
 */
export function mayAutoRetry(operationType: string, failure: AttemptFailureKind): boolean {
  if (failure === "not-dispatched") {
    return true;
  }
  if (failure === "ambiguous-transport") {
    return serverDedupeMode(operationType) !== "none";
  }
  return false;
}
