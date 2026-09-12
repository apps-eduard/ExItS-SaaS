import { PanelLeft, PanelLeftClose, PanelLeftOpen } from "lucide-react";
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
          {
            value: "reveal",
            label: t("navigationMode.reveal"),
            icon: <PanelLeftOpen className="size-3.5 shrink-0" aria-hidden="true" />,
          },
        ]}
      />
      <ul className="exits-type-muted m-0 list-none space-y-1 pb-4 ps-0">
        <li>
          <span className="font-medium text-foreground">{t("navigationMode.standard")}</span>
          {" — "}
          {t("navigationMode.helperStandard")}
        </li>
        <li>
          <span className="font-medium text-foreground">{t("navigationMode.compact")}</span>
          {" — "}
          {t("navigationMode.helperCompact")}
        </li>
        <li>
          <span className="font-medium text-foreground">{t("navigationMode.reveal")}</span>
          {" — "}
          {t("navigationMode.helperReveal")}
        </li>
      </ul>
    </div>
  );
}
