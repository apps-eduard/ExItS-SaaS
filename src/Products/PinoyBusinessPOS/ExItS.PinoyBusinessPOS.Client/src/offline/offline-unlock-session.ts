/**
 * In-memory holder for the unlocked local-store DEK.
 * Cleared on logout — never persisted in plaintext.
 */

let unlockedDek: CryptoKey | null = null;
let unlockedUserId: string | null = null;

export function isOfflineDekUnlocked(userId?: string | null): boolean {
  if (!unlockedDek) {
    return false;
  }
  if (userId && unlockedUserId !== userId) {
    return false;
  }
  return true;
}

export function getUnlockedDek(userId?: string | null): CryptoKey | null {
  if (!unlockedDek) {
    return null;
  }
  if (userId && unlockedUserId !== userId) {
    return null;
  }
  return unlockedDek;
}

export function setUnlockedDek(userId: string, dek: CryptoKey): void {
  unlockedUserId = userId;
  unlockedDek = dek;
}

export function clearUnlockedDek(): void {
  unlockedUserId = null;
  unlockedDek = null;
}
