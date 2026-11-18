/**
 * Personal main-card header (icon + label) — GLOBAL default (LOCKED).
 * One design/size for every Personal home main-card title row.
 * Each card keeps its identity chip color; change size/design here only.
 *
 * Keep `.cursor/rules/exits-personal-main-card-header.mdc` and
 * `src/styles/personal-main-card-header.css` aligned.
 *
 * Command: ext personal main-card-header icon-round label
 */

export const PERSONAL_MAIN_CARD_HEADER_DEFAULT_COMMAND =
  "ext personal main-card-header icon-round label" as const;

/** Reference: Stores to pay header (rounded purple chip + bold label). */
export const PERSONAL_MAIN_CARD_HEADER_REFERENCE =
  "personal home · stores to pay · main card header" as const;

/** Filled icon disc size. CSS: `--personal-main-card-header-icon-size`. */
export const PERSONAL_MAIN_CARD_HEADER_ICON_SIZE = "2rem" as const;

/** Fully rounded disc. CSS: `--personal-main-card-header-icon-radius`. */
export const PERSONAL_MAIN_CARD_HEADER_ICON_RADIUS = "999px" as const;

/** Glyph inside the disc. CSS: `--personal-main-card-header-icon-mark`. */
export const PERSONAL_MAIN_CARD_HEADER_ICON_MARK = "1.05rem" as const;

/** Title label size / weight. */
export const PERSONAL_MAIN_CARD_HEADER_LABEL_SIZE = "1.05rem" as const;
export const PERSONAL_MAIN_CARD_HEADER_LABEL_WEIGHT = 700 as const;
export const PERSONAL_MAIN_CARD_HEADER_LABEL_LINE_HEIGHT = 1.2 as const;
export const PERSONAL_MAIN_CARD_HEADER_LABEL_LETTER_SPACING = "-0.01em" as const;

/** Gap between icon and label. */
export const PERSONAL_MAIN_CARD_HEADER_GAP = "0.55rem" as const;

/** Dark title ink (same for every card). */
export const PERSONAL_MAIN_CARD_HEADER_DARK_INK = "#f1f5f9" as const;

/**
 * Light label: black mixed into the card chip.
 * CSS: `color-mix(in srgb, #000 N%, var(--personal-main-card-header-chip))`
 */
export const PERSONAL_MAIN_CARD_HEADER_LIGHT_LABEL_BLACK_PCT = 28 as const;

/**
 * Identity chips (retain per card — do not unify colors).
 * Sections set `--personal-main-card-header-chip` from these.
 */
export const PERSONAL_MAIN_CARD_HEADER_CHIPS = {
  /** Stores to pay + Recent activity home primary row. */
  stores: "var(--exits-primary)",
  attention: "#ff5722",
  quickActions: "#ff5722",
  /** To-do home summary — same primary row as Stores / Recent activity. */
  todo: "var(--exits-primary)",
  tracker: "#22c55e",
  /** Recent activity — same primary as Stores to pay. */
  activity: "var(--exits-primary)",
  /** People list main card — Soft Glass people identity. */
  people: "#4169e1",
  /** Money I owe plain header — Preferences primary. */
  moneyOwe: "var(--exits-primary)",
} as const;
