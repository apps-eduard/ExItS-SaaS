import { Menu } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuItem } from "@/components/ui/dropdown-menu";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import type { Density, Language, ThemeMode } from "@/lib/preferences/ui-preferences";

export function AppTopBar({
  onOpenNavigation,
  showNavigationTrigger,
}: {
  onOpenNavigation: () => void;
  showNavigationTrigger: boolean;
}) {
  const { t, theme, language, density, setTheme, setLanguage, setDensity } = usePreferences();
  const { session } = useSession();
  const showDev = areDevelopmentToolsAllowed();

  return (
    <header className="flex min-h-14 items-center gap-2 border-b border-border bg-surface px-3">
      {showNavigationTrigger ? (
        <Button
          type="button"
          variant="ghost"
          aria-label={t("shell.openNavigation")}
          onClick={onOpenNavigation}
        >
          <Menu aria-hidden="true" size={20} />
        </Button>
      ) : null}
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold">ExItS {t("auth.product")}</p>
        <p className="truncate text-[length:var(--exits-text-xs)] text-muted">
          {t("auth.productSubtitle")}
        </p>
      </div>
      {showDev ? (
        <span className="rounded-full border border-border px-2 py-0.5 text-[length:var(--exits-text-xs)] text-muted">
          {t("shell.environment.dev")}
        </span>
      ) : null}
      <DropdownMenu
        label={t("shell.preferences")}
        trigger={
          <Button type="button" variant="outline" size="sm">
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
      </DropdownMenu>
      <DropdownMenu
        label={t("shell.accountMenu")}
        trigger={
          <Button type="button" variant="ghost" aria-label={t("shell.accountMenu")}>
            {session?.displayName ?? session?.username ?? t("shell.accountMenu")}
          </Button>
        }
      >
        <DropdownMenuItem disabled>
          <span className="grid gap-0.5 text-left">
            <span className="font-semibold">{session?.displayName}</span>
            <span className="break-all text-muted">{session?.email || session?.username}</span>
          </span>
        </DropdownMenuItem>
      </DropdownMenu>
    </header>
  );
}
