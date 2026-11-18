import { describe, expect, it } from "vitest";
import {
  PERSONAL_MAIN_CARD_HEADER_CHIPS,
  PERSONAL_MAIN_CARD_HEADER_DARK_INK,
  PERSONAL_MAIN_CARD_HEADER_DEFAULT_COMMAND,
  PERSONAL_MAIN_CARD_HEADER_GAP,
  PERSONAL_MAIN_CARD_HEADER_ICON_MARK,
  PERSONAL_MAIN_CARD_HEADER_ICON_RADIUS,
  PERSONAL_MAIN_CARD_HEADER_ICON_SIZE,
  PERSONAL_MAIN_CARD_HEADER_LABEL_SIZE,
  PERSONAL_MAIN_CARD_HEADER_LABEL_WEIGHT,
  PERSONAL_MAIN_CARD_HEADER_LIGHT_LABEL_BLACK_PCT,
  PERSONAL_MAIN_CARD_HEADER_REFERENCE,
} from "./personal-main-card-header-global-default";

describe("personal-main-card-header-global-default", () => {
  it("locks Personal main-card header icon + label", () => {
    expect(PERSONAL_MAIN_CARD_HEADER_DEFAULT_COMMAND).toBe(
      "ext personal main-card-header icon-round label",
    );
    expect(PERSONAL_MAIN_CARD_HEADER_REFERENCE).toContain("stores to pay");
    expect(PERSONAL_MAIN_CARD_HEADER_ICON_SIZE).toBe("2rem");
    expect(PERSONAL_MAIN_CARD_HEADER_ICON_RADIUS).toBe("999px");
    expect(PERSONAL_MAIN_CARD_HEADER_ICON_MARK).toBe("1.05rem");
    expect(PERSONAL_MAIN_CARD_HEADER_LABEL_SIZE).toBe("1.05rem");
    expect(PERSONAL_MAIN_CARD_HEADER_LABEL_WEIGHT).toBe(700);
    expect(PERSONAL_MAIN_CARD_HEADER_GAP).toBe("0.55rem");
    expect(PERSONAL_MAIN_CARD_HEADER_DARK_INK).toBe("#f1f5f9");
    expect(PERSONAL_MAIN_CARD_HEADER_LIGHT_LABEL_BLACK_PCT).toBe(28);
  });

  it("retains per-card identity chip colors", () => {
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.stores).toBe("var(--exits-primary)");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.attention).toBe("#ff5722");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.quickActions).toBe("#ff5722");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.todo).toBe("var(--exits-primary)");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.tracker).toBe("#22c55e");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.activity).toBe("var(--exits-primary)");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.people).toBe("#4169e1");
    expect(PERSONAL_MAIN_CARD_HEADER_CHIPS.moneyOwe).toBe("var(--exits-primary)");
  });
});
