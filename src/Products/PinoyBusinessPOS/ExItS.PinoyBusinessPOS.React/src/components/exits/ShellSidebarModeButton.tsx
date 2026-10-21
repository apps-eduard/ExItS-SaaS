import type { CSSProperties, ReactNode } from "react";
import { ChevronLeft, ChevronsLeft, ChevronsRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { INTENT_GLOBAL_STANDARD } from "@/features/ui-standards/intent-color-global-default";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import {
  nextNavigationModeCycle,
  type NavigationModePreference,
} from "@/lib/preferences/ui-preferences";
import { cn } from "@/lib/cn";

const STANDARD_RAISED_STYLE: CSSProperties = {
  backgroundColor: INTENT_GLOBAL_STANDARD,
  color: "#000000",
  borderColor: "rgba(0, 0, 0, 0.2)",
};

function cycleLabelKey(mode: NavigationModePreference): MessageKey {
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
export function ShellSidebarModeButton({
  className,
  icon,
  icons,
}: {
  className?: string;
  icon?: ReactNode;
  icons?: Partial<Record<NavigationModePreference, ReactNode>>;
}) {
  const { t } = useI18n();
  const { preferences, setNavigationMode } = usePreferences();
  const mode = preferences.navigationMode;
  const next = nextNavigationModeCycle(mode);
  const label = t(cycleLabelKey(mode));
  const modeIcon = icons?.[mode] ?? (mode === "reveal" ? icons?.standard : undefined);

  return (
    <Button
      type="button"
      intent="neutral"
      appearance="solid"
      shape="round"
      size="icon"
      aria-label={label}
      title={label}
      data-testid="shell-sidebar-mode-button"
      data-intent="standard"
      data-appearance="raised"
      data-shape="rounded"
      data-navigation-mode={mode}
      data-next-navigation-mode={next}
      className={cn(
        "app-top-bar__sidebar-mode ui-standards-qbtn--raised ui-standards-qbtn--rounded border shadow-none",
        className,
      )}
      style={STANDARD_RAISED_STYLE}
      onClick={() => setNavigationMode(next)}
    >
      {modeIcon ??
        icon ??
        (mode === "compact" ? (
          <ChevronLeft aria-hidden="true" className="size-5" strokeWidth={2.25} />
        ) : mode === "hidden" ? (
          <ChevronsRight aria-hidden="true" className="size-5" strokeWidth={2.25} />
        ) : (
          <ChevronsLeft aria-hidden="true" className="size-5" strokeWidth={2.25} />
        ))}
    </Button>
  );
}
