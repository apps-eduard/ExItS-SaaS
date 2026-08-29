import type { ReactNode } from "react";
import { useEffect, useRef, useState } from "react";
import { Navigate, useLocation } from "react-router-dom";
import {
  canAccessReportsHub,
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
  canViewDashboard,
  canViewExpenses,
  canManageExpenses,
  canViewInventory,
  canViewPurchasing,
  canViewRegisters,
  canViewReports,
  canViewReturns,
  canViewShifts,
  canViewStatement,
  canViewSuppliers,
} from "@/access/pos-capabilities";
import { canAccessClassicReport } from "@/features/reports/report-access";
import { OnlineRequiredBoot } from "@/components/exits/OnlineRequiredBoot";
import { AppBootLoader } from "@/components/exits/loading/AppBootLoader";
import { PageHeader } from "@/components/exits/PageHeader";
import { useOptionalConnectivity } from "@/connectivity/ConnectivityProvider";
import { isAccountContextSwitchPath } from "@/features/account/account-context-switch-route";
import { ExperienceAccessDeniedPage } from "@/features/role/ExperienceAccessDeniedPage";
import { SellAccessDeniedPage } from "@/features/sell/SellAccessDeniedPage";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import { sessionAccountClass, type AccountClassName } from "@/session/account-class";
import { isAuthenticatedOrColdStartOffline, isOfflinePinFlowStatus, useSession } from "@/session/SessionProvider";
import { personalWebAllowsOfflineSession } from "@/runtime/personal-web-runtime-policy";
import { organizationWebAllowsOfflineSession } from "@/runtime/organization-web-runtime-policy";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workspaceRouteForOutcome } from "@/workspace/workspace-resolver";
import {
  workingExperienceRoute,
  type WorkingExperience,
} from "@/workspace/working-experience";

export function SessionLoading() {
  const { t } = useI18n();
  return (
    <AppBootLoader
      label={t("loading.preparingWorkspace")}
      brand={t("app.name")}
      testId="session-checking"
    />
  );
}

export function RequireSession({ children }: { children: ReactNode }) {
  const { status, refreshSession } = useSession();
  const location = useLocation();
  const isContextSwitch = isAccountContextSwitchPath(location.pathname);
  const connectivity = useOptionalConnectivity();
  const [retrying, setRetrying] = useState(false);

  if (status === "loading" && !isContextSwitch) {
    return <SessionLoading />;
  }
  if (status === "expired") {
    return <Navigate to="/sign-in" replace state={{ expired: true, from: location.pathname }} />;
  }
  if (status === "offline_pin_required" || status === "needs_offline_unlock") {
    // Web online-only: never route to offline PIN when both channel policies deny offline session.
    if (!personalWebAllowsOfflineSession() && !organizationWebAllowsOfflineSession()) {
      const offline =
        connectivity != null
          ? !connectivity.isOnline
          : typeof navigator !== "undefined" && !navigator.onLine;
      if (offline) {
        return (
          <OnlineRequiredBoot
            retrying={retrying}
            onRetry={async () => {
              setRetrying(true);
              try {
                const restored = connectivity ? await connectivity.retry() : true;
                if (restored) {
                  await refreshSession();
                }
              } finally {
                setRetrying(false);
              }
            }}
          />
        );
      }
      return <Navigate to="/sign-in" replace state={{ from: location.pathname }} />;
    }
    return <Navigate to="/offline-pin" replace state={{ from: location.pathname }} />;
  }
  if (isAuthenticatedOrColdStartOffline(status)) {
    return children;
  }

  const offline =
    connectivity != null ? !connectivity.isOnline : typeof navigator !== "undefined" && !navigator.onLine;

  if (offline) {
    return (
      <OnlineRequiredBoot
        retrying={retrying}
        onRetry={async () => {
          setRetrying(true);
          try {
            const restored = connectivity ? await connectivity.retry() : true;
            if (restored) {
              await refreshSession();
            }
          } finally {
            setRetrying(false);
          }
        }}
      />
    );
  }

  return <Navigate to="/sign-in" replace state={{ from: location.pathname }} />;
}

export function GuestOnly({ children }: { children: ReactNode }) {
  const { status } = useSession();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "authenticated") {
    return <Navigate to="/" replace />;
  }
  if (status === "cold_start_offline") {
    return <Navigate to="/" replace />;
  }
  if (isOfflinePinFlowStatus(status)) {
    return children;
  }
  return children;
}

export function RequireOnlineSession({ children }: { children: ReactNode }) {
  const { status } = useSession();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status !== "authenticated") {
    return <Navigate to="/sign-in" replace />;
  }
  return children;
}

export function RequireOfflinePinFlow({ children }: { children: ReactNode }) {
  const { status } = useSession();

  if (status === "loading") {
    return <SessionLoading />;
  }
  if (status === "authenticated" || isOfflinePinFlowStatus(status)) {
    return children;
  }
  if (status === "cold_start_offline") {
    return <Navigate to="/" replace />;
  }
  return <Navigate to="/sign-in" replace />;
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
  if (!isAuthenticatedOrColdStartOffline(status)) {
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
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("accountClass.deniedDetail")}</p>
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
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("accountClass.deniedDetail")}
          </p>
        </div>
      );
    }
  }

  return children;
}

