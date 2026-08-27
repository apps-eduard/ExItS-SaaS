import { ChevronRight, Link2 } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/ui/badge";
import {
  type PeopleConnectionStatus,
  type PeopleRowModel,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";

export type PeopleListFilter = "all" | "connected" | "pending" | "local" | "not_connected";

const FILTERS: ReadonlyArray<{
  key: PeopleListFilter;
  labelKey:
    | "people.filter.all"
    | "people.filter.connected"
    | "people.filter.pending"
    | "people.filter.local"
    | "people.filter.notConnected";
}> = [
  { key: "all", labelKey: "people.filter.all" },
  { key: "connected", labelKey: "people.filter.connected" },
  { key: "pending", labelKey: "people.filter.pending" },
  { key: "local", labelKey: "people.filter.local" },
  { key: "not_connected", labelKey: "people.filter.notConnected" },
];

function statusTone(
  status: PeopleConnectionStatus,
): "neutral" | "success" | "warning" | "info" {
  if (status === "connected") {
    return "success";
  }
  if (status === "request_sent" || status === "request_received" || status === "blocked") {
    return "warning";
  }
  if (status === "local") {
    return "info";
  }
  return "neutral";
}

function matchesFilter(row: PeopleRowModel, filter: PeopleListFilter): boolean {
  switch (filter) {
    case "connected":
      return row.connectionStatus === "connected";
    case "pending":
      return (
        row.connectionStatus === "request_sent" || row.connectionStatus === "request_received"
      );
    case "local":
      return row.connectionStatus === "local";
    case "not_connected":
      return row.connectionStatus === "not_connected";
    default:
      return true;
  }
}

export function filterPeopleRows(
  rows: PeopleRowModel[],
  filter: PeopleListFilter,
  search: string,
): PeopleRowModel[] {
  const needle = search.trim().toLowerCase();
  return rows
    .filter((row) => matchesFilter(row, filter))
    .filter((row) => {
      if (!needle) {
        return true;
      }
      const hay = `${row.contact.displayName} ${row.publicUserId ?? ""}`.toLowerCase();
      return hay.includes(needle);
    })
    .sort((a, b) => a.contact.displayName.localeCompare(b.contact.displayName));
}

function PeopleListRow({
  row,
  statusLabel,
}: {
  row: PeopleRowModel;
  statusLabel: string;
}) {
  const { t } = useI18n();

  return (
    <li>
      <Link
        to={`/personal/people/${row.contact.id}`}
        className="exits-list__card people-row block min-w-0 text-foreground no-underline"
        data-testid={`people-row-${row.contact.id}`}
      >
        <PersonAvatar name={row.contact.displayName} size="sm" className="people-row__avatar" />
        <span className="people-row__main min-w-0 flex-1">
          <span className="exits-list__name block truncate font-semibold">
            {row.contact.displayName}
          </span>
          <span className="people-row__meta mt-1 flex min-w-0 items-center gap-1 truncate text-[length:var(--exits-text-sm)] text-muted">
            {row.identityLine === "exits" && row.publicUserId ? (
              <>
                <Link2 className="size-3.5 shrink-0" aria-hidden="true" />
                <span className="truncate">{row.publicUserId}</span>
              </>
            ) : (
              <span className="truncate">{t("people.localContact")}</span>
            )}
          </span>
          {row.utangSummary ? (
            <span className="people-row__utang mt-1 block truncate text-[length:var(--exits-text-sm)] font-medium text-primary">
              {row.utangSummary}
            </span>
          ) : null}
        </span>
        <span className="customer-row__aside">
          <StatusChip tone={statusTone(row.connectionStatus)}>{statusLabel}</StatusChip>
          <ChevronRight className="customer-row__chevron size-4 shrink-0 text-muted" aria-hidden />
        </span>
      </Link>
    </li>
  );
}

export function PeopleListSection({
  rows,
  summary,
}: {
  rows: PeopleRowModel[];
  summary: { identified: number; local: number; total: number };
}) {
  const { t } = useI18n();
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<PeopleListFilter>("all");

  const filteredRows = useMemo(
    () => filterPeopleRows(rows, filter, search),
    [filter, rows, search],
  );

  function statusLabel(status: PeopleConnectionStatus): string {
    switch (status) {
      case "connected":
        return t("people.status.connected");
      case "request_sent":
        return t("people.status.requestSent");
      case "request_received":
        return t("people.status.requestReceived");
      case "blocked":
        return t("people.status.blocked");
      case "local":
        return t("people.status.local");
      default:
        return t("people.status.notConnected");
    }
  }

  const filterCounts = useMemo(() => {
    const counts: Record<PeopleListFilter, number> = {
      all: rows.length,
      connected: 0,
      pending: 0,
      local: 0,
      not_connected: 0,
    };
    for (const row of rows) {
      if (row.connectionStatus === "connected") {
        counts.connected += 1;
      }
      if (
        row.connectionStatus === "request_sent" ||
        row.connectionStatus === "request_received"
      ) {
        counts.pending += 1;
      }
      if (row.connectionStatus === "local") {
        counts.local += 1;
      }
      if (row.connectionStatus === "not_connected") {
        counts.not_connected += 1;
      }
    }
    return counts;
  }, [rows]);

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-3"
      data-testid="people-list-section"
      aria-label={t("people.listTitle")}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <h2 className="catalog-form-section__title m-0">{t("people.listTitle")}</h2>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("people.summary")
              .replace("{identified}", String(summary.identified))
              .replace("{local}", String(summary.local))}
          </p>
        </div>
        {summary.total > 0 ? (
          <StatusChip tone="neutral">{String(summary.total)}</StatusChip>
        ) : null}
      </div>

      <SearchField
        label={t("people.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("people.searchPlaceholder")}
        data-testid="people-search"
        containerClassName="people-page__search exits-page__search"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("people.filter.label")}
        testId="people-status-filters"
        className="exits-animate-toolbar"
        items={FILTERS.map((item) => ({
          key: item.key,
          label: `${t(item.labelKey)}${filterCounts[item.key] > 0 ? ` (${filterCounts[item.key]})` : ""}`,
          state: filter === item.key ? "active" : "idle",
          testId: `people-filter-${item.key}`,
          onSelect: () => setFilter(item.key),
        }))}
      />

      {rows.length === 0 ? (
        <EmptyState title={t("people.emptyTitle")} detail={t("people.emptyBody")} />
      ) : filteredRows.length === 0 ? (
        <EmptyState title={t("people.noResultsTitle")} detail={t("people.noResultsBody")} />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="people-list">
          {filteredRows.map((row) => (
            <PeopleListRow
              key={row.contact.id}
              row={row}
              statusLabel={statusLabel(row.connectionStatus)}
            />
          ))}
        </ul>
      )}
    </section>
  );
}
