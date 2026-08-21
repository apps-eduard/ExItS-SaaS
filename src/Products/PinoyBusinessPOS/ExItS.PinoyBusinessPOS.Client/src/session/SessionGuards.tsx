import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import {
  canCreateCustomer,
  canCreateSale,
  canEditCustomer,
  canEnterCashierRoleHome,
  canEnterManagerRoleHome,
  canEnterOwnerRoleHome,
  canInviteOrganizationStaff,
  canManageCatalog,
  canManageInventory,
  canManagePurchasing,
  canManageShifts,
  canManageSuppliers,
  canProcessReturn,
  canRecordRepayment,
  canUseAdminExperience,
  canViewCustomerOrders,
  canManageCustomerOrders,
  canViewCustomers,
  canViewInventory,
  canViewPurchasing,
  canViewRegisters,
  canViewReturns,
  canViewShifts,
  canViewStatement,
  canViewSuppliers,
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

export function RequireViewCustomers({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewCustomers(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customers-view-denied" />;
  }

  return children;
}

export function RequireCreateCustomer({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canCreateCustomer(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customers-create-denied" />;
  }

  return children;
}

export function RequireEditCustomer({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canEditCustomer(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customers-edit-denied" />;
  }

  return children;
}

export function RequireRecordRepayment({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canRecordRepayment(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customers-repay-denied" />;
  }

  return children;
}

export function RequireViewStatement({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewStatement(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customers-statement-denied" />;
  }

  return children;
}

export function RequireViewSuppliers({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewSuppliers(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="suppliers-view-denied" />;
  }

  return children;
}

export function RequireManageSuppliers({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManageSuppliers(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="suppliers-manage-denied" />;
  }

  return children;
}

export function RequireViewPurchasing({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewPurchasing(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="purchasing-view-denied" />;
  }

  return children;
}

export function RequireManagePurchasing({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManagePurchasing(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="purchasing-manage-denied" />;
  }

  return children;
}

/** Hub entry: ViewPurchasing OR ManageInventory OR ViewSuppliers (MAUI PurchasingHub). */
export function RequirePurchasingHubAccess({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (
    !canViewPurchasing(sessionGrant) &&
    !canManageInventory(sessionGrant) &&
    !canViewSuppliers(sessionGrant)
  ) {
    return <ExperienceAccessDeniedPage testId="purchasing-hub-denied" />;
  }

  return children;
}

export function RequireViewReturns({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewReturns(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="returns-view-denied" />;
  }

  return children;
}

export function RequireProcessReturn({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canProcessReturn(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="returns-process-denied" />;
  }

  return children;
}

export function RequireViewCustomerOrders({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewCustomerOrders(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customer-orders-view-denied" />;
  }

  return children;
}

export function RequireManageCustomerOrders({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManageCustomerOrders(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="customer-orders-manage-denied" />;
  }

  return children;
}
