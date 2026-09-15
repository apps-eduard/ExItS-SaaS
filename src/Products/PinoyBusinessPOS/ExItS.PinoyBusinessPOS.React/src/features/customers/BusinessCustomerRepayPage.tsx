import { Navigate, useParams } from "react-router-dom";
import { LoadingState } from "@/components/exits/LoadingState";
import { useI18n } from "@/i18n/I18nProvider";

/** Legacy full-page business repay route — opens shared RecordPaymentModal on detail. */
export function BusinessCustomerRepayPage() {
  const { t } = useI18n();
  const { connectionId } = useParams<{ connectionId: string }>();
  if (!connectionId) {
    return <LoadingState label={t("session.loading")} />;
  }
  return <Navigate to={`/customers/business/${connectionId}?recordPayment=1`} replace />;
}
