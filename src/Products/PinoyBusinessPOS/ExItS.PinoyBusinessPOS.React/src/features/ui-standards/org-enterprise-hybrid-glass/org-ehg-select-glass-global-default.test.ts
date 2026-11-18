import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_SELECT_GLASS_DEFAULT_ICON,
  ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION,
  ORG_EHG_SELECT_GLASS_DEFAULT_SHADOW,
  ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE,
  ORG_EHG_SELECT_GLASS_DEFAULT_SIZE,
  ORG_EHG_SELECT_GLASS_DEFAULT_TONE,
  ORG_EHG_SELECT_GLASS_RADIUS,
  ORG_EHG_SELECT_GLASS_TITLE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-select-glass-global-default";
import { ORG_EHG_QA_GLASS_RADIUS } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-qa-glass-global-default";
import { SOFT_GLASS_FIELD_RADIUS } from "@/features/ui-standards/soft-glass-card-global-default";

const playgroundSrc = readFileSync(
  resolve(__dirname, "OrgEhgQaSelectGlassPlayground.tsx"),
  "utf8",
);

describe("org-ehg-select-glass-global-default", () => {
  it("locks the Select glass creation command", () => {
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND).toBe(
      "ext org-ehg select-glass qa primary stacked md icon-none shape-standard no-shadow",
    );
    expect(ORG_EHG_SELECT_GLASS_TITLE).toBe("Select glass · Primary stacked");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_TONE).toBe("primary");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE).toBe("stacked");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_SIZE).toBe("md");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_ICON).toBe("icon-none");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE).toBe("standard");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_SHADOW).toBe("no-shadow");
    expect(ORG_EHG_SELECT_GLASS_DEFAULT_MENU_TRANSITION).toBe("scale");
  });

  it("locks Soft control field radius with Org EHG QA / Soft Glass fields", () => {
    expect(ORG_EHG_SELECT_GLASS_RADIUS).toBe("0.6875rem");
    expect(ORG_EHG_SELECT_GLASS_RADIUS).toBe(ORG_EHG_QA_GLASS_RADIUS);
    expect(ORG_EHG_SELECT_GLASS_RADIUS).toBe(SOFT_GLASS_FIELD_RADIUS);
  });

  it("wires Select glass playground defaults to the lock", () => {
    expect(playgroundSrc).toContain("org-ehg-select-glass-global-default");
    expect(playgroundSrc).toContain("ORG_EHG_SELECT_GLASS_DEFAULT_TONE");
    expect(playgroundSrc).toContain("ORG_EHG_SELECT_GLASS_DEFAULT_APPEARANCE");
    expect(playgroundSrc).toContain("ORG_EHG_SELECT_GLASS_DEFAULT_SIZE");
    expect(playgroundSrc).toContain("ORG_EHG_SELECT_GLASS_DEFAULT_SHAPE");
    expect(playgroundSrc).toContain("ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND");
  });
});
