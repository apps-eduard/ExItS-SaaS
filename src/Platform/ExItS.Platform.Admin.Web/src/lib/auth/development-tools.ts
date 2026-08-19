import { isLocalValidationToolsEnabled } from "@/lib/env";

const ALLOWED_FRONTEND_MODES = new Set(["development", "test", "testing"]);

export function areDevelopmentToolsAllowed(mode: string = import.meta.env.MODE): boolean {
  if (typeof mode !== "string") {
    return false;
  }

  const normalized = mode.trim().toLowerCase();
  if (normalized.length === 0) {
    return false;
  }

  return ALLOWED_FRONTEND_MODES.has(normalized);
}

export function areTestUserToolsPermitted(mode: string = import.meta.env.MODE): boolean {
  return areDevelopmentToolsAllowed(mode) || isLocalValidationToolsEnabled();
}
