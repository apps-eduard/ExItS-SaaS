import { useState } from "react";
import { Moon, RotateCcw, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { CompactSelect } from "@/features/ui-standards/UiStandardsPlaygrounds";
import { PlaygroundLabel, PlaygroundSection } from "@/features/ui-standards/UiStandardsSnippetBlock";
import { formatPhpCompact } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-format";
import { OrgEhgInputGlassField } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassField";

export type OrgEhgBgOption =
  | "soft-ambient"
  | "subtle-mesh"
  | "soft-aurora"
  | "neutral-solid"
  | "soft-vignette"
  | "horizon-wash"
  | "spotlight-glow";

type PreviewTheme = "light" | "dark";

const BG_OPTIONS: ReadonlyArray<{
  id: OrgEhgBgOption;
  number: number;
  name: string;
  description: string;
  recommended?: boolean;
}> = [
  {
    id: "soft-ambient",
    number: 1,
    name: "Soft Ambient Gradient",
    description:
      "A neutral background with very subtle blue or primary-color lighting around the edges. Elegant in both dark and light modes, without relying on images.",
    recommended: true,
  },
  {
    id: "subtle-mesh",
    number: 2,
    name: "Subtle Mesh Gradient",
    description:
      "Multiple softly blended colors provide more depth behind the glass. Good for dashboards, but should remain understated.",
  },
  {
    id: "soft-aurora",
    number: 3,
    name: "Soft Aurora Background",
    description:
      "Premium flowing aurora-like color bands in blue/teal/violet from theme tokens. Restrained intensity; static CSS only.",
  },
  {
    id: "neutral-solid",
    number: 4,
    name: "Neutral Solid Surface",
    description:
      "The cleanest and lightest option. Excellent for data-heavy screens, although the glass effect is less noticeable.",
  },
  {
    id: "soft-vignette",
    number: 5,
    name: "Soft Vignette",
    description:
      "Bright clear center with soft edge shade so glass cards sit in focus. Reads cleanly in light and dark.",
  },
  {
    id: "horizon-wash",
    number: 6,
    name: "Horizon Wash",
    description:
      "Cool top wash into a soft floor tint. Related light and dark recipes without neon glow.",
  },
  {
    id: "spotlight-glow",
    number: 7,
    name: "Spotlight Glow",
    description:
      "Single soft primary wash from the top center. Quiet accent that follows Preferences primary.",
  },
];

const BG_SELECT_OPTIONS = BG_OPTIONS.map((item) => ({
  label: item.recommended
    ? `${item.number}. ${item.name} (recommended)`
    : `${item.number}. ${item.name}`,
  value: item.id,
}));

const DEFAULTS = {
  option: "soft-ambient" as OrgEhgBgOption,
  theme: "light" as PreviewTheme,
  intensity: 15,
  showGlass: true,
  sideBySide: false,
};

function BgDecorPreview({
  option,
  theme,
}: {
  option: OrgEhgBgOption;
  theme: PreviewTheme;
}) {
  return (
    <div
      className="org-ehg-bg-lab__swatch"
      data-bg-option={option}
      data-org-ehg-theme={theme}
      aria-hidden
    >
      <span className="org-ehg-bg-lab__swatch-base" />
      <span className="org-ehg-bg-lab__swatch-decor" />
    </div>
  );
}

function SampleShell({
  theme,
  option,
  intensity,
  showGlass,
  inputValue,
  onInputChange,
  testId,
}: {
  theme: PreviewTheme;
  option: OrgEhgBgOption;
  intensity: number;
  showGlass: boolean;
  inputValue: string;
  onInputChange: (value: string) => void;
  testId: string;
}) {
  const intensityUnit = Math.max(0, Math.min(100, intensity)) / 100;

  return (
    <div
      className="org-ehg org-ehg-bg-lab__stage"
      data-org-ehg
      data-org-ehg-theme={theme}
      data-bg-option={option}
      data-show-glass={showGlass ? "true" : "false"}
      data-testid={testId}
      style={{ ["--org-ehg-bg-intensity" as string]: String(intensityUnit) }}
    >
      <div className="org-ehg-bg-lab__base" aria-hidden />
      <div className="org-ehg-bg-lab__decor" aria-hidden />
      <div className="org-ehg-bg-lab__content">
        <div className="org-ehg__shell org-ehg-bg-lab__shell">
          <header className="org-ehg__topbar org-ehg-bg-lab__topbar">
            <span className="org-ehg__brand">ABC Sari-Sari</span>
            <span className="org-ehg__hint m-0">Quiapo · Branch</span>
          </header>
          <nav className="org-ehg__sidenav org-ehg-bg-lab__sidenav" aria-label="Sample sidenav">
            <button type="button" className="org-ehg__nav-item" data-active="true">
              Dashboard
            </button>
            <button type="button" className="org-ehg__nav-item">
              Inventory
            </button>
            <button type="button" className="org-ehg__nav-item">
              Orders
            </button>
          </nav>
          <div className="org-ehg__main org-ehg-bg-lab__main">
            <div className="org-ehg__page-header">
              <div>
                <h3 className="org-ehg__page-title" style={{ fontSize: "1rem" }}>
                  Today
                </h3>
                <p className="org-ehg__page-lede">Judge backgrounds with real Hybrid Glass surfaces.</p>
              </div>
              <div className="org-ehg__actions">
                <Button
                  type="button"
                  intent="primary"
                  appearance="solid"
                  shape="soft"
                  size="sm"
                  data-testid={`${testId}-primary-btn`}
                >
                  Record sale
                </Button>
              </div>
            </div>

            <div
              className="org-ehg__kpi-grid"
              style={{ gridTemplateColumns: "1fr", maxWidth: "14rem", marginBottom: "0.75rem" }}
            >
              <article className="org-ehg__kpi org-ehg-bg-lab__kpi" data-testid={`${testId}-kpi`}>
                <p className="org-ehg__kpi-label">Sales</p>
                <p className="org-ehg__kpi-value" style={{ fontSize: "1.05rem" }}>
                  {formatPhpCompact(42850)}
                </p>
                <p className="org-ehg__kpi-meta">KPI glass card</p>
              </article>
            </div>

            <div className="org-ehg__panel org-ehg-bg-lab__solid-card" data-testid={`${testId}-solid-card`}>
              <div className="org-ehg__panel-head">
                <h4 className="org-ehg__panel-title">Solid data card</h4>
              </div>
              <div className="org-ehg__panel-body">
                <p className="org-ehg__hint m-0" style={{ marginBottom: "0.65rem" }}>
                  Opaque work surface stays readable over every background option.
                </p>
                <OrgEhgInputGlassField
                  intensity="medium"
                  size="sm"
                  label="SKU search"
                  placeholder="Scan or type…"
                  value={inputValue}
                  onChange={onInputChange}
                  menuTheme={theme}
                  testId={`${testId}-input`}
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/**
 * Organization Hybrid Glass background playground (UI Standards / DEV only).
 * Local preview state — does not persist preferences or change production Org UI.
 * Extends the existing Appearance lab (not a second playground system).
 */
export function OrgEhgAppearanceLab() {
  const [option, setOption] = useState(DEFAULTS.option);
  const [theme, setTheme] = useState(DEFAULTS.theme);
  const [intensity, setIntensity] = useState(DEFAULTS.intensity);
  const [showGlass, setShowGlass] = useState(DEFAULTS.showGlass);
  const [sideBySide, setSideBySide] = useState(DEFAULTS.sideBySide);
  const [inputValue, setInputValue] = useState("");

  const selected = BG_OPTIONS.find((item) => item.id === option) ?? BG_OPTIONS[0];

  function reset() {
    setOption(DEFAULTS.option);
    setTheme(DEFAULTS.theme);
    setIntensity(DEFAULTS.intensity);
    setShowGlass(DEFAULTS.showGlass);
    setSideBySide(DEFAULTS.sideBySide);
    setInputValue("");
  }

  return (
    <div className="org-ehg-bg-lab" data-testid="org-ehg-appearance">
      <div className="org-ehg" data-org-ehg>
        <p className="org-ehg__banner">
          <strong>Background playground</strong>
          {" — "}
          Pick a page background below. Local preview only; not applied to production Organization UI.
          Uses the Preferences primary color. Personal Soft Glass is unchanged.
        </p>
      </div>

      <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="org-ehg-bg-lab-card">
        <CardTitle>Background playground</CardTitle>
        <PlaygroundSection testId="org-ehg-bg-lab-playground" className="">
          <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            <CompactSelect
              label="Background"
              value={option}
              options={[...BG_SELECT_OPTIONS]}
              onChange={setOption}
              testId="org-ehg-bg-lab-background"
            />
          </div>

          <div
            className="org-ehg-bg-lab__option-list"
            role="radiogroup"
            aria-label="Background option"
            data-testid="org-ehg-bg-lab-options"
          >
            {BG_OPTIONS.map((item) => {
              const isSelected = option === item.id;
              return (
                <button
                  key={item.id}
                  type="button"
                  role="radio"
                  aria-checked={isSelected}
                  className={[
                    "org-ehg-bg-lab__option-row",
                    isSelected ? "org-ehg-bg-lab__option-row--selected" : "",
                  ]
                    .filter(Boolean)
                    .join(" ")}
                  data-testid={`org-ehg-bg-lab-option-${item.id}`}
                  onClick={() => setOption(item.id)}
                >
                  <BgDecorPreview option={item.id} theme={item.id === "soft-aurora" ? "dark" : "light"} />
                  <div className="org-ehg-bg-lab__option-meta">
                    <span className="org-ehg-bg-lab__option-name">
                      {item.number}. {item.name}
                      {item.recommended ? (
                        <span className="org-ehg-bg-lab__option-badge">My recommendation</span>
                      ) : null}
                      {isSelected ? (
                        <span className="org-ehg-bg-lab__option-selected-mark">Selected</span>
                      ) : null}
                    </span>
                    <span className="org-ehg-bg-lab__option-desc">{item.description}</span>
                  </div>
                </button>
              );
            })}
          </div>

          <div className="org-ehg-bg-lab__controls">
            <div className="org-ehg-bg-lab__control-row">
              <PlaygroundLabel>Preview theme</PlaygroundLabel>
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  intent={theme === "light" ? "primary" : "neutral"}
                  appearance={theme === "light" ? "solid" : "outline"}
                  shape="soft"
                  size="sm"
                  aria-pressed={theme === "light"}
                  data-testid="org-ehg-bg-lab-theme-light"
                  onClick={() => setTheme("light")}
                >
                  <Sun className="size-3.5" aria-hidden />
                  Light
                </Button>
                <Button
                  type="button"
                  intent={theme === "dark" ? "primary" : "neutral"}
                  appearance={theme === "dark" ? "solid" : "outline"}
                  shape="soft"
                  size="sm"
                  aria-pressed={theme === "dark"}
                  data-testid="org-ehg-bg-lab-theme-dark"
                  onClick={() => setTheme("dark")}
                >
                  <Moon className="size-3.5" aria-hidden />
                  Dark
                </Button>
              </div>
            </div>

            <label className="org-ehg-bg-lab__intensity" htmlFor="org-ehg-bg-lab-intensity">
              <span className="org-ehg-bg-lab__intensity-label">
                Ambient intensity
                <span className="org-ehg-bg-lab__intensity-value">{intensity}%</span>
              </span>
              <input
                id="org-ehg-bg-lab-intensity"
                type="range"
                min={0}
                max={100}
                step={1}
                value={intensity}
                disabled={option === "neutral-solid"}
                data-testid="org-ehg-bg-lab-intensity"
                onChange={(event) => setIntensity(Number(event.target.value))}
              />
              <span className="org-ehg-bg-lab__intensity-hint">
                Affects only the decorative glow layer — not the base or content opacity.
              </span>
            </label>

            <div className="org-ehg-bg-lab__toggles">
              <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <Switch
                  checked={showGlass}
                  onCheckedChange={setShowGlass}
                  aria-label="Show glass surfaces"
                  data-testid="org-ehg-bg-lab-show-glass"
                />
                Show Glass Surfaces
              </label>
              <label className="inline-flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <Switch
                  checked={sideBySide}
                  onCheckedChange={setSideBySide}
                  aria-label="Side by side light and dark"
                  data-testid="org-ehg-bg-lab-side-by-side"
                />
                Side-by-side light / dark
              </label>
              <Button
                type="button"
                intent="neutral"
                appearance="outline"
                shape="soft"
                size="sm"
                data-testid="org-ehg-bg-lab-reset"
                onClick={reset}
              >
                <RotateCcw className="size-3.5" aria-hidden />
                Reset to Recommended Default
              </Button>
            </div>
          </div>

          <div className="org-ehg-bg-lab__preview-block">
            <div className="flex items-center justify-between gap-3">
              <PlaygroundLabel>Main preview</PlaygroundLabel>
              <span className="org-ehg-bg-lab__preview-caption" data-testid="org-ehg-bg-lab-selected-name">
                {selected.number}. {selected.name}
              </span>
            </div>

            {sideBySide ? (
              <div className="org-ehg-bg-lab__compare" data-testid="org-ehg-light-dark">
                <SampleShell
                  theme="light"
                  option={option}
                  intensity={intensity}
                  showGlass={showGlass}
                  inputValue={inputValue}
                  onInputChange={setInputValue}
                  testId="org-ehg-bg-lab-preview-light"
                />
                <SampleShell
                  theme="dark"
                  option={option}
                  intensity={intensity}
                  showGlass={showGlass}
                  inputValue={inputValue}
                  onInputChange={setInputValue}
                  testId="org-ehg-bg-lab-preview-dark"
                />
              </div>
            ) : (
              <SampleShell
                theme={theme}
                option={option}
                intensity={intensity}
                showGlass={showGlass}
                inputValue={inputValue}
                onInputChange={setInputValue}
                testId="org-ehg-bg-lab-preview"
              />
            )}
          </div>
        </PlaygroundSection>
      </Card>
    </div>
  );
}
