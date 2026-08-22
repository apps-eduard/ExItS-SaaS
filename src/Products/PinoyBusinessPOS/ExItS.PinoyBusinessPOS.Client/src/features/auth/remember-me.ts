const REMEMBER_ME_KEY = "exits.pos-client.auth.remember-me.v1";
const REMEMBERED_USERNAME_KEY = "exits.pos-client.auth.remembered-username.v1";

export function readRememberMePreference(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    return window.localStorage.getItem(REMEMBER_ME_KEY) === "1";
  } catch {
    return false;
  }
}

export function writeRememberMePreference(enabled: boolean): void {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  try {
    if (enabled) {
      window.localStorage.setItem(REMEMBER_ME_KEY, "1");
    } else {
      window.localStorage.removeItem(REMEMBER_ME_KEY);
      window.localStorage.removeItem(REMEMBERED_USERNAME_KEY);
    }
  } catch {
    // ignore
  }
}

export function readRememberedUsername(): string {
  if (!readRememberMePreference()) {
    return "";
  }
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return "";
  }
  try {
    return window.localStorage.getItem(REMEMBERED_USERNAME_KEY)?.trim() ?? "";
  } catch {
    return "";
  }
}

export function persistRememberedUsername(username: string, remember: boolean): void {
  writeRememberMePreference(remember);
  if (!remember || typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  try {
    window.localStorage.setItem(REMEMBERED_USERNAME_KEY, username.trim());
  } catch {
    // ignore
  }
}
