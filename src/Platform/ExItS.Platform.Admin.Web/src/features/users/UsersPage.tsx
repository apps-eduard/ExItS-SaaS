import { useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  isUnrecognizedDirectoryParam,
  parseUserListSearchParams,
} from "@/api/users/user-list-query";
import { PageHeader } from "@/components/exits/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
import { UsersList } from "@/features/users/UsersList";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

const DIRECTORY_TITLES: Record<string, MessageKey> = {
  PlatformStaff: "nav.platformStaff",
  Organization: "nav.orgAccounts",
  Personal: "nav.personalAccounts",
  Unassigned: "nav.needsReview",
};

export function UsersPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const [searchParams] = useSearchParams();
  const urlState = useMemo(() => parseUserListSearchParams(searchParams), [searchParams]);
  const invalidDirectory = isUnrecognizedDirectoryParam(searchParams);
  const canList =
    authorization.status === "loaded" &&
    authorization.hasAnyPermission([PLATFORM_PERMISSIONS.managePlatformUsers]);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!canList) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.managePlatformUsers} />;
  }

  const title = urlState.directory
    ? `${t("nav.allAccounts")} / ${t(DIRECTORY_TITLES[urlState.directory] ?? "nav.allAccounts")}`
    : t("nav.allAccounts");

  return (
    <section className="grid gap-4">
      <PageHeader title={title} description={t("users.description")} />
      {invalidDirectory ? (
        <p
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted"
          role="status"
        >
          {t("users.directory.invalid")}
        </p>
      ) : (
        <UsersList enabled={canList} />
      )}
    </section>
  );
}
