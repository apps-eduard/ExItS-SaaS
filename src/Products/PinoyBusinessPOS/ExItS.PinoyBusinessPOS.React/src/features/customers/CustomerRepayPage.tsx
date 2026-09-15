import { Navigate, useParams } from "react-router-dom";
import { LoadingState } from "@/components/exits/LoadingState";
import { useI18n } from "@/i18n/I18nProvider";

/** Legacy full-page repay route — opens shared RecordPaymentModal on customer detail. */
export function CustomerRepayPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  if (!customerId) {
    return <LoadingState label={t("session.loading")} />;
  }
  return <Navigate to={`/customers/${customerId}?recordPayment=1`} replace />;
}
