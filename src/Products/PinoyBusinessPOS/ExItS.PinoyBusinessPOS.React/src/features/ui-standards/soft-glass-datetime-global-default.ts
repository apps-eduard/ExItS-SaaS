/**
 * Soft Glass date / date-time global default (Soft Glass Roll · Primary).
 * Showcase: Date UI standard card Soft Glass Roll · Primary, global default.
 * Color follows Preferences primary. Change only when the user asks.
 *
 * Keep `.cursor/rules/exits-soft-glass-datetime.mdc`,
 * `exits-datetime-roll.css` (product SoftGlassDateTimeField + showcase),
 * `.ui-standards-soft-glass-roll-card` in `ui-standards.css`, and
 * Soft Glass datetime chrome in `globals.css` aligned.
 *
 * Command: ext soft-glass date|datetime roll global-default
 */
export const SOFT_GLASS_DATETIME_COMMAND = "ext soft-glass date datetime roll global-default" as const;
export const SOFT_GLASS_DATE_COMMAND = "ext soft-glass date roll global-default" as const;
export const SOFT_GLASS_DATETIME_PAIR_COMMAND = "ext soft-glass datetime roll global-default" as const;

export const SOFT_GLASS_DATETIME_TITLE = "Soft Glass Roll · Primary, global default" as const;

/** Preferences primary. Do not hard-code the sample blue. */
export const SOFT_GLASS_DATETIME_COLOR = "var(--exits-primary)" as const;

/** Soft Glass roll calendar / time appearance. */
export const SOFT_GLASS_DATETIME_APPEARANCE = "soft-glass" as const;
export const SOFT_GLASS_DATETIME_VARIETY = "roll" as const;

/** Trailing field icons. */
export const SOFT_GLASS_DATE_ICON = "calendar-days" as const;
export const SOFT_GLASS_DATETIME_ICON = "calendar-clock" as const;

/**
 * Trigger field starts empty. Raised label matches Soft Glass · Primary, global default
 * (`SOFT_GLASS_PRIMARY_FIELDS_LABEL_*` / Add local person).
 */
export const SOFT_GLASS_DATETIME_DEFAULT_EMPTY = true as const;

/**
 * Pick-only trigger: no manual typing, no text caret / input focus.
 * Click opens the Soft Glass roll popup. Placeholder still shows while empty (raised label when open).
 */
export const SOFT_GLASS_DATETIME_TYPEABLE = false as const;
export const SOFT_GLASS_DATE_PLACEHOLDER = "mm/dd/yyyy" as const;

/**
 * Hour format default is **12-hour AM/PM** (`SOFT_GLASS_DATETIME_DEFAULT_HOUR24` = false).
 * Placeholder (empty field): `mm/dd/yyyy hh:mm am`. When the header switch is **24**, placeholder is `mm/dd/yyyy hh:mm`.
 */
export const SOFT_GLASS_DATETIME_DEFAULT_HOUR24 = false as const;
export const SOFT_GLASS_DATETIME_PLACEHOLDER = "mm/dd/yyyy hh:mm am" as const;
export const SOFT_GLASS_DATETIME_PLACEHOLDER_24 = "mm/dd/yyyy hh:mm" as const;

/**
 * When the field has a picked value, show the playground reset mark (RotateCcw), not the clear X.
 * `clearMark="reset"` on the Soft Glass Roll trigger.
 */
export const SOFT_GLASS_DATETIME_RESET_MARK = "reset" as const;

/** Date-only Soft Glass Roll keeps Today in the header. Combined date+time uses footer Now. */
export const SOFT_GLASS_DATETIME_TODAY_PLACEMENT = "header" as const;
/** Combined Soft Glass date+time: Now is in the footer (middle); applies current date+time and closes. */
export const SOFT_GLASS_DATETIME_NOW_PLACEMENT = "footer" as const;
export const SOFT_GLASS_DATETIME_NOW_COMMITS_AND_CLOSES = true as const;

/** Date-time hour switch is in the time header; captions are 12H / 24H (not ON / OFF). Default selected: 12H. */
export const SOFT_GLASS_DATETIME_FORMAT_PLACEMENT = "header" as const;
export const SOFT_GLASS_DATETIME_FORMAT_CHECKED_LABEL = "24H" as const;
export const SOFT_GLASS_DATETIME_FORMAT_UNCHECKED_LABEL = "12H" as const;

/**
 * Soft Glass date+time (SoftGlassDateTimeField + Soft Glass Roll card):
 * One Soft Glass card — header (full month + 12H·24H) + date/time rolls + footer Cancel / Now / Set.
 * Opens in a full-viewport flex portal + scrim so modals do not clip the rolls.
 */
export const SOFT_GLASS_DATETIME_COMBINED_CARD = true as const;
/** @deprecated Prefer SOFT_GLASS_DATETIME_COMBINED_CARD — independent shells removed. */
export const SOFT_GLASS_DATETIME_INDEPENDENT_PANELS = false as const;
/** Product field is one combined card (not date-then-time sequential). */
export const SOFT_GLASS_DATETIME_SEQUENTIAL = false as const;

/** One Set footer on the combined Soft Glass date+time card. */
export const SOFT_GLASS_DATETIME_SHOW_SET = true as const;

/** Middle hour / minute opens a quick-select grid (same idea as year on the date roll). */
export const SOFT_GLASS_DATETIME_ROLL_QUICK_SELECT = true as const;

/** Open calendar / time with no value yet shows today's date / now. */
export const SOFT_GLASS_DATETIME_OPEN_SHOWS_CURRENT = true as const;
