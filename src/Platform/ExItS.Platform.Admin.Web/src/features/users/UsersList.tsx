import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import {
  hasActiveUserFilters,
  parseUserListSearchParams,
  userListSearchParams,
  type UserListUrlState,
} from "@/api/users/user-list-query";
import {
  ACCOUNT_STATUSES,
  USER_DIRECTORY_FILTERS,
  USER_LIST_PAGE_SIZE,
  USER_LIST_SORT_BY,
  type PlatformUserListItem,
  type UserListSortBy,
} from "@/api/users/user-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { CreatePlatformStaffPanel } from "@/features/users/CreatePlatformStaffPanel";
import { useUserListQuery } from "@/features/users/use-user-list-query";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<UserListSortBy, MessageKey> = {
  DisplayName: "users.sort.displayName",
  Username: "users.sort.username",
  Email: "users.sort.email",
  Status: "users.sort.status",
  UpdatedUtc: "users.sort.updatedUtc",
  AccountType: "users.sort.accountType",
  Organization: "users.sort.organization",
};

const DIRECTORY_LABELS: Record<string, MessageKey> = {
  PlatformStaff: "nav.platformStaff",
  Organization: "nav.orgAccounts",
  Personal: "nav.personalAccounts",
  Unassigned: "nav.needsReview",
};

const STATUS_LABELS: Record<string, MessageKey> = {
  Active: "dashboard.status.Active",
  Suspended: "dashboard.status.Suspended",
  Deactivated: "users.status.Deactivated",
  PendingVerification: "dashboard.status.PendingVerification",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Active") {
    return "success";
  }
  if (status === "Suspended" || status === "PendingVerification") {
    return "warning";
  }
  if (status === "Deactivated") {
    return "danger";
  }
  return "neutral";
}

export function UsersList({ enabled }: { enabled: boolean }) {
  const { t } = usePreferences();
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = parseUserListSearchParams(searchParams);
  const query = useUserListQuery(
    {
      page: state.page,
      pageSize: USER_LIST_PAGE_SIZE,
      status: state.status || undefined,
      search: state.search || undefined,
      directory: state.directory || undefined,
      sortBy: state.sortBy,
      sortDesc: state.sortDesc,
    },
    enabled,
  );

  function replaceState(patch: Partial<typeof state>) {
    const current = parseUserListSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(userListSearchParams({ ...current, ...patch }), { replace: true });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / USER_LIST_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load platform users",
      })
    : null;

  return (
    <div className="grid gap-3">
      <UserFilterForm
        key={`${state.search}|${state.directory}|${state.status}|${state.sortBy}|${state.sortDesc}`}
        search={state.search}
        directory={state.directory}
        status={state.status}
        sortBy={state.sortBy}
        sortDesc={state.sortDesc}
        onSubmitSearch={(search) => replaceState({ search, page: 1 })}
        onReplace={replaceState}
      />

      {state.directory === "PlatformStaff" ? <CreatePlatformStaffPanel /> : null}

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("users.loading")}
        >
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("users.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <UserResults
          items={query.data.items}
          totalCount={query.data.totalCount}
          page={state.page}
          totalPages={totalPages}
          filtered={hasActiveUserFilters(state)}
          showTable={showTable}
          onPage={(nextPage) => replaceState({ page: nextPage })}
          onReset={() =>
            replaceState({
              page: 1,
              search: "",
              status: "",
              directory: "",
              sortBy: "Username",
              sortDesc: false,
            })
          }
        />
      ) : null}
    </div>
  );
}

