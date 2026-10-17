import { useState, type CSSProperties } from "react";
import { DROPDOWN_STANDARD_COLOR, UiStandardsQdropdown } from "@/features/ui-standards/UiStandardsDropdownGallery";
import { PlaygroundPreviewCard } from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { UiStandardsCopyCommand } from "@/features/ui-standards/UiStandardsCopyCommand";
import { Switch } from "@/components/ui/switch";

/**
 * Shadow playground. One surface shows the ExItS elevation tokens.
 * Showcase only — not a product shadow component.
 */
type ShadowElevation = "none" | "small" | "medium" | "large";

const ELEVATION_OPTIONS: ReadonlyArray<{ label: string; value: ShadowElevation; shadow: string }> = [
  { label: "None", value: "none", shadow: "none" },
  { label: "Small", value: "small", shadow: "var(--exits-shadow-sm)" },
  { label: "Medium", value: "medium", shadow: "var(--exits-shadow-md)" },
  { label: "Large", value: "large", shadow: "var(--exits-shadow-lg)" },
];

function shadowCursorCommand(elevation: ShadowElevation, inset: boolean): string {
  const parts = ["ext shadow"];
  if (elevation !== "medium") parts.push(elevation);
  if (inset && elevation !== "none") parts.push("inset");
  return parts.join(" ");
}

export function UiStandardsShadowPlayground() {
  const [elevation, setElevation] = useState<ShadowElevation>("medium");
  const [inset, setInset] = useState(false);
  const selected = ELEVATION_OPTIONS.find((option) => option.value === elevation) ?? ELEVATION_OPTIONS[2];
  const shadow = inset && selected.shadow !== "none" ? `inset ${selected.shadow}` : selected.shadow;
  const plateStyle = { boxShadow: shadow } as CSSProperties;

  function resetPreview() {
    setElevation("medium");
    setInset(false);
  }

  return (
    <PlaygroundSection testId="ui-standard-shadow-playground" className="">
      <div className="grid min-w-0 gap-2 sm:grid-cols-2">
        <UiStandardsQdropdown
          label="Elevation"
          color={DROPDOWN_STANDARD_COLOR}
          value={selected.label}
          options={ELEVATION_OPTIONS.map((option) => option.label)}
          standard
          searchable={false}
          onSelect={(nextLabel) => {
            const match = ELEVATION_OPTIONS.find((option) => option.label === nextLabel);
            if (match) setElevation(match.value);
          }}
          testId="ui-standard-shadow-pg-elevation"
        />
        <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
          <Switch
            checked={inset}
            onCheckedChange={setInset}
            aria-label="Inset"
            data-testid="ui-standard-shadow-pg-inset"
          />
          Inset
        </label>
      </div>
      <PlaygroundPreviewCard
        previewTestId="ui-standard-shadow-pg-preview"
        themeTestId="ui-standard-shadow-pg-theme"
        resetTestId="ui-standard-shadow-pg-reset"
        onReset={resetPreview}
        previewClassName="ui-standards-shadow-stage"
      >
        <div
          className="ui-standards-shadow-plate"
          style={plateStyle}
          data-testid="ui-standard-shadow-pg-plate"
          data-elevation={elevation}
          data-inset={inset ? "true" : "false"}
        >
          Shadow
        </div>
      </PlaygroundPreviewCard>
      <UiStandardsCopyCommand standard="Shadow" standardStatus="pilot" command={shadowCursorCommand(elevation, inset)} />
    </PlaygroundSection>
  );
}
