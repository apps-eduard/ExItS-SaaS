import { Check } from "lucide-react";
import { useId } from "react";
import { cn } from "@/lib/cn";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import {
  PRIMARY_COLOR_OPTIONS,
  type PrimaryColorPreference,
} from "@/lib/preferences/ui-preferences";

const SWATCH_COLORS: Record<PrimaryColorPreference, string> = {
  green: "#166534",
  blue: "#1d4ed8",
  violet: "#6d28d9",
  orange: "#c2410c",
  rose: "#be123c",
};

const LABEL_KEYS = {
  green: "appearance.primary.green",
  blue: "appearance.primary.blue",
  violet: "appearance.primary.violet",
  orange: "appearance.primary.orange",
  rose: "appearance.primary.rose",
} as const;

/**
 * Clickable Primary color swatches — whole circle is the interactive target.
 */
export function PrimaryColorControl() {
  const { t } = useI18n();
  const { preferences, setPrimaryColor } = usePreferences();
  const labelId = useId();

  return (
    <div className="@container flex min-w-0 flex-col gap-3 py-4" data-testid="preferences-primary-color">
      <span id={labelId} className="exits-type-label text-foreground">
        {t("appearance.primary.label")}
      </span>
      <div
        role="radiogroup"
        aria-labelledby={labelId}
        className="flex flex-wrap items-center gap-1"
      >
        {PRIMARY_COLOR_OPTIONS.map((value) => {
          const selected = preferences.primaryColor === value;
          const name = t(LABEL_KEYS[value]);
          return (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={name}
              title={name}
              data-testid={`preferences-primary-${value}`}
              data-primary-swatch={value}
              data-selected={selected ? "true" : "false"}
              className={cn(
                "inline-flex size-10 shrink-0 items-center justify-center rounded-full",
                "transition-[box-shadow,transform] duration-[var(--exits-motion-fast)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
                "focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--exits-bg)]",
                selected && "ring-2 ring-[var(--exits-primary)] ring-offset-2 ring-offset-[var(--exits-bg)]",
              )}
              onClick={() => setPrimaryColor(value)}
            >
              <span
                className="relative inline-flex size-[1.375rem] items-center justify-center rounded-full border border-black/10 shadow-sm"
                style={{ backgroundColor: SWATCH_COLORS[value] }}
                aria-hidden
              >
                {selected ? <Check className="size-3 text-white drop-shadow-sm" strokeWidth={3} /> : null}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
