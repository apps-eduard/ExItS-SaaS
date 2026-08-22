import { Link } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { subscriptionsListHref } from "@/api/subscriptions/subscription-portfolio-query";
import { usePreferences } from "@/hooks/use-preferences";

export function SubscriptionNotFoundPage() {
  const { t } = usePreferences();
  return (
    <section className="grid max-w-xl gap-3">
      <PageHeader
        title={t("subscriptions.detail.notFound.title")}
        description={t("subscriptions.detail.notFound.body")}
      />
      <Button type="button" variant="outline" size="sm" asChild>
        <Link to={subscriptionsListHref()}>{t("subscriptions.detail.notFound.back")}</Link>
      </Button>
    </section>
  );
}
