import { Monitor, Moon, Sun } from "lucide-react";
import { SegmentedControl, SegmentedOption } from "@/components/ui/segmented-control";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { ThemePreference } from "@/lib/preferences/ui-preferences";

const OPTIONS: Array<{
  value: ThemePreference;
  labelKey: "theme.system" | "theme.light" | "theme.dark";
  icon: typeof Sun;
}> = [
  { value: "system", labelKey: "theme.system", icon: Monitor },
  { value: "light", labelKey: "theme.light", icon: Sun },
  { value: "dark", labelKey: "theme.dark", icon: Moon },
];

export function ThemeControl() {
  const { t } = useI18n();
  const { preferences, setTheme } = usePreferences();

  return (
    <SegmentedControl label={t("theme.label")}>
      {OPTIONS.map(({ value, labelKey, icon: Icon }) => (
        <SegmentedOption
          key={value}
          selected={preferences.theme === value}
          onSelect={() => setTheme(value)}
        >
          <Icon className="size-4" aria-hidden="true" />
          {t(labelKey)}
        </SegmentedOption>
      ))}
    </SegmentedControl>
  );
}
