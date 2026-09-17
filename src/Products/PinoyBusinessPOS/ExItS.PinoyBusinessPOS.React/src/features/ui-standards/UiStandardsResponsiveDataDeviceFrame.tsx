import type { ReactNode } from "react";
import { SettingsSelect } from "@/components/ui/settings-select";

/** Design preview widths for /ui-standards QA — not production breakpoints. */
export const UI_STANDARDS_DATA_PREVIEW_WIDTHS = {
  desktop: 1280,
  tablet: 834,
  mobile: 390,
} as const;

export type UiStandardsDataPreviewDevice = keyof typeof UI_STANDARDS_DATA_PREVIEW_WIDTHS;

export const UI_STANDARDS_DATA_PREVIEW_DEFAULT: UiStandardsDataPreviewDevice = "desktop";

export function uiStandardsDataPreviewLabel(
  device: UiStandardsDataPreviewDevice,
  layout: "table" | "list",
): string {
  const width = UI_STANDARDS_DATA_PREVIEW_WIDTHS[device];
  const deviceLabel =
    device === "desktop" ? "Desktop" : device === "tablet" ? "Tablet" : "Mobile";
  const mode = layout === "table" ? "TABLE" : "LIST";
  return `${deviceLabel} · ${width}px · ${mode}`;
}

type UiStandardsResponsiveDataDeviceFrameProps = {
  device: UiStandardsDataPreviewDevice;
  onDeviceChange: (device: UiStandardsDataPreviewDevice) => void;
  layout: "table" | "list";
  children: ReactNode;
  /** Prefix for data-testid hooks (defaults to responsive-data sample ids). */
  testIdPrefix?: string;
};

/**
 * Preview-size chrome for /ui-standards data samples.
 * Caps visual width; layout mode comes from `layoutWidthPx` override on the hook.
 */
export function UiStandardsResponsiveDataDeviceFrame({
  device,
  onDeviceChange,
  layout,
  children,
  testIdPrefix = "ui-standard-responsive-data",
}: UiStandardsResponsiveDataDeviceFrameProps) {
  const widthPx = UI_STANDARDS_DATA_PREVIEW_WIDTHS[device];

  return (
    <div className="flex min-w-0 flex-col gap-2" data-testid={`${testIdPrefix}-preview`}>
      <SettingsSelect<UiStandardsDataPreviewDevice>
        label="Preview size"
        value={device}
        onChange={onDeviceChange}
        variant="segmented"
        testId={`${testIdPrefix}-preview-size`}
        options={[
          { value: "desktop", label: "Desktop" },
          { value: "tablet", label: "Tablet" },
          { value: "mobile", label: "Mobile" },
        ]}
      />
      <p
        className="m-0 text-center text-[length:var(--exits-text-xs)] font-medium text-muted"
        data-testid={`${testIdPrefix}-mode`}
      >
        {uiStandardsDataPreviewLabel(device, layout)}
      </p>
      <div className="flex min-w-0 justify-center overflow-x-hidden">
        <div
          className="min-w-0 w-full overflow-x-hidden rounded-[var(--exits-radius-md)] border border-border bg-[color-mix(in_srgb,var(--exits-surface-muted)_35%,var(--exits-surface))] p-2"
          style={{ maxWidth: widthPx }}
          data-preview-device={device}
          data-preview-width={widthPx}
          data-testid={`${testIdPrefix}-frame`}
        >
          {children}
        </div>
      </div>
    </div>
  );
}
