/** Mirrors MAUI `OrganizationSlug` — suggest/validate org URL slugs (not shown to users). */

import { createSecureMutationId } from "@/lib/secure-mutation-id";

export function suggestOrganizationSlugFromDisplayName(
  displayName: string | null | undefined,
): string {
  if (!displayName || !displayName.trim()) {
    return "";
  }

  const chars = Array.from(displayName.trim().toLowerCase()).map((c) =>
    /[a-z0-9]/.test(c) ? c : "-",
  );
  let slug = chars.join("");
  while (slug.includes("--")) {
    slug = slug.replaceAll("--", "-");
  }
  return slug.replace(/^-+|-+$/g, "");
}

export function isValidOrganizationSlugFormat(slug: string | null | undefined): boolean {
  if (!slug || !slug.trim()) {
    return false;
  }

  const value = slug.trim();
  if (value.length < 2 || value.length > 64) {
    return false;
  }

  for (let i = 0; i < value.length; i += 1) {
    const c = value[i]!;
    if ((c >= "a" && c <= "z") || (c >= "0" && c <= "9")) {
      continue;
    }
    if (c === "-" && i > 0 && i < value.length - 1 && value[i - 1] !== "-") {
      continue;
    }
    return false;
  }

  return true;
}

/** Non-security uniqueness for hidden org slugs (Tailscale HTTP may lack randomUUID). */
function slugSuffix(): string {
  const secure = createSecureMutationId();
  if (secure.ok) {
    return secure.id.replaceAll("-", "").slice(0, 8);
  }
  // Last resort for insecure contexts — Platform still enforces slug uniqueness.
  return `${Date.now().toString(36)}${Math.floor(Math.random() * 1e6).toString(36)}`.slice(0, 8);
}

/** Ensure a unique-enough slug when the display-name suggestion is too short. */
export function ensureOrganizationSlug(displayName: string): string {
  let slug = suggestOrganizationSlugFromDisplayName(displayName);
  if (!isValidOrganizationSlugFormat(slug)) {
    const base = slug.replaceAll(/[^a-z0-9]/g, "").slice(0, 48) || "business";
    slug = `${base}-${slugSuffix()}`;
  }
  return slug;
}
