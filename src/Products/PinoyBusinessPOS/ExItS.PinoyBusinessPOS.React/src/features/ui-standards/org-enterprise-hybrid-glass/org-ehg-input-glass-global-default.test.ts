import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE,
  ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND,
  ORG_EHG_INPUT_GLASS_DEFAULT_ICON,
  ORG_EHG_INPUT_GLASS_DEFAULT_SHADOW,
  ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE,
  ORG_EHG_INPUT_GLASS_DEFAULT_SIZE,
  ORG_EHG_INPUT_GLASS_DEFAULT_TONE,
  ORG_EHG_INPUT_GLASS_RADIUS,
  ORG_EHG_INPUT_GLASS_TITLE,
  ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND,
  ORG_EHG_SEARCH_GLASS_DEFAULT_ICON,
  ORG_EHG_SEARCH_GLASS_DEFAULT_ICON_ID,
  ORG_EHG_SEARCH_GLASS_TITLE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-input-glass-global-default";
import { ORG_EHG_QA_GLASS_RADIUS } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-qa-glass-global-default";
import { SOFT_GLASS_FIELD_RADIUS } from "@/features/ui-standards/soft-glass-card-global-default";

const playgroundSrc = readFileSync(
  resolve(__dirname, "OrgEhgQaInputGlassPlayground.tsx"),
  "utf8",
);

describe("org-ehg-input-glass-global-default", () => {
  it("locks the Input glass creation command", () => {
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND).toBe(
      "ext org-ehg input-glass qa primary stacked md icon-none shape-standard no-shadow",
    );
    expect(ORG_EHG_INPUT_GLASS_TITLE).toBe("Input glass · Primary stacked");
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_TONE).toBe("primary");
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE).toBe("stacked");
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_SIZE).toBe("md");
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_ICON).toBe("icon-none");
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE).toBe("standard");
    expect(ORG_EHG_INPUT_GLASS_DEFAULT_SHADOW).toBe("no-shadow");
  });

  it("locks the Search glass creation command (bare search mark)", () => {
    expect(ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND).toBe(
      "ext org-ehg input-glass qa primary stacked md icon-bare shape-standard no-shadow",
    );
    expect(ORG_EHG_SEARCH_GLASS_TITLE).toBe("Search glass · Primary stacked bare");
    expect(ORG_EHG_SEARCH_GLASS_DEFAULT_ICON).toBe("icon-bare");
    expect(ORG_EHG_SEARCH_GLASS_DEFAULT_ICON_ID).toBe("search");
  });

  it("locks Soft control field radius with Org EHG QA / Soft Glass fields", () => {
    expect(ORG_EHG_INPUT_GLASS_RADIUS).toBe("0.6875rem");
    expect(ORG_EHG_INPUT_GLASS_RADIUS).toBe(ORG_EHG_QA_GLASS_RADIUS);
    expect(ORG_EHG_INPUT_GLASS_RADIUS).toBe(SOFT_GLASS_FIELD_RADIUS);
  });

  it("wires Input glass playground defaults to the lock", () => {
    expect(playgroundSrc).toContain("org-ehg-input-glass-global-default");
    expect(playgroundSrc).toContain("ORG_EHG_INPUT_GLASS_DEFAULT_TONE");
    expect(playgroundSrc).toContain("ORG_EHG_INPUT_GLASS_DEFAULT_APPEARANCE");
    expect(playgroundSrc).toContain("ORG_EHG_INPUT_GLASS_DEFAULT_SIZE");
    expect(playgroundSrc).toContain("ORG_EHG_INPUT_GLASS_DEFAULT_SHAPE");
    expect(playgroundSrc).toContain("ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND");
    expect(playgroundSrc).toContain("ORG_EHG_SEARCH_GLASS_DEFAULT_COMMAND");
  });
});
