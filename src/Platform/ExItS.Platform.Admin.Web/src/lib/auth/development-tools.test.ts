import { afterEach, describe, expect, it, vi } from "vitest";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";

describe("areDevelopmentToolsAllowed", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it.each(["development", "test", "testing", "Development", "TEST"] as const)(
    "allows explicit Development/Testing mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(true);
    },
  );

  it.each(["production", "staging", "preview", "qa", "uat", "unknown", "", "  "] as const)(
    "denies unrecognized or non-dev mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(false);
    },
  );
});
