import { useMemo, useState } from "react";
import { Notice } from "@/components/exits/Notice";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, MapPin, Store, Warehouse } from "lucide-react";
import { listOrganizationNotifications } from "@/api/platform/organization-notifications-client";
import { AccountMenu } from "@/components/exits/AccountMenu";
import { ShellConnectionButton } from "@/components/exits/ShellConnectionButton";
import { ShellNeedsAttentionButton } from "@/components/exits/ShellNeedsAttentionButton";
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

type AppTopBarProps = {
  /**
   * Desktop admin/ops shells own branding in the sidebar.
   * Hide the duplicate logo/name at lg+ while keeping mobile brand chrome.
   */
  hideDesktopBrand?: boolean;
};

export function AppTopBar({ hideDesktopBrand = false }: AppTopBarProps) {
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

  const typeChipLabel =
    indicator.typeLabel === "Warehouse"
      ? t("branches.type.warehouse")
      : indicator.typeLabel === "Retail"
        ? t("branches.type.retail")
        : null;

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
    const areaOnly = indicator.areaName?.trim() || null;
    const showSplitSecondary = Boolean(areaOnly && typeChipLabel);

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
          indicator.typeLabel === "Warehouse" && "app-top-bar__workspace--warehouse",
          indicator.typeLabel === "Retail" && "app-top-bar__workspace--retail",
        )}
        title={indicator.title}
        aria-label={ariaLabel}
        onClick={openWorkspaceSwitcher}
        disabled={!canSwitchWorkspace}
      >
        <span className="app-top-bar__workspace-icon-wrap" aria-hidden="true">
          <LocationIcon className="app-top-bar__workspace-icon" strokeWidth={2.25} />
        </span>
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
                {showSplitSecondary ? (
                  <>
                    <span className="app-top-bar__workspace-area">{areaOnly}</span>
                    <span className="app-top-bar__workspace-sep" aria-hidden>
                      {" "}
                      ·{" "}
                    </span>
                    <span className="app-top-bar__workspace-type">{typeChipLabel}</span>
                  </>
                ) : typeChipLabel && indicator.secondary === typeChipLabel ? (
                  <span className="app-top-bar__workspace-type">{typeChipLabel}</span>
                ) : (
                  indicator.secondary
                )}
              </span>
            </>
          ) : null}
        </span>
        {canSwitchWorkspace ? (
          <span className="app-top-bar__workspace-chevron-wrap" aria-hidden="true">
            <ChevronDown className="app-top-bar__workspace-chevron" strokeWidth={2.25} />
          </span>
        ) : null}
      </button>
    );
  }

  return (
    <header
      className={cn("app-top-bar", hideDesktopBrand && "app-top-bar--shell-desktop")}
      data-testid="app-top-bar"
      data-hide-desktop-brand={hideDesktopBrand ? "true" : "false"}
    >
      <div
        className={cn(
          "app-top-bar__row",
          hideDesktopBrand && "app-top-bar__row--shell-desktop",
        )}
      >
        <div
          className={cn(
            "app-top-bar__brand",
            hideDesktopBrand && "app-top-bar__brand--shell-mobile",
          )}
        >
          <span className="app-top-bar__mark" aria-hidden="true">
            <span className="app-top-bar__mark-glyph">E</span>
          </span>
          <div className="app-top-bar__brand-copy">
            {/* Mobile topbar: no branch selector — switch via Account menu → Workspace. */}
            <p className="app-top-bar__app-name">{t("app.name")}</p>
          </div>
        </div>

        <div
          className={cn(
            "app-top-bar__center hidden lg:flex",
            hideDesktopBrand && "app-top-bar__center--shell-desktop",
          )}
        >
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
          {boundWorkspace ? (
            <ShellNeedsAttentionButton
              testId="org-needs-attention"
              className="app-top-bar__action"
            />
          ) : null}
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
          <span className="app-top-bar__actions-divider" aria-hidden="true" />
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
        <Notice tone="danger">{signOutError}</Notice>
      ) : null}
    </header>
  );
}
