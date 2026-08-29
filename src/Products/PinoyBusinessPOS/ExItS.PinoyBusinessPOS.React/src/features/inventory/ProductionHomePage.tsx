import { Factory, History, Settings2 } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { Card } from "@/components/ui/card";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ProductionHomePage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  return (
    <div
      className="production-home-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="production-home-page"
    >
      <PageHeader
        title={t("production.title")}
        description={t("production.lede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      {!online ? (
        <Card>
          <p className="m-0">{t("production.offline")}</p>
        </Card>
      ) : null}

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("production.title")}
        testId="production-home-actions"
        className="exits-animate-toolbar"
        items={[
          {
            key: "produce",
            label: t("production.homeProduce"),
            icon: <Factory />,
            href: online && allowManage ? "/inventory/production/produce" : undefined,
            disabled: !online || !allowManage,
            testId: "production-open-produce",
            emphasis: "primary",
          },
          {
            key: "setups",
            label: t("production.homeSetups"),
            icon: <Settings2 />,
            href: online ? "/inventory/production/setups" : undefined,
            disabled: !online,
            testId: "production-open-setups",
          },
          {
            key: "history",
            label: t("production.homeHistory"),
            icon: <History />,
            href: online ? "/inventory/production/runs" : undefined,
            disabled: !online,
            testId: "production-open-runs",
          },
        ]}
      />

      {!allowManage ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("production.manageDenied")}
        </p>
      ) : null}
    </div>
  );
}
