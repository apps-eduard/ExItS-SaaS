import { PanelLeft, PanelLeftClose } from "lucide-react";
import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { NavigationModePreference } from "@/lib/preferences/ui-preferences";

export function NavigationModeControl() {
  const { t } = useI18n();
  const { preferences, setNavigationMode } = usePreferences();

  return (
    <div data-testid="preferences-navigation-mode">
      <SettingsSelect<NavigationModePreference>
        label={t("navigationMode.label")}
        value={preferences.navigationMode}
        onChange={setNavigationMode}
        options={[
          {
            value: "standard",
            label: t("navigationMode.standard"),
            icon: <PanelLeft className="size-3.5 shrink-0" aria-hidden="true" />,
          },
          {
            value: "compact",
            label: t("navigationMode.compact"),
            icon: <PanelLeftClose className="size-3.5 shrink-0" aria-hidden="true" />,
          },
        ]}
      />
      <p className="exits-type-muted m-0 pb-4">{t("navigationMode.helper")}</p>
    </div>
  );
}
