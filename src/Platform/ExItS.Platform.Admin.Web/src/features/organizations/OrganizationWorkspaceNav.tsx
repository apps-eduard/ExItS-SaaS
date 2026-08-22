import { NavLink, useLocation, useParams } from "react-router-dom";
import {
  organizationWorkspaceHref,
  parseOrganizationId,
  type OrganizationsLocationState,
} from "@/api/organizations/organization-id";
import { usePreferences } from "@/hooks/use-preferences";
import { cn } from "@/lib/utils";

export function OrganizationWorkspaceNav() {
  const { t } = usePreferences();
  const params = useParams();
  const location = useLocation();
  const organizationId = parseOrganizationId(params.organizationId);
  if (!organizationId) {
    return null;
  }
  const state: OrganizationsLocationState =
    (location.state as OrganizationsLocationState | null) ?? {};

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      "rounded-[var(--exits-density-radius)] px-2 py-1 text-[length:var(--exits-text-sm)] font-medium",
      isActive
        ? "bg-surface-muted text-foreground"
        : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
    );

  return (
    <nav aria-label={t("organization.workspace.nav")} className="min-w-0">
      <ul className="flex flex-wrap gap-1">
        <li>
          <NavLink
            className={linkClass}
            end
            state={state}
            to={organizationWorkspaceHref(organizationId)}
          >
            {t("organization.workspace.nav.overview")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "branches")}
          >
            {t("organization.workspace.nav.branches")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "people")}
          >
            {t("organization.workspace.nav.people")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "roles")}
          >
            {t("organization.workspace.nav.roles")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "product-access")}
          >
            {t("organization.workspace.nav.productAccess")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "enabled-products")}
          >
            {t("organization.workspace.nav.enabledProducts")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "products")}
          >
            {t("organization.workspace.nav.products")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "subscription")}
          >
            {t("organization.workspace.nav.subscription")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "entitlements")}
          >
            {t("organization.workspace.nav.entitlements")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "billing")}
          >
            {t("organization.workspace.nav.billing")}
          </NavLink>
        </li>
        <li>
          <NavLink
            className={linkClass}
            state={state}
            to={organizationWorkspaceHref(organizationId, "activity")}
          >
            {t("organization.workspace.nav.activity")}
          </NavLink>
        </li>
      </ul>
    </nav>
  );
}
