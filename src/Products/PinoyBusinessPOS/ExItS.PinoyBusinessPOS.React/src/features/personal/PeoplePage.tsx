import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageSkeleton } from "@/components/exits/loading/PageSkeleton";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { AddPersonDialog } from "@/features/personal/AddPersonDialog";
import { parsePersonCreateKind, type PersonCreateKind } from "@/features/personal/PersonFormPage";
import {
  usePersonalConnectionRequestsQuery,
  usePersonalContactsQuery,
  usePersonalUtangSummariesQuery,
} from "@/features/personal/people-queries";
import { PeopleListSection } from "@/features/personal/PeopleListSection";
import { buildPeopleRows } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";

export function PeoplePage() {
  const { t } = useI18n();
  const [searchParams, setSearchParams] = useSearchParams();
  const addFromUrl = searchParams.get("add") === "1";
  const urlKind = parsePersonCreateKind(searchParams.get("kind"));
  const linkPublicId = searchParams.get("linkPublicId");

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

  function openAddPanel(kind: PersonCreateKind) {
    setAddOpen(true);
    const next = new URLSearchParams(searchParams);
    next.set("add", "1");
    next.set("kind", kind);
    if (kind !== "exits") {
      next.delete("linkPublicId");
    }
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
      {isRefreshing ? <BackgroundRefreshIndicator active label={t("loading.updating")} /> : null}
      {isInitialLoading ? <PageSkeleton label={t("loading.label")} /> : null}
      {!isInitialLoading ? (
        <>
          <AddPersonDialog
            open={addOpen}
            initialKind={urlKind}
            linkPublicId={linkPublicId}
            onOpenChange={(next) => {
              if (!next) {
                closeAddPanel();
              }
            }}
            onSwitchToExits={() => openAddPanel("exits")}
            onSwitchToLocal={() => openAddPanel("walkin")}
          />

          <PeopleListSection
            rows={rows}
            onAddLocal={() => openAddPanel("walkin")}
            onAddExits={() => openAddPanel("exits")}
          />
        </>
      ) : null}
    </section>
  );
}
