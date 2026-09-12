import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  ClipboardList,
  FilePlus,
  Inbox,
  PackageCheck,
  PackagePlus,
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
import { ExitsTabs, type ExitsTabCountTone, type ExitsTabItem } from "@/components/exits/ExitsTabs";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useMediaMin } from "@/hooks/useMediaQuery";
import {
  purchasingHubDirectPurchasesQueryKey,
  purchasingHubIncomingPendingQueryKey,
  purchasingHubPurchaseOrdersQueryKey,
  purchasingHubSuppliersQueryKey,
} from "@/features/purchasing/purchasing-nav-activity";
import { cn } from "@/lib/cn";
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
  countTone: ExitsTabCountTone;
};

export function PurchasingHubPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [browseValue, setBrowseValue] = useState("");
  /** Desktop keeps Soft Tabs; mobile uses continuous Pill Bar (solid Primary active + scroll). */
  const wideBrowseBar = useMediaMin(768);

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

  const browseDefs = useMemo(() => {
    const items: BrowseDef[] = [];
    if (allowViewPurchasing) {
      items.push({
        key: "orders",
        label: t("purchasing.orders"),
        icon: ClipboardList,
        href: "/purchasing/orders",
        testId: "purchasing-orders",
        count: ordersTotal,
        countTone: "neutral",
      });
      items.push({
        key: "incoming",
        label: t("incomingOrders.title"),
        icon: Inbox,
        href: "/purchasing/incoming-orders",
        testId: "purchasing-incoming-orders",
        count: incomingPendingCount,
        countTone: incomingPendingCount > 0 ? "warning" : "neutral",
      });
      items.push({
        key: "receipts",
        label: t("purchasing.receipts"),
        icon: Truck,
        href: "/purchasing/receipts",
        testId: "purchasing-receipts",
        count: receivableCount,
        countTone: receivableCount > 0 ? "warning" : "neutral",
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
        countTone: "neutral",
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
        countTone: "neutral",
      });
    }
    return items;
  }, [
    allowDirect,
    allowSuppliers,
    allowViewPurchasing,
    directTotal,
    incomingPendingCount,
    ordersTotal,
    receivableCount,
    suppliersTotal,
    t,
  ]);

  const browseItems: ExitsTabItem[] = browseDefs.map((item) => ({
    key: item.key,
    label: item.label,
    icon: item.icon,
    count: item.count,
    countTone: item.countTone,
    testId: item.testId,
    title: item.label,
  }));

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
        <ExitsTabs
          variant={wideBrowseBar ? "soft" : "pillBar"}
          layout="content"
          activeTreatment="solid"
          scrollable
          ariaLabel={t("purchasing.title")}
          testId="purchasing-toolbar"
          listClassName={cn(
            "[&_.exits-tabs__trigger]:text-[length:var(--exits-text-md)]",
            "bg-[var(--exits-surface)] border-[var(--exits-border)]",
            "[&_.exits-tabs__trigger]:text-[var(--exits-text)]",
            "[&_.exits-tabs__trigger]:hover:text-[var(--exits-text)]",
            !wideBrowseBar && "shadow-[var(--exits-shadow-sm)]",
          )}
          value={browseValue}
          onValueChange={(key) => {
            setBrowseValue(key);
            const target = browseDefs.find((item) => item.key === key);
            if (target) navigate(target.href);
          }}
          items={browseItems}
        />
      ) : null}
    </div>
  );
}
