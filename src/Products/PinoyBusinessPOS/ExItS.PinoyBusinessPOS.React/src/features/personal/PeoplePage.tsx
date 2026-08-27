import { ArrowLeft, ChevronRight, Info, Plus } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/ui/skeleton";
import { PeopleInfoDialog } from "@/features/personal/PeopleInfoDialog";
import {
  parsePersonCreateKind,
  PersonCreateForm,
} from "@/features/personal/PersonFormPage";
import {
  usePersonalConnectionRequestsQuery,
  usePersonalContactsQuery,
  usePersonalUtangSummariesQuery,
} from "@/features/personal/people-queries";
import { PeopleListSection } from "@/features/personal/PeopleListSection";
import {
  buildPeopleRows,
  summarizePeopleContacts,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export function PeoplePage() {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const addFromUrl = searchParams.get("add") === "1";
  const urlKind = parsePersonCreateKind(searchParams.get("kind"));
  const linkPublicId = searchParams.get("linkPublicId");

  const [infoOpen, setInfoOpen] = useState(false);
  const [addOpen, setAddOpen] = useState(addFromUrl);

  useEffect(() => {
    setAddOpen(addFromUrl);
  }, [addFromUrl]);

  function closeAddPanel() {
    setAddOpen(false);
    const next = new URLSearchParams(searchParams);
    next.delete("add");
    next.delete("kind");
    next.delete("linkPublicId");
    setSearchParams(next, { replace: true });
  }

  function toggleAddPanel() {
    if (addOpen) {
      closeAddPanel();
      return;
    }
    setAddOpen(true);
    const next = new URLSearchParams(searchParams);
    next.set("add", "1");
    setSearchParams(next, { replace: true });
  }
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
    });
  }, [contactsQuery.data, connectionsQuery.data, utangQuery.data]);

  if (isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (error) {
    const detail =
      error instanceof PlatformApiError
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
    <section className="personal-page people-page exits-page mx-auto flex w-full max-w-3xl flex-col gap-4">
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

      <Card className="flex flex-col gap-3" data-testid="people-add-panel">
        <button
          type="button"
          className={cn(
            "flex w-full min-h-[var(--exits-touch-target-min)] items-start gap-3 rounded-[var(--exits-radius-md)] border-0 bg-transparent p-0 text-left text-inherit",
            "transition-colors hover:bg-surface-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          )}
          data-testid="people-add-toggle"
          aria-label={t("people.add.toggle")}
          aria-expanded={addOpen}
          aria-controls="people-add-form"
          onClick={toggleAddPanel}
        >
          <span
            className={cn(
              "inline-flex size-10 shrink-0 items-center justify-center rounded-full bg-[var(--exits-surface-muted)] text-primary transition-transform",
              addOpen && "rotate-45",
            )}
            aria-hidden
          >
            <Plus className="size-5" />
          </span>
          <span className="min-w-0 flex-1 pt-0.5">
            <span className="block text-[length:var(--exits-text-lg)] font-semibold">
              {t("people.newTitle")}
            </span>
            <span className="mt-1 block text-[length:var(--exits-text-sm)] text-muted">
              {addOpen ? t("people.createKindLede") : t("people.howToAdd.lede")}
            </span>
          </span>
        </button>

        {addOpen ? (
          <div id="people-add-form" className="border-t border-border pt-3">
            <PersonCreateForm
              key={`${urlKind ?? "pick"}-${linkPublicId ?? ""}`}
              embedded
              initialKind={urlKind}
              linkPublicId={linkPublicId}
              onCancel={closeAddPanel}
            />
          </div>
        ) : null}
      </Card>

      <Card className="flex flex-col gap-2">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("people.connectionInbox")}
            </h2>
            <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {t("people.connectionInboxHelp")}
            </p>
          </div>
          {(() => {
            const pendingCount = (connectionsQuery.data ?? []).filter(
              (item) => item.status.toLowerCase() === "pending",
            ).length;
            return pendingCount > 0 ? (
              <StatusChip tone="warning">
                {t("people.connectionInboxBadge").replace("{count}", String(pendingCount))}
              </StatusChip>
            ) : null;
          })()}
        </div>
        <Button asChild variant="outline" className="min-h-[var(--exits-touch-target-min)] justify-between">
          <Link to="/personal/invitations">
            <span>{t("people.connectionInbox")}</span>
            <ChevronRight className="size-4" aria-hidden="true" />
          </Link>
        </Button>
      </Card>

      <PeopleListSection rows={rows} summary={summary} />

      <PeopleInfoDialog open={infoOpen} onClose={() => setInfoOpen(false)} />
    </section>
  );
}
