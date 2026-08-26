import { Link, useLocation } from "react-router-dom";
import { plansListHref } from "@/api/catalog/plan-list-query";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export const PLANS_LIST_STATE_KEY = "plansListSearch";

export type PlansLocationState = {
  [PLANS_LIST_STATE_KEY]?: string;
};

export function PlanNotFoundPage() {
  const { t } = usePreferences();
  const location = useLocation();
  const state = (location.state as PlansLocationState | null) ?? null;
  const backHref = plansListHref(state?.[PLANS_LIST_STATE_KEY]);

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("plans.detail.notFound.title")}
        description={t("plans.detail.notFound.body")}
      />
      <p>
        <Link className="text-primary hover:underline" to={backHref}>
          {t("plans.detail.notFound.back")}
        </Link>
      </p>
    </section>
  );
}
