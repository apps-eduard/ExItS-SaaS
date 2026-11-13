import { describe, expect, it } from "vitest";
import { toastCursorCommand } from "./toast-cursor-command";

describe("toastCursorCommand", () => {
  it("defaults to info bottom-end without flags", () => {
    expect(toastCursorCommand()).toBe("ext toast");
  });

  it("includes tone, position, and description when set", () => {
    expect(toastCursorCommand({ tone: "success" })).toBe("ext toast success");
    expect(toastCursorCommand({ tone: "danger", description: true })).toBe("ext toast danger description");
    expect(toastCursorCommand({ tone: "info", description: true })).toBe("ext toast description");
    expect(toastCursorCommand({ tone: "warning", position: "top-start" })).toBe("ext toast warning top-start");
    expect(toastCursorCommand({ tone: "success", position: "top-end", description: true })).toBe(
      "ext toast success top-end description",
    );
    expect(toastCursorCommand({ position: "bottom-center" })).toBe("ext toast bottom-center");
  });
});
