import { readFileSync, existsSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * Lightweight dependency / client-boundary audit notes for WEB-10.
 * Keeps claims grounded in repository files rather than inventing metrics.
 */
describe("WEB-10 dependency and client boundary audit", () => {
  const pkg = JSON.parse(
    readFileSync(join(process.cwd(), "package.json"), "utf8"),
  ) as {
    dependencies: Record<string, string>;
    scripts: Record<string, string>;
  };

  it("keeps Lighthouse CI scripts available", () => {
    expect(pkg.scripts["lighthouse:ci"]).toContain("scripts/lighthouse-run.cjs");
    expect(pkg.scripts["lighthouse:ci:desktop"]).toContain("--desktop");
  });

  it("does not add analytics vendors while WEB-D-02 is open", () => {
    const deps = Object.keys(pkg.dependencies);
    expect(deps).not.toContain("gtag");
    expect(deps.some((name) => /analytics|gtag|plausible|umami|posthog/i.test(name))).toBe(
      false,
    );
  });

  it("documents expected client islands without expanding framer-motion surface", () => {
    expect(pkg.dependencies["framer-motion"]).toBeTruthy();
    expect(existsSync(join(process.cwd(), "src/lib/motion.tsx"))).toBe(true);
  });
});
