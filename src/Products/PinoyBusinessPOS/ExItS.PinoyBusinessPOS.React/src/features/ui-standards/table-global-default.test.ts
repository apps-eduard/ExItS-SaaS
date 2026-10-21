import { describe, expect, it } from "vitest";
import {
  TABLE_GLOBAL_DEFAULT_COMMAND,
  TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_FOOTER_INTENT,
  TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE,
  TABLE_GLOBAL_DEFAULT_HEADER_INTENT,
  TABLE_GLOBAL_DEFAULT_INTENT,
  TABLE_GLOBAL_DEFAULT_SEARCH,
  TABLE_GLOBAL_DEFAULT_SHELL,
  TABLE_GLOBAL_HEADER_COL_EDGE_DARK,
  TABLE_GLOBAL_HEADER_COL_EDGE_LIGHT,
  TABLE_GLOBAL_HEADER_RAISE_SHADOW,
  TABLE_GLOBAL_SOFT_GLASS_RADIUS,
} from "@/features/ui-standards/table-global-default";
import {
  TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE,
  TABLE_FOOTER_GLOBAL_DEFAULT_INTENT,
} from "@/features/ui-standards/table-footer-global-default";

describe("Table global default", () => {
  it("locks the default standard Cursor command", () => {
    expect(TABLE_GLOBAL_DEFAULT_COMMAND).toBe(
      "ext table soft-glass header-glossy-raise footer-glossy-raise search",
    );
  });

  it("locks Soft Glass shell with glossy-raise header/footer plus search", () => {
    expect(TABLE_GLOBAL_DEFAULT_INTENT).toBe("standard");
    expect(TABLE_GLOBAL_DEFAULT_SHELL).toBe("soft-glass");
    expect(TABLE_GLOBAL_SOFT_GLASS_RADIUS).toBe("1.1rem");
    expect(TABLE_GLOBAL_DEFAULT_HEADER_INTENT).toBe("standard");
    expect(TABLE_GLOBAL_DEFAULT_HEADER_APPEARANCE).toBe("glossy-raise");
    expect(TABLE_GLOBAL_DEFAULT_FOOTER_INTENT).toBe("standard");
    expect(TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE).toBe("glossy-raise");
    expect(TABLE_GLOBAL_DEFAULT_SEARCH).toBe(true);
  });

  it("locks glossy-raise header soft raise and column side edges", () => {
    expect(TABLE_GLOBAL_HEADER_RAISE_SHADOW).toBe("0 1px 2px rgba(0, 0, 0, 0.08)");
    expect(TABLE_GLOBAL_HEADER_COL_EDGE_LIGHT).toBe(
      "inset 2px 0 3px -1px rgba(255, 255, 255, 0.7)",
    );
    expect(TABLE_GLOBAL_HEADER_COL_EDGE_DARK).toBe("inset -2px 0 3px -1px rgba(0, 0, 0, 0.12)");
  });

  it("keeps footer chrome lock appearance in sync", () => {
    expect(TABLE_FOOTER_GLOBAL_DEFAULT_INTENT).toBe(TABLE_GLOBAL_DEFAULT_FOOTER_INTENT);
    expect(TABLE_FOOTER_GLOBAL_DEFAULT_APPEARANCE).toBe(TABLE_GLOBAL_DEFAULT_FOOTER_APPEARANCE);
  });
});
