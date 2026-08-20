import { useNavigate } from "react-router-dom";
import { useState } from "react";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { useI18n } from "@/i18n/I18nProvider";
import { isOrganizationContextLocked } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

export function AppTopBar() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { signOut, session } = useSession();
  const { boundWorkspace, clearBoundWorkspace, workspaces } = useWorkspace();
  const [signingOut, setSigningOut] = useState(false);
  const [signOutError, setSignOutError] = useState<string | null>(null);

  const canSwitchWorkspace = workspaces.length > 0 && !isOrganizationContextLocked(session);

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

  const workspaceLabel = boundWorkspace
    ? boundWorkspace.branchName
      ? `${boundWorkspace.organizationDisplayName} · ${boundWorkspace.branchName}`
      : boundWorkspace.organizationDisplayName
    : null;

  return (
    <header
      className="flex min-w-0 flex-col gap-2 border-b border-border py-2"
      data-testid="app-top-bar"
    >
      <div className="grid min-w-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-3 md:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)_auto]">
        <div className="flex min-w-0 items-center gap-2.5">
          <div
            className="flex size-8 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-primary text-[length:var(--exits-text-xs)] font-bold text-primary-foreground sm:size-9"
            aria-hidden="true"
          >
            E
          </div>
          <div className="min-w-0 md:hidden">
            {boundWorkspace ? (
              <button
                type="button"
                data-testid="workspace-context-mobile"
                className={cn(
                  "flex min-w-0 max-w-full flex-col items-start rounded-[var(--exits-radius-md)] px-0.5 py-0.5 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                  canSwitchWorkspace ? "hover:bg-[var(--exits-surface-muted)]" : "cursor-default",
                )}
                title={workspaceLabel ?? undefined}
                aria-label={
                  canSwitchWorkspace
                    ? `${t("workspace.switch")}: ${workspaceLabel}`
                    : (workspaceLabel ?? undefined)
                }
                onClick={() => {
                  if (canSwitchWorkspace) {
                    navigate("/workspace");
                  }
                }}
                disabled={!canSwitchWorkspace}
              >
                <span className="m-0 w-full truncate text-[length:var(--exits-text-sm)] font-semibold text-foreground">
                  {boundWorkspace.organizationDisplayName}
                </span>
                <span className="m-0 w-full truncate text-[length:var(--exits-text-xs)] text-muted">
                  {boundWorkspace.branchName ?? t("experience.manageBusiness")}
                </span>
              </button>
            ) : (
              <p className="m-0 truncate text-[length:var(--exits-text-xs)] font-semibold tracking-[0.08em] uppercase text-muted sm:text-[length:var(--exits-text-sm)]">
                {t("app.name")}
              </p>
            )}
          </div>
          <div className="hidden min-w-0 md:block">
            <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-semibold tracking-[0.08em] uppercase text-muted">
              {t("app.name")}
            </p>
          </div>
        </div>

        <div className="hidden min-w-0 justify-center md:flex">
          {boundWorkspace ? (
            <button
              type="button"
              data-testid="workspace-context"
              className={cn(
                "max-w-full truncate rounded-[var(--exits-radius-md)] px-2 py-1 text-center text-[length:var(--exits-text-sm)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                canSwitchWorkspace ? "hover:bg-[var(--exits-surface-muted)]" : "cursor-default",
              )}
              title={workspaceLabel ?? undefined}
              aria-label={
                canSwitchWorkspace
                  ? `${t("workspace.switch")}: ${workspaceLabel}`
                  : (workspaceLabel ?? undefined)
              }
              onClick={() => {
                if (canSwitchWorkspace) {
                  navigate("/workspace");
                }
              }}
              disabled={!canSwitchWorkspace}
            >
              <span className="font-semibold text-foreground">
                {boundWorkspace.organizationDisplayName}
              </span>
              {boundWorkspace.branchName ? (
                <>
                  <span className="mx-1.5 text-muted">·</span>
                  <span className="text-muted">{boundWorkspace.branchName}</span>
                </>
              ) : null}
            </button>
          ) : (
            <span className="sr-only">{t("topbar.workspacePending")}</span>
          )}
        </div>

        <div className="flex shrink-0 items-center justify-end">
          <AccountMenu
            signingOut={signingOut}
            onSignOut={() => {
              void handleSignOut();
            }}
          />
        </div>
      </div>

      {signOutError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {signOutError}
        </p>
      ) : null}
    </header>
  );
}
