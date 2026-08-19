import { afterEach, describe, expect, it, vi } from "vitest";
import { copyDiagnosticText } from "@/lib/diagnostics/copy-diagnostic-text";

describe("copyDiagnosticText", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns true when writeText succeeds", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal("navigator", { clipboard: { writeText } });
    await expect(copyDiagnosticText("EXITS ERROR DIAGNOSTICS")).resolves.toBe(true);
    expect(writeText).toHaveBeenCalledWith("EXITS ERROR DIAGNOSTICS");
  });

  it("returns false when writeText fails", async () => {
    vi.stubGlobal("navigator", {
      clipboard: { writeText: vi.fn().mockRejectedValue(new Error("denied")) },
    });
    await expect(copyDiagnosticText("EXITS ERROR DIAGNOSTICS")).resolves.toBe(false);
  });

  it("returns false when clipboard is missing", async () => {
    vi.stubGlobal("navigator", {});
    await expect(copyDiagnosticText("EXITS ERROR DIAGNOSTICS")).resolves.toBe(false);
  });
});
