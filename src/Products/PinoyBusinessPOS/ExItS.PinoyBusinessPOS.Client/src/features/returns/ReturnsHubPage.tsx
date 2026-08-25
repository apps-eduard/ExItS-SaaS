import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Search } from "lucide-react";
import { canProcessReturn } from "@/access/pos-capabilities";
import { listSaleReturns } from "@/api/pos/pos-sale-returns-client";
import { formatPaymentMethodLabel, listSales } from "@/api/pos/pos-sales-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ReturnsHubPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [searchedNumber, setSearchedNumber] = useState<string | null>(null);

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

  const allowProcess = canProcessReturn(sessionGrant);

  const recentReturnsQuery = useQuery({
    queryKey: ["sale-returns", "recent", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => listSaleReturns(workspace!, { page: 1, pageSize: 15 }, signal),
  });

  const saleSearchQuery = useQuery({
    queryKey: [
      "sale-returns",
      "sale-search",
      workspace?.organizationId,
      workspace?.branchId,
      searchedNumber,
    ],
    enabled: Boolean(workspace && searchedNumber),
    queryFn: ({ signal }) =>
      listSales(
        workspace!,
        {
          saleNumber: searchedNumber ?? undefined,
          page: 1,
          pageSize: 10,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function runSearch() {
    const value = debounced || search.trim();
    setSearchedNumber(value || null);
  }

  function onSearchSubmit(event: React.FormEvent) {
    event.preventDefault();
    runSearch();
  }

  return (
    <div className="returns-hub-page exits-page flex min-w-0 flex-col gap-3" data-testid="returns-hub-page">
      <PageHeader
        title={t("returns.title")}
        description={t("returns.lede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-returns"
      />

      <form className="flex min-w-0 flex-col gap-2" onSubmit={onSearchSubmit}>
        <SearchField
          label={t("returns.searchTransaction")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => {
            setSearch("");
            setSearchedNumber(null);
          }}
          placeholder={t("returns.transactionNumber")}
          data-testid="returns-search-input"
          containerClassName="returns-hub-page__search exits-page__search"
        />
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("returns.search")}
          testId="returns-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "search",
              label: t("returns.search"),
              icon: <Search />,
              testId: "returns-search-submit",
              emphasis: "primary",
              onSelect: runSearch,
            },
          ]}
        />
      </form>

      {searchedNumber ? (
        <section
          className="catalog-form-section exits-animate-panel gap-2"
          data-testid="returns-search-results"
        >
          <h2 className="catalog-form-section__title">{t("returns.searchResults")}</h2>
          {saleSearchQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {saleSearchQuery.isError ? (
            <ErrorState
              title={t("error.title")}
              detail={(saleSearchQuery.error as Error).message}
            />
          ) : null}
          {saleSearchQuery.isSuccess && saleSearchQuery.data.items.length === 0 ? (
            <EmptyState title={t("returns.transactionNotFound")} detail={t("returns.tryAnother")} />
          ) : null}
          <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="returns-search-list">
            {saleSearchQuery.data?.items.map((sale) => {
              const voided = sale.status === "Voided" || Boolean(sale.voidedAtUtc);
              const canReturn = allowProcess && !voided && sale.status === "Completed";
              return (
                <li key={sale.saleId}>
                  <div
                    className="exits-list__card returns-row min-w-0"
                    data-testid={`returns-row-${sale.saleId}`}
                  >
                    <span className="min-w-0">
                      <span
                        className="exits-list__name block truncate font-semibold"
                        data-testid={`returns-sale-${sale.saleId}`}
                      >
                        {sale.saleNumber}
                      </span>
                      <span className="mb-0 mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                        {formatPaymentMethodLabel(sale.paymentMethod)} · {sale.status}
                      </span>
                    </span>

                    <span className="returns-row__aside">
                      {canReturn ? (
                        <Button
                          type="button"
                          className="min-h-11"
                          data-testid={`returns-open-sale-${sale.saleId}`}
                          onClick={() => navigate(`/returns/sale/${sale.saleId}`)}
                        >
                          {t("returns.returnItems")}
                        </Button>
                      ) : (
                        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted text-right">
                          {voided
                            ? t("returns.cannotReturnVoided")
                            : allowProcess
                              ? t("returns.cannotReturn")
                              : t("returns.processDenied")}
                        </p>
                      )}
                    </span>
                  </div>
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}

      <section
        className="catalog-form-section exits-animate-panel gap-2"
        data-testid="returns-recent-list"
      >
        <h2 className="catalog-form-section__title">{t("returns.recentTitle")}</h2>
        {recentReturnsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {recentReturnsQuery.isError ? (
          <ErrorState
            title={t("error.title")}
            detail={(recentReturnsQuery.error as Error).message}
          />
        ) : null}
        {recentReturnsQuery.isSuccess && recentReturnsQuery.data.items.length === 0 ? (
          <EmptyState title={t("returns.recentEmpty")} detail={t("returns.recentEmptyDetail")} />
        ) : null}
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="returns-recent-list-items">
          {recentReturnsQuery.data?.items.map((item) => (
            <li key={item.returnId}>
              <Link
                className="exits-list__card returns-row block min-w-0 text-foreground no-underline"
                to={`/returns/${item.returnId}`}
                data-testid={`returns-row-${item.returnId}`}
              >
                <span className="min-w-0">
                  <span className="exits-list__name block truncate font-semibold">
                    {item.returnNumber}
                  </span>
                  <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {item.reason}
                  </span>
                </span>

                <span className="returns-row__aside">
                  <MoneyDisplay amount={item.totalRefundAmount} />
                  <ChevronRight className="returns-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
