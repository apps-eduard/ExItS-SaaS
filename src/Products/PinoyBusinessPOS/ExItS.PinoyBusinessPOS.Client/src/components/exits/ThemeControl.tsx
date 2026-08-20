import { Monitor, Moon, Sun } from "lucide-react";
import { SegmentedControl, SegmentedOption } from "@/components/ui/segmented-control";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { ThemePreference } from "@/lib/preferences/ui-preferences";

const options: {
  value: ThemePreference;
  icon: typeof Monitor;
  labelKey: "theme.system" | "theme.light" | "theme.dark";
}[] = [
  { value: "system", icon: Monitor, labelKey: "theme.system" },
  { value: "light", icon: Sun, labelKey: "theme.light" },
  { value: "dark", icon: Moon, labelKey: "theme.dark" },
];

export function ThemeControl() {
  const { t } = useI18n();
  const { preferences, setTheme } = usePreferences();

  return (
    <SegmentedControl label={t("theme.label")}>
      {options.map(({ value, icon: Icon, labelKey }) => (
        <SegmentedOption
          key={value}
          selected={preferences.theme === value}
          onSelect={() => setTheme(value)}
        >
          <Icon className="size-3.5 shrink-0" aria-hidden="true" />
          {t(labelKey)}
        </SegmentedOption>
      ))}
    </SegmentedControl>
  );
}
