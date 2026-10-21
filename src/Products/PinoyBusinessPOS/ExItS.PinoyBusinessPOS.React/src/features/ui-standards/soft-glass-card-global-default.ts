/**
 * Soft Glass Enterprise card lock — Quick Actions approved (light + dark).
 * Use when creating Personal / Utang cards. No blur, neon, lift, or scale.
 */

export const SOFT_GLASS_LABEL_SIZE = "0.875rem" as const;
export const SOFT_GLASS_LABEL_WEIGHT = 600 as const;
export const SOFT_GLASS_FOCUS = "#8b5cf6" as const;
export const SOFT_GLASS_TRANSITION_MS = 120 as const;
/** Accent elevation mix — light cards. */
export const SOFT_GLASS_SHADOW_ACCENT_PCT = 8 as const;
/** Slate mix in light borders. */
export const SOFT_GLASS_BORDER_SLATE = "#e2e8f0" as const;
export const SOFT_GLASS_BORDER_SLATE_PCT = 62 as const;

/** Light identities — match `.personal-qa-glass--*`. */
export const SOFT_GLASS_LIGHT = {
  start: {
    chip: "#06b6d4",
    deep: "#0e7490",
    soft: "#67e8f9",
    wash: "#ecfeff",
    mid: "#cffafe",
    ink: "#0f766e",
  },
  lent: {
    chip: "#10b981",
    deep: "#047857",
    soft: "#6ee7b7",
    wash: "#ecfdf5",
    mid: "#d1fae5",
    ink: "#065f46",
  },
  owe: {
    chip: "#3b82f6",
    deep: "#1d4ed8",
    soft: "#93c5fd",
    wash: "#eff6ff",
    mid: "#dbeafe",
    ink: "#1e40af",
  },
  stores: {
    chip: "#8b5cf6",
    deep: "#6d28d9",
    soft: "#c4b5fd",
    wash: "#f5f3ff",
    mid: "#ede9fe",
    ink: "#5b21b6",
  },
  people: {
    chip: "#4169e1",
    deep: "#1e3a8a",
    soft: "#a5b4fc",
    wash: "#eef2ff",
    mid: "#e0e7ff",
    ink: "#312e81",
  },
  info: {
    chip: "#0ea5e9",
    deep: "#0369a1",
    soft: "#7dd3fc",
    wash: "#f0f9ff",
    mid: "#e0f2fe",
    ink: "#075985",
  },
} as const;

/** Dark identities — restrained, no neon. */
export const SOFT_GLASS_DARK = {
  start: { chip: "#0f766e", deep: "#134e4a", soft: "#5eead4" },
  lent: { chip: "#047857", deep: "#064e3b", soft: "#6ee7b7" },
  owe: { chip: "#1d4ed8", deep: "#1e3a8a", soft: "#93c5fd" },
  stores: { chip: "#6d28d9", deep: "#4c1d95", soft: "#c4b5fd" },
  people: { chip: "#3730a3", deep: "#1e1b4b", soft: "#a5b4fc" },
  info: { chip: "#0369a1", deep: "#0c4a6e", soft: "#7dd3fc" },
  label: "#e2e8f0",
  surfaceMid: "#1e293b",
  surfaceDeep: "#0f172a",
} as const;

export type SoftGlassIdentity = keyof typeof SOFT_GLASS_LIGHT;
