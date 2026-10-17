import { PrimaryColorControl } from "@/components/exits/PrimaryColorControl";
import { Button } from "@/components/ui/button";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import type {
  ControlShapePreference,
  DensityPreference,
  MotionPreference,
  ThemePreference,
} from "@/lib/preferences/ui-preferences";
import { ChevronDown } from "lucide-react";
import { useState } from "react";

/**
 * Live AUTO previews + compact inline controls wired to the real Preferences store.
 */
export function UiStandardsPreferencePreview() {
  const {
    preferences,
    setControlShape,
    setDensity,
    setTheme,
    setMotion,
  } = usePreferences();
  const [previewSearch, setPreviewSearch] = useState("");
  const [previewQty, setPreviewQty] = useState(2);
  const [open, setOpen] = useState(true);

  return (
    <section
      className="ui-standards-preference-preview flex min-w-0 flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
      data-testid="ui-standards-preference-preview"
    >
      <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
        <button
          type="button"
          className="ui-standards-preview-toggle"
          aria-expanded={open}
          aria-controls="ui-standards-preference-preview-panel"
          data-testid="ui-standards-preference-preview-toggle"
          onClick={() => setOpen((current) => !current)}
        >
          <span>Global preview</span>
          <ChevronDown className="ui-standards-preview-toggle__chevron size-4 shrink-0" aria-hidden />
        </button>
      </h2>
      {open ? (
        <div id="ui-standards-preference-preview-panel" className="flex min-w-0 flex-col gap-3">
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
        Try Preferences → Primary color, Control Shape, Density, Theme, and Motion. Auto samples
        below use the real preference store (no demo-only state).
      </p>

      <PrimaryColorControl />
      <div className="grid min-w-0 gap-2 sm:grid-cols-2 xl:grid-cols-4">
        <SettingsSelect<ControlShapePreference>
          label="Control Shape"
          value={preferences.controlShape}
          onChange={setControlShape}
          variant="segmented"
          testId="ui-standards-pref-control-shape"
          options={[
            { value: "square", label: "Square" },
            { value: "standard", label: "Standard" },
            { value: "soft", label: "Soft" },
            { value: "pill", label: "Pill" },
          ]}
        />
        <SettingsSelect<DensityPreference>
          label="Density"
          value={preferences.density}
          onChange={setDensity}
          variant="segmented"
          compactLabels
          testId="ui-standards-pref-density"
          options={[
            { value: "compact", label: "Compact" },
            { value: "balance", label: "Standard" },
            { value: "comfort", label: "Comfort" },
          ]}
        />
        <SettingsSelect<ThemePreference>
          label="Theme"
          value={preferences.theme}
          onChange={setTheme}
          variant="segmented"
          testId="ui-standards-pref-theme"
          options={[
            { value: "system", label: "System" },
            { value: "light", label: "Light" },
            { value: "dark", label: "Dark" },
          ]}
        />
        <SettingsSelect<MotionPreference>
          label="Motion"
          value={preferences.motion}
          onChange={setMotion}
          variant="segmented"
          testId="ui-standards-pref-motion"
          options={[
            { value: "system", label: "System" },
            { value: "reduced", label: "Reduced" },
          ]}
        />
      </div>

      <div
        className="flex min-w-0 flex-wrap items-end gap-3 border-t border-border pt-3"
        data-testid="ui-standards-preference-auto-samples"
      >
        <div className="flex min-w-0 flex-col gap-1">
          <span className="text-[length:var(--exits-text-xs)] text-muted">Button Auto</span>
          <Button type="button" intent="primary" appearance="solid" shape="auto">
            Save
          </Button>
        </div>
        <div className="flex min-w-0 flex-col gap-1">
          <span className="text-[length:var(--exits-text-xs)] text-muted">StatusChip Auto</span>
          <StatusChip tone="success" appearance="soft" shape="auto">
            Active
          </StatusChip>
        </div>
        <div className="flex min-w-0 flex-col gap-1">
          <span className="text-[length:var(--exits-text-xs)] text-muted">
            QuantityStepper Outline (standard)
          </span>
          <QuantityStepper
            compact
            variant="outline"
            value={previewQty}
            onChange={setPreviewQty}
            min={1}
            step={1}
            precision={0}
            decreaseLabel="Decrease preview quantity"
            increaseLabel="Increase preview quantity"
            ariaLabel="Preference preview quantity"
            valueTestId="ui-standards-preference-qty"
          />
        </div>
        <div className="min-w-0 flex-1 basis-[12rem]">
          <SearchField
            label="Preference preview search"
            value={previewSearch}
            onChange={(e) => setPreviewSearch(e.target.value)}
            onClear={() => setPreviewSearch("")}
            placeholder="Search..."
            shape="auto"
            testId="ui-standards-preference-search"
          />
        </div>
      </div>
        </div>
      ) : null}
    </section>
  );
}
