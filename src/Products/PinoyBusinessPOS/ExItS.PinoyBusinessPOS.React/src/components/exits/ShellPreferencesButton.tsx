import { Settings } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useLocation } from "react-router-dom";
import { usePreferencesOverlay } from "@/features/preferences/PreferencesOverlay";
import { PREFERENCES_DEFAULT_SECTION } from "@/features/preferences/preferences-sections";
import { cn } from "@/lib/cn";
import { usePrefersReducedMotion } from "@/lib/motion";
import {
  isDocumentThemeDark,
  pickNextAmbientPrimary,
  resolveAmbientPrimaryAccent,
} from "@/lib/primary-palette-accents";
import type { PrimaryColorPreference } from "@/lib/preferences/ui-preferences";

export type ShellPreferencesButtonProps = {
  /** @deprecated Overlay opens in place; kept for call-site compatibility. */
  to?: string;
  label: string;
  testId?: string;
  className?: string;
};

const COLOR_CYCLE_MS = 4000;
const COLOR_TRANSITION_MS = 600;

/**
 * Compact shell preferences control — icon only; opens the preferences overlay
 * on the current page (does not navigate away).
 * Ambient gear (category AMBIENT): slow rotation + decorative Primary cycle.
 * Does not mutate data-primary or Preferences storage. Pauses when document is hidden.
 */
export function ShellPreferencesButton({
  label,
  testId = "shell-preferences-button",
  className,
}: ShellPreferencesButtonProps) {
  const location = useLocation();
  const { openPreferences } = usePreferencesOverlay();
  const reducedMotion = usePrefersReducedMotion();
  const [ambientColor, setAmbientColor] = useState<PrimaryColorPreference | null>(null);
  const [darkSurface, setDarkSurface] = useState(false);
  const intervalRef = useRef<number | null>(null);

  useEffect(() => {
    if (reducedMotion) {
      setAmbientColor(null);
      return;
    }

    setDarkSurface(isDocumentThemeDark());
    setAmbientColor((prev) => prev ?? pickNextAmbientPrimary(null));

    const tick = () => {
      if (typeof document !== "undefined" && document.hidden) {
        return;
      }
      setAmbientColor((prev) => pickNextAmbientPrimary(prev));
      setDarkSurface(isDocumentThemeDark());
    };

    const start = () => {
      if (intervalRef.current != null) {
        return;
      }
      intervalRef.current = window.setInterval(tick, COLOR_CYCLE_MS);
    };

    const stop = () => {
      if (intervalRef.current != null) {
        window.clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };

    const onVisibility = () => {
      if (document.hidden) {
        stop();
      } else {
        start();
      }
    };

    if (!document.hidden) {
      start();
    }
    document.addEventListener("visibilitychange", onVisibility);

    return () => {
      stop();
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, [reducedMotion]);

  const ambientEnabled = !reducedMotion && ambientColor != null;
  const iconColor = ambientEnabled
    ? resolveAmbientPrimaryAccent(ambientColor, darkSurface)
    : undefined;

  return (
    <button
      type="button"
      data-testid={testId}
      data-ambient-settings={ambientEnabled ? "on" : "off"}
      aria-label={label}
      title={label}
      className={cn(
        "group/settings relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
      onClick={() =>
        openPreferences({
          section: PREFERENCES_DEFAULT_SECTION,
          returnTo: `${location.pathname}${location.search}`,
        })
      }
    >
      <Settings
        className={cn(
          "size-5 exits-settings-gear exits-motion-ambient",
          ambientEnabled && "exits-settings-gear--ambient",
          !ambientEnabled && "text-[var(--exits-primary)]",
        )}
        style={
          ambientEnabled
            ? {
                color: iconColor,
                transition: `color ${COLOR_TRANSITION_MS}ms var(--exits-ease-standard)`,
              }
            : undefined
        }
        data-testid={`${testId}-gear`}
        data-ambient-color={ambientColor ?? "primary"}
        aria-hidden
      />
      <span className="sr-only">{label}</span>
    </button>
  );
}
