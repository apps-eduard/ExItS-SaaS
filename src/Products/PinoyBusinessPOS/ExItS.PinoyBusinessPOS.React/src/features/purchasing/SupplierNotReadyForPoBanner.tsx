import { useState } from "react";
import { X } from "lucide-react";
import { Notice } from "@/components/exits/Notice";
import { Button } from "@/components/ui/button";
import { buildBuyerPoNotReadyCopy } from "@/features/purchasing/buyer-po-readiness-banner";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Buyer-facing only — blocker categories, never supplier internal missing-setup details.
 * Dismiss hides the banner for this mount only; Create PO stays blocked while not ready.
 */
export function SupplierNotReadyForPoBanner({
  testId = "po-supplier-not-ready-banner",
  blockerCategories,
}: {
  testId?: string;
  blockerCategories?: ReadonlyArray<string> | null;
}) {
  const { t } = useI18n();
  const [dismissed, setDismissed] = useState(false);
  const copy = buildBuyerPoNotReadyCopy(blockerCategories, t);

  if (dismissed) {
    return null;
  }

  const body =
    copy.listPhrase != null
      ? t(copy.bodyKey).replace("{reasons}", copy.listPhrase)
      : t(copy.bodyKey);

  return (
    <div className="relative" data-testid={`${testId}-wrap`}>
      <Notice tone="warning" testId={testId} title={t("purchasing.supplierNotReadyTitle")} className="pe-10">
        <p className="m-0">{body}</p>
        {copy.issueLine ? (
          <p
            className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted"
            data-testid={`${testId}-issues`}
          >
            {copy.issueLine}
          </p>
        ) : null}
      </Notice>
      <Button
        type="button"
        intent="neutral"
        appearance="ghost"
        size="icon"
        className="absolute end-1 top-1 size-8 shrink-0"
        aria-label={t("purchasing.supplierNotReady.dismiss")}
        data-testid={`${testId}-dismiss`}
        onClick={() => setDismissed(true)}
      >
        <X className="size-4" aria-hidden />
      </Button>
    </div>
  );
}
