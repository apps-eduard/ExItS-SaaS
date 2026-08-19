import { Menu } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuItem } from "@/components/ui/dropdown-menu";
import { AppBreadcrumbs } from "@/components/exits/AppBreadcrumbs";
import { PreferencesMenu } from "@/components/exits/PreferencesMenu";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";

export function AppTopBar({
  onOpenNavigation,
  showNavigationTrigger,
}: {
  onOpenNavigation: () => void;
  showNavigationTrigger: boolean;
}) {
  const { t } = usePreferences();
  const { session } = useSession();
  const showDev = areDevelopmentToolsAllowed();

  return (
    <header className="flex h-12 shrink-0 items-center gap-2 border-b border-border bg-surface px-3 lg:px-4">
      {showNavigationTrigger ? (
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label={t("shell.openNavigation")}
          onClick={onOpenNavigation}
        >
          <Menu aria-hidden="true" size={18} />
        </Button>
      ) : null}
      <div className="min-w-0 flex-1">
        <AppBreadcrumbs />
      </div>
      {showDev ? (
        <span className="hidden rounded-sm border border-border px-1.5 py-0.5 text-[length:var(--exits-text-xs)] text-muted sm:inline">
          {t("shell.environment.dev")}
        </span>
      ) : null}
      <PreferencesMenu />
      <DropdownMenu
        align="end"
        label={t("shell.accountMenu")}
        trigger={
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="max-w-44 truncate"
            aria-label={t("shell.accountMenu")}
          >
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
