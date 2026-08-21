import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import {
  canCreateSale,
  canEnterCashierRoleHome,
  canEnterManagerRoleHome,
  canEnterOwnerRoleHome,
  canInviteOrganizationStaff,
  canManageCatalog,
  canManageInventory,
  canManageShifts,
  canUseAdminExperience,
  canViewInventory,
  canViewRegisters,
  canViewShifts,
} from "@/access/pos-capabilities";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { ExperienceAccessDeniedPage } from "@/features/role/ExperienceAccessDeniedPage";
import { SellAccessDeniedPage } from "@/features/sell/SellAccessDeniedPage";
import { useI18n } from "@/i18n/I18nProvider";
import { sessionAccountClass, type AccountClassName } from "@/session/account-class";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workspaceRouteForOutcome } from "@/workspace/workspace-resolver";

export function SessionLoading() {
  const { t } = useI18n();
  return <LoadingState label={t("session.loading")} />;
}

export function RequireSession({ children }: { children: ReactNode }) {
  const { status } = useSession();
  const location = useLocation();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "expired") {
    return <Navigate to="/sign-in" replace state={{ expired: true, from: location.pathname }} />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/sign-in" replace state={{ from: location.pathname }} />;
  }
  return children;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useSession();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }
  return children;
}

/**
 * Enforces server-reported AccountClass. Never infers class from email/username.
 * LinkedPersonalUserId is correlation only and must not bypass this guard.
 */
export function RequireAccountClass({
  allow,
  children,
}: {
  allow: readonly AccountClassName[];
  children: ReactNode;
}) {
  const { status, session } = useSession();
  const { t } = useI18n();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/sign-in" replace />;
  }

  const accountClass = sessionAccountClass(session);
  if (accountClass && allow.includes(accountClass)) {
    return children;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="account-class-denied">
      <PageHeader
        title={t("accountClass.deniedTitle")}
        description={t("accountClass.deniedLede")}
      />
      <ErrorState title={t("accountClass.deniedTitle")} detail={t("accountClass.deniedDetail")} />
    </div>
  );
}

export function RequirePersonalSession({ children }: { children: ReactNode }) {
  return <RequireAccountClass allow={["Personal"]}>{children}</RequireAccountClass>;
}

export function RequireOrganizationSession({ children }: { children: ReactNode }) {
  return <RequireAccountClass allow={["Organization"]}>{children}</RequireAccountClass>;
}

/**
 * Staff invitation accept: anonymous or Personal only.
 * Organization/Platform sessions cannot convert via LinkedPersonalUserId or accept-as-personal.
 */
export function AllowInvitationAccept({ children }: { children: ReactNode }) {
  const { status, session } = useSession();
  const { t } = useI18n();

  if (status === "loading") {
    return <SessionLoading />;
  }

  if (status === "authenticated") {
    const accountClass = sessionAccountClass(session);
    if (accountClass === "Organization" || accountClass === "Platform") {
      return (
        <div className="flex min-w-0 flex-col gap-4" data-testid="account-class-denied">
          <PageHeader
            title={t("accountClass.deniedTitle")}
            description={t("accountClass.deniedLede")}
          />
          <ErrorState
            title={t("accountClass.deniedTitle")}
            detail={t("accountClass.deniedDetail")}
          />
        </div>
      );
    }
  }

  return children;
}

export function RequireWorkspaceBound({ children }: { children: ReactNode }) {
  const { status, boundWorkspace, routingPlan } = useWorkspace();

  if (status === "loading" || status === "binding" || status === "idle") {
    return <SessionLoading />;
  }
  // Branch-scoped surfaces prefer a branch; org-only Manage Business still reaches
  // capability guards (CreateSale / catalog) so denials stay explicit.
  if (boundWorkspace) {
    return children;
  }
  // Auto destination bind is in flight via WorkspaceProvider.
  if (routingPlan?.outcome === "AutoSelect" || routingPlan?.outcome === "AutoDestination") {
    return <SessionLoading />;
  }
  if (routingPlan) {
    return <Navigate to={workspaceRouteForOutcome(routingPlan.outcome)} replace />;
  }
  return <Navigate to="/workspace" replace />;
}

/** Organization-level Manage Business — branch optional. */
export function RequireOrganizationBound({ children }: { children: ReactNode }) {
  const { status, boundWorkspace, routingPlan } = useWorkspace();

  if (status === "loading" || status === "binding" || status === "idle") {
    return <SessionLoading />;
  }
  if (boundWorkspace?.organizationId) {
    return children;
  }
  if (routingPlan?.outcome === "AutoSelect" || routingPlan?.outcome === "AutoDestination") {
    return <SessionLoading />;
  }
  if (routingPlan) {
    return <Navigate to={workspaceRouteForOutcome(routingPlan.outcome)} replace />;
  }
  return <Navigate to="/workspace" replace />;
}

export function WorkspaceBootGate({ children }: { children: ReactNode }) {
  const { status: sessionStatus } = useSession();
  const { status } = useWorkspace();

  if (sessionStatus === "loading") {
    return <SessionLoading />;
  }
  if (sessionStatus === "authenticated" && (status === "loading" || status === "binding")) {
    return <SessionLoading />;
  }
  return children;
}

export function RequireCreateSale({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canCreateSale(sessionGrant)) {
    return <SellAccessDeniedPage />;
  }

  return children;
}

/** Organization Web / admin experience — Owner or OrganizationAdministrator, not POS Manager alone. */
export function RequireAdminExperience({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canUseAdminExperience(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="admin-experience-denied" />;
  }

  return children;
}

export function RequireInviteStaff({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canInviteOrganizationStaff(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="staff-invite-denied" />;
  }

  return children;
}

export function RequireOwnerRoleHome({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canEnterOwnerRoleHome(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="owner-role-denied" />;
  }

  return children;
}

export function RequireManagerRoleHome({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canEnterManagerRoleHome(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="manager-role-denied" />;
  }

  return children;
}

export function RequireCashierRoleHome({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canEnterCashierRoleHome(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="cashier-role-denied" />;
  }

  return children;
}

export function RequireManageCatalog({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManageCatalog(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="catalog-manage-denied" />;
  }

  return children;
}

export function RequireViewInventory({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewInventory(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="inventory-view-denied" />;
  }

  return children;
}

export function RequireManageInventory({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManageInventory(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="inventory-manage-denied" />;
  }

  return children;
}

export function RequireViewShifts({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewShifts(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="shifts-view-denied" />;
  }

  return children;
}

export function RequireManageShifts({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManageShifts(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="shifts-manage-denied" />;
  }

  return children;
}

export function RequireViewRegisters({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewRegisters(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="registers-view-denied" />;
  }

  return children;
}
