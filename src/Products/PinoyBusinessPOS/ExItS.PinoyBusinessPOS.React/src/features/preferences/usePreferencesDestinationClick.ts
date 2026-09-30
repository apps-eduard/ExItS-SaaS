import type { MouseEvent } from "react";
import { useLocation } from "react-router-dom";
import { usePreferencesOverlay } from "@/features/preferences/PreferencesOverlay";
import { isPreferencesDestination } from "@/features/preferences/preferences-return";
import {
  PREFERENCES_DEFAULT_SECTION,
  parsePreferencesSection,
} from "@/features/preferences/preferences-sections";

/**
 * Prefer the in-place preferences overlay over navigating to /settings/preferences,
 * so the current page stays mounted under the drawer.
 */
export function usePreferencesDestinationClick() {
  const location = useLocation();
  const { openPreferences } = usePreferencesOverlay();

  return (to: string, event?: MouseEvent) => {
    if (!isPreferencesDestination(to)) {
      return false;
    }
    event?.preventDefault();
    event?.stopPropagation();
    openPreferences({
      section: parsePreferencesSection(to) ?? PREFERENCES_DEFAULT_SECTION,
      returnTo: `${location.pathname}${location.search}`,
    });
    return true;
  };
}
