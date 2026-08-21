import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canCreateCustomer } from "@/access/pos-capabilities";
import { listCustomers } from "@/api/pos/pos-customers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function CustomersListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<"Active" | "Inactive" | "">("Active");

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

  const allowCreate = canCreateCustomer(sessionGrant);

  const query = useQuery({
    queryKey: [
      "customers",
      "list",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCustomers(
        workspace!,
        {
          search: debounced || undefined,
          status: status || undefined,
          pageSize: 50,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customers-list-page">
      <PageHeader title={t("customers.title")} description={t("customers.lede")} />
      <div className="flex flex-wrap gap-2">
        {allowCreate ? (
          <Button asChild className="min-h-11" data-testid="customers-new">
            <Link to="/customers/new">{t("customers.add")}</Link>
          </Button>
        ) : null}
      </div>
      <SearchField
        label={t("customers.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("customers.search")}
      />
      <div className="flex flex-wrap gap-2" role="group" aria-label={t("customers.statusFilter")}>
        {(
          [
            ["Active", "customers.statusActive"],
            ["Inactive", "customers.statusInactive"],
            ["", "customers.statusAll"],
          ] as const
        ).map(([value, labelKey]) => (
          <Button
            key={value || "all"}
            type="button"
            variant={status === value ? "default" : "ghost"}
            className="min-h-11"
            data-testid={`customers-status-${value || "all"}`}
            onClick={() => setStatus(value)}
          >
            {t(labelKey)}
          </Button>
        ))}
      </div>
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("customers.empty")} detail={t("customers.emptyDetail")} />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="customers-list">
        {query.data?.items.map((customer) => (
          <li key={customer.customerId}>
            <Card className="p-3">
              <Link
                className="block min-w-0 text-foreground no-underline"
                to={`/customers/${customer.customerId}`}
                data-testid={`customer-row-${customer.customerId}`}
              >
                <span className="block truncate font-semibold">{customer.displayName}</span>
                <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {[customer.mobileNumber, customer.status].filter(Boolean).join(" · ")}
                </span>
              </Link>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}
