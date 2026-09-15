import { describe, expect, it, vi } from "vitest";
import { copyUiSnippet, formatJsxProps } from "@/features/ui-standards/copyUiSnippet";

describe("formatJsxProps", () => {
  it("omits false/undefined and formats strings and booleans", () => {
    expect(
      formatJsxProps({
        intent: "primary",
        disabled: true,
        searchable: false,
        size: undefined,
        "aria-busy": true,
      }),
    ).toBe('  intent="primary"\n  disabled\n  aria-busy');
  });
});

describe("copyUiSnippet", () => {
  it("writes trimmed text to the clipboard API", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    await expect(copyUiSnippet("  <Button />\n")).resolves.toBe(true);
    expect(writeText).toHaveBeenCalledWith("<Button />");
  });
});
