export const PREMIUM_STORAGE_KEYS = {
  theme: "exits.premium.theme",
  palette: "exits.premium.palette",
  radius: "exits.premium.radius",
  density: "exits.premium.density",
} as const;

export const V1_UI_PREFERENCES_KEY = "exits.pos-client.ui-preferences.v1";

export const THEMES = ["light", "dark", "system"] as const;
export const PALETTES = ["teal", "blue", "violet", "orange", "rose"] as const;
export const RADII = ["standard", "soft", "pill"] as const;
export const DENSITIES = ["compact", "standard", "large"] as const;

export type ThemeMode = (typeof THEMES)[number];
export type Palette = (typeof PALETTES)[number];
export type Radius = (typeof RADII)[number];
export type Density = (typeof DENSITIES)[number];
export type ResolvedTheme = "light" | "dark";

export type Appearance = {
  theme: ThemeMode;
  palette: Palette;
  radius: Radius;
  density: Density;
};

export const DEFAULT_APPEARANCE: Appearance = {
  theme: "system",
  palette: "teal",
  radius: "soft",
  density: "standard",
};

export const PALETTE_LABELS: Record<Palette, string> = {
  teal: "Teal",
  blue: "Blue",
  violet: "Violet",
  orange: "Orange",
  rose: "Rose",
};

type StorageLike = Pick<Storage, "getItem" | "setItem">;

function pick<T extends string>(value: string | null, allowed: readonly T[], fallback: T): T {
  return allowed.includes(value as T) ? (value as T) : fallback;
}

export function readAppearance(storage: Pick<Storage, "getItem">): Appearance {
  return {
    theme: pick(storage.getItem(PREMIUM_STORAGE_KEYS.theme), THEMES, DEFAULT_APPEARANCE.theme),
    palette: pick(storage.getItem(PREMIUM_STORAGE_KEYS.palette), PALETTES, DEFAULT_APPEARANCE.palette),
    radius: pick(storage.getItem(PREMIUM_STORAGE_KEYS.radius), RADII, DEFAULT_APPEARANCE.radius),
    density: pick(storage.getItem(PREMIUM_STORAGE_KEYS.density), DENSITIES, DEFAULT_APPEARANCE.density),
  };
}

export function writeAppearance(storage: StorageLike, value: Appearance) {
  storage.setItem(PREMIUM_STORAGE_KEYS.theme, value.theme);
  storage.setItem(PREMIUM_STORAGE_KEYS.palette, value.palette);
  storage.setItem(PREMIUM_STORAGE_KEYS.radius, value.radius);
  storage.setItem(PREMIUM_STORAGE_KEYS.density, value.density);
}

export function resolveTheme(theme: ThemeMode, systemDark: boolean): ResolvedTheme {
  if (theme === "dark") return "dark";
  if (theme === "light") return "light";
  return systemDark ? "dark" : "light";
}

export function applyAppearanceAttributes(root: HTMLElement, value: Appearance, systemDark: boolean) {
  root.setAttribute("data-theme", resolveTheme(value.theme, systemDark));
  root.setAttribute("data-palette", value.palette);
  root.setAttribute("data-radius", value.radius);
  root.setAttribute("data-density", value.density);
}

export function systemPrefersDark() {
  const query = window.matchMedia?.("(prefers-color-scheme: dark)");
  return query?.matches ?? false;
}
