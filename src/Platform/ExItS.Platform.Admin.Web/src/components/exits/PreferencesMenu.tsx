import { SlidersHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuItem } from "@/components/ui/dropdown-menu";
import { usePreferences } from "@/hooks/use-preferences";
import type { Density, Language, ThemeMode } from "@/lib/preferences/ui-preferences";

export function PreferencesMenu({ includeDensity = true }: { includeDensity?: boolean }) {
  const { t, theme, language, density, setTheme, setLanguage, setDensity } = usePreferences();

  return (
    <DropdownMenu
      align="end"
      label={t("shell.preferences")}
      trigger={
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-8 gap-1.5 px-2.5 text-[length:var(--exits-text-sm)]"
          aria-label={t("shell.preferences")}
        >
          <SlidersHorizontal aria-hidden="true" size={14} />
          {t("shell.preferences")}
        </Button>
      }
    >
      <DropdownMenuItem onSelect={() => setLanguage("en" satisfies Language)}>
        {t("preferences.language.en")}
        {language === "en" ? " ✓" : ""}
      </DropdownMenuItem>
      <DropdownMenuItem onSelect={() => setLanguage("fil-PH")}>
        {t("preferences.language.fil")}
        {language === "fil-PH" ? " ✓" : ""}
      </DropdownMenuItem>
      <DropdownMenuItem onSelect={() => setTheme("system" satisfies ThemeMode)}>
        {t("preferences.theme.system")}
        {theme === "system" ? " ✓" : ""}
      </DropdownMenuItem>
      <DropdownMenuItem onSelect={() => setTheme("light")}>
        {t("preferences.theme.light")}
        {theme === "light" ? " ✓" : ""}
      </DropdownMenuItem>
      <DropdownMenuItem onSelect={() => setTheme("dark")}>
        {t("preferences.theme.dark")}
        {theme === "dark" ? " ✓" : ""}
      </DropdownMenuItem>
      {includeDensity ? (
        <>
          <DropdownMenuItem onSelect={() => setDensity("comfortable" satisfies Density)}>
            {t("preferences.density.comfortable")}
            {density === "comfortable" ? " ✓" : ""}
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => setDensity("balanced")}>
            {t("preferences.density.balanced")}
            {density === "balanced" ? " ✓" : ""}
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => setDensity("compact")}>
            {t("preferences.density.compact")}
            {density === "compact" ? " ✓" : ""}
          </DropdownMenuItem>
        </>
      ) : null}
    </DropdownMenu>
  );
}
