import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { Wallet } from "lucide-react";
import { useParams } from "react-router-dom";

export function BusinessCustomerRepayPage() {
  const { t } = useI18n();
  const { connectionId } = useParams<{ connectionId: string }>();
  const backTo = connectionId ? `/customers/business/${connectionId}` : "/customers?kind=businesses";

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="business-customer-repay-page">
      <PageHeader
        title={t("customers.repayTitle")}
        description={t("customers.business.repayLede")}
        backTo={backTo}
        backLabel={t("customers.backDetail")}
        backTestId="page-header-back-business-customer"
      />
      <EmptyState
        align="center"
        icon={<Wallet className="size-5" strokeWidth={1.75} />}
        title={t("customers.paymentsEmpty")}
        detail={t("customers.business.repayUnavailableDetail")}
      />
    </div>
  );
}
