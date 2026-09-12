import { Settings } from "lucide-react";
import { useEffect, useState } from "react";
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
 * Ambient gear: slow continuous rotation + decorative Primary accent cycle (client-only).
 * Does not mutate data-primary or Preferences storage.
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

  useEffect(() => {
    if (reducedMotion) {
      setAmbientColor(null);
      return;
    }

    setDarkSurface(isDocumentThemeDark());
    setAmbientColor((prev) => prev ?? pickNextAmbientPrimary(null));

    const id = window.setInterval(() => {
      setAmbientColor((prev) => pickNextAmbientPrimary(prev));
      setDarkSurface(isDocumentThemeDark());
    }, COLOR_CYCLE_MS);

    return () => window.clearInterval(id);
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
          "size-5 exits-settings-gear",
          ambientEnabled && "exits-settings-gear--ambient",
          !ambientEnabled && "text-[var(--exits-primary)]",
        )}
        style={
          ambientEnabled
            ? {
                color: iconColor,
                transition: `color ${COLOR_TRANSITION_MS}ms ease`,
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
