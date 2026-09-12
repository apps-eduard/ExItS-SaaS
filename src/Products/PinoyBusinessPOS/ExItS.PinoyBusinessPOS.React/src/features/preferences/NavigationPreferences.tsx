import { useI18n } from "@/i18n/I18nProvider";
import {
  PreferencesEmptyState,
  PreferencesSectionPanel,
} from "@/features/preferences/PreferencesSectionPanel";

/**
 * Navigation — personal navigation presentation preferences.
 * No configurable options yet (sidebar remember-state, etc. come later).
 * Do not put badges, notifications, or permissions here.
 */
export function NavigationPreferences() {
  const { t } = useI18n();

  return (
    <PreferencesSectionPanel
      title={t("preferences.section.navigation")}
      titleId="preferences-navigation-heading"
      testId="preferences-section-navigation"
    >
      <PreferencesEmptyState
        message={t("preferences.navigationEmpty")}
        testId="preferences-navigation-empty"
      />
    </PreferencesSectionPanel>
  );
}
