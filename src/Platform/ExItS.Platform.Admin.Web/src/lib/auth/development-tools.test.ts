import { describe, expect, it, vi } from "vitest";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";

describe("areDevelopmentToolsAllowed", () => {
  it("is absent in production builds", () => {
    vi.stubEnv("MODE", "production");
    expect(areDevelopmentToolsAllowed()).toBe(false);
    vi.unstubAllEnvs();
  });
});
