import { ChevronDown, LogOut, Menu, PanelLeftClose, PanelLeftOpen, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Avatar } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { AppBreadcrumbs } from "@/components/exits/AppBreadcrumbs";
import { PreferencesMenu } from "@/components/exits/PreferencesMenu";
import {
  areDevelopmentToolsAllowed,
  areTestUserToolsPermitted,
} from "@/lib/auth/development-tools";
import { initialsFromIdentity } from "@/lib/identity/initials";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";

export function AppTopBar({
  onOpenNavigation,
  showNavigationTrigger,
}: {
  onOpenNavigation: () => void;
  showNavigationTrigger: boolean;
}) {
  const { t, sidebarCollapsed, setSidebarCollapsed } = usePreferences();
  const { session, signOut } = useSession();
  const showDev = areDevelopmentToolsAllowed();
  const showRuntimeBadge = areTestUserToolsPermitted();
  const initials = initialsFromIdentity(session?.displayName, session?.username, session?.email);

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
      ) : (
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-pressed={sidebarCollapsed}
          aria-label={sidebarCollapsed ? t("shell.expandSidebar") : t("shell.collapseSidebar")}
          onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
        >
          {sidebarCollapsed ? (
            <PanelLeftOpen aria-hidden="true" size={16} />
          ) : (
            <PanelLeftClose aria-hidden="true" size={16} />
          )}
        </Button>
      )}
      <div className="min-w-0 flex-1">
        <AppBreadcrumbs />
      </div>
      {showRuntimeBadge ? (
        <span className="hidden rounded-sm border border-border px-1.5 py-0.5 text-[length:var(--exits-text-xs)] text-muted sm:inline">
          {showDev ? t("shell.environment.dev") : t("shell.environment.localValidation")}
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
            className="max-w-56 gap-2 px-1.5"
            aria-label={t("shell.accountMenu")}
          >
            {initials ? (
              <Avatar initials={initials} className="size-7 text-[10px]" />
            ) : (
              <span className="grid size-7 place-items-center rounded-full bg-[var(--exits-primary-soft)] text-primary">
                <User aria-hidden="true" size={14} />
              </span>
            )}
            <span className="hidden min-w-0 truncate text-left sm:inline">
              {session?.displayName ?? session?.username ?? t("shell.accountMenu")}
            </span>
            <ChevronDown
              aria-hidden="true"
              size={14}
              className="hidden shrink-0 text-muted sm:inline"
            />
          </Button>
        }
      >
        <DropdownMenuItem disabled>
          <span className="grid gap-0.5 text-left">
            <span className="font-semibold">{session?.displayName}</span>
            <span className="break-all text-muted">{session?.email || session?.username}</span>
          </span>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={() => {
            void signOut().catch(() => {
              /* Diagnostic already reported; keep the session when logout fails. */
            });
          }}
        >
          <LogOut aria-hidden="true" size={14} />
          {t("shell.signOut")}
        </DropdownMenuItem>
      </DropdownMenu>
    </header>
  );
}
