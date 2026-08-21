import {
  canCreateSale,
  canManageCatalog,
  canManageShifts,
  canViewCustomers,
  canViewCustomerOrders,
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
};

export function RoleHomeShell({
  titleKey,
  ledeKey,
  badgeKey,
  bodyKey,
  returnRoute,
  primarySell = false,
  showExperienceChooser = false,
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
  const canDevices = hasOrganizationManagementAuthority(sessionGrant);
  const securityRole = resolveEffectivePosRoleCode(sessionGrant);

  function startSelling() {
    enter(returnRoute);
    navigate("/sell");
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
        <Link to="/devices/register">{t("devices.redeemTitle")}</Link>
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
    />
  );
}
