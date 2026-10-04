import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { TabStylePreference } from "@/lib/preferences/ui-preferences";

function TabStyleIcon({ kind }: { kind: TabStylePreference }) {
  if (kind === "underline") {
    return (
      <span className="flex h-3.5 w-3.5 shrink-0 items-end justify-center" aria-hidden="true">
        <span className="h-0.5 w-3.5 rounded-full bg-current" />
      </span>
    );
  }
  return (
    <span className="flex h-3.5 w-3.5 shrink-0 items-center gap-0.5" aria-hidden="true">
      <span className="h-2.5 w-1.5 rounded-[2px] border border-current" />
      <span className="h-2.5 w-1.5 rounded-[2px] border border-current bg-current" />
    </span>
  );
}

export function TabStyleControl() {
  const { t } = useI18n();
  const { preferences, setTabStyle } = usePreferences();

  return (
    <SettingsSelect<TabStylePreference>
      label={t("appearance.tabStyle.label")}
      value={preferences.tabStyle}
      onChange={setTabStyle}
      variant="segmented"
      testId="preferences-tab-style"
      options={[
        {
          value: "underline",
          label: t("appearance.tabStyle.underline"),
          icon: <TabStyleIcon kind="underline" />,
        },
        {
          value: "tabs",
          label: t("appearance.tabStyle.tabs"),
          icon: <TabStyleIcon kind="tabs" />,
        },
      ]}
    />
  );
}
