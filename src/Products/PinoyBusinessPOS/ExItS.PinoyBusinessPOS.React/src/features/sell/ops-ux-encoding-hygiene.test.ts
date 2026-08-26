import fs from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

describe("ops-ux-repair-01 encoding hygiene", () => {
  it("is UTF-8 without BOM and without mojibake arrows", () => {
    const filePath = path.resolve(process.cwd(), "e2e/ops-ux-repair-01.spec.ts");
    const buf = fs.readFileSync(filePath);
    expect(buf[0] === 0xef && buf[1] === 0xbb && buf[2] === 0xbf).toBe(false);
    const text = buf.toString("utf8");
    expect(text).not.toMatch(/â†’|ï»¿|â€”/);
    expect(text).toContain("cashier unregistered -> view-only sell");
  });
});
