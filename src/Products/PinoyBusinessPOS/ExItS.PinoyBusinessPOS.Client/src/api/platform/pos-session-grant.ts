import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

/** In-memory session grant metadata — never persisted in browser storage. */
let inMemorySessionGrant: SessionGrantResponse | null = null;

export function setPosSessionGrant(grant: SessionGrantResponse | null): void {
  inMemorySessionGrant = grant;
}

export function getPosSessionGrant(): SessionGrantResponse | null {
  return inMemorySessionGrant;
}

export function clearPosSessionGrant(): void {
  inMemorySessionGrant = null;
}
