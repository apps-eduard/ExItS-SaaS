import { Button } from "../components/button/Button";
import { SegmentedControl } from "../components/segmented/SegmentedControl";
import { PALETTE_LABELS, PALETTES, type Density, type Radius, type ThemeMode } from "../theme/appearance";
import { useAppearance } from "../theme/AppearanceProvider";
import styles from "./Playground.module.scss";

const THEME_OPTIONS: Array<{ value: ThemeMode; label: string }> = [
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
  { value: "system", label: "System" },
];

const RADIUS_OPTIONS: Array<{ value: Radius; label: string }> = [
  { value: "standard", label: "Standard" },
  { value: "soft", label: "Soft" },
  { value: "pill", label: "Pill" },
];

const DENSITY_OPTIONS: Array<{ value: Density; label: string }> = [
  { value: "compact", label: "Compact" },
  { value: "standard", label: "Standard" },
  { value: "large", label: "Large" },
];

export function AppearanceControls({ name }: { name: string }) {
  const appearance = useAppearance();

  return (
    <div className={styles.controls}>
      <SegmentedControl label="Theme" name={`${name}-theme`} value={appearance.theme} options={THEME_OPTIONS} onChange={appearance.setTheme} />
      <p className="type-caption">
        Resolved appearance: {appearance.resolvedTheme}.
        {appearance.theme === "system" ? " System follows this device." : ""}
      </p>
      <div className={styles.stack}>
        <p className="type-label">Primary</p>
        <div className={styles.swatches}>
          {PALETTES.map((palette) => (
            <button
              key={palette}
              type="button"
              className={styles.swatch}
              data-theme="light"
              data-palette={palette}
              aria-label={`${PALETTE_LABELS[palette]} primary`}
              aria-pressed={appearance.palette === palette}
              onClick={() => appearance.setPalette(palette)}
            />
          ))}
        </div>
      </div>
      <SegmentedControl label="Corner shape" name={`${name}-radius`} value={appearance.radius} options={RADIUS_OPTIONS} onChange={appearance.setRadius} />
      <SegmentedControl label="Density" name={`${name}-density`} value={appearance.density} options={DENSITY_OPTIONS} onChange={appearance.setDensity} />
      <Button variant="secondary" fullWidth onClick={appearance.reset}>
        Reset to defaults
      </Button>
      <p className="type-caption">Defaults are System, Teal, Soft, and Standard. Stored only under ExItS Premium keys.</p>
    </div>
  );
}
