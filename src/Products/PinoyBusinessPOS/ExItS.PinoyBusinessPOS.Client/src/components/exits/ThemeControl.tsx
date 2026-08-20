import { Monitor, Moon, Sun } from "lucide-react";
import { useState } from "react";
import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { ThemePreference } from "@/lib/preferences/ui-preferences";

export function ThemeControl() {
  const { t } = useI18n();
  const { preferences, setTheme } = usePreferences();
  const [open, setOpen] = useState(false);

  return (
    <SettingsSelect<ThemePreference>
      label={t("theme.label")}
      value={preferences.theme}
      open={open}
      onOpenChange={setOpen}
      onChange={setTheme}
      options={[
        {
          value: "system",
          label: t("theme.system"),
          icon: <Monitor className="size-3.5 shrink-0" aria-hidden="true" />,
        },
        {
          value: "light",
          label: t("theme.light"),
          icon: <Sun className="size-3.5 shrink-0" aria-hidden="true" />,
        },
        {
          value: "dark",
          label: t("theme.dark"),
          icon: <Moon className="size-3.5 shrink-0" aria-hidden="true" />,
        },
      ]}
    />
  );
}
