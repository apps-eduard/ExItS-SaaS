import { describe, expect, it } from "vitest";
import { toastCursorCommand } from "./toast-cursor-command";

describe("toastCursorCommand", () => {
  it("defaults to info without flags", () => {
    expect(toastCursorCommand()).toBe("ext toast");
  });

  it("includes tone and description when set", () => {
    expect(toastCursorCommand({ tone: "success" })).toBe("ext toast success");
    expect(toastCursorCommand({ tone: "danger", description: true })).toBe("ext toast danger description");
    expect(toastCursorCommand({ tone: "info", description: true })).toBe("ext toast description");
    expect(toastCursorCommand({ tone: "warning" })).toBe("ext toast warning");
  });
});
