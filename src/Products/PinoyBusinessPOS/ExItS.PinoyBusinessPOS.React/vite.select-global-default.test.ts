import { describe, expect, it } from "vitest";
import { selectGlobalDefaultSource } from "./vite.select-global-default.js";

describe("select global default", () => {
  it("writes the locked command", () => {
    const source = selectGlobalDefaultSource("primary", "outlined");
    expect(source).toContain('export const SELECT_GLOBAL_DEFAULT_INTENT = "primary" as const;');
    expect(source).toContain('export const SELECT_GLOBAL_DEFAULT_APPEARANCE = "outlined" as const;');
    expect(source).toContain("Command: ext dropdown primary outlined");
  });

  it("rejects an unknown appearance", () => {
    expect(selectGlobalDefaultSource("primary", "pill")).toBeNull();
  });
});
