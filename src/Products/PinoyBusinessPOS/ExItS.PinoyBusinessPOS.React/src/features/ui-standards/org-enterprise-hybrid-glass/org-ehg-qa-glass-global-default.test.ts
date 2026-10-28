import { describe, expect, it } from "vitest";
import {
  ORG_EHG_QA_GLASS_CLASS_NAME,
  ORG_EHG_QA_GLASS_CLASS_TOKENS,
  ORG_EHG_QA_GLASS_DARK_CHIP_FILL_PCT,
  ORG_EHG_QA_GLASS_DARK_INK,
  ORG_EHG_QA_GLASS_DEFAULT_CARD,
  ORG_EHG_QA_GLASS_DEFAULT_COMMAND,
  ORG_EHG_QA_GLASS_DEFAULT_ICON,
  ORG_EHG_QA_GLASS_DEFAULT_SHADOW,
  ORG_EHG_QA_GLASS_DEFAULT_SIZE,
  ORG_EHG_QA_GLASS_ICON_RADIUS,
  ORG_EHG_QA_GLASS_LIGHT_CHIP_BORDER_PCT,
  ORG_EHG_QA_GLASS_LIGHT_CHIP_FILL_PCT,
  ORG_EHG_QA_GLASS_NEW_COLOR,
  ORG_EHG_QA_GLASS_NEW_IDENTITY,
  ORG_EHG_QA_GLASS_RADIUS,
  ORG_EHG_QA_GLASS_REFERENCE,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-qa-glass-global-default";
import { SOFT_GLASS_FIELD_RADIUS } from "@/features/ui-standards/soft-glass-card-global-default";

describe("Org EHG QA glass global default", () => {
  it("locks Stores to pay recipe command and tokens", () => {
    expect(ORG_EHG_QA_GLASS_DEFAULT_COMMAND).toBe(
      "ext org-ehg qa-glass md icon-soft card-soft trail-auto no-shadow no-icon-shadow",
    );
    expect(ORG_EHG_QA_GLASS_REFERENCE).toContain("stores to pay");
    expect(ORG_EHG_QA_GLASS_DEFAULT_SIZE).toBe("md");
    expect(ORG_EHG_QA_GLASS_DEFAULT_ICON).toBe("icon-soft");
    expect(ORG_EHG_QA_GLASS_DEFAULT_CARD).toBe("card-soft");
    expect(ORG_EHG_QA_GLASS_DEFAULT_SHADOW).toBe("no-shadow");
    expect(ORG_EHG_QA_GLASS_CLASS_TOKENS).toContain("personal-qa-glass--card-soft");
    expect(ORG_EHG_QA_GLASS_CLASS_NAME).toContain("personal-qa-glass--card-soft");
    expect(ORG_EHG_QA_GLASS_CLASS_NAME).toContain("personal-qa-glass--md");
  });

  it("locks Soft control shell radius and squircle icon", () => {
    expect(ORG_EHG_QA_GLASS_RADIUS).toBe("0.6875rem");
    expect(ORG_EHG_QA_GLASS_RADIUS).toBe(SOFT_GLASS_FIELD_RADIUS);
    expect(ORG_EHG_QA_GLASS_ICON_RADIUS).toBe("0.65rem");
  });

  it("locks light and dark wash percentages", () => {
    expect(ORG_EHG_QA_GLASS_LIGHT_CHIP_BORDER_PCT).toBe(28);
    expect(ORG_EHG_QA_GLASS_LIGHT_CHIP_FILL_PCT).toBe(8);
    expect(ORG_EHG_QA_GLASS_DARK_CHIP_FILL_PCT).toBe(12);
    expect(ORG_EHG_QA_GLASS_DARK_INK).toBe("#f1f5f9");
  });

  it("locks new-QA color to Preferences primary; convert keeps identity", () => {
    expect(ORG_EHG_QA_GLASS_NEW_IDENTITY).toBe("primary");
    expect(ORG_EHG_QA_GLASS_NEW_COLOR).toBe("var(--exits-primary)");
  });
});
