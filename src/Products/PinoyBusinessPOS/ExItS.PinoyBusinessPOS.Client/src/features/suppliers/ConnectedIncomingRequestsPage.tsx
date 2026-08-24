import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Loader2, Users, X } from "lucide-react";
import { canManageSuppliers } from "@/access/pos-capabilities";
import {
  approveConnection,
  declineConnection,
  listRelationships,
  type ConnectedSupplierRelationship,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatRelativeOrDate } from "@/features/devices/device-presentation";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function matchesIncomingSearch(item: ConnectedSupplierRelationship, query: string): boolean {
  if (!query) return true;

  const haystack = [
    item.counterpartyDisplayName,
    item.counterpartyPublicOrganizationId,
    item.status,
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();

  return haystack.includes(query.toLowerCase());
}

export function ConnectedIncomingRequestsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { preferences } = usePreferences();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [sharePrompt, setSharePrompt] = useState<{
    relationshipId: string;
    name: string;
  } | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);

  const query = useQuery({
    queryKey: ["connected-suppliers", "incoming", workspace?.organizationId],
    enabled: Boolean(workspace),
    queryFn: async ({ signal }) => {
      const rows = await listRelationships(workspace!, "supplier", signal);
      return rows.filter((row) => row.status.toLowerCase() === "pending");
    },
  });

  const items = useMemo(() => {
    const rows = query.data ?? [];
    return rows.filter((item) => matchesIncomingSearch(item, debounced));
  }, [query.data, debounced]);

  async function respond(relationshipId: string, accept: boolean, name: string) {
    if (!workspace || !allowManage || busyId) {
      return;
    }
    setBusyId(relationshipId);
    setActionError(null);
    try {
      if (accept) {
        await approveConnection(workspace, relationshipId);
        setSharePrompt({ relationshipId, name });
      } else {
        await declineConnection(workspace, relationshipId);
      }
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
    } catch (err) {
      setActionError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.respondFailed"),
      );
    } finally {
      setBusyId(null);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const hasLoaded = query.isSuccess;
  const totalIncoming = query.data?.length ?? 0;
  const showFilteredEmpty = hasLoaded && totalIncoming > 0 && items.length === 0;
  const showTrueEmpty = hasLoaded && totalIncoming === 0;

  return (
    <div
      className="connected-incoming-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="connected-incoming-page"
    >
      <PageHeader
        title={t("connected.incomingTitle")}
        description={t("connected.incomingHelp")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t("connected.backToSuppliers")}
        backTestId="page-header-back-suppliers"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("connected.incomingTitle")}
        testId="connected-incoming-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "buyers",
            label: t("connected.buyersTitle"),
            icon: <Users />,
            href: "/suppliers/connected/buyers",
            testId: "connected-incoming-buyers",
          },
        ]}
      />

      <SearchField
        label={t("connected.searchIncoming")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("connected.searchIncoming")}
        data-testid="connected-incoming-search"
        containerClassName="connected-incoming-page__search exits-page__search"
      />

      {actionError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{actionError}</p>
        </div>
      ) : null}

      {sharePrompt ? (
        <section className="catalog-form-section connected-share-prompt" data-testid="connected-share-prompt">
          <h2 className="catalog-form-section__title m-0">{t("connected.sharePromptTitle")}</h2>
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("connected.sharePromptHelp").replace("{name}", sharePrompt.name)}
          </p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("connected.exposableNotSharedNote")}
          </p>
          <div className="connected-incoming-row__actions mt-3">
            <Button
              type="button"
              className="min-h-11"
              data-testid="connected-share-now"
              onClick={() =>
                navigate(`/suppliers/connected/buyers/${sharePrompt.relationshipId}/shared-products`)
              }
            >
              {t("connected.shareProductsNow")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="connected-share-later"
              onClick={() => setSharePrompt(null)}
            >
              {t("connected.notNow")}
            </Button>
          </div>
        </section>
      ) : null}

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            query.error instanceof PosApiError
              ? (query.error.problem.detail ?? query.error.message)
              : t("connected.loadFailed")
          }
        />
      ) : null}
      {showTrueEmpty ? (
        <EmptyState title={t("connected.noIncoming")} detail={t("connected.noIncomingHelp")} />
      ) : null}
      {showFilteredEmpty ? (
        <EmptyState
          title={t("connected.incomingNoMatch")}
          detail={t("connected.incomingNoMatchHelp")}
        />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="connected-incoming-list">
        {items.map((item) => {
          const name = item.counterpartyDisplayName?.trim() || t("connected.requestingBusiness");
          const requestedWhen = formatRelativeOrDate(
            item.requestedAtUtc,
            new Date(),
            preferences.locale,
          );
          const isBusy = busyId === item.relationshipId;

          return (
            <li key={item.relationshipId}>
              <div
                className="exits-list__card connected-incoming-row"
                data-testid={`connected-incoming-row-${item.relationshipId}`}
              >
                <div className="connected-incoming-row__header">
                  <div className="connected-incoming-row__main min-w-0">
                    <span className="exits-list__name block truncate font-semibold">{name}</span>
                    {item.counterpartyPublicOrganizationId ? (
                      <span className="connected-incoming-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                        {item.counterpartyPublicOrganizationId}
                      </span>
                    ) : null}
                    <span className="connected-incoming-row__meta mt-1 block text-[length:var(--exits-text-sm)] text-muted">
                      {t("connected.incomingMessage").replace("{name}", name)}
                    </span>
                    {requestedWhen ? (
                      <span className="connected-incoming-row__meta mt-1 block text-[length:var(--exits-text-sm)] text-muted">
                        {t("connected.requestedAt").replace("{when}", requestedWhen)}
                      </span>
                    ) : null}
                  </div>
                  <StatusChip tone="warning">{item.status}</StatusChip>
                </div>

                {allowManage ? (
                  <div className="connected-incoming-row__actions">
                    <Button
                      type="button"
                      variant="outline"
                      className="min-h-11"
                      data-testid={`connected-approve-${item.relationshipId}`}
                      disabled={Boolean(busyId)}
                      onClick={() => void respond(item.relationshipId, true, name)}
                    >
                      {isBusy ? (
                        <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                      ) : (
                        <Check className="size-4 shrink-0" aria-hidden />
                      )}
                      {t("connected.accept")}
                    </Button>
                    <Button
                      type="button"
                      variant="destructive"
                      className="min-h-11"
                      data-testid={`connected-decline-${item.relationshipId}`}
                      disabled={Boolean(busyId)}
                      onClick={() => void respond(item.relationshipId, false, name)}
                    >
                      {isBusy ? (
                        <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                      ) : (
                        <X className="size-4 shrink-0" aria-hidden />
                      )}
                      {t("connected.decline")}
                    </Button>
                  </div>
                ) : null}
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
