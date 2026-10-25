import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative, resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  ORG_EHG_CATEGORIES,
  ORG_EHG_LIVE_CATEGORIES,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/UiStandardsOrgHybridGlass";
import {
  UI_STANDARD_ORG_NAV,
  UI_STANDARD_PERSONAL_NAV,
  isUiStandardNavGroup,
  isUiStandardNavSubgroup,
} from "@/features/ui-standards/ui-standard-catalog";

const srcDir = resolve(__dirname, "../../..");
const read = (path: string) => readFileSync(resolve(srcDir, path), "utf8");

function sourceFiles(dir: string): string[] {
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name);
    if (statSync(full).isDirectory()) return sourceFiles(full);
    return /\.(ts|tsx|css)$/.test(name) && !/\.test\.tsx?$/.test(name) ? [full] : [];
  });
}

describe("Organization Enterprise Hybrid Glass isolation", () => {
  it("keeps Personal playground nav separate from Organization Hybrid Glass nav", () => {
    expect(UI_STANDARD_PERSONAL_NAV.some((e) => !isUiStandardNavGroup(e) && e.id === "all")).toBe(
      true,
    );
    const personalButtons = UI_STANDARD_PERSONAL_NAV.find(
      (e) => isUiStandardNavGroup(e) && e.id === "button-components",
    );
    expect(personalButtons && isUiStandardNavGroup(personalButtons)).toBe(true);
    if (!personalButtons || !isUiStandardNavGroup(personalButtons)) return;
    expect(personalButtons.children.map((c) => ("id" in c ? c.id : ""))).not.toContain("button-glass");

    const personalGlass = UI_STANDARD_PERSONAL_NAV.find(
      (e) => isUiStandardNavGroup(e) && e.id === "glass-components",
    );
    expect(personalGlass && isUiStandardNavGroup(personalGlass)).toBe(true);
    if (!personalGlass || !isUiStandardNavGroup(personalGlass)) return;
    expect(personalGlass.label).toBe("Glass components");
    expect(personalGlass.children.map((c) => ("id" in c ? c.id : ""))).toEqual([
      "button-glass",
      "fab-glass",
      "input-glass",
      "date-glass",
      "quick-action-glass",
      "card-glass",
    ]);
    const personalCardGlass = personalGlass.children.find(
      (c) => isUiStandardNavSubgroup(c) && c.id === "card-glass",
    );
    expect(personalCardGlass && isUiStandardNavSubgroup(personalCardGlass)).toBe(true);
    if (!personalCardGlass || !isUiStandardNavSubgroup(personalCardGlass)) return;
    expect(personalCardGlass.label).toBe("Card glass");
    expect(personalCardGlass.children.map((c) => ("id" in c ? c.id : ""))).toEqual([
      "card-glass-menu",
      "card-glass-soft-kpi",
      "card-glass-hybrid-kpi",
      "card-glass-solid",
      "card-glass-timeline",
      "card-glass-table",
      "card-glass-settings",
    ]);

    const personalForms = UI_STANDARD_PERSONAL_NAV.find(
      (e) => isUiStandardNavGroup(e) && e.id === "form-components",
    );
    expect(personalForms && isUiStandardNavGroup(personalForms)).toBe(true);
    if (!personalForms || !isUiStandardNavGroup(personalForms)) return;
    expect(personalForms.children.map((c) => ("id" in c ? c.id : ""))).not.toContain("input-glass");

    const liveGroup = UI_STANDARD_ORG_NAV.find(
      (e) => isUiStandardNavGroup(e) && e.id === "org-enterprise-hybrid-glass",
    );
    expect(liveGroup && isUiStandardNavGroup(liveGroup)).toBe(true);
    if (!liveGroup || !isUiStandardNavGroup(liveGroup)) return;
    expect(liveGroup.label).toContain("Organization");
    expect(liveGroup.label).toContain("Enterprise Hybrid Glass");
    const flattenLeaves = (
      children: readonly { id?: string; children?: readonly { id?: string; children?: readonly unknown[] }[] }[],
    ): string[] =>
      children.flatMap((child) => {
        if (child.children) {
          return flattenLeaves(child.children as typeof children);
        }
        return child.id ? [child.id] : [];
      });
    const leafIds = flattenLeaves(liveGroup.children);
    expect(leafIds).toEqual([...ORG_EHG_CATEGORIES]);
    expect(leafIds.slice(0, 5)).toEqual([...ORG_EHG_LIVE_CATEGORIES]);
    const glassSub = liveGroup.children.find(
      (c) => isUiStandardNavSubgroup(c) && c.id === "glass-components",
    );
    expect(glassSub && isUiStandardNavSubgroup(glassSub)).toBe(true);
    if (!glassSub || !isUiStandardNavSubgroup(glassSub)) return;
    expect(glassSub.children.map((c) => ("id" in c ? c.id : ""))).toEqual([
      "org-ehg-button-glass",
      "org-ehg-fab-glass",
      "org-ehg-input-glass",
      "org-ehg-date-glass",
      "org-ehg-quick-action-glass",
      "card-glass",
    ]);
    const orgCardGlass = glassSub.children.find(
      (c) => isUiStandardNavSubgroup(c) && c.id === "card-glass",
    );
    expect(orgCardGlass && isUiStandardNavSubgroup(orgCardGlass)).toBe(true);
    if (!orgCardGlass || !isUiStandardNavSubgroup(orgCardGlass)) return;
    expect(orgCardGlass.children.map((c) => ("id" in c ? c.id : ""))).toEqual([
      "org-ehg-card-glass-menu",
      "org-ehg-card-glass-soft-kpi",
      "org-ehg-card-glass-hybrid-kpi",
      "org-ehg-card-glass-solid",
      "org-ehg-card-glass-timeline",
      "org-ehg-card-glass-table",
      "org-ehg-card-glass-settings",
    ]);
  });

  it("scopes class selectors to org-ehg* only", () => {
    const css = read(
      "features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css",
    );
    const withoutUrls = css.replace(/url\((["']).*?\1\)/gs, "url()");
    const classes = [...withoutUrls.matchAll(/\.([a-zA-Z_][\w-]*)/g)].map((m) => m[1]);
    const foreign = [...new Set(classes)].filter((name) => !name.startsWith("org-ehg"));
    expect(foreign).toEqual([]);
  });

  it("does not put .org-ehg rules into globals.css", () => {
    const globals = read("styles/globals.css");
    expect(globals).not.toMatch(/\.org-ehg/);
    expect(globals).not.toMatch(/data-org-ehg/);
  });

  it("does not redefine Personal Soft Glass tokens inside org-ehg CSS", () => {
    const css = read(
      "features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css",
    );
    expect(css).not.toMatch(/--soft-glass-/);
    expect(css).not.toMatch(/\.personal-/);
    expect(css).not.toMatch(/personal-qa-glass/);
  });

  it("is not imported by product feature folders outside ui-standards (exits bridge allowed)", () => {
    const offenders = sourceFiles(srcDir)
      .map((file) => relative(srcDir, file).replace(/\\/g, "/"))
      .filter((file) => !file.startsWith("features/ui-standards/"))
      // Product bridge: components/exits may wrap Org EHG (same pattern as SoftGlassDateTimeField).
      .filter((file) => !file.startsWith("components/exits/"))
      .filter((file) => {
        const text = read(file);
        return (
          text.includes("org-enterprise-hybrid-glass") ||
          text.includes("OrgEhg") ||
          text.includes("data-org-ehg")
        );
      });
    expect(offenders).toEqual([]);
  });
});

