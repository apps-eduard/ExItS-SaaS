import { useContext, useEffect, useState } from "react";
import { PreferencesContext } from "@/hooks/usePreferences";
import {
  readUiPreferences,
  type NavigationModePreference,
} from "@/lib/preferences/ui-preferences";

/**
 * Compact always uses professional nav tooltips.
 * Reveal uses tooltips only on no-hover (touch) desktops as an accessible fallback.
 * Falls back to stored preferences when PreferencesProvider is absent (legacy tests).
 */
export function useSidebarNavTooltipEnabled(): boolean {
  const context = useContext(PreferencesContext);
  const mode: NavigationModePreference =
    context?.preferences.navigationMode ?? readUiPreferences().navigationMode;
  const [noHover, setNoHover] = useState(false);

  useEffect(() => {
    if (typeof window === "undefined" || typeof window.matchMedia !== "function") {
      return;
    }
    const mq = window.matchMedia("(hover: none)");
    const sync = () => setNoHover(mq.matches);
    sync();
    mq.addEventListener("change", sync);
    return () => mq.removeEventListener("change", sync);
  }, []);

  if (mode === "compact") {
    return true;
  }
  if (mode === "reveal" && noHover) {
    return true;
  }
  return false;
}
