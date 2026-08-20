const ALLOWED_MODES = new Set(["development", "test", "testing"]);

/** Local-validation quick-login UI is for Vite development/test only — never production builds. */
export function isFrontendLocalValidationMode(mode = import.meta.env.MODE): boolean {
  return ALLOWED_MODES.has(mode);
}
