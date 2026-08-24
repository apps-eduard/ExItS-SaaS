import {
  ArrowLeftRight,
  BarChart3,
  Boxes,
  ClipboardList,
  Clock3,
  LayoutDashboard,
  MapPin,
  MonitorSmartphone,
  Package,
  PackagePlus,
  Receipt,
  RefreshCw,
  ShoppingCart,
  Truck,
  Users,
} from "lucide-react";
import type { ReactNode } from "react";
import {
  canAccessReportsHub,
  canCreateSale,
  canManageCatalog,
  canManageShifts,
  canViewCustomers,
  canViewCustomerOrders,
  canViewDashboard,
  canViewInventory,
  canViewPurchasing,
  canViewRegisters,
  canViewReturns,
  canViewShifts,
  canViewSuppliers,
  canUseAdminExperience,
  canUseOperationsExperience,
  hasOrganizationManagementAuthority,
  resolveEffectivePosRoleCode,
} from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ActionTileGrid, type ActionTileDef } from "@/components/exits/ActionTileGrid";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useSellingMode } from "@/selling/SellingModeProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { Link, useNavigate } from "react-router-dom";

type RoleHomeShellProps = {
  titleKey: "role.ownerTitle" | "role.managerTitle" | "role.cashierTitle";
  ledeKey: "role.ownerLede" | "role.managerLede" | "role.cashierLede";
  badgeKey: "role.ownerBadge" | "role.managerBadge" | "role.cashierBadge";
  bodyKey: "role.ownerBody" | "role.managerBody" | "role.cashierBody";
  returnRoute: string;
  primarySell?: boolean;
  showExperienceChooser?: boolean;
  /** Owner-dashboard-style sections + icon-left tiles (Manager / Cashier home). */
  dashboardGuide?: boolean;
  homeTestId?: string;
};

type TileDef = ActionTileDef;

function GuideSection({
  title,
  children,
  testId,
}: {
  title: string;
  children: ReactNode;
  testId?: string;
}) {
  return (
    <section
      className="catalog-form-section exits-animate-panel manager-home-section gap-3"
      data-testid={testId}
    >
      <h2 className="catalog-form-section__title text-muted">{title}</h2>
      {children}
    </section>
  );
}

