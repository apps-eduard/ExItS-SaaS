import { createContext } from "react";
import type { PreferencesSectionId } from "@/features/preferences/preferences-sections";

export type PreferencesOverlayContextValue = {
  open: boolean;
  section: PreferencesSectionId;
  openPreferences: (options?: {
    section?: PreferencesSectionId;
    returnTo?: string;
  }) => void;
  setSection: (section: PreferencesSectionId) => void;
  closePreferences: () => void;
};

/**
 * Kept outside the overlay component module so a hot reload of that file
 * does not create a second context and crash Personal shell with
 * "usePreferencesOverlay must be used within PreferencesOverlayProvider".
 */
export const PreferencesOverlayContext =
  createContext<PreferencesOverlayContextValue | null>(null);
