import { Link } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { paymentsListHref } from "@/api/payments/payment-client";
import { usePreferences } from "@/hooks/use-preferences";

export function PaymentNotFoundPage() {
  const { t } = usePreferences();
  return (
    <section className="grid max-w-xl gap-3">
      <PageHeader
        title={t("payments.detail.notFound.title")}
        description={t("payments.detail.notFound.body")}
      />
      <Button type="button" variant="outline" size="sm" asChild>
        <Link to={paymentsListHref()}>{t("payments.detail.notFound.back")}</Link>
      </Button>
    </section>
  );
}
