import { Link } from "react-router-dom";
import { Building2, ChevronRight, Keyboard, QrCode } from "lucide-react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Business customer path: local (not on ExItS) vs existing ExItS Organization.
 */
export function CustomerBusinessAddChooserPage() {
  const { t } = useI18n();

  return (
    <div
      className="supplier-add-chooser flex min-w-0 flex-col gap-3"
      data-testid="customer-business-add-chooser"
    >
      <PageHeader
        title={t("customers.addBusiness")}
        description={t("customers.addBusinessChooserLede")}
        backTo="/customers/new"
        backLabel={t("customers.add")}
        backTestId="page-header-back-customer-add"
      />

      <ExitsChipBar
        variant="steps"
        ariaLabel={t("customers.addStepsAria")}
        testId="customer-business-add-steps"
        items={[
          { key: "choose", label: t("customers.addStepChoose"), state: "done" },
          { key: "business", label: t("customers.addStepBusiness"), state: "active" },
          { key: "complete", label: t("customers.addStepComplete"), state: "idle" },
        ]}
      />

      <div className="supplier-add-chooser__options">
        <Link
          to="/customers/new/business/local"
          className="supplier-add-chooser__card"
          data-testid="customer-add-business-local"
        >
          <span className="supplier-add-chooser__icon" aria-hidden>
            <Keyboard className="size-5" />
          </span>
          <span className="supplier-add-chooser__copy">
            <span className="supplier-add-chooser__title">{t("customers.addBusinessLocal")}</span>
            <span className="supplier-add-chooser__detail">
              {t("customers.addBusinessLocalDetail")}
            </span>
          </span>
          <ChevronRight className="supplier-add-chooser__chevron size-4 shrink-0" aria-hidden />
        </Link>

        <Link
          to="/customers/new/business/organization"
          className="supplier-add-chooser__card supplier-add-chooser__card--primary"
          data-testid="customer-add-business-organization"
        >
          <span className="supplier-add-chooser__icon" aria-hidden>
            <QrCode className="size-5" />
          </span>
          <span className="supplier-add-chooser__copy">
            <span className="supplier-add-chooser__title">
              {t("customers.addBusinessOrganization")}
            </span>
            <span className="supplier-add-chooser__detail">
              {t("customers.addBusinessOrganizationDetail")}
            </span>
          </span>
          <ChevronRight className="supplier-add-chooser__chevron size-4 shrink-0" aria-hidden />
        </Link>
      </div>

      <p className="supplier-add-chooser__hint m-0 flex items-start gap-2 text-[length:var(--exits-text-xs)] text-muted">
        <Building2 className="mt-0.5 size-3.5 shrink-0" aria-hidden />
        <span>{t("customers.addBusinessChooserHint")}</span>
      </p>
    </div>
  );
}