function UserFilterForm({
  search,
  directory,
  status,
  sortBy,
  sortDesc,
  onSubmitSearch,
  onReplace,
}: {
  search: string;
  directory: string;
  status: string;
  sortBy: UserListSortBy;
  sortDesc: boolean;
  onSubmitSearch: (search: string) => void;
  onReplace: (patch: Partial<UserListUrlState>) => void;
}) {
  const { t } = usePreferences();
  const [searchDraft, setSearchDraft] = useState(search);

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmitSearch(searchDraft.trim());
  }

  return (
    <form
      className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(0,1fr)_minmax(10rem,14rem)_minmax(8rem,11rem)_10rem_9rem_auto] md:items-end"
      onSubmit={onSearchSubmit}
    >
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="user-list-search"
      >
        {t("users.search")}
        <Input
          id="user-list-search"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
          placeholder={t("users.searchPlaceholder")}
          name="search"
          autoComplete="off"
        />
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="user-list-directory"
      >
        {t("users.directory")}
        <select
          id="user-list-directory"
          className={controlClass}
          value={directory}
          onChange={(event) =>
            onReplace({
              directory: event.target.value as UserListUrlState["directory"],
              page: 1,
            })
          }
        >
          <option value="">{t("users.directory.all")}</option>
          {USER_DIRECTORY_FILTERS.map((item) => (
            <option key={item} value={item}>
              {t(DIRECTORY_LABELS[item] ?? "nav.allAccounts")}
            </option>
          ))}
        </select>
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="user-list-status"
      >
        {t("users.status")}
        <select
          id="user-list-status"
          className={controlClass}
          value={status}
          onChange={(event) =>
            onReplace({
              status: event.target.value as UserListUrlState["status"],
              page: 1,
            })
          }
        >
          <option value="">{t("users.status.all")}</option>
          {ACCOUNT_STATUSES.map((item) => (
            <option key={item} value={item}>
              {t(STATUS_LABELS[item] ?? "dashboard.status.Active")}
            </option>
          ))}
        </select>
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="user-list-sort"
      >
        {t("users.sort")}
        <select
          id="user-list-sort"
          className={controlClass}
          value={sortBy}
          onChange={(event) =>
            onReplace({
              sortBy: event.target.value as UserListSortBy,
              page: 1,
            })
          }
        >
          {USER_LIST_SORT_BY.map((item) => (
            <option key={item} value={item}>
              {t(SORT_LABELS[item])}
            </option>
          ))}
        </select>
      </label>
      <label
        className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="user-list-order"
      >
        {t("users.sort.direction")}
        <select
          id="user-list-order"
          className={controlClass}
          value={sortDesc ? "desc" : "asc"}
          onChange={(event) =>
            onReplace({
              sortDesc: event.target.value === "desc",
              page: 1,
            })
          }
        >
          <option value="asc">{t("users.sort.asc")}</option>
          <option value="desc">{t("users.sort.desc")}</option>
        </select>
      </label>
      <div className="flex flex-wrap gap-2">
        <Button type="submit" size="sm">
          {t("users.searchSubmit")}
        </Button>
        {hasActiveUserFilters({
          page: 1,
          search,
          status: status as never,
          directory: directory as never,
          sortBy,
          sortDesc,
        }) ? (
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => {
              setSearchDraft("");
              onReplace({
                page: 1,
                search: "",
                status: "",
                directory: "",
                sortBy: "Username",
                sortDesc: false,
              });
            }}
          >
            {t("users.reset")}
          </Button>
        ) : null}
      </div>
    </form>
  );
}

function UserResults({
  items,
  totalCount,
  page,
  totalPages,
  filtered,
  showTable,
  onPage,
  onReset,
}: {
  items: PlatformUserListItem[];
  totalCount: number;
  page: number;
  totalPages: number;
  filtered: boolean;
  showTable: boolean;
  onPage: (page: number) => void;
  onReset: () => void;
}) {
  const { t } = usePreferences();
  const empty = filtered ? t("users.zeroResult") : t("users.empty");

  return (
    <div className="grid gap-3">
      {showTable ? (
        <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("users.caption")}
            empty={empty}
            columns={[
              {
                id: "name",
                header: t("users.column.displayName"),
                cell: (user) => (
                  <Link
                    className="font-medium text-primary hover:underline"
                    to={`/admin/users/${user.id}`}
                  >
                    {user.displayName}
                  </Link>
                ),
              },
              {
                id: "username",
                header: t("users.column.username"),
                cell: (user) => (
                  <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                    {user.username}
                  </span>
                ),
              },
              {
                id: "email",
                header: t("users.column.email"),
                cell: (user) => user.email,
              },
              {
                id: "class",
                header: t("users.column.accountClass"),
                cell: (user) => user.accountClasses.join(", ") || "—",
              },
              {
                id: "status",
                header: t("users.column.status"),
                cell: (user) => (
                  <StatusIndicator
                    tone={statusTone(user.status)}
                    label={
                      STATUS_LABELS[user.status] ? t(STATUS_LABELS[user.status]!) : user.status
                    }
                  />
                ),
              },
            ]}
            rows={items}
          />
        </div>
      ) : (
        <ul className="grid gap-2">
          {items.length === 0 ? (
            <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
              {empty}
            </li>
          ) : (
            items.map((user) => (
              <li
                key={user.id}
                className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
              >
                <p className="font-medium">{user.displayName}</p>
                <p className="mt-0.5 font-mono text-[length:var(--exits-text-xs)] text-muted">
                  {user.username}
                </p>
                <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">{user.email}</p>
                <div className="mt-1.5 flex flex-wrap items-center gap-2">
                  <StatusIndicator
                    tone={statusTone(user.status)}
                    label={
                      STATUS_LABELS[user.status] ? t(STATUS_LABELS[user.status]!) : user.status
                    }
                  />
                  <Link className="text-primary hover:underline" to={`/admin/users/${user.id}`}>
                    {t("users.open")}
                  </Link>
                </div>
              </li>
            ))
          )}
        </ul>
      )}

      {filtered && items.length === 0 ? (
        <Button type="button" size="sm" variant="outline" className="w-fit" onClick={onReset}>
          {t("users.reset")}
        </Button>
      ) : null}

      {totalCount > 0 ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page <= 1}
            onClick={() => onPage(page - 1)}
          >
            {t("users.previous")}
          </Button>
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("users.page")} {page} / {totalPages}
          </p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={page >= totalPages}
            onClick={() => onPage(page + 1)}
          >
            {t("users.next")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
