const ALLOWED_MODES = new Set(["development", "test", "testing"]);

export function isFrontendLocalValidationMode(mode = import.meta.env.MODE): boolean {
  return ALLOWED_MODES.has(mode);
}
