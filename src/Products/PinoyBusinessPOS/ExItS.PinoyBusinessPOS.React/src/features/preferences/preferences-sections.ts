import { Accessibility, Globe2, Palette, PanelLeft } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import type { MessageKey } from "@/i18n/messages";

export type PreferencesSectionId =
  | "appearance"
  | "language-region"
  | "navigation"
  | "accessibility";

export type PreferencesSectionDef = {
  id: PreferencesSectionId;
  /** Nested path segment under /settings/preferences/ */
  path: PreferencesSectionId;
  labelKey: MessageKey;
  icon: LucideIcon;
  testId: string;
};

/** Canonical Preferences menu order. */
export const PREFERENCES_SECTIONS: ReadonlyArray<PreferencesSectionDef> = [
  {
    id: "appearance",
    path: "appearance",
    labelKey: "preferences.section.appearance",
    icon: Palette,
    testId: "preferences-nav-appearance",
  },
  {
    id: "language-region",
    path: "language-region",
    labelKey: "preferences.section.languageRegion",
    icon: Globe2,
    testId: "preferences-nav-language-region",
  },
  {
    id: "navigation",
    path: "navigation",
    labelKey: "preferences.section.navigation",
    icon: PanelLeft,
    testId: "preferences-nav-navigation",
  },
  {
    id: "accessibility",
    path: "accessibility",
    labelKey: "preferences.section.accessibility",
    icon: Accessibility,
    testId: "preferences-nav-accessibility",
  },
];

export const PREFERENCES_DEFAULT_SECTION: PreferencesSectionId = "appearance";

export function preferencesSectionPath(section: PreferencesSectionId = PREFERENCES_DEFAULT_SECTION): string {
  return `/settings/preferences/${section}`;
}

export function parsePreferencesSection(pathname: string): PreferencesSectionId | null {
  const match = pathname.match(/^\/settings\/preferences\/([^/?#]+)/);
  if (!match) return null;
  const id = match[1] as PreferencesSectionId;
  return PREFERENCES_SECTIONS.some((section) => section.id === id) ? id : null;
}
