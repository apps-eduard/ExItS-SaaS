import { Link, useLocation } from "react-router-dom";
import {
  ORGANIZATIONS_LIST_STATE_KEY,
  isOrganizationWorkspacePath,
  isOrganizationWorkspaceSectionPath,
  organizationWorkspaceHref,
  organizationsListHref,
  parseOrganizationWorkspaceSection,
  type OrganizationsLocationState,
} from "@/api/organizations/organization-id";
import { useOrganizationWorkspaceIdentity } from "@/features/organizations/organization-workspace-context";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import type { MessageKey } from "@/lib/i18n/messages";
import { itemsForPathname, resolveKnownReactRoute } from "@/lib/navigation/known-react-routes";

const SECTION_LABELS: Record<string, MessageKey> = {
  branches: "organization.workspace.nav.branches",
  people: "organization.workspace.nav.people",
  products: "organization.workspace.nav.products",
  subscription: "organization.workspace.nav.subscription",
  entitlements: "organization.workspace.nav.entitlements",
  billing: "organization.workspace.nav.billing",
  activity: "organization.workspace.nav.activity",
};

function labelForAuthorizedPath(pathname: string, t: (key: MessageKey) => string): string | null {
  const item = itemsForPathname(pathname)[0];
  return item ? t(item.labelKey) : null;
}

export function AppBreadcrumbs() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const location = useLocation();
  const workspace = useOrganizationWorkspaceIdentity();
  const path =
    location.pathname.length > 1 && location.pathname.endsWith("/")
      ? location.pathname.slice(0, -1)
      : location.pathname;
  const isOverview = path === "/admin";
  const isOrgWorkspace = isOrganizationWorkspacePath(path);
  const workspaceSection = parseOrganizationWorkspaceSection(path);
  const isOrgSection = isOrganizationWorkspaceSectionPath(path);
  const orgName = workspace?.identity?.displayName ?? t("organization.breadcrumb.fallback");
  const listState = (location.state as OrganizationsLocationState | null) ?? null;
  const organizationsHref = organizationsListHref(listState?.[ORGANIZATIONS_LIST_STATE_KEY]);
  const overviewHref = workspace?.identity
    ? organizationWorkspaceHref(workspace.identity.id)
    : organizationsHref;
  const resolution = resolveKnownReactRoute({
    pathname: path,
    permissionStatus: authorization.status,
    hasAnyPermission: authorization.hasAnyPermission,
    isPlatformAdministrator: authorization.isPlatformAdministrator,
    developmentToolsAllowed: areDevelopmentToolsAllowed(),
  });
  const currentLabel = isOverview
    ? t("nav.overview")
    : isOrgWorkspace
      ? orgName
      : isOrgSection && workspaceSection && SECTION_LABELS[workspaceSection]
        ? t(SECTION_LABELS[workspaceSection]!)
        : resolution === "implemented"
          ? (labelForAuthorizedPath(path, t) ?? t("shell.notFound.title"))
          : resolution === "under-development"
            ? (labelForAuthorizedPath(path, t) ?? t("underDevelopment.title"))
            : t("shell.notFound.title");

  return (
    <nav aria-label={t("shell.breadcrumb")} className="min-w-0 overflow-hidden">
      <ol className="flex flex-wrap items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
        {isOverview ? (
          <li className="truncate text-foreground" aria-current="page">
            {currentLabel}
          </li>
        ) : isOrgWorkspace ? (
          <>
            <li>
              <Link className="text-primary hover:underline" to="/admin">
                {t("nav.overview")}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <Link className="text-primary hover:underline" to={organizationsHref}>
                {t("nav.organizations")}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <span className="truncate text-foreground" aria-current="page">
                {currentLabel}
              </span>
            </li>
          </>
        ) : isOrgSection ? (
          <>
            <li>
              <Link className="text-primary hover:underline" to="/admin">
                {t("nav.overview")}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <Link className="text-primary hover:underline" to={organizationsHref}>
                {t("nav.organizations")}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <Link
                className="truncate text-primary hover:underline"
                state={listState}
                to={overviewHref}
              >
                {orgName}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <span className="truncate text-foreground" aria-current="page">
                {currentLabel}
              </span>
            </li>
          </>
        ) : resolution === "pending" ? (
          <li>
            <Link className="text-primary hover:underline" to="/admin">
              {t("nav.overview")}
            </Link>
          </li>
        ) : (
          <>
            <li>
              <Link className="text-primary hover:underline" to="/admin">
                {t("nav.overview")}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <span className="truncate text-foreground" aria-current="page">
                {currentLabel}
              </span>
            </li>
          </>
        )}
      </ol>
    </nav>
  );
}
