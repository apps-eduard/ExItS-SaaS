import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Loader2, RefreshCw } from "lucide-react";
import {
  listPersonalBlockedBusinesses,
  unblockPersonalBusiness,
} from "@/api/platform/customer-link-requests-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useState } from "react";

export function PersonalBlockedBusinessesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["personal", "blocked-businesses"],
    queryFn: ({ signal }) => listPersonalBlockedBusinesses(signal),
  });

  const unblock = useMutation({
    mutationFn: (organizationId: string) => unblockPersonalBusiness(organizationId),
    onSuccess: async () => {
      setActionError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "blocked-businesses"] });
    },
    onError: (error) =>
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.blockedBusinesses.unblockFailed"),
      ),
  });

  if (query.isPending) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("personal.blockedBusinesses.title")}
          description={t("personal.blockedBusinesses.lede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-blocked-businesses"
        />
        <LoadingSkeleton label={t("loading.label")} />
      </div>
    );
  }

  if (query.isError) {
    return (
      <div
        className="personal-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="personal-blocked-businesses-page"
      >
        <PageHeader
          title={t("personal.blockedBusinesses.title")}
          description={t("personal.blockedBusinesses.lede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-blocked-businesses"
        />
        <ErrorState
          title={t("error.title")}
          detail={t("error.detail")}
        />
        <ActionTileGrid
          tiles={[
            {
              key: "retry",
              label: t("orders.retry"),
              icon: RefreshCw,
              onClick: () => void query.refetch(),
            },
          ]}
        />
      </div>
    );
  }

  const items = query.data;

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-blocked-businesses-page"
    >
      <PageHeader
        title={t("personal.blockedBusinesses.title")}
        description={t("personal.blockedBusinesses.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-blocked-businesses"
      />

      <PersonalCommerceNav active="links" />

      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {actionError}
        </p>
      ) : null}

      {items.length === 0 ? (
        <EmptyState
          title={t("personal.blockedBusinesses.empty")}
          detail={t("personal.blockedBusinesses.lede")}
        />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0">
          {items.map((row) => (
            <li key={row.organizationId}>
              <article
                className="exits-list__card"
                data-testid={`blocked-business-${row.organizationId}`}
              >
                <div className="flex items-start gap-3">
                  <Ban className="mt-1 size-5 shrink-0 text-muted" aria-hidden />
                  <div className="min-w-0 flex-1">
                    <p className="m-0 font-semibold">{row.organizationDisplayName}</p>
                    <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {t("personal.blockedBusinesses.blockedAt").replace(
                        "{date}",
                        new Date(row.blockedAtUtc).toLocaleDateString(),
                      )}
                    </p>
                    <Button
                      type="button"
                      variant="outline"
                      className="mt-3 min-h-11"
                      disabled={unblock.isPending}
                      data-testid={`unblock-business-${row.organizationId}`}
                      onClick={() => unblock.mutate(row.organizationId)}
                    >
                      {unblock.isPending && unblock.variables === row.organizationId ? (
                        <Loader2 className="size-4 animate-spin" aria-hidden />
                      ) : null}
                      {t("personal.blockedBusinesses.unblock")}
                    </Button>
                  </div>
                </div>
              </article>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
