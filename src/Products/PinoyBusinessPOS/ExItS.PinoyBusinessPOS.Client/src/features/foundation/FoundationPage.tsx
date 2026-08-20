import { Card } from "@/components/ui/card";
import { ConnectivityIndicator } from "@/components/exits/ConnectivityIndicator";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";

export function FoundationPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("foundation.title")} description={t("foundation.lede")} />
      <StatusChip tone="info">{t("status.foundation")}</StatusChip>
      <Card>
        <p className="m-0 text-[length:var(--exits-text-md)]">{t("foundation.next")}</p>
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("foundation.scope")}
        </p>
      </Card>
      <EmptyState title={t("empty.title")} detail={t("empty.detail")} />
      <ConnectivityIndicator
        online
        onlineLabel={t("connectivity.online")}
        offlineTitle={t("connectivity.offlineTitle")}
        offlineDetail={t("connectivity.offlineDetail")}
      />
    </div>
  );
}
