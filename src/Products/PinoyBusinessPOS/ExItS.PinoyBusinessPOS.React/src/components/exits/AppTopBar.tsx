import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Building2, ChevronDown } from "lucide-react";
import { listOrganizationNotifications } from "@/api/platform/organization-notifications-client";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNotificationButton } from "@/components/exits/ShellNotificationButton";
import {
  countUnreadOrganizationNotifications,
  formatUnreadNotificationBadge,
  organizationNotificationsQueryKey,
} from "@/features/org/org-notifications";
import { useI18n } from "@/i18n/I18nProvider";
import { isOrganizationContextLocked, sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

type OrgNotificationsLinkState = {
  returnTo: string;
};

export function AppTopBar() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { signOut, session } = useSession();
  const { boundWorkspace, clearBoundWorkspace, workspaces } = useWorkspace();
  const [signingOut, setSigningOut] = useState(false);
  const [signOutError, setSignOutError] = useState<string | null>(null);

  const canSwitchWorkspace = workspaces.length > 0 && !isOrganizationContextLocked(session);
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canOpenOrgNotifications =
    sessionAccountClass(session) === "Organization" && Boolean(organizationId);
  const onOrgNotificationsPage = location.pathname.startsWith("/org/notifications");
  const returnTo = onOrgNotificationsPage
    ? null
    : `${location.pathname}${location.search}`;
  const notificationsLinkState: OrgNotificationsLinkState | undefined = returnTo
    ? { returnTo }
    : undefined;

  const notificationsQuery = useQuery({
    queryKey: organizationId
      ? organizationNotificationsQueryKey(organizationId)
      : ["organization", "notifications", "none"],
    enabled: canOpenOrgNotifications && organizationId !== null,
    queryFn: ({ signal }) => listOrganizationNotifications(organizationId!, signal),
  });
  const unreadCount = countUnreadOrganizationNotifications(notificationsQuery.data);
  const badge = formatUnreadNotificationBadge(unreadCount);

  async function handleSignOut() {
    if (signingOut) {
      return;
    }
    setSigningOut(true);
    setSignOutError(null);
    const result = await signOut();
    if (!result.ok) {
      setSignOutError(
        result.detail === "__ANTIFORGERY__"
          ? t("accessDenied.antiforgery")
          : result.detail || t("topbar.signOutFailed"),
      );
      setSigningOut(false);
      return;
    }
    clearBoundWorkspace();
    navigate(result.nextRoute, { replace: true });
  }

  const workspaceLabel = boundWorkspace
    ? boundWorkspace.branchName
      ? `${boundWorkspace.organizationDisplayName} · ${boundWorkspace.branchName}`
      : boundWorkspace.organizationDisplayName
    : null;

  function openWorkspaceSwitcher() {
    if (canSwitchWorkspace) {
      navigate("/workspace");
    }
  }

  return (
    <header className="app-top-bar" data-testid="app-top-bar">
      <div className="app-top-bar__row">
        <div className="app-top-bar__brand">
          <span className="app-top-bar__mark" aria-hidden="true">
            E
          </span>
          <div className="app-top-bar__brand-copy md:hidden">
            {boundWorkspace ? (
              <button
                type="button"
                data-testid="workspace-context-mobile"
                className={cn(
                  "app-top-bar__workspace app-top-bar__workspace--stacked",
                  canSwitchWorkspace
                    ? "app-top-bar__workspace--interactive"
                    : "app-top-bar__workspace--static",
                )}
                title={workspaceLabel ?? undefined}
                aria-label={
                  canSwitchWorkspace
                    ? `${t("workspace.switch")}: ${workspaceLabel}`
                    : (workspaceLabel ?? undefined)
                }
                onClick={openWorkspaceSwitcher}
                disabled={!canSwitchWorkspace}
              >
                <span className="app-top-bar__workspace-org">
                  {boundWorkspace.organizationDisplayName}
                </span>
                <span className="app-top-bar__workspace-branch">
                  {boundWorkspace.branchName ?? t("experience.manageBusiness")}
                </span>
              </button>
            ) : (
              <p className="app-top-bar__app-name">{t("app.name")}</p>
            )}
          </div>
          <div className="app-top-bar__brand-copy hidden md:block">
            <p className="app-top-bar__app-name">{t("app.name")}</p>
          </div>
        </div>

        <div className="app-top-bar__center hidden md:flex">
          {boundWorkspace ? (
            <button
              type="button"
              data-testid="workspace-context"
              className={cn(
                "app-top-bar__workspace",
                canSwitchWorkspace
                  ? "app-top-bar__workspace--interactive"
                  : "app-top-bar__workspace--static",
              )}
              title={workspaceLabel ?? undefined}
              aria-label={
                canSwitchWorkspace
                  ? `${t("workspace.switch")}: ${workspaceLabel}`
                  : (workspaceLabel ?? undefined)
              }
              onClick={openWorkspaceSwitcher}
              disabled={!canSwitchWorkspace}
            >
              <Building2 className="app-top-bar__workspace-icon" aria-hidden />
              <span className="app-top-bar__workspace-text">
                <span className="app-top-bar__workspace-org">
                  {boundWorkspace.organizationDisplayName}
                </span>
                {boundWorkspace.branchName ? (
                  <>
                    <span className="app-top-bar__workspace-sep" aria-hidden>
                      ·
                    </span>
                    <span className="app-top-bar__workspace-branch">
                      {boundWorkspace.branchName}
                    </span>
                  </>
                ) : null}
              </span>
              {canSwitchWorkspace ? (
                <ChevronDown className="app-top-bar__workspace-chevron" aria-hidden />
              ) : null}
            </button>
          ) : (
            <span className="sr-only">{t("topbar.workspacePending")}</span>
          )}
        </div>

        <div className="app-top-bar__actions">
          <ShellConnectionButton
            testId="org-shell-connection-button"
            className="app-top-bar__action"
          />
          {canOpenOrgNotifications ? (
            <ShellNotificationButton
              to="/org/notifications"
              label={t("shell.notifications.label")}
              unreadLabel={t("shell.notifications.unreadLabel")}
              badge={badge}
              testId="org-notification-bell"
              className="app-top-bar__action"
              linkState={notificationsLinkState}
              onNavigate={
                onOrgNotificationsPage || !returnTo
                  ? undefined
                  : () => {
                      navigate("/org/notifications", {
                        state: notificationsLinkState,
                      });
                    }
              }
            />
          ) : null}
          <AccountMenu
            compact
            signingOut={signingOut}
            onSignOut={() => {
              void handleSignOut();
            }}
          />
        </div>
      </div>

      {signOutError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{signOutError}</p>
        </div>
      ) : null}
    </header>
  );
}
