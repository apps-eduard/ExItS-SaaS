export const PLATFORM_API_BASE_PATH = "/platform-api";

const ABSOLUTE_API_PATTERN = /^https?:\/\//i;

export function platformApiUrl(path: string): string {
  if (ABSOLUTE_API_PATTERN.test(path) || path.includes("://") || /:(?:8091)\b/.test(path)) {
    throw new Error("Platform API calls must stay on the relative /platform-api origin.");
  }

  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${PLATFORM_API_BASE_PATH}${normalized}`;
}

export type PlatformLoginWire = {
  sessionId?: string;
  userId?: string;
  username?: string;
  displayName?: string;
  email?: string;
  expiresAtUtc?: string;
  absoluteExpiresAtUtc?: string;
  sessionToken?: string;
};

export type BrowserSessionSnapshot = Omit<PlatformLoginWire, "sessionToken">;

export function toBrowserSessionSnapshot(wire: PlatformLoginWire): BrowserSessionSnapshot {
  const safe: BrowserSessionSnapshot & { sessionToken?: string } = { ...wire };
  delete safe.sessionToken;
  return safe;
}

export function assertBrowserStorageHasNoSessionToken(storage: Storage): void {
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index);
    if (!key) {
      continue;
    }
    const value = storage.getItem(key) ?? "";
    if (/sessionToken/i.test(key) || /sessionToken/i.test(value)) {
      throw new Error("SessionToken must not be persisted in browser storage.");
    }
  }
}

export async function platformApiJson<T>(
  path: string,
  init?: RequestInit,
): Promise<{ status: number; body: T | null }> {
  const response = await fetch(platformApiUrl(path), {
    ...init,
    credentials: "include",
    headers: {
      Accept: "application/json",
      ...(init?.headers ?? {}),
    },
  });
  if (response.status === 204) {
    return { status: response.status, body: null };
  }
  const text = await response.text();
  if (!text) {
    return { status: response.status, body: null };
  }
  return { status: response.status, body: JSON.parse(text) as T };
}
