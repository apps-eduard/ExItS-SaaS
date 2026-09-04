import { Link, useParams } from "react-router-dom";
import { CloudUpload } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Honest offline outcome (RMAP-21D): the sale is saved on this device and waiting to sync.
 * It is deliberately not a Transaction Summary — no sale number, no receipt, no server totals,
 * because nothing has been recorded on the server yet.
 */
export function OfflineSaleQueuedPage() {
  const { t } = useI18n();
  const { saleId } = useParams<{ saleId: string }>();

  return (
    <div data-testid="offline-sale-queued" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("offline.queuedTitle")} description={t("offline.queuedDetail")} />
      <Card className="flex items-start gap-3">
        <CloudUpload className="mt-0.5 size-6 shrink-0 text-primary" aria-hidden />
        <div className="min-w-0">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{t("offline.queuedPending")}</p>
          {saleId ? (
            <p
              data-testid="offline-sale-queued-reference"
              className="mb-0 mt-1 break-all text-[length:var(--exits-text-xs)] text-muted"
            >
              {t("offline.queuedReference").replace("{reference}", saleId)}
            </p>
          ) : null}
        </div>
      </Card>
      <div className="flex flex-wrap gap-2">
        <Button asChild data-testid="offline-sale-queued-new-sale">
          <Link to="/sell">{t("offline.queuedNewSale")}</Link>
        </Button>
      </div>
    </div>
  );
}
