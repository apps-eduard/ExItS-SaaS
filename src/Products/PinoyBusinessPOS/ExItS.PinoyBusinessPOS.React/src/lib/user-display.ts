/** Safe display helpers for signed-in shell chrome — no GUIDs or raw claims. */

export function resolveUserDisplayName(
  session: {
    displayName?: string | null;
    username?: string | null;
    email?: string | null;
  } | null,
): string {
  const displayName = session?.displayName?.trim();
  if (displayName) {
    return displayName;
  }
  const username = session?.username?.trim();
  if (username) {
    return username;
  }
  const email = session?.email?.trim();
  if (email) {
    return email;
  }
  return "";
}

export function resolveUserSecondaryIdentity(
  session: {
    displayName?: string | null;
    username?: string | null;
    email?: string | null;
  } | null,
): string | null {
  const displayName = session?.displayName?.trim();
  const username = session?.username?.trim();
  const email = session?.email?.trim();

  if (displayName) {
    if (
      username &&
      username.localeCompare(displayName, undefined, { sensitivity: "accent" }) !== 0
    ) {
      return username;
    }
    if (email && email.localeCompare(displayName, undefined, { sensitivity: "accent" }) !== 0) {
      return email;
    }
    return null;
  }

  if (
    username &&
    email &&
    email.localeCompare(username, undefined, { sensitivity: "accent" }) !== 0
  ) {
    return email;
  }

  return null;
}

/** Initials from display name (e.g. Olivia Mendoza → OM); falls back to username/email. */
export function deriveUserInitials(
  session: {
    displayName?: string | null;
    username?: string | null;
    email?: string | null;
  } | null,
): string | null {
  const displayName = session?.displayName?.trim();
  if (displayName) {
    const parts = displayName.split(/\s+/).filter(Boolean);
    if (parts.length >= 2) {
      const first = parts[0]?.[0];
      const last = parts[parts.length - 1]?.[0];
      if (first && last) {
        return `${first}${last}`.toUpperCase();
      }
    }
    const compact = displayName.replace(/\s+/g, "");
    if (compact.length >= 2) {
      return compact.slice(0, 2).toUpperCase();
    }
    if (compact.length === 1) {
      return compact.toUpperCase();
    }
  }

  const fallback = (session?.username ?? session?.email ?? "").trim();
  if (fallback.length >= 2) {
    return fallback.slice(0, 2).toUpperCase();
  }
  if (fallback.length === 1) {
    return fallback.toUpperCase();
  }
  return null;
}

export type FriendlyPosRole = "owner" | "manager" | "cashier";

/** Map resolved POS role codes to a friendly label key — omit unknown/raw codes. */
export function resolveFriendlyPosRole(
  roleCode: string | null | undefined,
): FriendlyPosRole | null {
  if (!roleCode?.trim()) {
    return null;
  }
  switch (roleCode.trim().toLowerCase()) {
    case "owner":
    case "admin":
      return "owner";
    case "storemanager":
    case "manager":
      return "manager";
    case "cashier":
      return "cashier";
    default:
      return null;
  }
}
