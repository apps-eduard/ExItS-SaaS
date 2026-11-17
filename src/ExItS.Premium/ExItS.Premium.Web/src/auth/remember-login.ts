const REMEMBER_KEY = "exits.premium.auth.remember-me.v1";
const EMAIL_KEY = "exits.premium.auth.remembered-email.v1";

function storage() {
  if (typeof window === "undefined") return null;
  try {
    return window.localStorage;
  } catch {
    return null;
  }
}

export function readRememberedEmail() {
  const store = storage();
  if (!store || store.getItem(REMEMBER_KEY) !== "1") return "";
  return store.getItem(EMAIL_KEY)?.trim() ?? "";
}

/** Stores the email only. The password is never written. */
export function persistRememberedEmail(email: string, remember: boolean) {
  const store = storage();
  if (!store) return;
  if (!remember) {
    store.removeItem(REMEMBER_KEY);
    store.removeItem(EMAIL_KEY);
    return;
  }
  store.setItem(REMEMBER_KEY, "1");
  store.setItem(EMAIL_KEY, email.trim());
}
