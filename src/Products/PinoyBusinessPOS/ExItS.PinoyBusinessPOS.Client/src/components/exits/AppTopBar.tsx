import { Lock, LogOut, Settings, Trash2 } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function AppTopBar() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { signOut } = useSession();
  const { boundWorkspace } = useWorkspace();

  return (
    <header className="flex min-w-0 flex-col gap-3 border-b border-border py-4">
      <div className="flex min-w-0 items-start justify-between gap-3">
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
            ) : null}
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            disabled
            title={t("topbar.lockDisabled")}
            aria-label={t("topbar.lock")}
          >
            <Lock className="size-5" aria-hidden="true" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            disabled
            title={t("topbar.removeDisabled")}
            aria-label={t("topbar.remove")}
          >
            <Trash2 className="size-5" aria-hidden="true" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            aria-label={t("preferences.title")}
            onClick={() => navigate("/settings/preferences")}
          >
            <Settings className="size-5" aria-hidden="true" />
          </Button>
          <Button
            variant="ghost"
            size="default"
            onClick={() => {
              void signOut().then(() => navigate("/sign-in", { replace: true }));
            }}
          >
            <LogOut className="size-4" aria-hidden="true" />
            {t("topbar.signOut")}
          </Button>
        </div>
      </div>
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
        {t("topbar.pinPolicyOpen")}
      </p>
      <Link to="/settings/preferences" className="sr-only">
        {t("preferences.title")}
      </Link>
    </header>
  );
}
