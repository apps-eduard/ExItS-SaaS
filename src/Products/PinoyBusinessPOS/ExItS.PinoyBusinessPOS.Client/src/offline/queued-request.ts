/**
 * Encrypted outbox payload shape for operations queued from RMAP-21E onwards.
 *
 * A queued operation stores the exact HTTP request it will replay, so the sync processor never
 * has to re-derive money, a route, or a scope from UI state that no longer exists. Payload
 * version 1 (the RMAP-21D Cash sale) stores only the server body and the processor supplies the
 * route; version 2 stores this envelope.
 */
export const QUEUED_REQUEST_PAYLOAD_VERSION = 2 as const;

/** Which API origin replays the request. Personal work must never replay against the POS API. */
export type QueuedRequestApi = "pos" | "platform";

export type QueuedRequestMethod = "POST" | "PUT" | "PATCH" | "DELETE";

export type QueuedRequestEnvelope = {
  api: QueuedRequestApi;
  method: QueuedRequestMethod;
  /** Relative path under the API base path — never an absolute URL. */
  path: string;
  body?: unknown;
};

export function buildQueuedRequestEnvelope(envelope: QueuedRequestEnvelope): QueuedRequestEnvelope {
  if (/^https?:\/\//i.test(envelope.path) || envelope.path.includes("://")) {
    throw new Error("Queued offline requests must stay on a relative API path.");
  }
  if (!envelope.path.startsWith("/")) {
    throw new Error("Queued offline request paths must start with '/'.");
  }
  return envelope;
}

export function serializeQueuedRequest(envelope: QueuedRequestEnvelope): string {
  return JSON.stringify(buildQueuedRequestEnvelope(envelope));
}

function isQueuedRequestMethod(value: unknown): value is QueuedRequestMethod {
  return value === "POST" || value === "PUT" || value === "PATCH" || value === "DELETE";
}

/** Parse a decrypted payload back into a replayable request, or null when it is not trustworthy. */
export function parseQueuedRequest(plaintextJson: string): QueuedRequestEnvelope | null {
  let raw: unknown;
  try {
    raw = JSON.parse(plaintextJson);
  } catch {
    return null;
  }
  if (typeof raw !== "object" || raw === null) {
    return null;
  }
  const record = raw as Record<string, unknown>;
  if (record.api !== "pos" && record.api !== "platform") {
    return null;
  }
  if (!isQueuedRequestMethod(record.method)) {
    return null;
  }
  if (typeof record.path !== "string" || !record.path.startsWith("/")) {
    return null;
  }
  if (record.path.includes("://")) {
    return null;
  }
  return { api: record.api, method: record.method, path: record.path, body: record.body };
}
