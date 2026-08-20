import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useSellingMode } from "@/selling/SellingModeProvider";

type RoleHomeShellProps = {
  titleKey: "role.ownerTitle" | "role.managerTitle" | "role.cashierTitle";
  ledeKey: "role.ownerLede" | "role.managerLede" | "role.cashierLede";
  badgeKey: "role.ownerBadge" | "role.managerBadge" | "role.cashierBadge";
  bodyKey: "role.ownerBody" | "role.managerBody" | "role.cashierBody";
  returnRoute: string;
  primarySell?: boolean;
};

export function RoleHomeShell({
  titleKey,
  ledeKey,
  badgeKey,
  bodyKey,
  returnRoute,
  primarySell = false,
}: RoleHomeShellProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { enter } = useSellingMode();

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
      </Card>
      <div className="flex flex-wrap gap-2">
        <Button type="button" variant={primarySell ? "default" : "ghost"} onClick={startSelling}>
          {primarySell ? t("role.openSellFloor") : t("role.startSelling")}
        </Button>
        {primarySell ? (
          <Button type="button" variant="ghost" onClick={() => navigate("/workspace")}>
            {t("workspace.switch")}
          </Button>
        ) : null}
      </div>
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
