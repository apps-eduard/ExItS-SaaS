import { Check } from "lucide-react";
import { useId } from "react";
import { cn } from "@/lib/cn";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import {
  BACKGROUND_COLOR_OPTIONS,
  type BackgroundColorPreference,
} from "@/lib/preferences/ui-preferences";

const LABEL_KEYS = {
  theme: "appearance.background.theme",
  blue: "appearance.background.blue",
  violet: "appearance.background.violet",
  teal: "appearance.background.teal",
  rose: "appearance.background.rose",
  sand: "appearance.background.sand",
  slate: "appearance.background.slate",
} as const satisfies Record<BackgroundColorPreference, string>;

const SWATCH: Record<BackgroundColorPreference, string> = {
  theme: "var(--exits-primary)",
  blue: "#93c5fd",
  violet: "#c4b5fd",
  teal: "#5eead4",
  rose: "#fda4af",
  sand: "#fcd34d",
  slate: "#cbd5e1",
};

/** Clickable page-wash swatches. */
export function BackgroundColorControl({
  labelKey = "appearance.background.label",
}: {
  labelKey?: MessageKey;
} = {}) {
  const { t } = useI18n();
  const { preferences, setBackgroundColor } = usePreferences();
  const labelId = useId();

  return (
    <div className="flex min-w-0 flex-col gap-2.5" data-testid="preferences-background-color">
      <span id={labelId} className="exits-type-label text-foreground">
        {t(labelKey)}
      </span>
      <div role="radiogroup" aria-labelledby={labelId} className="flex flex-wrap items-center gap-0.5">
        {BACKGROUND_COLOR_OPTIONS.map((value) => {
          const selected = preferences.backgroundColor === value;
          const name = t(LABEL_KEYS[value]);
          return (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={name}
              title={name}
              data-testid={`preferences-background-${value}`}
              className={cn(
                "inline-flex size-9 shrink-0 items-center justify-center rounded-full",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
                selected && "ring-2 ring-[var(--exits-primary)] ring-offset-2 ring-offset-[var(--exits-bg)]",
              )}
              onClick={() => setBackgroundColor(value)}
            >
              <span
                className="relative inline-flex size-[1.25rem] items-center justify-center rounded-full border border-black/10 shadow-sm"
                style={{ backgroundColor: SWATCH[value] }}
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
