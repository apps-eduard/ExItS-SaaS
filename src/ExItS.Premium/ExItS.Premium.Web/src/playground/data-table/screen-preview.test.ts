import { describe, expect, it } from "vitest";
import { DATA_TABLE_PREVIEW_PATH, SCREEN_PREVIEWS, screenPreviewById, screenPreviewScale } from "./screen-preview";

describe("data table screen preview", () => {
  it("offers the required viewport widths and defaults to desktop", () => {
    expect(SCREEN_PREVIEWS.map((item) => item.width)).toEqual([320, 390, 768, 1280, 1440]);
    expect(screenPreviewById("desktop-1280").label).toBe("Desktop — 1280px");
    expect(screenPreviewById("unknown").width).toBe(1280);
    expect(DATA_TABLE_PREVIEW_PATH).toBe("/data-table-preview");
  });

  it("scales the frame down without changing the internal viewport width", () => {
    expect(screenPreviewScale(832, 1280)).toBeCloseTo(832 / 1280);
    expect(screenPreviewScale(1600, 1440)).toBe(1);
    expect(screenPreviewScale(390, 390)).toBe(1);
    expect(screenPreviewScale(0, 320)).toBe(1);
  });
});
