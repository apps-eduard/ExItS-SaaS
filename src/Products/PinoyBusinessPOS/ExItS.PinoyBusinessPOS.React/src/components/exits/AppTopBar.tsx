import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, MapPin, Store, Warehouse } from "lucide-react";
import { listOrganizationNotifications } from "@/api/platform/organization-notifications-client";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNotificationButton } from "@/components/exits/ShellNotificationButton";
import { ShellPreferencesButton } from "@/components/exits/ShellPreferencesButton";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import {
  countUnreadOrganizationNotifications,
  formatUnreadNotificationBadge,
  organizationNotificationsQueryKey,
} from "@/features/org/org-notifications";
import { useI18n } from "@/i18n/I18nProvider";
import { isOrganizationContextLocked, sessionAccountClass } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import {
  isWorkspaceChooserPath,
  resolveWorkspaceLocationIndicator,
} from "@/workspace/workspace-location-indicator";
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
  const branchId = boundWorkspace?.branchId ?? null;
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
      ? organizationNotificationsQueryKey(organizationId, branchId)
      : ["organization", "notifications", "none"],
    enabled: canOpenOrgNotifications && organizationId !== null,
    queryFn: ({ signal }) =>
      listOrganizationNotifications(organizationId!, signal, branchId),
  });
  const unreadCount = countUnreadOrganizationNotifications(notificationsQuery.data);
  const badge = formatUnreadNotificationBadge(unreadCount);

  const indicator = useMemo(
    () =>
      resolveWorkspaceLocationIndicator({
        boundWorkspace,
        workspaces,
        chooseWorkspaceLabel: t("workspace.title"),
        retailLabel: t("branches.type.retail"),
        warehouseLabel: t("branches.type.warehouse"),
      }),
    [boundWorkspace, t, workspaces],
  );

  const LocationIcon =
    indicator.typeLabel === "Warehouse"
      ? Warehouse
      : indicator.typeLabel === "Retail"
        ? Store
        : MapPin;

  const showWorkspaceControl = Boolean(boundWorkspace) || canSwitchWorkspace;
  const ariaLabel = canSwitchWorkspace
    ? t("workspace.changeLocationAria").replace("{details}", indicator.detailsForAria)
    : indicator.detailsForAria;

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

  function openWorkspaceSwitcher() {
    if (!canSwitchWorkspace) return;
    if (isWorkspaceChooserPath(location.pathname)) return;
    navigate("/workspace");
  }

  function renderWorkspaceButton(testId: string, stacked: boolean) {
    return (
      <button
        type="button"
        data-testid={testId}
        data-has-location={indicator.hasBoundLocation ? "true" : "false"}
        data-location-type={indicator.typeLabel ?? "none"}
        className={cn(
          "app-top-bar__workspace",
          stacked && "app-top-bar__workspace--stacked",
          canSwitchWorkspace
            ? "app-top-bar__workspace--interactive"
            : "app-top-bar__workspace--static",
        )}
        title={indicator.title}
        aria-label={ariaLabel}
        onClick={openWorkspaceSwitcher}
        disabled={!canSwitchWorkspace}
      >
        <LocationIcon className="app-top-bar__workspace-icon" aria-hidden />
        <span className="app-top-bar__workspace-text">
          <span className="app-top-bar__workspace-primary" data-testid={`${testId}-primary`}>
            {indicator.primary}
          </span>
          {indicator.secondary ? (
            <>
              <span className="app-top-bar__workspace-sep" aria-hidden>
                ·
              </span>
              <span
                className="app-top-bar__workspace-secondary"
                data-testid={`${testId}-secondary`}
              >
                {indicator.secondary}
              </span>
            </>
          ) : null}
        </span>
        {canSwitchWorkspace ? (
          <ChevronDown className="app-top-bar__workspace-chevron" aria-hidden />
        ) : null}
      </button>
    );
  }

  return (
    <header className="app-top-bar" data-testid="app-top-bar">
      <div className="app-top-bar__row">
        <div className="app-top-bar__brand">
          <span className="app-top-bar__mark" aria-hidden="true">
            E
          </span>
          <div className="app-top-bar__brand-copy md:hidden">
            {showWorkspaceControl ? (
              renderWorkspaceButton("workspace-context-mobile", true)
            ) : (
              <p className="app-top-bar__app-name">{t("app.name")}</p>
            )}
          </div>
          <div className="app-top-bar__brand-copy hidden md:block">
            <p className="app-top-bar__app-name">{t("app.name")}</p>
          </div>
        </div>

        <div className="app-top-bar__center hidden md:flex">
          {showWorkspaceControl ? (
            renderWorkspaceButton("workspace-context", false)
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
          <ShellUiStandardsButton
            label={t("uiStandards.topbar")}
            className="app-top-bar__action"
          />
          <ShellPreferencesButton
            label={t("topbar.preferences")}
            className="app-top-bar__action"
          />
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
