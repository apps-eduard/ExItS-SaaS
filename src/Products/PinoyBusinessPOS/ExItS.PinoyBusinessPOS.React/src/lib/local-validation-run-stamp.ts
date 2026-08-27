/**
 * Local Validation appends a compact datetime to seeded names.
 * Compact form: YYYYMMDDHHmmss, with optional milliseconds.
 * Stored data keeps the stamp; UI strips it for display.
 */
const COMPACT_RUN_STAMP =
  /\s+(20\d{2})(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])([01]\d|2[0-3])([0-5]\d)([0-5]\d)(\d{3})?$/;

export function stripLocalValidationRunStamp(value: string): string {
  return value.trim().replace(COMPACT_RUN_STAMP, "").replace(/\s+/g, " ").trim();
}
