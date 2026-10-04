import {
  buildPersonalConnectPath,
  normalizePersonalConnectPath,
} from "@/lib/personal-connect-url";
import { isPublicUserId, normalizePublicUserId } from "@/lib/exits-qr/envelope";

const STORAGE_KEY = "exits.personal.connectIntent";
const MAX_AGE_MS = 24 * 60 * 60 * 1000;

type StoredConnectIntent = {
  publicUserId: string;
  continuePath: string;
  savedAtUtc: number;
};

export type PersonalConnectIntent = {
  publicUserId: string;
  continuePath: string;
};

function readStore(store: Storage | undefined): string | null {
  if (!store) {
    return null;
  }
  try {
    return store.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStore(store: Storage | undefined, value: string): void {
  if (!store) {
    return;
  }
  try {
    store.setItem(STORAGE_KEY, value);
  } catch {
    /* ignore quota */
  }
}

function removeStore(store: Storage | undefined): void {
  if (!store) {
    return;
  }
  try {
    store.removeItem(STORAGE_KEY);
  } catch {
    /* ignore */
  }
}

const ALLOWED_KEYS = new Set(["publicUserId", "continuePath", "savedAtUtc"]);

function parseStored(raw: string | null): PersonalConnectIntent | null {
  if (!raw) {
    return null;
  }
  try {
    const parsed = JSON.parse(raw) as Partial<StoredConnectIntent>;
    if (parsed === null || typeof parsed !== "object" || Array.isArray(parsed)) {
      return null;
    }
    if (Object.keys(parsed).some((key) => !ALLOWED_KEYS.has(key))) {
      return null;
    }
    const publicUserId = normalizePublicUserId(String(parsed.publicUserId ?? ""));
    const continuePath = normalizePersonalConnectPath(parsed.continuePath);
    const savedAtUtc = typeof parsed.savedAtUtc === "number" ? parsed.savedAtUtc : 0;
    if (!isPublicUserId(publicUserId) || continuePath !== buildPersonalConnectPath(publicUserId)) {
      return null;
    }
    if (Date.now() - savedAtUtc > MAX_AGE_MS) {
      return null;
    }
    return { publicUserId, continuePath };
  } catch {
    return null;
  }
}

function readValidIntent(store: Storage | undefined): PersonalConnectIntent | null {
  const raw = readStore(store);
  if (!raw) {
    return null;
  }
  const intent = parseStored(raw);
  if (!intent) {
    removeStore(store);
  }
  return intent;
}

/** Remembers only the public ExItS ID and its relative connect route. */
export function rememberPersonalConnectIntent(publicUserId: string): void {
  const id = normalizePublicUserId(publicUserId);
  if (!isPublicUserId(id)) {
    return;
  }
  const payload = JSON.stringify({
    publicUserId: id,
    continuePath: buildPersonalConnectPath(id),
    savedAtUtc: Date.now(),
  } satisfies StoredConnectIntent);
  writeStore(globalThis.sessionStorage, payload);
  writeStore(globalThis.localStorage, payload);
}

export function peekPersonalConnectIntent(): PersonalConnectIntent | null {
  const sessionIntent = readValidIntent(globalThis.sessionStorage);
  const localIntent = readValidIntent(globalThis.localStorage);
  return sessionIntent ?? localIntent;
}

export function clearPersonalConnectIntent(): void {
  removeStore(globalThis.sessionStorage);
  removeStore(globalThis.localStorage);
}
