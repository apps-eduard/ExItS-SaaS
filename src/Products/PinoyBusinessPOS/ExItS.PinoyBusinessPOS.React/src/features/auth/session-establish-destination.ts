export type HandoffDestination =
  | { ok: true; path: string }
  | { ok: false; reason: "wrong-target" | "class-mismatch" };

function safeRelativePath(raw: string | null | undefined): string | null {
  if (!raw) {
    return null;
  }
  const value = raw.trim();
  if (
    value.length === 0 ||
    !value.startsWith("/") ||
    value.startsWith("//") ||
    value.includes("\\") ||
    value.includes("://")
  ) {
    return null;
  }
  return value;
}

function isPersonalPath(path: string): boolean {
  return path === "/personal" || path.startsWith("/personal/");
}

/**
 * Destination comes from the redeemed server payload.
 * Query return paths are used only when they stay inside that target.
 */
export function resolveHandoffDestination(input: {
  targetApp?: string | null;
  accountClass?: string | null;
  queryReturnPath?: string | null;
  serverReturnPath?: string | null;
}): HandoffDestination {
  const target = input.targetApp?.trim().toLowerCase() ?? "";
  const account = input.accountClass?.trim().toLowerCase() ?? "";
  if (target !== "personal" && target !== "organization") {
    return { ok: false, reason: "wrong-target" };
  }
  if (account !== target) {
    return { ok: false, reason: "class-mismatch" };
  }

  const fallback = target === "personal" ? "/personal" : "/";
  const candidate =
    safeRelativePath(input.queryReturnPath) ?? safeRelativePath(input.serverReturnPath);
  if (!candidate) {
    return { ok: true, path: fallback };
  }
  if (target === "personal" && !isPersonalPath(candidate)) {
    return { ok: true, path: fallback };
  }
  if (target === "organization" && isPersonalPath(candidate)) {
    return { ok: true, path: fallback };
  }
  return { ok: true, path: candidate };
}
