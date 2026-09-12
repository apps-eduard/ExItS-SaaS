import { Settings } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import {
  capturePreferencesReturnFrom,
  preferencesNavigationState,
} from "@/features/preferences/preferences-return";
import { cn } from "@/lib/cn";
import { usePrefersReducedMotion } from "@/lib/motion";
import {
  isDocumentThemeDark,
  pickNextAmbientPrimary,
  resolveAmbientPrimaryAccent,
} from "@/lib/primary-palette-accents";
import type { PrimaryColorPreference } from "@/lib/preferences/ui-preferences";

export type ShellPreferencesButtonProps = {
  to?: string;
  label: string;
  testId?: string;
  className?: string;
};

const COLOR_CYCLE_MS = 4000;
const COLOR_TRANSITION_MS = 600;

/**
 * Compact shell preferences control — icon only; opens the preferences drawer route.
 * Ambient gear (category AMBIENT): slow rotation + decorative Primary cycle.
 * Does not mutate data-primary or Preferences storage. Pauses when document is hidden.
 */
export function ShellPreferencesButton({
  to = "/settings/preferences",
  label,
  testId = "shell-preferences-button",
  className,
}: ShellPreferencesButtonProps) {
  const location = useLocation();
  const preferencesState = preferencesNavigationState(location.pathname, location.search);
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
    <Link
      to={to}
      state={preferencesState}
      onClick={
        preferencesState
          ? () => capturePreferencesReturnFrom(location.pathname, location.search)
          : undefined
      }
      data-testid={testId}
      data-ambient-settings={ambientEnabled ? "on" : "off"}
      aria-label={label}
      title={label}
      className={cn(
        "group/settings relative inline-flex size-11 min-w-11 shrink-0 items-center justify-center rounded-full text-foreground no-underline transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
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
    </Link>
  );
}
