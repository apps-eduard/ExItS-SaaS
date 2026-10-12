import { symbol } from "@/components/icons/MaterialSymbol";

const PaletteIcon = symbol("palette");
const LightModeIcon = symbol("light_mode");
const DarkModeIcon = symbol("dark_mode");
import { useEffect, useId, useRef, useState } from "react";
import { PrimaryColorControl } from "@/components/exits/PrimaryColorControl";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { isDocumentThemeDark } from "@/lib/primary-palette-accents";
import type { PrimaryColorPreference } from "@/lib/preferences/ui-preferences";

/** AdminUX theme-1 colors. The page wash follows the same choice. */
const PERSONAL_THEME_SWATCHES: Record<PrimaryColorPreference, string> = {
  green: "#08a046",
  teal: "#00a885",
  cyan: "#03aed2",
  blue: "#0049e8",
  indigo: "#4d10f2",
  violet: "#471ca8",
  fuchsia: "#e50a8e",
  rose: "#c80036",
  orange: "#fc7a1e",
};

/** Navbar palette for theme color and page background. */
export function PersonalAppearanceMenu({ className }: { className?: string }) {
  const { t } = useI18n();
  const { preferences, setTheme } = usePreferences();
  const [open, setOpen] = useState(false);
  const dark =
    preferences.theme === "dark" || (preferences.theme !== "light" && isDocumentThemeDark());
  const rootRef = useRef<HTMLDivElement>(null);
  const panelId = useId();

  useEffect(() => {
    if (!open) {
      return;
    }
    function onPointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
      }
    }
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        className={cn(
          "inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full",
          className,
        )}
        data-testid="personal-appearance-menu"
        aria-label={t("appearance.canvas.label")}
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((current) => !current)}
      >
        <PaletteIcon className="size-5" aria-hidden />
      </button>
      {open ? (
        <div
          id={panelId}
          className="absolute end-0 top-[calc(100%+0.4rem)] z-40 flex w-[17.5rem] flex-col gap-4 rounded-xl border border-border bg-[var(--exits-surface)] p-3 shadow-lg"
          data-testid="personal-appearance-panel"
        >
          <PrimaryColorControl
            labelKey="appearance.themeColor.label"
            swatches={PERSONAL_THEME_SWATCHES}
          />
          <div className="flex min-w-0 flex-col gap-2.5" data-testid="personal-appearance-mode">
            <div className="flex gap-2">
              <button
                type="button"
                className={cn(
                  "inline-flex flex-1 items-center justify-center gap-1.5 rounded-full border px-3 py-1.5 text-sm",
                  !dark ? "border-[var(--exits-primary)] bg-[var(--exits-primary-soft)]" : "border-border",
                )}
                aria-pressed={!dark}
                onClick={() => setTheme("light")}
              >
                <LightModeIcon className="size-4" aria-hidden />
                {t("theme.light")}
              </button>
              <button
                type="button"
                className={cn(
                  "inline-flex flex-1 items-center justify-center gap-1.5 rounded-full border px-3 py-1.5 text-sm",
                  dark ? "border-[var(--exits-primary)] bg-[var(--exits-primary-soft)]" : "border-border",
                )}
                aria-pressed={dark}
                onClick={() => setTheme("dark")}
              >
                <DarkModeIcon className="size-4" aria-hidden />
                {t("theme.dark")}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
