import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptCustomerLinkRequest,
  declineCustomerLinkRequest,
  listPendingCustomerLinkRequests,
} from "@/api/platform/customer-link-requests-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalCustomerLinksPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["personal", "customer-link-requests"],
    queryFn: ({ signal }) => listPendingCustomerLinkRequests(signal),
  });

  const accept = useMutation({
    mutationFn: (id: string) => acceptCustomerLinkRequest(id),
    onSuccess: async () => {
      setActionError(null);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["personal", "customer-link-requests"] }),
        queryClient.invalidateQueries({ queryKey: ["personal", "linked-merchants"] }),
      ]);
    },
    onError: (error) =>
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.acceptFailed"),
      ),
  });

  const decline = useMutation({
    mutationFn: (id: string) => declineCustomerLinkRequest(id),
    onSuccess: async () => {
      setActionError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "customer-link-requests"] });
    },
    onError: (error) =>
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.customerLinks.declineFailed"),
      ),
  });

  if (query.isPending) {
    return <LoadingSkeleton />;
  }

  if (query.isError) {
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="personal-customer-links-page">
        <ErrorState
          title={t("personal.customerLinks.errorTitle")}
          detail={t("personal.customerLinks.errorDetail")}
        />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void query.refetch()}>
          {t("orders.retry")}
        </Button>
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/personal/more">{t("personal.more.back")}</Link>
        </Button>
      </div>
    );
  }

  const busy = accept.isPending || decline.isPending;
  const items = query.data;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-customer-links-page">
      <PageHeader
        title={t("personal.customerLinks.title")}
        description={t("personal.customerLinks.lede")}
      />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/linked-merchants">{t("personal.merchantsTitle")}</Link>
      </Button>
      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {actionError}
        </p>
      ) : null}
      {items.length === 0 ? (
        <EmptyState
          title={t("personal.customerLinks.emptyTitle")}
          detail={t("personal.customerLinks.emptyDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {items.map((request) => (
            <li
              key={request.id}
              className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
              data-testid={`customer-link-request-${request.id}`}
            >
              <p className="m-0 font-semibold">{request.organizationDisplayName}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.customerLinks.statusPending")}
                {" · "}
                {t("personal.customerLinks.requestedAt")}:{" "}
                {new Date(request.createdAtUtc).toLocaleString()}
                {" · "}
                {t("personal.customerLinks.expiresAt")}:{" "}
                {new Date(request.expiresAtUtc).toLocaleString()}
              </p>
              <div className="mt-2 flex flex-wrap gap-2">
                <Button
                  type="button"
                  className="min-h-11"
                  disabled={busy}
                  data-testid={`customer-link-accept-${request.id}`}
                  onClick={() => accept.mutate(request.id)}
                >
                  {t("personal.customerLinks.accept")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  disabled={busy}
                  data-testid={`customer-link-decline-${request.id}`}
                  onClick={() => decline.mutate(request.id)}
                >
                  {t("personal.customerLinks.decline")}
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/more">{t("personal.more.back")}</Link>
      </Button>
    </div>
  );
}
