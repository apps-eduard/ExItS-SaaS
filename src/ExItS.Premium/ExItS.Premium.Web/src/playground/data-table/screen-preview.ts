export const DATA_TABLE_PREVIEW_PATH = "/data-table-preview";

export const SCREEN_PREVIEWS = [
  { id: "mobile-320", label: "Small mobile — 320px", width: 320, height: 640 },
  { id: "mobile-390", label: "Mobile — 390px", width: 390, height: 844 },
  { id: "tablet-768", label: "Tablet — 768px", width: 768, height: 1024 },
  { id: "desktop-1280", label: "Desktop — 1280px", width: 1280, height: 800 },
  { id: "large-1440", label: "Large Screen — 1440px", width: 1440, height: 900 },
] as const;

export type ScreenPreviewId = (typeof SCREEN_PREVIEWS)[number]["id"];

export const DEFAULT_SCREEN_PREVIEW: ScreenPreviewId = "desktop-1280";

export function screenPreviewById(id: string) {
  return SCREEN_PREVIEWS.find((item) => item.id === id) ?? SCREEN_PREVIEWS.find((item) => item.id === DEFAULT_SCREEN_PREVIEW)!;
}

/** Visual scale only. The iframe keeps `viewportWidth` as its CSS viewport. */
export function screenPreviewScale(availableWidth: number, viewportWidth: number) {
  if (viewportWidth <= 0 || availableWidth <= 0) return 1;
  return Math.min(1, availableWidth / viewportWidth);
}
