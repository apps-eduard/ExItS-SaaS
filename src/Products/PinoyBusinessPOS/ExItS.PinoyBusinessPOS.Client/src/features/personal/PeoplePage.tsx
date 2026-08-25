import { ChevronRight, Plus, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/http";
import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/ui/badge";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { LoadingState } from "@/components/ui/skeleton";
import {
  usePersonalContactsQuery,
  usePersonalInvitationsQuery,
  usePersonalUtangSummariesQuery,
} from "@/features/personal/people-queries";
import {
  buildPeopleRows,
  initialsFor,
  readResolvedPublicIdCache,
  type PeopleConnectionStatus,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

function statusTone(status: PeopleConnectionStatus): "neutral" | "success" | "warning" | "info" {
  if (status === "connected") {
    return "success";
  }
  if (status === "request_pending") {
    return "warning";
  }
  return "neutral";
}

export function PeoplePage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const [search, setSearch] = useState("");
  const contactsQuery = usePersonalContactsQuery();
  const invitationsQuery = usePersonalInvitationsQuery();
  const utangQuery = usePersonalUtangSummariesQuery();

  const isLoading = contactsQuery.isLoading || invitationsQuery.isLoading || utangQuery.isLoading;
  const error = contactsQuery.error ?? invitationsQuery.error ?? utangQuery.error;

  const rows = useMemo(() => {
    if (!contactsQuery.data || !invitationsQuery.data || !utangQuery.data) {
      return [];
    }
    return buildPeopleRows({
      contacts: contactsQuery.data,
      invitations: invitationsQuery.data,
      lent: utangQuery.data.lent,
      borrowed: utangQuery.data.borrowed,
      resolvedPublicIds: readResolvedPublicIdCache(),
      search,
    });
  }, [contactsQuery.data, invitationsQuery.data, utangQuery.data, search]);

  if (isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (error) {
    const detail =
      error instanceof ApiClientError
        ? (error.problem.detail ?? error.message)
        : t("people.loadError");
    return (
      <ErrorState
        title={t("error.title")}
        body={detail}
        record={normalizeDiagnosticError(error, {
          locale: preferences.locale,
          theme: preferences.theme,
          pathname: "/personal/people",
        })}
        action={
          <Button
            type="button"
            variant="secondary"
            onClick={() => {
              void contactsQuery.refetch();
              void invitationsQuery.refetch();
              void utangQuery.refetch();
            }}
          >
            {t("error.reset")}
          </Button>
        }
      />
    );
  }

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title={t("people.title")}
        actions={
          <Button asChild>
            <Link to="/personal/people/add">
              <Plus className="size-4" aria-hidden="true" />
              {t("people.add")}
            </Link>
          </Button>
        }
      />

      <label className="relative block">
        <span className="sr-only">{t("people.search")}</span>
        <Search
          className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted"
          aria-hidden="true"
        />
        <input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={t("people.searchPlaceholder")}
          className="h-[var(--exits-control-height)] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface pl-10 pr-3 text-[length:var(--exits-text-md)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
        />
      </label>

      {rows.length === 0 ? (
        <EmptyState
          title={t("people.emptyTitle")}
          body={t("people.emptyBody")}
          action={
            <Button asChild>
              <Link to="/personal/people/add">{t("people.addPerson")}</Link>
            </Button>
          }
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" role="list">
          {rows.map((row) => (
            <li key={row.contact.id}>
              <Link
                to={`/personal/people/${row.contact.id}`}
                className="flex min-h-[var(--exits-touch-target-min)] items-center gap-3 rounded-[var(--exits-radius-lg)] border border-border bg-surface px-3 py-3 text-inherit no-underline transition-colors hover:bg-surface-muted"
              >
                <span
                  className="flex size-11 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground"
                  aria-hidden="true"
                >
                  {initialsFor(row.contact.displayName)}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate font-semibold">{row.contact.displayName}</span>
                  <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                    {row.identityLine === "exits" && row.publicUserId
                      ? row.publicUserId
                      : t("people.localContact")}
                  </span>
                  <span className="mt-1 flex flex-wrap items-center gap-2">
                    <StatusChip tone={statusTone(row.connectionStatus)}>
                      {row.connectionStatus === "connected"
                        ? t("people.status.connected")
                        : row.connectionStatus === "request_pending"
                          ? t("people.status.requestPending")
                          : t("people.status.notConnected")}
                    </StatusChip>
                    {row.utangSummary ? (
                      <span className="text-[length:var(--exits-text-sm)] text-muted">
                        {row.utangSummary}
                      </span>
                    ) : null}
                  </span>
                </span>
                <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden="true" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
