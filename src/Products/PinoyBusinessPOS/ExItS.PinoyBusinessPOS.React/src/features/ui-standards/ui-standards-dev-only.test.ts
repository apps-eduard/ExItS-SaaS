import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative, resolve } from "node:path";
import { describe, expect, it } from "vitest";

const srcDir = resolve(__dirname, "../..");
const read = (path: string) => readFileSync(resolve(srcDir, path), "utf8");

/** Showcase classes that product components render; these stay in globals.css. */
const PRODUCT_SHOWCASE_CLASSES = new Set([
  "ui-standards-qcheckbox",
  "ui-standards-qcheckbox--dense",
  "ui-standards-qcheckbox--disabled",
  "ui-standards-qcheckbox--on-dark",
  "ui-standards-qcheckbox__inner",
  "ui-standards-qcheckbox__inner--truthy",
  "ui-standards-qcheckbox__inner--indet",
  "ui-standards-qcheckbox__bg",
  "ui-standards-qcheckbox__svg",
  "ui-standards-qcheckbox__truthy",
  "ui-standards-qcheckbox__label",
  "ui-standards-qbtn--raised",
  "ui-standards-qcard",
  "ui-standards-qcard--settings-card",
  "ui-standards-qcard__settings-circle",
  "ui-standards-qcard__settings-circle-core",
  "ui-standards-qcard__settings-fold",
  "ui-standards-qcard__settings-ribbon",
  "ui-standards-qcard__settings-tab",
]);

function sourceFiles(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name);
    if (statSync(full).isDirectory()) return sourceFiles(full);
    return /\.(ts|tsx)$/.test(name) && !/\.test\.tsx?$/.test(name) ? [full] : [];
  });
}

describe("UI Standards is development-only", () => {
  it("registers the route and page only behind import.meta.env.DEV", () => {
    const router = read("app/router.tsx");
    expect(router).not.toMatch(/import\s*\{\s*UiStandardsPage\s*\}\s*from/);
    expect(router).toMatch(
      /const UiStandardsPage = import\.meta\.env\.DEV\s*\?\s*lazy\(\(\) =>\s*import\("@\/features\/ui-standards\/UiStandardsPage"\)/,
    );
    expect(router).toContain("...uiStandardsDevRoutes,");
    expect(router).not.toMatch(/path:\s*"ui-standards",\s*element:\s*<UiStandardsPage \/>\s*\}/);
  });

  it("hides the shell shortcut outside development", () => {
    expect(read("components/exits/ShellUiStandardsButton.tsx")).toContain(
      "if (!import.meta.env.DEV || !isFrontendLocalValidationMode())",
    );
  });

  it("keeps showcase-only CSS out of the global stylesheet", () => {
    const globals = read("styles/globals.css");
    const leaked = [...new Set([...globals.matchAll(/\.(ui-standards-[\w-]+)/g)].map((m) => m[1]))].filter(
      (name) => !PRODUCT_SHOWCASE_CLASSES.has(name),
    );
    expect(leaked).toEqual([]);
  });

  it("loads the showcase stylesheet only from the UI Standards page", () => {
    const importers = sourceFiles(srcDir)
      .filter((file) => readFileSync(file, "utf8").includes("ui-standards/ui-standards.css"))
      .map((file) => relative(srcDir, file).replace(/\\/g, "/"));
    expect(importers).toEqual(["features/ui-standards/UiStandardsPage.tsx"]);
  });

  it("is not imported by product code", () => {
    const importers = sourceFiles(srcDir)
      .map((file) => relative(srcDir, file).replace(/\\/g, "/"))
      .filter((file) => !file.startsWith("features/ui-standards/") && file !== "app/router.tsx")
      .filter((file) => /from "@\/features\/ui-standards\/UiStandards/.test(read(file)));
    expect(importers).toEqual([]);
  });
});
