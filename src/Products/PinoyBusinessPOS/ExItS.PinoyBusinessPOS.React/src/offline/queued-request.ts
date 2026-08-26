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

/**
 * A relationship queued offline points at a contact that does not exist on the server yet, so the
 * queued request carries a placeholder instead of a real id. The sync processor rewrites every
 * placeholder from the entity map right before replay, and refuses to send while any is unresolved.
 *
 * Keeping the marker inside the payload means the dependency travels with the encrypted request
 * rather than living in UI state that a reload would lose.
 */
/** A placeholder can be a whole body value or one segment of a path, so matching is not anchored. */
const LOCAL_REF_PATTERN = /\{\{local:([^{}]+)\}\}/g;

export function localRefToken(localId: string): string {
  if (!localId || localId.includes("{{") || localId.includes("}}")) {
    throw new Error("A local reference token needs a plain local id.");
  }
  return `{{local:${localId}}}`;
}

function walk(value: unknown, visit: (localId: string) => string): unknown {
  if (typeof value === "string") {
    return value.replace(LOCAL_REF_PATTERN, (_token, localId: string) => visit(localId));
  }
  if (Array.isArray(value)) {
    return value.map((item) => walk(item, visit));
  }
  if (typeof value === "object" && value !== null) {
    const mapped: Record<string, unknown> = {};
    for (const [key, item] of Object.entries(value as Record<string, unknown>)) {
      mapped[key] = walk(item, visit);
    }
    return mapped;
  }
  return value;
}

export function collectLocalRefs(envelope: QueuedRequestEnvelope): string[] {
  const found = new Set<string>();
  const record = (localId: string): string => {
    found.add(localId);
    return localRefToken(localId);
  };
  walk(envelope.body, record);
  walk(envelope.path, record);
  return [...found];
}

export type LocalRefResolution =
  { resolved: true; envelope: QueuedRequestEnvelope } | { resolved: false; missing: string[] };

export function resolveLocalRefs(
  envelope: QueuedRequestEnvelope,
  lookup: (localId: string) => string | null,
): LocalRefResolution {
  const missing: string[] = [];
  const substitute = (localId: string): string => {
    const serverId = lookup(localId);
    if (!serverId) {
      missing.push(localId);
      return localRefToken(localId);
    }
    return serverId;
  };
  const body = walk(envelope.body, substitute);
  const path = walk(envelope.path, substitute) as string;
  if (missing.length > 0) {
    return { resolved: false, missing: [...new Set(missing)] };
  }
  return { resolved: true, envelope: { ...envelope, path, body } };
}
