import { LogOut, Settings } from "lucide-react";
import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

export function AppTopBar() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { signOut } = useSession();
  const { boundWorkspace, clearBoundWorkspace } = useWorkspace();
  const [signingOut, setSigningOut] = useState(false);
  const [signOutError, setSignOutError] = useState<string | null>(null);

  const preferencesActive = location.pathname.startsWith("/settings/preferences");

  async function handleSignOut() {
    if (signingOut) {
      return;
    }
    setSigningOut(true);
    setSignOutError(null);
    const result = await signOut();
    if (!result.ok) {
      setSignOutError(result.detail || t("topbar.signOutFailed"));
      setSigningOut(false);
      return;
    }
    clearBoundWorkspace();
    navigate("/sign-in", { replace: true });
  }

  return (
    <header className="flex min-w-0 flex-col gap-3 border-b border-border py-3">
      <div className="flex min-w-0 items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <div
            className="flex size-9 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-primary text-xs font-bold text-primary-foreground"
            aria-hidden="true"
          >
            E
          </div>
          <div className="min-w-0">
            <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold tracking-wide uppercase text-muted">
              {t("app.name")}
            </p>
            {boundWorkspace ? (
              <p className="m-0 truncate text-[length:var(--exits-text-md)] font-semibold text-foreground">
                {boundWorkspace.organizationDisplayName} · {boundWorkspace.branchName}
              </p>
            ) : (
              <p className="m-0 truncate text-[length:var(--exits-text-sm)] text-muted">
                {t("topbar.noWorkspace")}
              </p>
            )}
          </div>
        </div>

        <nav className="flex shrink-0 items-center gap-1 sm:gap-2" aria-label={t("app.name")}>
          <Button
            variant="ghost"
            size="default"
            className={cn(preferencesActive && "bg-[var(--exits-surface-muted)]")}
            aria-current={preferencesActive ? "page" : undefined}
            aria-label={t("topbar.preferences")}
            title={t("topbar.preferences")}
            onClick={() => navigate("/settings/preferences")}
          >
            <Settings className="size-4" aria-hidden="true" />
            <span className="hidden sm:inline">{t("topbar.preferences")}</span>
          </Button>
          <Button
            variant="ghost"
            size="default"
            disabled={signingOut}
            aria-busy={signingOut || undefined}
            aria-label={t("topbar.signOut")}
            title={t("topbar.signOut")}
            onClick={() => {
              void handleSignOut();
            }}
          >
            <LogOut className="size-4" aria-hidden="true" />
            <span className="hidden sm:inline">
              {signingOut ? t("topbar.signingOut") : t("topbar.signOut")}
            </span>
          </Button>
        </nav>
      </div>

      {signOutError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {signOutError}
        </p>
      ) : null}
    </header>
  );
}
