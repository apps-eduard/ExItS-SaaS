import { describe, expect, it } from "vitest";
import { renderHook } from "@testing-library/react";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import {
  UI_STANDARDS_DATA_PREVIEW_WIDTHS,
  uiStandardsDataPreviewLabel,
} from "@/features/ui-standards/UiStandardsResponsiveDataDeviceFrame";

describe("useResponsiveDataLayout layoutWidthPx override", () => {
  it("uses preview width instead of viewport media for TABLE vs LIST", () => {
    const desktop = renderHook(() =>
      useResponsiveDataLayout({
        tableMinWidthPx: 1024,
        layoutWidthPx: UI_STANDARDS_DATA_PREVIEW_WIDTHS.desktop,
      }),
    );
    expect(desktop.result.current.layout).toBe("table");

    const tablet = renderHook(() =>
      useResponsiveDataLayout({
        tableMinWidthPx: 1024,
        layoutWidthPx: UI_STANDARDS_DATA_PREVIEW_WIDTHS.tablet,
      }),
    );
    expect(tablet.result.current.layout).toBe("list");

    const mobile = renderHook(() =>
      useResponsiveDataLayout({
        tableMinWidthPx: 1024,
        layoutWidthPx: UI_STANDARDS_DATA_PREVIEW_WIDTHS.mobile,
      }),
    );
    expect(mobile.result.current.layout).toBe("list");
  });
});

describe("uiStandardsDataPreviewLabel", () => {
  it("formats device · width · mode", () => {
    expect(uiStandardsDataPreviewLabel("desktop", "table")).toBe("Desktop · 1280px · TABLE");
    expect(uiStandardsDataPreviewLabel("tablet", "list")).toBe("Tablet · 834px · LIST");
    expect(uiStandardsDataPreviewLabel("mobile", "list")).toBe("Mobile · 390px · LIST");
  });
});
