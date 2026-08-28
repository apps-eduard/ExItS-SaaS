export type ExpirationSettingsFocus = "assign" | "warning";

export function expirationSettingsPath(
  productId: string,
  focus?: ExpirationSettingsFocus,
): string {
  const base = `/inventory/${productId}/expiration`;
  if (!focus) {
    return base;
  }
  return `${base}?focus=${focus}`;
}

export function parseExpirationSettingsFocus(search: string): ExpirationSettingsFocus | null {
  const value = new URLSearchParams(search).get("focus");
  if (value === "assign" || value === "warning") {
    return value;
  }
  return null;
}

/** Emphasizes the targeted card when arriving from inventory detail actions. */
export const expirationSettingsHighlightClass =
  "border-primary bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))] shadow-[inset_0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_35%,transparent)]";
