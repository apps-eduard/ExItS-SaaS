import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManagePurchasing, canViewPurchasing } from "@/access/pos-capabilities";
import { listLinks, unlinkProduct } from "@/api/pos/pos-connected-suppliers-client";
import { getSupplier, isConnectedSupplier } from "@/api/pos/pos-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function LinkedProductsPage() {
  const { t } = useI18n();
  const { supplierId } = useParams<{ supplierId: string }>();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim().toLowerCase()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);
  const allowManage = canManagePurchasing(sessionGrant);

  const supplierQuery = useQuery({
    queryKey: ["suppliers", "detail", workspace?.organizationId, supplierId],
    enabled: Boolean(workspace) && Boolean(supplierId),
    queryFn: ({ signal }) => getSupplier(workspace!, supplierId!, signal),
  });

  const relationshipId = supplierQuery.data?.connectedRelationshipId ?? null;

  const linksQuery = useQuery({
    queryKey: ["connected-suppliers", "links", relationshipId],
    enabled: Boolean(workspace) && Boolean(relationshipId) && allowView,
    queryFn: ({ signal }) => listLinks(workspace!, relationshipId!, signal),
  });

  const filtered = useMemo(() => {
    const items = (linksQuery.data ?? []).filter((link) => link.isActive);
    if (!debounced) {
      return items;
    }
    return items.filter((link) => {
      const hay = `${link.supplierNameSnapshot} ${link.supplierSkuSnapshot ?? ""}`.toLowerCase();
      return hay.includes(debounced);
    });
  }, [linksQuery.data, debounced]);

  async function unlink(linkId: string) {
    if (!workspace || !allowManage || busyId) {
      return;
    }
    setBusyId(linkId);
    setMessage(null);
    try {
      await unlinkProduct(workspace, linkId);
      setMessage(t("connected.unlinkSucceeded"));
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "links"] });
    } catch (err) {
      setMessage(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.unlinkFailed"),
      );
    } finally {
      setBusyId(null);
    }
  }

  if (!workspace || !supplierId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (supplierQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (!supplierQuery.data || !isConnectedSupplier(supplierQuery.data) || !relationshipId) {
    return (
      <EmptyState
        title={t("connected.relationshipMissing")}
        detail={t("connected.relationshipMissingHelp")}
      />
    );
  }

  if (!allowView) {
    return <ErrorState title={t("error.title")} detail={t("connected.catalogDenied")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="linked-products-page">
      <PageHeader
        title={t("connected.linkedTitle")}
        description={t("connected.linkedHelp")}
        backTo={`/suppliers/${supplierId}`}
        backLabel={t("connected.backToSupplier")}
        backTestId="page-header-back-suppliers"
      />
      <Button asChild className="min-h-11 self-start" data-testid="linked-browse-catalog">
        <Link to={`/suppliers/${supplierId}/connected-catalog`}>
          {t("connected.browseProducts")}
        </Link>
      </Button>
      {message ? (
        <Card data-testid="linked-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{message}</p>
        </Card>
      ) : null}
      <SearchField
        label={t("connected.searchLinked")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("connected.searchLinked")}
        data-testid="linked-search"
      />
      {linksQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {linksQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            linksQuery.error instanceof PosApiError
              ? (linksQuery.error.problem.detail ?? linksQuery.error.message)
              : t("connected.loadFailed")
          }
        />
      ) : null}
      {linksQuery.isSuccess && filtered.length === 0 ? (
        <EmptyState
          title={debounced ? t("connected.linkedNoMatch") : t("connected.linkedEmpty")}
          detail={debounced ? t("connected.linkedNoMatchHelp") : t("connected.linkedEmptyHelp")}
        />
      ) : null}
      <ul className="m-0 grid list-none gap-2 p-0" data-testid="linked-products-list">
        {filtered.map((link) => (
          <li key={link.linkId}>
            <Card className="p-3" data-testid={`linked-item-${link.linkId}`}>
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="m-0 font-semibold">{link.supplierNameSnapshot}</p>
                  <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {link.supplierSkuSnapshot ?? t("connected.noSku")} · {t("connected.poPrice")}:{" "}
                    {link.lastKnownOrderPrice}
                  </p>
                </div>
                {allowManage ? (
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    data-testid={`linked-unlink-${link.linkId}`}
                    disabled={busyId === link.linkId}
                    onClick={() => void unlink(link.linkId)}
                  >
                    {t("connected.unlink")}
                  </Button>
                ) : null}
              </div>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}
