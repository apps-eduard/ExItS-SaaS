export function initialsFromIdentity(
  displayName?: string | null,
  username?: string | null,
  email?: string | null,
): string | null {
  const source = displayName?.trim() || username?.trim() || email?.split("@")[0]?.trim();
  if (!source) {
    return null;
  }

  const words = source.split(/[\s._-]+/).filter((word) => word.length > 0);
  if (words.length >= 2) {
    const first = words[0] ?? "";
    const last = words[words.length - 1] ?? "";
    return `${first[0] ?? ""}${last[0] ?? ""}`.toUpperCase();
  }

  const letters = source.replace(/[^A-Za-z0-9]/g, "");
  if (letters.length >= 2) {
    return letters.slice(0, 2).toUpperCase();
  }
  if (letters.length === 1) {
    return letters.toUpperCase();
  }
  return null;
}
