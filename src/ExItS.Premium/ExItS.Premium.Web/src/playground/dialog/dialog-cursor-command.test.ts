import { describe, expect, it } from "vitest";
import { dialogCursorCommand } from "./dialog-cursor-command";

describe("dialogCursorCommand", () => {
  it("defaults to the locked medium dismissible recipe", () => {
    expect(
      dialogCursorCommand({
        size: "md",
        description: true,
        footer: true,
        dismissible: true,
        icon: true,
      }),
    ).toBe("ext dialog premium");
  });

  it("appends size and opt-outs", () => {
    expect(
      dialogCursorCommand({
        size: "sm",
        transition: "fade",
        description: false,
        footer: false,
        dismissible: false,
        icon: false,
        lineTop: false,
        lineBottom: false,
        grayFooter: false,
      }),
    ).toBe(
      "ext dialog premium sm fade no-description no-icon no-footer no-line-top no-line-bottom no-gray-footer persistent",
    );
  });
});
