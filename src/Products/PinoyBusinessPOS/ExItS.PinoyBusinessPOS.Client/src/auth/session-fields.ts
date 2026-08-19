export const SESSION_TOKEN_JSON_KEYS = ["sessionToken", "SessionToken"] as const;

export type PlatformSessionSnapshot = {
  sessionId: string;
  userId: string;
  username: string;
  displayName: string;
  email: string;
  accountClass?: string | null;
  allowedScope?: string | null;
};

export function omitSessionToken(payload: unknown): Record<string, unknown> {
  if (typeof payload !== "object" || payload === null || Array.isArray(payload)) {
    return {};
  }

  const copy: Record<string, unknown> = { ...(payload as Record<string, unknown>) };
  for (const key of SESSION_TOKEN_JSON_KEYS) {
    delete copy[key];
  }
  return copy;
}

function readString(record: Record<string, unknown>, key: string): string | undefined {
  const value = record[key];
  return typeof value === "string" && value.trim().length > 0 ? value : undefined;
}

export function readSessionSnapshot(payload: unknown): PlatformSessionSnapshot | null {
  const record = omitSessionToken(payload);
  const sessionId = readString(record, "sessionId");
  const userId = readString(record, "userId");
  const username = readString(record, "username");
  const displayName = readString(record, "displayName");
  const email = readString(record, "email");
  if (!sessionId || !userId || !username || !displayName || !email) {
    return null;
  }

  return {
    sessionId,
    userId,
    username,
    displayName,
    email,
    accountClass: readString(record, "accountClass") ?? null,
    allowedScope: readString(record, "allowedScope") ?? null,
  };
}
