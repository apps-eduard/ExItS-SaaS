import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  INTENT_GLOBAL_AMBER,
  INTENT_GLOBAL_WARN,
  INTENT_GLOBAL_WARN_FOREGROUND,
} from "@/features/ui-standards/intent-color-global-default";

const globalsCss = readFileSync(
  resolve(__dirname, "../../styles/globals.css"),
  "utf8",
);

describe("Intent color global default (warn)", () => {
  it("locks warn to Button Standard orange, never brown", () => {
    expect(INTENT_GLOBAL_WARN).toBe("#ff5722");
    expect(INTENT_GLOBAL_WARN_FOREGROUND).toBe("#ffffff");
    expect(INTENT_GLOBAL_WARN.toLowerCase()).not.toBe("#92400e");
    expect(INTENT_GLOBAL_AMBER).toBe("#ffc107");
    expect(INTENT_GLOBAL_WARN).not.toBe(INTENT_GLOBAL_AMBER);
  });

  it("maps light --exits-warning and --exits-severity-warn to the locked warn", () => {
    expect(globalsCss).toMatch(/--exits-warning:\s*#ff5722;/i);
    expect(globalsCss).toMatch(/--exits-severity-warn:\s*#ff5722;/i);
    expect(globalsCss).not.toMatch(/--exits-warning:\s*#92400e;/i);
  });
});
