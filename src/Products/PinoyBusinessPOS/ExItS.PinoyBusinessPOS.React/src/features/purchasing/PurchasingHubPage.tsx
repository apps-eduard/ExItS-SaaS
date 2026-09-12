import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  ClipboardList,
  FilePlus,
  Inbox,
  PackageCheck,
  PackagePlus,
  ShoppingCart,
  Store,
  Truck,
  Users,
  type LucideIcon,
} from "lucide-react";
import {
  canManageInventory,
  canManagePurchasing,
  canViewInventory,
  canViewPurchasing,
  canViewSuppliers,
} from "@/access/pos-capabilities";
import { listIncomingOrders } from "@/api/pos/pos-connected-suppliers-client";
import { listDirectPurchases } from "@/api/pos/pos-direct-purchases-client";
import {
  isPurchaseOrderReceivable,
  listPurchaseOrders,
} from "@/api/pos/pos-purchase-orders-client";
import { listSuppliers } from "@/api/pos/pos-suppliers-client";
import { ExitsChipBar, type ExitsChipItem } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { Card } from "@/components/ui/card";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  purchasingHubDirectPurchasesQueryKey,
  purchasingHubIncomingPendingQueryKey,
  purchasingHubPurchaseOrdersQueryKey,
  purchasingHubSuppliersQueryKey,
} from "@/features/purchasing/purchasing-nav-activity";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type BrowseKey = "orders" | "incoming" | "receipts" | "direct" | "suppliers";

type BrowseDef = {
  key: BrowseKey;
  label: string;
  icon: LucideIcon;
  href: string;
  testId: string;
  count: number;
};

function toChipItems(defs: BrowseDef[]): ExitsChipItem[] {
  return defs.map((item) => {
    const Icon = item.icon;
    return {
      key: item.key,
      href: item.href,
      testId: item.testId,
      icon: <Icon />,
      label: item.label,
      count: item.count,
    };
  });
}

