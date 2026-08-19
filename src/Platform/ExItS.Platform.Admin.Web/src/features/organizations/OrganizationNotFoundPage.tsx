import { Link, useLocation } from "react-router-dom";
import {
  ORGANIZATIONS_LIST_STATE_KEY,
  organizationsListHref,
  type OrganizationsLocationState,
} from "@/api/organizations/organization-id";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export function OrganizationNotFoundPage() {
  const { t } = usePreferences();
  const location = useLocation();
  const state = (location.state as OrganizationsLocationState | null) ?? null;
  const backHref = organizationsListHref(state?.[ORGANIZATIONS_LIST_STATE_KEY]);

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.notFound.title")}
        description={t("organization.notFound.body")}
      />
      <p>
        <Link className="text-primary hover:underline" to={backHref}>
          {t("organization.notFound.back")}
        </Link>
      </p>
    </section>
  );
}
