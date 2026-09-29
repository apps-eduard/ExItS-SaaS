import { PanelLeft, PanelLeftClose, PanelLeftOpen } from "lucide-react";
import { IconButton } from "@/components/ui/icon-button";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import {
  nextNavigationModeCycle,
  type NavigationModePreference,
} from "@/lib/preferences/ui-preferences";
import { cn } from "@/lib/cn";

function cycleLabelKey(mode: NavigationModePreference): string {
  const next = nextNavigationModeCycle(mode);
  if (next === "compact") {
    return "navigationMode.cycleToCompact";
  }
  if (next === "hidden") {
    return "navigationMode.cycleToHidden";
  }
  return "navigationMode.cycleToStandard";
}

/**
 * Desktop shell control: cycles sidenav full → icons → hidden → full.
 * Persists via navigationMode preference (same as Preferences → Sidebar).
 */
export function ShellSidebarModeButton({ className }: { className?: string }) {
  const { t } = useI18n();
  const { preferences, setNavigationMode } = usePreferences();
  const mode = preferences.navigationMode;
  const next = nextNavigationModeCycle(mode);
  const label = t(cycleLabelKey(mode));

  return (
    <IconButton
      label={label}
      title={label}
      data-testid="shell-sidebar-mode-button"
      data-navigation-mode={mode}
      data-next-navigation-mode={next}
      className={cn("app-top-bar__sidebar-mode", className)}
      onClick={() => setNavigationMode(next)}
    >
      {mode === "compact" ? (
        <PanelLeftClose aria-hidden="true" className="size-5" strokeWidth={2.15} />
      ) : mode === "hidden" ? (
        <PanelLeftOpen aria-hidden="true" className="size-5" strokeWidth={2.15} />
      ) : (
        <PanelLeft aria-hidden="true" className="size-5" strokeWidth={2.15} />
      )}
    </IconButton>
  );
}
