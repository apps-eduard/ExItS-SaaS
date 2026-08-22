import { Link } from "react-router-dom";
import {
  canManageInventory,
  canManagePurchasing,
  canViewInventory,
  canViewPurchasing,
  canViewSuppliers,
} from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function PurchasingHubPage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();

  const allowViewPurchasing = canViewPurchasing(sessionGrant);
  const allowManagePurchasing = canManagePurchasing(sessionGrant);
  const allowManageInventory = canManageInventory(sessionGrant);
  const allowViewInventory = canViewInventory(sessionGrant);
  const allowSuppliers = canViewSuppliers(sessionGrant);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="purchasing-hub-page">
      <PageHeader
        title={t("purchasing.title")}
        description={t("purchasing.hubLede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      <Card>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("purchasing.choiceReceive")}
        </p>
        <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("purchasing.choiceOrder")}
        </p>
      </Card>
      <div className="flex flex-wrap gap-2">
        {allowManageInventory ? (
          <Button asChild className="min-h-11" data-testid="purchasing-receive-stock">
            <Link to="/purchasing/receive-stock">{t("purchasing.receiveStock")}</Link>
          </Button>
        ) : null}
        {allowViewPurchasing ? (
          <>
            <Button asChild variant="ghost" className="min-h-11" data-testid="purchasing-orders">
              <Link to="/purchasing/orders">{t("purchasing.orders")}</Link>
            </Button>
            <Button asChild variant="ghost" className="min-h-11" data-testid="purchasing-receipts">
              <Link to="/purchasing/receipts">{t("purchasing.receipts")}</Link>
            </Button>
          </>
        ) : null}
        {allowViewInventory || allowManageInventory ? (
          <Button asChild variant="ghost" className="min-h-11" data-testid="purchasing-direct">
            <Link to="/purchasing/direct-purchases">{t("purchasing.directPurchases")}</Link>
          </Button>
        ) : null}
        {allowSuppliers ? (
          <Button asChild variant="ghost" className="min-h-11" data-testid="purchasing-suppliers">
            <Link to="/suppliers">{t("purchasing.suppliers")}</Link>
          </Button>
        ) : null}
        {allowManagePurchasing ? (
          <Button asChild variant="ghost" className="min-h-11" data-testid="purchasing-new">
            <Link to="/purchasing/new">{t("purchasing.newOrder")}</Link>
          </Button>
        ) : null}
      </div>
    </div>
  );
}
