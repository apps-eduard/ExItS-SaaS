import { Monitor, Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import type { ThemePreference } from "@/lib/preferences/ui-preferences";

const OPTIONS: Array<{
  value: ThemePreference;
  icon: typeof Sun;
  labelKey: "theme.system" | "theme.light" | "theme.dark";
}> = [
  { value: "system", icon: Monitor, labelKey: "theme.system" },
  { value: "light", icon: Sun, labelKey: "theme.light" },
  { value: "dark", icon: Moon, labelKey: "theme.dark" },
];

export function ThemeControl() {
  const { t } = useI18n();
  const { preferences, setTheme } = usePreferences();

  return (
    <fieldset className="m-0 min-w-0 border-0 p-0">
      <legend className="mb-2 text-[length:var(--exits-text-sm)] font-semibold text-muted">
        {t("theme.label")}
      </legend>
      <div className="grid grid-cols-3 gap-2" role="radiogroup" aria-label={t("theme.label")}>
        {OPTIONS.map(({ value, icon: Icon, labelKey }) => {
          const selected = preferences.theme === value;
          return (
            <Button
              key={value}
              type="button"
              variant={selected ? "default" : "outline"}
              aria-pressed={selected}
              onClick={() => setTheme(value)}
              className="min-w-0 px-2"
            >
              <Icon className="size-4 shrink-0" aria-hidden="true" />
              <span className="truncate">{t(labelKey)}</span>
            </Button>
          );
        })}
      </div>
    </fieldset>
  );
}
