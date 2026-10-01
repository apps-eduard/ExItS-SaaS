import { ArrowLeft, ChevronDown, Info, Plus } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PeopleInfoPopover } from "@/features/personal/PeopleInfoDialog";
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
  const infoRootRef = useRef<HTMLDivElement>(null);
  const infoButtonRef = useRef<HTMLButtonElement>(null);
  const [addOpen, setAddOpen] = useState(addFromUrl);

  useEffect(() => {
    if (!infoOpen) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setInfoOpen(false);
      }
    }
    function onPointerDown(event: PointerEvent) {
      if (!infoRootRef.current?.contains(event.target as Node)) {
        setInfoOpen(false);
      }
    }
    document.addEventListener("keydown", onKeyDown);
    document.addEventListener("pointerdown", onPointerDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.removeEventListener("pointerdown", onPointerDown);
    };
  }, [infoOpen]);

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

  const isInitialLoading =
    (contactsQuery.isLoading || connectionsQuery.isLoading || utangQuery.isLoading) &&
    !contactsQuery.data;
  const isRefreshing =
    (contactsQuery.isFetching || connectionsQuery.isFetching || utangQuery.isFetching) &&
    Boolean(contactsQuery.data);
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

  if (error && !contactsQuery.data) {
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
    <section className="personal-page people-page exits-page flex w-full min-w-0 flex-col gap-4">
      <header
        ref={infoRootRef}
        className={cn("relative flex items-center gap-2", infoOpen && "z-30")}
      >
        <Button asChild variant="ghost" size="icon" className="shrink-0" aria-label={t("shell.back")}>
          <Link to="/personal">
            <ArrowLeft className="size-5" aria-hidden="true" />
          </Link>
        </Button>
        <div className="flex min-w-0 items-center gap-1">
          <h1 className="m-0 min-w-0 text-[length:var(--exits-text-2xl)] font-bold tracking-tight">
            {t("people.title")}
          </h1>
          <Button
            ref={infoButtonRef}
            type="button"
            intent="info"
            appearance="ghost"
            size="icon"
            className="shrink-0"
            aria-label={t("people.info.open")}
            aria-expanded={infoOpen}
            aria-controls="people-info-popover"
            onClick={() => setInfoOpen((open) => !open)}
          >
            <Info className="size-5" aria-hidden="true" />
          </Button>
        </div>
        {infoOpen ? <PeopleInfoPopover anchorRef={infoButtonRef} /> : null}
      </header>

      {isRefreshing ? <BackgroundRefreshIndicator active label={t("loading.updating")} /> : null}
      {isInitialLoading ? <PageSkeleton label={t("loading.label")} /> : null}
      {!isInitialLoading ? (
        <>
      <Card className="people-add-card flex flex-col gap-3" data-testid="people-add-panel">
        <button
          type="button"
          className={cn(
            "flex w-full items-center gap-3 rounded-[var(--exits-radius-md)] border-0 bg-transparent p-0 text-left text-inherit",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
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
          <span className="min-w-0 flex-1 text-[length:var(--exits-text-lg)] font-semibold">
            {t("people.newTitle")}
          </span>
          <ChevronDown
            className={cn(
              "size-4 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)]",
              addOpen && "rotate-180",
            )}
            aria-hidden="true"
          />
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

      <PeopleListSection rows={rows} summary={summary} />
        </>
      ) : null}
    </section>
  );
}
