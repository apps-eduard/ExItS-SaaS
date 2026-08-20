import { Link, useLocation } from "react-router-dom";
import { USERS_LIST_STATE_KEY, usersListHref, type UsersLocationState } from "@/api/users/user-id";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export function UserNotFoundPage() {
  const { t } = usePreferences();
  const location = useLocation();
  const state = (location.state as UsersLocationState | null) ?? null;
  const backHref = usersListHref(state?.[USERS_LIST_STATE_KEY]);

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader title={t("users.detail.notFound.title")} description={t("users.detail.notFound.body")} />
      <p>
        <Link className="text-primary hover:underline" to={backHref}>
          {t("users.detail.notFound.back")}
        </Link>
      </p>
    </section>
  );
}
