import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { FileText } from "lucide-react";
import { useParams } from "react-router-dom";

export function BusinessCustomerStatementPage() {
  const { t } = useI18n();
  const { connectionId } = useParams<{ connectionId: string }>();
  const backTo = connectionId ? `/customers/business/${connectionId}` : "/customers?kind=businesses";

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="business-customer-statement-page">
      <PageHeader
        title={t("customers.statementTitle")}
        description={t("customers.business.statementLede")}
        backTo={backTo}
        backLabel={t("customers.backDetail")}
        backTestId="page-header-back-business-customer"
      />
      <EmptyState
        align="center"
        icon={<FileText className="size-5" strokeWidth={1.75} />}
        title={t("customers.statementEmpty")}
        detail={t("customers.business.statementUnavailableDetail")}
      />
    </div>
  );
}
