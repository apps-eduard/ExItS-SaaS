import { Notice } from "@/components/exits/Notice";
import { useI18n } from "@/i18n/I18nProvider";

/** Buyer-facing only — never includes supplier internal missing-setup details. */
export function SupplierNotReadyForPoBanner({ testId = "po-supplier-not-ready-banner" }: { testId?: string }) {
  const { t } = useI18n();
  return (
    <Notice tone="warning" testId={testId}>
      <p className="m-0 font-semibold">{t("purchasing.supplierNotReadyTitle")}</p>
      <p className="mb-0 mt-1">{t("purchasing.supplierNotReadyBody")}</p>
    </Notice>
  );
}