export function RoleHomeShell({
  titleKey,
  ledeKey,
  badgeKey,
  bodyKey,
  returnRoute,
  primarySell = false,
  showExperienceChooser = false,
  dashboardGuide = false,
  homeTestId = "manager-home",
}: RoleHomeShellProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { enter } = useSellingMode();
  const { sessionGrant } = useWorkspace();

  const canAdmin = canUseAdminExperience(sessionGrant);
  const canOps = canUseOperationsExperience(sessionGrant);
  const canSell = canCreateSale(sessionGrant);
  const canCatalog = canManageCatalog(sessionGrant);
  const canInventory = canViewInventory(sessionGrant);
  const canShifts = canViewShifts(sessionGrant);
  const canOpenShift = canManageShifts(sessionGrant);
  const canRegisters = canViewRegisters(sessionGrant);
  const canCustomers = canViewCustomers(sessionGrant);
  const canCustomerOrders = canViewCustomerOrders(sessionGrant);
  const canSuppliers = canViewSuppliers(sessionGrant);
  const canPurchasing = canViewPurchasing(sessionGrant) || canViewInventory(sessionGrant);
  const canReturns = canViewReturns(sessionGrant);
  const canDashboard = canViewDashboard(sessionGrant);
  const canReports = canAccessReportsHub(sessionGrant);
  const canDevices = hasOrganizationManagementAuthority(sessionGrant);
  const securityRole = resolveEffectivePosRoleCode(sessionGrant);

  function startSelling() {
    enter(returnRoute);
    navigate("/sell");
  }

  const quickTiles: TileDef[] = [];
  if (canSell) {
    quickTiles.push({
      key: "sell",
      label: primarySell ? t("role.openSellFloor") : t("role.startSelling"),
      icon: ShoppingCart,
      testId: "role-start-selling",
      primary: true,
      onClick: startSelling,
    });
  }
  if (primarySell) {
    quickTiles.push({
      key: "workspace",
      label: t("workspace.switch"),
      icon: ArrowLeftRight,
      testId: "role-switch-workspace",
      onClick: () => navigate("/workspace"),
    });
  }

  const operationTiles: TileDef[] = [];
  if (canShifts) {
    operationTiles.push({
      key: "shifts",
      label: t("shift.hubTitle"),
      icon: RefreshCw,
      testId: "open-shifts",
      to: "/shifts",
    });
  }
  if (canOpenShift) {
    operationTiles.push({
      key: "open-shift",
      label: t("shift.openTitle"),
      icon: Clock3,
      testId: "open-shift-open",
      to: "/shifts/open",
    });
  }
  if (canRegisters) {
    operationTiles.push({
      key: "registers",
      label: t("register.listTitle"),
      icon: LayoutDashboard,
      testId: "open-registers",
      to: "/registers",
    });
  }
  if (canCatalog) {
    operationTiles.push({
      key: "catalog",
      label: t("catalog.openCatalog"),
      icon: Package,
      testId: "open-catalog",
      to: "/catalog",
    });
  }
  if (canInventory) {
    operationTiles.push({
      key: "inventory",
      label: t("inventory.open"),
      icon: Boxes,
      testId: "open-inventory",
      to: "/inventory",
    });
    operationTiles.push({
      key: "expiring",
      label: t("inventory.openExpiring"),
      icon: ClipboardList,
      testId: "open-expiring-stock-home",
      to: "/inventory/expiration",
    });
  }
  if (canPurchasing) {
    operationTiles.push({
      key: "purchasing",
      label: t("purchasing.open"),
      icon: PackagePlus,
      testId: "open-purchasing",
      to: "/purchasing",
    });
  }
  if (canSuppliers) {
    operationTiles.push({
      key: "suppliers",
      label: t("suppliers.open"),
      icon: Truck,
      testId: "open-suppliers",
      to: "/suppliers",
    });
  }
  if (canCustomers) {
    operationTiles.push({
      key: "customers",
      label: t("customers.open"),
      icon: Users,
      testId: "open-customers",
      to: "/customers",
    });
  }
  if (canCustomerOrders) {
    operationTiles.push({
      key: "orders",
      label: t("orders.openQueue"),
      icon: ClipboardList,
      testId: "open-customer-orders",
      to: "/orders",
    });
  }
  if (canReturns) {
    operationTiles.push({
      key: "returns",
      label: t("returns.open"),
      icon: Receipt,
      testId: "open-returns",
      to: "/returns",
    });
  }

  const insightTiles: TileDef[] = [];
  if (canDashboard) {
    insightTiles.push({
      key: "dashboard",
      label: t("dashboard.open"),
      icon: BarChart3,
      testId: "open-dashboard",
      to: "/dashboard",
    });
  }
  if (canReports) {
    insightTiles.push({
      key: "reports",
      label: t("reports.open"),
      icon: BarChart3,
      testId: "open-reports",
      to: "/reports",
    });
  }

  const deviceTiles: TileDef[] = [];
  if (canDevices) {
    deviceTiles.push({
      key: "devices",
      label: t("devices.listTitle"),
      icon: MonitorSmartphone,
      testId: "open-org-devices",
      to: "/org/devices",
    });
    deviceTiles.push({
      key: "branches",
      label: t("org.branchesLink"),
      icon: MapPin,
      testId: "open-branch-fulfillment",
      to: "/org/branches",
    });
  }
  deviceTiles.push({
    key: "register-browser",
    label: t("devices.registerThisDevice"),
    icon: MonitorSmartphone,
    testId: "open-device-register",
    to: "/devices/register",
  });

  if (dashboardGuide) {
    return (
      <div
        className="manager-home-page exits-page mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-3"
        data-testid={homeTestId}
      >
        <PageHeader title={t(titleKey)} description={t(ledeKey)} />

        {quickTiles.length > 0 ? (
          <GuideSection title={t("role.section.quickActions")} testId="manager-quick-actions">
            <ActionTileGrid
              tiles={quickTiles}
              /* Cashier: Open new sale + Switch workspace share one row. */
              emphasizePrimary={!(primarySell && quickTiles.length > 1)}
            />
            {canSell ? (
              <p className="manager-home-hint m-0 text-[length:var(--exits-text-xs)] text-muted">
                {t("role.startSellingHint")}
              </p>
            ) : null}
          </GuideSection>
        ) : null}

        {operationTiles.length > 0 ? (
          <GuideSection title={t("role.section.operations")} testId="manager-operations">
            <ActionTileGrid tiles={operationTiles} />
          </GuideSection>
        ) : null}

        {deviceTiles.length > 0 ? (
          <GuideSection title={t("role.section.devices")} testId="manager-devices">
            <ActionTileGrid tiles={deviceTiles} />
          </GuideSection>
        ) : null}

        {insightTiles.length > 0 ? (
          <GuideSection title={t("role.section.insights")} testId="manager-insights">
            <ActionTileGrid tiles={insightTiles} />
          </GuideSection>
        ) : null}
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t(titleKey)} description={t(ledeKey)} />
      <StatusChip tone="success">{t(badgeKey)}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t(bodyKey)}</p>
        {securityRole ? (
          <p
            className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="security-role-label"
          >
            {t("experience.securityRole")}: {securityRole}
          </p>
        ) : null}
      </Card>

      {showExperienceChooser ? (
        <div
          className="grid min-w-0 grid-cols-1 gap-2 sm:grid-cols-3"
          data-testid="owner-experience-chooser"
          role="group"
          aria-label={t("experience.chooserLabel")}
        >
          {canAdmin ? (
            <Button asChild className="min-h-11 w-full">
              <Link to="/org">{t("experience.manageBusiness")}</Link>
            </Button>
          ) : null}
          {canOps ? (
            <Button asChild variant="ghost" className="min-h-11 w-full">
              <Link to="/role/manager">{t("experience.operations")}</Link>
            </Button>
          ) : null}
          {canSell ? (
            <Button type="button" className="min-h-11 w-full" onClick={startSelling}>
              {t("experience.startSelling")}
            </Button>
          ) : null}
        </div>
      ) : (
        <div className="flex flex-wrap gap-2">
          {canSell ? (
            <Button
              type="button"
              className="min-h-11"
              variant={primarySell ? "default" : "ghost"}
              onClick={startSelling}
            >
              {primarySell ? t("role.openSellFloor") : t("role.startSelling")}
            </Button>
          ) : null}
          {primarySell ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => navigate("/workspace")}
            >
              {t("workspace.switch")}
            </Button>
          ) : null}
        </div>
      )}

      {canShifts ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-shifts">
          <Link to="/shifts">{t("shift.hubTitle")}</Link>
        </Button>
      ) : null}
      {canOpenShift ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-shift-open">
          <Link to="/shifts/open">{t("shift.openTitle")}</Link>
        </Button>
      ) : null}
      {canRegisters ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-registers">
          <Link to="/registers">{t("register.listTitle")}</Link>
        </Button>
      ) : null}
      {canDevices ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-org-devices">
          <Link to="/org/devices">{t("devices.listTitle")}</Link>
        </Button>
      ) : null}
      {canDevices ? (
        <Button
          asChild
          variant="ghost"
          className="min-h-11 w-fit"
          data-testid="open-branch-fulfillment"
        >
          <Link to="/org/branches">{t("org.branchesLink")}</Link>
        </Button>
      ) : null}
      <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-device-register">
        <Link to="/devices/register">{t("devices.registerThisDevice")}</Link>
      </Button>
      {canCatalog ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-catalog">
          <Link to="/catalog">{t("catalog.openCatalog")}</Link>
        </Button>
      ) : null}
      {canInventory ? (
        <>
          <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-inventory">
            <Link to="/inventory">{t("inventory.open")}</Link>
          </Button>
          <Button
            asChild
            variant="ghost"
            className="min-h-11 w-fit"
            data-testid="open-expiring-stock-home"
          >
            <Link to="/inventory/expiration">{t("inventory.openExpiring")}</Link>
          </Button>
        </>
      ) : null}
      {canCustomers ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-customers">
          <Link to="/customers">{t("customers.open")}</Link>
        </Button>
      ) : null}
      {canCustomerOrders ? (
        <Button
          asChild
          variant="ghost"
          className="min-h-11 w-fit"
          data-testid="open-customer-orders"
        >
          <Link to="/orders">{t("orders.openQueue")}</Link>
        </Button>
      ) : null}
      {canSuppliers ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-suppliers">
          <Link to="/suppliers">{t("suppliers.open")}</Link>
        </Button>
      ) : null}
      {canPurchasing ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-purchasing">
          <Link to="/purchasing">{t("purchasing.open")}</Link>
        </Button>
      ) : null}
      {canReturns ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-returns">
          <Link to="/returns">{t("returns.open")}</Link>
        </Button>
      ) : null}
      {canDashboard ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-dashboard">
          <Link to="/dashboard">{t("dashboard.open")}</Link>
        </Button>
      ) : null}
      {canReports ? (
        <Button asChild variant="ghost" className="min-h-11 w-fit" data-testid="open-reports">
          <Link to="/reports">{t("reports.open")}</Link>
        </Button>
      ) : null}
    </div>
  );
}

export function OwnerRoleHomePage() {
  return (
    <RoleHomeShell
      titleKey="role.ownerTitle"
      ledeKey="role.ownerLede"
      badgeKey="role.ownerBadge"
      bodyKey="role.ownerBody"
      returnRoute="/role/owner"
      showExperienceChooser
    />
  );
}

export function ManagerRoleHomePage() {
  return (
    <RoleHomeShell
      titleKey="role.managerTitle"
      ledeKey="role.managerLede"
      badgeKey="role.managerBadge"
      bodyKey="role.managerBody"
      returnRoute="/role/manager"
      dashboardGuide
      homeTestId="manager-home"
    />
  );
}

export function CashierRoleHomePage() {
  return (
    <RoleHomeShell
      titleKey="role.cashierTitle"
      ledeKey="role.cashierLede"
      badgeKey="role.cashierBadge"
      bodyKey="role.cashierBody"
      returnRoute="/role/cashier"
      primarySell
      dashboardGuide
      homeTestId="cashier-home"
    />
  );
}
