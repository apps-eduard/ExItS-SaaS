import { Link } from "react-router-dom";
import { Building2, ChevronRight, Keyboard, QrCode } from "lucide-react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Choose how to add a supplier: ExItS Business QR / org ID, or local manual details.
 */
export function SupplierAddChooserPage() {
  const { t } = useI18n();

  return (
    <div className="supplier-add-chooser flex min-w-0 flex-col gap-3" data-testid="supplier-add-chooser">
      <PageHeader
        title={t("suppliers.add")}
        description={t("suppliers.addChooserLede")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t(pageBackNav.suppliers.labelKey)}
        backTestId="page-header-back-suppliers"
      />

      <ExitsChipBar
        variant="steps"
        ariaLabel={t("suppliers.addStepsAria")}
        testId="supplier-add-steps"
        items={[
          { key: "choose", label: t("suppliers.addStepChoose"), state: "active" },
          { key: "complete", label: t("suppliers.addStepComplete"), state: "idle" },
        ]}
      />

      <div className="supplier-add-chooser__options">
        <Link
          to="/suppliers/connected/request"
          className="supplier-add-chooser__card supplier-add-chooser__card--primary"
          data-testid="supplier-add-scan"
        >
          <span className="supplier-add-chooser__icon" aria-hidden>
            <QrCode className="size-5" />
          </span>
          <span className="supplier-add-chooser__copy">
            <span className="supplier-add-chooser__title">{t("suppliers.addViaQr")}</span>
            <span className="supplier-add-chooser__detail">{t("suppliers.addViaQrDetail")}</span>
          </span>
          <ChevronRight className="supplier-add-chooser__chevron size-4 shrink-0" aria-hidden />
        </Link>

        <Link
          to="/suppliers/new/manual"
          className="supplier-add-chooser__card"
          data-testid="supplier-add-manual"
        >
          <span className="supplier-add-chooser__icon" aria-hidden>
            <Keyboard className="size-5" />
          </span>
          <span className="supplier-add-chooser__copy">
            <span className="supplier-add-chooser__title">{t("suppliers.addManual")}</span>
            <span className="supplier-add-chooser__detail">{t("suppliers.addManualDetail")}</span>
          </span>
          <ChevronRight className="supplier-add-chooser__chevron size-4 shrink-0" aria-hidden />
        </Link>
      </div>

      <p className="supplier-add-chooser__hint m-0 flex items-start gap-2 text-[length:var(--exits-text-xs)] text-muted">
        <Building2 className="mt-0.5 size-3.5 shrink-0" aria-hidden />
        <span>{t("suppliers.addChooserHint")}</span>
      </p>
    </div>
  );
}
