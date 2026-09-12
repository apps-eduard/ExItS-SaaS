import { describe, expect, it, beforeEach } from "vitest";
import {
  UI_STANDARDS_DEFAULT_VIEW,
  UI_STANDARDS_VIEW_STORAGE_KEY,
  isUiStandardsViewMode,
  parseUiStandardsViewParam,
  readUiStandardsView,
  writeUiStandardsView,
} from "@/features/ui-standards/ui-standards-view";

describe("ui-standards view storage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("defaults to classic and uses ExItS-namespaced key", () => {
    expect(UI_STANDARDS_DEFAULT_VIEW).toBe("classic");
    expect(UI_STANDARDS_VIEW_STORAGE_KEY).toBe("exits.uiStandards.view.v1");
    expect(readUiStandardsView()).toBe("classic");
  });

  it("persists classic and simple only", () => {
    writeUiStandardsView("simple");
    expect(readUiStandardsView()).toBe("simple");
    writeUiStandardsView("classic");
    expect(readUiStandardsView()).toBe("classic");
    expect(isUiStandardsViewMode("old")).toBe(false);
    window.localStorage.setItem(UI_STANDARDS_VIEW_STORAGE_KEY, "legacy");
    expect(readUiStandardsView()).toBe("classic");
  });

  it("parses view query params", () => {
    expect(parseUiStandardsViewParam("?view=simple")).toBe("simple");
    expect(parseUiStandardsViewParam("view=classic")).toBe("classic");
    expect(parseUiStandardsViewParam("?view=new")).toBeNull();
  });
});
