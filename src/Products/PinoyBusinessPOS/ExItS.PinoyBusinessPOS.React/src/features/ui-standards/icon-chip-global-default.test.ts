import { describe, expect, it } from "vitest";
import {
  ICON_CHIP_APPEARANCE_CATALOG,
  ICON_CHIP_DEFAULT_APPEARANCE,
  ICON_CHIP_DEFAULT_COMMAND,
  ICON_CHIP_DEFAULT_SHAPE,
  ICON_CHIP_DEFAULT_SIZE,
  ICON_CHIP_DEFAULT_ICON_SIZE,
  ICON_CHIP_DEFAULT_PREVIEW_HOST,
  ICON_CHIP_DEFAULTS,
  ICON_CHIP_GLYPH_MD,
  ICON_CHIP_LIBRARY,
  ICON_CHIP_PREVIEW_HOST_CATALOG,
  ICON_CHIP_SIZE_MD,
  ICON_CHIP_SOFT_FILL_RADIUS,
  ICON_CHIP_SQUIRCLE_RADIUS,
  ICON_CHIP_STROKE,
  iconChipAppearanceIsEhg,
  iconChipCursorCommand,
  iconChipGlyphSizeToken,
  iconChipSizeToken,
} from "@/features/ui-standards/icon-chip-global-default";

describe("Icon chip global default", () => {
  it("locks Lucide icon chrome playground defaults", () => {
    expect(ICON_CHIP_LIBRARY).toBe("lucide");
    expect(ICON_CHIP_DEFAULT_COMMAND).toBe("ext icon soft-primary squircle md");
    expect(ICON_CHIP_DEFAULT_APPEARANCE).toBe("soft-primary");
    expect(ICON_CHIP_DEFAULT_SHAPE).toBe("squircle");
    expect(ICON_CHIP_DEFAULT_SIZE).toBe("md");
    expect(ICON_CHIP_DEFAULT_ICON_SIZE).toBe("md");
    expect(ICON_CHIP_SIZE_MD).toBe("2rem");
    expect(ICON_CHIP_GLYPH_MD).toBe("1.05rem");
    expect(ICON_CHIP_SQUIRCLE_RADIUS).toBe("0.55rem");
    expect(ICON_CHIP_SOFT_FILL_RADIUS).toBe("0.65rem");
    expect(ICON_CHIP_STROKE).toBe(2);
  });

  it("builds default ext icon command with non-default knobs omitted", () => {
    expect(iconChipCursorCommand(ICON_CHIP_DEFAULTS)).toBe("ext icon soft-primary squircle md");
  });

  it("adds optional command tokens for adjusted knobs", () => {
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        appearance: "soft-fill",
        shape: "disc",
        tone: "warn",
        stroke: 1.75,
        glyphId: "cart",
        padding: "loose",
        borderWidth: "medium",
        shadow: false,
        markStyle: "filled",
      }),
    ).toBe(
      "ext icon soft-fill disc md warn stroke-1-75 mark-cart padding-loose border-medium no-shadow glyph-filled",
    );
  });

  it("maps chip and glyph size tokens independently", () => {
    expect(iconChipSizeToken("xl")).toBe("3rem");
    expect(iconChipGlyphSizeToken("xs")).toBe("0.8rem");
    expect(iconChipGlyphSizeToken("lg")).toBe("1.25rem");
  });

  it("emits icon-{size} when mark size differs from chip or default md", () => {
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        size: "lg",
        iconSize: "md",
      }),
    ).toBe("ext icon soft-primary squircle lg icon-md");
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        size: "md",
        iconSize: "lg",
      }),
    ).toBe("ext icon soft-primary squircle md icon-lg");
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        size: "lg",
        iconSize: "lg",
      }),
    ).toBe("ext icon soft-primary squircle lg icon-lg");
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        size: "md",
        iconSize: "md",
      }),
    ).toBe("ext icon soft-primary squircle md");
  });

  it("lists icon playground preview host samples with Action as default", () => {
    expect(ICON_CHIP_PREVIEW_HOST_CATALOG.map((entry) => entry.id)).toEqual([
      "action",
      "button",
      "card",
      "kpi",
      "pennant",
      "list-row",
    ]);
    expect(ICON_CHIP_DEFAULT_PREVIEW_HOST).toBe("action");
  });

  it("lists generic and Org EHG appearance pairs in catalog order", () => {
    expect(ICON_CHIP_APPEARANCE_CATALOG.map((entry) => entry.id)).toEqual([
      "bare",
      "soft-primary",
      "soft-fill",
      "soft-glass",
      "outline",
      "ghost",
      "soft-primary-ehg",
      "soft-fill-ehg",
      "soft-glass-ehg",
      "outline-ehg",
      "ghost-ehg",
    ]);
    expect(iconChipAppearanceIsEhg("soft-fill-ehg")).toBe(true);
    expect(iconChipAppearanceIsEhg("soft-primary")).toBe(false);
  });

  it("emits EHG appearance tokens in ext icon commands", () => {
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        appearance: "soft-primary-ehg",
      }),
    ).toBe("ext icon soft-primary-ehg squircle md");
    expect(
      iconChipCursorCommand({
        ...ICON_CHIP_DEFAULTS,
        appearance: "soft-glass",
      }),
    ).toBe("ext icon soft-glass squircle md");
  });
});