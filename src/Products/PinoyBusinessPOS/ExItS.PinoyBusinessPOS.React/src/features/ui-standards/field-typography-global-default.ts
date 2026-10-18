/**
 * Locked input and select type (default global). Applies to every appearance except Label above.
 * Kept apart from `input-global-default.ts` / `select-global-default.ts` because the playground
 * lock button rewrites those files.
 */
export const FIELD_GLOBAL_LABEL_SIZE = "13px" as const;
export const FIELD_GLOBAL_TEXT_SIZE = "14px" as const;
/** The raised label stays full size. It slides up and does not shrink. */
export const FIELD_GLOBAL_FLOAT_SCALE = 1 as const;
/** Label above keeps its own static label. */
export const FIELD_GLOBAL_STACKED_LABEL_SIZE = "14px" as const;

/** Select open state: a thick bottom edge grows from the center on every appearance. */
export const SELECT_GLOBAL_BAR_THICKNESS = "3px" as const;
export const SELECT_GLOBAL_BAR_DURATION = "0.7s" as const;
