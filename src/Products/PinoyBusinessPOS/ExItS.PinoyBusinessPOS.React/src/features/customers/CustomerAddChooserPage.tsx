import { Link, useSearchParams, Navigate } from "react-router-dom";
import { Building2, ChevronRight, UserRound } from "lucide-react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * First step of Add customer: Person (individual) vs Business (org / local company).
 */
export function CustomerAddChooserPage() {
  const { t } = useI18n();
  const [searchParams] = useSearchParams();
  const linkPublicId = searchParams.get("linkPublicId")?.trim();
  const returnTo = searchParams.get("returnTo");

  if (linkPublicId) {
    const params = new URLSearchParams();
    params.set("linkPublicId", linkPublicId);
    if (returnTo) params.set("returnTo", returnTo);
    return <Navigate to={`/customers/new/person?${params.toString()}`} replace />;
  }

  return (
    <div className="supplier-add-chooser flex min-w-0 flex-col gap-3" data-testid="customer-add-chooser">
      <PageHeader
        title={t("customers.add")}
        description={t("customers.addChooserLede")}
        backTo={pageBackNav.customers.to}
        backLabel={t(pageBackNav.customers.labelKey)}
        backTestId="page-header-back-customers"
      />

      <ExitsChipBar
        variant="steps"
        ariaLabel={t("customers.addStepsAria")}
        testId="customer-add-steps"
        items={[
          { key: "choose", label: t("customers.addStepChoose"), state: "active" },
          { key: "complete", label: t("customers.addStepComplete"), state: "idle" },
        ]}
      />

      <div className="supplier-add-chooser__options">
        <Link
          to="/customers/new/person"
          className="supplier-add-chooser__card supplier-add-chooser__card--primary"
          data-testid="customer-add-person"
        >
          <span className="supplier-add-chooser__icon" aria-hidden>
            <UserRound className="size-5" />
          </span>
          <span className="supplier-add-chooser__copy">
            <span className="supplier-add-chooser__title">{t("customers.addPerson")}</span>
            <span className="supplier-add-chooser__detail">{t("customers.addPersonDetail")}</span>
          </span>
          <ChevronRight className="supplier-add-chooser__chevron size-4 shrink-0" aria-hidden />
        </Link>

        <Link
          to="/customers/new/business"
          className="supplier-add-chooser__card"
          data-testid="customer-add-business"
        >
          <span className="supplier-add-chooser__icon" aria-hidden>
            <Building2 className="size-5" />
          </span>
          <span className="supplier-add-chooser__copy">
            <span className="supplier-add-chooser__title">{t("customers.addBusiness")}</span>
            <span className="supplier-add-chooser__detail">{t("customers.addBusinessDetail")}</span>
          </span>
          <ChevronRight className="supplier-add-chooser__chevron size-4 shrink-0" aria-hidden />
        </Link>
      </div>
    </div>
  );
}