export function RequireWorkspaceBound({ children }: { children: ReactNode }) {
  const { status, boundWorkspace, routingPlan } = useWorkspace();

  // Prefer an existing bind over a background reload spinner. Intentional rebinds
  // still use status === "binding".
  if (status === "binding" || ((status === "loading" || status === "idle") && !boundWorkspace)) {
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

/**
 * Branch-scoped org surfaces (Catalog Sell floor, Inventory, Shifts, …).
 * Manage Business binds org-only (no branch) — never leave the user on endless
 * "Checking session…" when they open these tabs.
 *
 * Start a Business creates a Main Branch while the owner often stays on Manage Business.
 * Opening Sell/Catalog with exactly one branch auto-binds it instead of "Choose a branch".
 */
export function RequireBranchBound({ children }: { children: ReactNode }) {
  const { status, boundWorkspace, routingPlan, workspaces, bindDestination } = useWorkspace();
  const location = useLocation();
  const autoBindKeyRef = useRef<string | null>(null);

  const orgWorkspace = boundWorkspace
    ? workspaces.find(
        (item) =>
          item.organizationId.localeCompare(boundWorkspace.organizationId, undefined, {
            sensitivity: "accent",
          }) === 0,
      )
    : undefined;
  const availableBranches = orgWorkspace?.branches ?? [];
  const singleBranch =
    boundWorkspace && !boundWorkspace.branchId && availableBranches.length === 1
      ? availableBranches[0]
      : null;

  useEffect(() => {
    if (!boundWorkspace || !singleBranch) {
      return;
    }
    const experience: WorkingExperience = location.pathname.startsWith("/sell")
      ? "start_selling"
      : "operations";
    const key = `${boundWorkspace.organizationId}:${singleBranch.branchId}:${experience}`;
    if (autoBindKeyRef.current === key) {
      return;
    }
    autoBindKeyRef.current = key;
    void bindDestination({
      organizationId: boundWorkspace.organizationId,
      organizationDisplayName: boundWorkspace.organizationDisplayName,
      branchId: singleBranch.branchId,
      branchName: singleBranch.name,
      experience,
      route: workingExperienceRoute(experience),
      labelKey:
        experience === "start_selling" ? "experience.startSelling" : "experience.operations",
    }).then((ok) => {
      if (!ok) {
        autoBindKeyRef.current = null;
      }
    });
  }, [bindDestination, boundWorkspace, location.pathname, singleBranch]);

  if (status === "binding" || ((status === "loading" || status === "idle") && !boundWorkspace)) {
    return <SessionLoading />;
  }
  if (boundWorkspace?.branchId) {
    return children;
  }
  if (boundWorkspace) {
    if (availableBranches.length === 0) {
      return <Navigate to="/org/branches" replace />;
    }
    if (singleBranch) {
      // Auto-bind in flight (Start a Business Main Branch while still on Manage Business).
      return <SessionLoading />;
    }
    return <BranchRequiredPanel />;
  }
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

  if (status === "binding" || ((status === "loading" || status === "idle") && !boundWorkspace)) {
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
  const { status, boundWorkspace } = useWorkspace();
  const location = useLocation();
  const isContextSwitch = isAccountContextSwitchPath(location.pathname);

  if (isContextSwitch) {
    return children;
  }

  if (sessionStatus === "loading") {
    return <SessionLoading />;
  }
  // Keep shell painted during intentional bind — RootLayout shows the overlay.
  if (sessionStatus === "authenticated" && status === "binding") {
    return children;
  }
  if (
    sessionStatus === "authenticated" &&
    ((status === "loading" || status === "idle") && !boundWorkspace)
  ) {
    return <SessionLoading />;
  }
  if (sessionStatus === "cold_start_offline" && status === "idle") {
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

/** Organization ownership transfer — Owner membership only (not Admin alone). */
export function RequireOrganizationOwnerMembership({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canInviteOrganizationStaff(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="org-owner-membership-denied" />;
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

export function RequireViewExpenses({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewExpenses(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="expenses-view-denied" />;
  }

  return children;
}

export function RequireManageExpenses({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canManageExpenses(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="expenses-manage-denied" />;
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

export function RequireViewDashboard({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewDashboard(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="dashboard-view-denied" />;
  }

  return children;
}

export function RequireAccessReportsHub({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canAccessReportsHub(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="reports-hub-denied" />;
  }

  return children;
}

export function RequireViewReports({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();

  if (!canViewReports(sessionGrant)) {
    return <ExperienceAccessDeniedPage testId="reports-view-denied" />;
  }

  return children;
}

export function RequireClassicSalesReport({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();
  if (!canAccessClassicReport(sessionGrant, "sales")) {
    return <ExperienceAccessDeniedPage testId="classic-sales-denied" />;
  }
  return children;
}

export function RequireClassicUtangReport({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();
  if (!canAccessClassicReport(sessionGrant, "utang")) {
    return <ExperienceAccessDeniedPage testId="classic-utang-denied" />;
  }
  return children;
}

export function RequireClassicInventoryReport({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();
  if (!canAccessClassicReport(sessionGrant, "inventory")) {
    return <ExperienceAccessDeniedPage testId="classic-inventory-denied" />;
  }
  return children;
}

export function RequireClassicExpensesReport({ children }: { children: ReactNode }) {
  const { sessionGrant } = useWorkspace();
  if (!canAccessClassicReport(sessionGrant, "expenses")) {
    return <ExperienceAccessDeniedPage testId="classic-expenses-denied" />;
  }
  return children;
}
