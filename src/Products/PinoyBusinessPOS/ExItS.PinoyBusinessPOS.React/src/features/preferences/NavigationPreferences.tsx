import { NavigationModeControl } from "@/components/exits/NavigationModeControl";
import { useI18n } from "@/i18n/I18nProvider";
import { PreferencesSectionPanel } from "@/features/preferences/PreferencesSectionPanel";

/**
 * Navigation — desktop sidebar presentation (Standard / Compact).
 * Mobile/tablet bottom navigation is independent and unchanged.
 */
export function NavigationPreferences() {
  const { t } = useI18n();

  return (
    <PreferencesSectionPanel
      title={t("preferences.section.navigation")}
      titleId="preferences-navigation-heading"
      testId="preferences-section-navigation"
    >
      <div className="divide-y divide-border px-4">
        <NavigationModeControl />
      </div>
    </PreferencesSectionPanel>
  );
}
