import { Link } from "react-router-dom";
import {
  ClipboardList,
  FilePlus,
  Inbox,
  PackageCheck,
  PackagePlus,
  Truck,
  Users,
} from "lucide-react";
import {
  canManageInventory,
  canManagePurchasing,
  canViewInventory,
  canViewPurchasing,
  canViewSuppliers,
} from "@/access/pos-capabilities";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
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

  const browseItems = [
    allowViewPurchasing
      ? {
          key: "orders",
          label: t("purchasing.orders"),
          icon: <ClipboardList />,
          href: "/purchasing/orders",
          testId: "purchasing-orders",
        }
      : null,
    allowViewPurchasing
      ? {
          key: "incoming",
          label: t("incomingOrders.title"),
          icon: <Inbox />,
          href: "/purchasing/incoming-orders",
          testId: "purchasing-incoming-orders",
        }
      : null,
    allowViewPurchasing
      ? {
          key: "receipts",
          label: t("purchasing.receipts"),
          icon: <Truck />,
          href: "/purchasing/receipts",
          testId: "purchasing-receipts",
        }
      : null,
    allowViewInventory || allowManageInventory
      ? {
          key: "direct",
          label: t("purchasing.directPurchases"),
          icon: <PackageCheck />,
          href: "/purchasing/direct-purchases",
          testId: "purchasing-direct",
        }
      : null,
    allowSuppliers
      ? {
          key: "suppliers",
          label: t("purchasing.suppliers"),
          icon: <Users />,
          href: "/suppliers",
          testId: "purchasing-suppliers",
        }
      : null,
  ].filter((item): item is NonNullable<typeof item> => item != null);

  return (
    <div
      className="purchasing-hub-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="purchasing-hub-page"
    >
      <PageHeader
        title={t("purchasing.title")}
        description={t("purchasing.hubLede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-purchasing"
      />

      <div className="purchasing-hub-choices">
        {allowManageInventory ? (
          <Link
            className="exits-list__card purchasing-hub-choice text-foreground no-underline"
            to="/purchasing/receive-stock"
            data-testid="purchasing-receive-stock"
          >
            <span className="purchasing-hub-choice__icon" aria-hidden>
              <PackagePlus />
            </span>
            <span className="purchasing-hub-choice__copy min-w-0">
              <span className="purchasing-hub-choice__title">{t("purchasing.receiveStock")}</span>
              <span className="purchasing-hub-choice__lede">{t("purchasing.choiceReceive")}</span>
            </span>
          </Link>
        ) : null}
        {allowManagePurchasing ? (
          <Link
            className="exits-list__card purchasing-hub-choice text-foreground no-underline"
            to="/purchasing/new"
            data-testid="purchasing-new"
          >
            <span className="purchasing-hub-choice__icon" aria-hidden>
              <FilePlus />
            </span>
            <span className="purchasing-hub-choice__copy min-w-0">
              <span className="purchasing-hub-choice__title">{t("purchasing.newOrder")}</span>
              <span className="purchasing-hub-choice__lede">{t("purchasing.choiceOrder")}</span>
            </span>
          </Link>
        ) : null}
      </div>

      {browseItems.length > 0 ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("purchasing.title")}
          testId="purchasing-toolbar"
          className="exits-animate-toolbar"
          items={browseItems}
        />
      ) : null}
    </div>
  );
}