export function PurchasingHubPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowViewPurchasing = canViewPurchasing(sessionGrant);
  const allowManagePurchasing = canManagePurchasing(sessionGrant);
  const allowManageInventory = canManageInventory(sessionGrant);
  const allowViewInventory = canViewInventory(sessionGrant);
  const allowSuppliers = canViewSuppliers(sessionGrant);
  const allowDirect = allowViewInventory || allowManageInventory;

  const purchaseOrdersQuery = useQuery({
    queryKey: purchasingHubPurchaseOrdersQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled: Boolean(workspace) && online && allowViewPurchasing,
    staleTime: 30_000,
    queryFn: ({ signal }) => listPurchaseOrders(workspace!, { page: 1, pageSize: 40 }, signal),
  });

  const incomingPendingQuery = useQuery({
    queryKey: purchasingHubIncomingPendingQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled: Boolean(workspace) && online && allowViewPurchasing,
    staleTime: 30_000,
    queryFn: ({ signal }) => listIncomingOrders(workspace!, { status: "New" }, signal),
  });

  const directPurchasesQuery = useQuery({
    queryKey: purchasingHubDirectPurchasesQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled: Boolean(workspace) && online && allowDirect,
    staleTime: 30_000,
    queryFn: ({ signal }) => listDirectPurchases(workspace!, { page: 1, pageSize: 1 }, signal),
  });

  const suppliersQuery = useQuery({
    queryKey: purchasingHubSuppliersQueryKey(workspace?.organizationId, workspace?.branchId),
    enabled: Boolean(workspace) && online && allowSuppliers,
    staleTime: 30_000,
    queryFn: ({ signal }) => listSuppliers(workspace!, { page: 1, pageSize: 1 }, signal),
  });

  const ordersTotal = purchaseOrdersQuery.data?.totalCount ?? 0;
  const receivableCount = (purchaseOrdersQuery.data?.items ?? []).filter((po) =>
    isPurchaseOrderReceivable(po),
  ).length;
  const incomingPendingCount = incomingPendingQuery.data?.length ?? 0;
  const directTotal = directPurchasesQuery.data?.totalCount ?? 0;
  const suppliersTotal = suppliersQuery.data?.totalCount ?? 0;

  const buyingPrimaryItems = useMemo(() => {
    const items: ExitsChipItem[] = [];
    if (allowManageInventory) {
      items.push({
        key: "receive",
        label: t("purchasing.receiveStock"),
        icon: <PackagePlus />,
        href: "/purchasing/receive-stock",
        testId: "purchasing-receive-stock",
        emphasis: "primary",
      });
    }
    if (allowManagePurchasing) {
      items.push({
        key: "new",
        label: t("purchasing.newOrder"),
        icon: <FilePlus />,
        href: "/purchasing/new",
        testId: "purchasing-new",
        emphasis: "primary",
      });
    }
    return items;
  }, [allowManageInventory, allowManagePurchasing, t]);

  const buyingManageDefs = useMemo(() => {
    const items: BrowseDef[] = [];
    if (allowViewPurchasing) {
      items.push({
        key: "orders",
        label: t("purchasing.orders"),
        icon: ClipboardList,
        href: "/purchasing/orders",
        testId: "purchasing-orders",
        count: ordersTotal,
      });
      items.push({
        key: "receipts",
        label: t("purchasing.receipts"),
        icon: Truck,
        href: "/purchasing/receipts",
        testId: "purchasing-receipts",
        count: receivableCount,
      });
    }
    if (allowDirect) {
      items.push({
        key: "direct",
        label: t("purchasing.directPurchases"),
        icon: PackageCheck,
        href: "/purchasing/direct-purchases",
        testId: "purchasing-direct",
        count: directTotal,
      });
    }
    if (allowSuppliers) {
      items.push({
        key: "suppliers",
        label: t("purchasing.suppliers"),
        icon: Users,
        href: "/suppliers",
        testId: "purchasing-suppliers",
        count: suppliersTotal,
      });
    }
    return items;
  }, [
    allowDirect,
    allowSuppliers,
    allowViewPurchasing,
    directTotal,
    ordersTotal,
    receivableCount,
    suppliersTotal,
    t,
  ]);

  const sellingDefs = useMemo(() => {
    const items: BrowseDef[] = [];
    if (allowViewPurchasing) {
      items.push({
        key: "incoming",
        label: t("incomingOrders.title"),
        icon: Inbox,
        href: "/purchasing/incoming-orders",
        testId: "purchasing-incoming-orders",
        count: incomingPendingCount,
      });
    }
    return items;
  }, [allowViewPurchasing, incomingPendingCount, t]);

  const buyingManageItems = toChipItems(buyingManageDefs);
  const sellingItems = toChipItems(sellingDefs);
  const showBuying = buyingPrimaryItems.length > 0 || buyingManageItems.length > 0;
  const showSelling = sellingItems.length > 0;

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

      {showBuying || showSelling ? (
        <div className="purchasing-hub-directions" data-testid="purchasing-directions">
          {showBuying ? (
            <Card
              as="section"
              padding="none"
              className="purchasing-hub-direction"
              data-testid="purchasing-buying"
              aria-labelledby="purchasing-buying-title"
            >
              <div className="purchasing-hub-direction__header">
                <span className="purchasing-hub-direction__icon" aria-hidden>
                  <ShoppingCart />
                </span>
                <div className="purchasing-hub-direction__copy min-w-0">
                  <h2 id="purchasing-buying-title" className="purchasing-hub-direction__title">
                    {t("purchasing.buyingTitle")}
                  </h2>
                  <p className="purchasing-hub-direction__lede m-0">{t("purchasing.buyingLede")}</p>
                </div>
              </div>

              {buyingPrimaryItems.length > 0 ? (
                <div className="purchasing-hub-direction__group">
                  <p className="purchasing-hub-direction__group-label m-0">
                    {t("purchasing.buyingPrimary")}
                  </p>
                  <ExitsChipBar
                    variant="actions"
                    ariaLabel={t("purchasing.buyingPrimary")}
                    testId="purchasing-buying-primary"
                    className="purchasing-hub-direction__actions purchasing-hub-direction__actions--primary exits-animate-toolbar"
                    items={buyingPrimaryItems}
                  />
                </div>
              ) : null}

              {buyingManageItems.length > 0 ? (
                <div className="purchasing-hub-direction__group">
                  <p className="purchasing-hub-direction__group-label m-0">
                    {t("purchasing.buyingManage")}
                  </p>
                  <ExitsChipBar
                    variant="actions"
                    ariaLabel={t("purchasing.buyingManage")}
                    testId="purchasing-buying-actions"
                    className="purchasing-hub-direction__actions exits-animate-toolbar"
                    items={buyingManageItems}
                  />
                </div>
              ) : null}
            </Card>
          ) : null}

          {showSelling ? (
            <Card
              as="section"
              padding="none"
              className="purchasing-hub-direction"
              data-testid="purchasing-selling"
              aria-labelledby="purchasing-selling-title"
            >
              <div className="purchasing-hub-direction__header">
                <span className="purchasing-hub-direction__icon" aria-hidden>
                  <Store />
                </span>
                <div className="purchasing-hub-direction__copy min-w-0">
                  <h2 id="purchasing-selling-title" className="purchasing-hub-direction__title">
                    {t("purchasing.sellingTitle")}
                  </h2>
                  <p className="purchasing-hub-direction__lede m-0">{t("purchasing.sellingLede")}</p>
                </div>
              </div>
              <ExitsChipBar
                variant="actions"
                ariaLabel={t("purchasing.sellingTitle")}
                testId="purchasing-selling-actions"
                className="purchasing-hub-direction__actions exits-animate-toolbar"
                items={sellingItems}
              />
            </Card>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
