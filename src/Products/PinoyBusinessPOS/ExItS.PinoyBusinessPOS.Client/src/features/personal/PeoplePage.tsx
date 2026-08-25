import { ArrowLeft, ChevronRight, IdCard, Info, Link2, Search, UserRound } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ApiClientError } from "@/api/http";
import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/ui/skeleton";
import { PeopleInfoDialog } from "@/features/personal/PeopleInfoDialog";
import {
  usePersonalConnectionRequestsQuery,
  usePersonalContactsQuery,
  usePersonalUtangSummariesQuery,
} from "@/features/personal/people-queries";
import {
  buildPeopleRows,
  initialsFor,
  summarizePeopleContacts,
  type PeopleConnectionStatus,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

function statusTone(
  status: PeopleConnectionStatus,
): "neutral" | "success" | "warning" | "info" {
  if (status === "connected") {
    return "success";
  }
  if (status === "request_pending" || status === "blocked") {
    return "warning";
  }
  return "neutral";
}

export function PeoplePage() {
  const { t } = useI18n();
  const [searchOpen, setSearchOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [infoOpen, setInfoOpen] = useState(false);
  const contactsQuery = usePersonalContactsQuery();
  const connectionsQuery = usePersonalConnectionRequestsQuery();
  const utangQuery = usePersonalUtangSummariesQuery();

  const isLoading =
    contactsQuery.isLoading || connectionsQuery.isLoading || utangQuery.isLoading;
  const error = contactsQuery.error ?? connectionsQuery.error ?? utangQuery.error;

  const summary = useMemo(
    () => summarizePeopleContacts(contactsQuery.data ?? []),
    [contactsQuery.data],
  );

  const rows = useMemo(() => {
    if (!contactsQuery.data || !connectionsQuery.data || !utangQuery.data) {
      return [];
    }
    return buildPeopleRows({
      contacts: contactsQuery.data,
      connectionRequests: connectionsQuery.data,
      lent: utangQuery.data.lent,
      borrowed: utangQuery.data.borrowed,
      search: searchOpen ? search : "",
    });
  }, [contactsQuery.data, connectionsQuery.data, utangQuery.data, search, searchOpen]);

  function statusLabel(status: PeopleConnectionStatus): string {
    switch (status) {
      case "connected":
        return t("people.status.connected");
      case "request_pending":
        return t("people.status.requestPending");
      case "blocked":
        return t("people.status.blocked");
      case "local":
        return t("people.status.local");
      default:
        return t("people.status.notConnected");
    }
  }

  if (isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (error) {
    const detail =
      error instanceof ApiClientError
        ? (error.problem.detail ?? error.message)
        : t("people.loadError");
    return (
      <div className="flex flex-col gap-3">
        <ErrorState title={t("error.title")} detail={detail} error={error} />
        <Button
          type="button"
          variant="outline"
          onClick={() => {
            void contactsQuery.refetch();
            void connectionsQuery.refetch();
            void utangQuery.refetch();
          }}
        >
          {t("personal.home.retry")}
        </Button>
      </div>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <header className="flex items-center gap-2">
        <Button asChild variant="ghost" size="icon" className="shrink-0" aria-label={t("shell.back")}>
          <Link to="/personal">
            <ArrowLeft className="size-5" aria-hidden="true" />
          </Link>
        </Button>
        <h1 className="m-0 min-w-0 flex-1 text-[length:var(--exits-text-2xl)] font-bold tracking-tight">
          {t("people.title")}
        </h1>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="shrink-0"
          aria-label={t("people.info.open")}
          onClick={() => setInfoOpen(true)}
        >
          <Info className="size-5" aria-hidden="true" />
        </Button>
      </header>

      <Card className="flex flex-col gap-3">
        <div>
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("people.howToAdd.title")}
          </h2>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("people.howToAdd.lede")}
          </p>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Link
            to="/personal/people/add/local"
            className="flex min-h-[var(--exits-touch-target-min)] flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 text-inherit no-underline transition-colors hover:border-primary/40 hover:bg-surface-muted"
          >
            <UserRound className="size-6 text-primary" aria-hidden="true" />
            <span className="font-semibold">{t("people.howToAdd.withoutId")}</span>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {t("people.howToAdd.withoutIdHelp")}
            </span>
          </Link>
          <Link
            to="/personal/people/add"
            className="flex min-h-[var(--exits-touch-target-min)] flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-4 text-inherit no-underline transition-colors hover:border-primary/40 hover:bg-surface-muted"
          >
            <IdCard className="size-6 text-primary" aria-hidden="true" />
            <span className="font-semibold">{t("people.howToAdd.withId")}</span>
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {t("people.howToAdd.withIdHelp")}
            </span>
          </Link>
        </div>
      </Card>

      <Card className="flex flex-col gap-3">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("people.title")}
            </h2>
            <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {t("people.summary")
                .replace("{identified}", String(summary.identified))
                .replace("{local}", String(summary.local))}
            </p>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label={t("people.search")}
            aria-pressed={searchOpen}
            onClick={() => setSearchOpen((value) => !value)}
          >
            <Search className="size-5" aria-hidden="true" />
          </Button>
        </div>

        {searchOpen ? (
          <label className="block">
            <span className="sr-only">{t("people.search")}</span>
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={t("people.searchPlaceholder")}
              className="h-[var(--exits-control-height)] w-full rounded-[var(--exits-radius-md)] border border-border bg-background px-3 text-[length:var(--exits-text-md)] outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
            />
          </label>
        ) : null}

        {rows.length === 0 ? (
          <p className="m-0 py-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("people.emptyBody")}
          </p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0" role="list">
            {rows.map((row) => (
              <li key={row.contact.id}>
                <Link
                  to={`/personal/people/${row.contact.id}`}
                  className="flex min-h-[var(--exits-touch-target-min)] items-center gap-3 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 py-3 text-inherit no-underline transition-colors hover:bg-surface-muted"
                >
                  <span
                    className="flex size-11 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground"
                    aria-hidden="true"
                  >
                    {initialsFor(row.contact.displayName)}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-semibold">{row.contact.displayName}</span>
                    <span className="mt-1 flex items-center justify-between gap-2">
                      <span
                        className={cn(
                          "flex min-w-0 items-center gap-1 truncate text-[length:var(--exits-text-sm)] text-muted",
                        )}
                      >
                        {row.identityLine === "exits" && row.publicUserId ? (
                          <>
                            <Link2 className="size-3.5 shrink-0" aria-hidden="true" />
                            <span className="truncate">{row.publicUserId}</span>
                          </>
                        ) : (
                          <span className="truncate">{t("people.localContact")}</span>
                        )}
                      </span>
                      <StatusChip tone={statusTone(row.connectionStatus)} className="shrink-0">
                        {statusLabel(row.connectionStatus)}
                      </StatusChip>
                    </span>
                    {row.utangSummary ? (
                      <span className="mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                        {row.utangSummary}
                      </span>
                    ) : null}
                  </span>
                  <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden="true" />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </Card>

      <PeopleInfoDialog open={infoOpen} onClose={() => setInfoOpen(false)} />
    </section>
  );
}
