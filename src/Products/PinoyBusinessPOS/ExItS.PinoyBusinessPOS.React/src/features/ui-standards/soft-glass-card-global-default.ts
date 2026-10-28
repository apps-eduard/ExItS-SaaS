/**
 * Soft Glass Enterprise card lock — Quick Actions approved (light + dark).
 * Use when creating Personal / Utang cards and Soft Glass quick-action rows.
 * Main card shells use solid tint fill (no wash gradient). No blur, neon, lift, or scale.
 * Change only when the user asks.
 *
 * Keep `.cursor/rules/exits-soft-glass-card.mdc` and Soft Glass CSS in
 * `globals.css` aligned (shell, QA, title ribbon).
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

/**
 * Soft Glass screen-edge frame (NOT Soft Glass cards).
 * Applied to UI Standards `.app-shell--full` and Personal `.personal-crystal-screen`.
 * Same Soft Glass border recipe as cards/tables: 1px slate+primary soft, inset highlight,
 * soft elevation. Color follows Preferences primary.
 * Command: `ext ui-standards crystal screen-border`
 */
export const UI_STANDARDS_CRYSTAL_SCREEN_BORDER_COMMAND = "ext ui-standards crystal screen-border" as const;
/** Light border: slate mix % into primary-soft (matches `SOFT_GLASS_BORDER_SLATE_PCT`). */
export const UI_STANDARDS_CRYSTAL_BORDER_SLATE_PCT = 62 as const;
/** Dark border: primary chip % into slate (matches `SOFT_GLASS_DARK_BORDER_CHIP_PCT`). */
export const UI_STANDARDS_CRYSTAL_BORDER_DARK_CHIP_PCT = 16 as const;
/** Soft elevation accent mix % (matches `SOFT_GLASS_SHADOW_ACCENT_PCT`). */
export const UI_STANDARDS_CRYSTAL_SHADOW_ACCENT_PCT = 8 as const;
/** Inset from viewport so the screen rim is visible (CSS `--ui-standards-crystal-inset`). */
export const UI_STANDARDS_CRYSTAL_SCREEN_INSET = "0.45rem" as const;
/** Frame corner radius — Soft Glass shell (CSS `--ui-standards-crystal-radius`). */
export const UI_STANDARDS_CRYSTAL_SCREEN_RADIUS = "1.1rem" as const;

/** @deprecated Use `UI_STANDARDS_CRYSTAL_BORDER_SLATE_PCT` — Soft Glass edge, not white/primary mix. */
export const UI_STANDARDS_CRYSTAL_BORDER_WHITE_PCT = UI_STANDARDS_CRYSTAL_BORDER_SLATE_PCT;
/** @deprecated Use `UI_STANDARDS_CRYSTAL_BORDER_DARK_CHIP_PCT`. */
export const UI_STANDARDS_CRYSTAL_BORDER_DARK_WHITE_PCT = UI_STANDARDS_CRYSTAL_BORDER_DARK_CHIP_PCT;

/**
 * Soft Glass shell / card corner radius (LOCKED).
 * Same value as `TABLE_GLOBAL_SOFT_GLASS_RADIUS`. CSS: `--soft-glass-radius`.
 */
export const SOFT_GLASS_RADIUS = "1.1rem" as const;

/**
 * Soft Glass title ribbon radius (LOCKED — default for Soft Glass ribbon cards).
 * Same shell radius on three corners; fold corner (bottom-start) stays `0`.
 * Command: `ext soft-glass ribbon radius`
 * CSS: `--soft-glass-ribbon-radius` on Soft Glass ribbon cards /
 * `.personal-tracker-card__ribbon` under Soft Glass shells.
 */
export const SOFT_GLASS_RIBBON_RADIUS = "1.1rem 1.1rem 1.1rem 0" as const;
export const SOFT_GLASS_RIBBON_DEFAULT_COMMAND = "ext soft-glass ribbon radius" as const;

/**
 * Soft Glass input / select / dropdown menu radius (LOCKED).
 * Matches global Soft control shape (`--exits-radius-soft`), not the shell `1.1rem`.
 * Command: `ext soft-glass field radius`
 * CSS: `--soft-glass-field-radius` on Soft Glass inputs, selects, dropdown menus,
 * and Soft Glass modal field chrome (e.g. Update Balance).
 */
export const SOFT_GLASS_FIELD_RADIUS = "0.6875rem" as const;
export const SOFT_GLASS_FIELD_DEFAULT_COMMAND = "ext soft-glass field radius" as const;

/**
 * Dark Soft Glass Quick Action border (LOCKED — home Quick Actions).
 * CSS: `1px solid color-mix(in srgb, var(--personal-qa-chip) N%, SLATE)`.
 * Applies to every Soft Glass identity including `info` (detail QA).
 */
export const SOFT_GLASS_DARK_BORDER_SLATE = "#475569" as const;
export const SOFT_GLASS_DARK_BORDER_CHIP_PCT = 16 as const;
export const SOFT_GLASS_DARK_BORDER_HOVER_SLATE = "#64748b" as const;
export const SOFT_GLASS_DARK_BORDER_HOVER_CHIP_PCT = 22 as const;

/**
 * Trailing end of a Soft Glass Quick Action row (LOCKED).
 * - `count` — circular Soft Glass badge with a number (filters / stats)
 * - `chevron` — navigate caret when there is no count
 * - `add` — Soft Glass + chip for create / open-add actions
 */
export const SOFT_GLASS_QA_TRAILING = ["count", "chevron", "add"] as const;
export type SoftGlassQaTrailing = (typeof SOFT_GLASS_QA_TRAILING)[number];

/** Resolve trailing mark: count wins; else add for create; else chevron navigate. */
export function softGlassQaTrailing(options: {
  count?: number | null;
  trailing?: SoftGlassQaTrailing;
}): SoftGlassQaTrailing {
  if (options.trailing) {
    return options.trailing;
  }
  if (options.count != null) {
    return "count";
  }
  return "chevron";
}

export const SOFT_GLASS_QA_DEFAULT_COMMAND = "ext soft-glass qa trailing-auto" as const;

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
  /** New Quick Actions — Preferences primary (CSS resolves tokens). */
  primary: {
    chip: "var(--exits-primary)",
    deep: "var(--exits-primary-hover)",
    soft: "var(--exits-primary-soft, var(--exits-primary))",
    wash: "color-mix(in srgb, var(--exits-primary) 8%, #fff)",
    mid: "color-mix(in srgb, var(--exits-primary) 16%, #fff)",
    ink: "var(--exits-primary)",
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
  /** Org EHG dark keeps light primary chip (see EHG QA dark chip lock). */
  primary: {
    chip: "var(--exits-primary)",
    deep: "var(--exits-primary-hover)",
    soft: "var(--exits-primary-soft, var(--exits-primary))",
  },
  label: "#e2e8f0",
  surfaceMid: "#1e293b",
  surfaceDeep: "#0f172a",
} as const;

export type SoftGlassIdentity = keyof typeof SOFT_GLASS_LIGHT;
