/** In-memory POS bearer from session grant only — never persisted in browser storage. */
let inMemoryAccessToken: string | null = null;

export function setPosAccessToken(token: string | null) {
  inMemoryAccessToken = token?.trim() ? token.trim() : null;
}

export function getPosAccessToken(): string | null {
  return inMemoryAccessToken;
}

export function clearPosAccessToken() {
  inMemoryAccessToken = null;
}
