import { useMemo, useState } from "react";

import { Link } from "react-router-dom";

import { PlatformApiError } from "@/api/platform/platform-http";

import { EmptyState } from "@/components/exits/EmptyState";

import { ErrorState } from "@/components/exits/ErrorState";

import { PageHeader } from "@/components/exits/PageHeader";

import { StatusChip } from "@/components/ui/badge";

import { Button } from "@/components/ui/button";

import { Card } from "@/components/ui/card";

import { LoadingState } from "@/components/ui/skeleton";

import {

  useAcceptConnectionMutation,

  useDeclineConnectionMutation,

  usePersonalConnectionRequestsQuery,

  useRevokeConnectionMutation,

} from "@/features/personal/people-queries";

import { formatShortDate, isPendingConnectionRequest } from "@/features/personal/people-status";

import { useI18n } from "@/i18n/I18nProvider";

export function InvitationsPage() {
  const { t } = useI18n();

  const [actionError, setActionError] = useState<string | null>(null);

  const connectionsQuery = usePersonalConnectionRequestsQuery();

  const acceptMutation = useAcceptConnectionMutation();

  const declineMutation = useDeclineConnectionMutation();

  const revokeMutation = useRevokeConnectionMutation();



  const { received, sent } = useMemo(() => {

    const list = connectionsQuery.data ?? [];

    const pending = list.filter(isPendingConnectionRequest);

    return {

      received: pending.filter((item) => item.direction.toLowerCase() === "received"),

      sent: pending.filter((item) => item.direction.toLowerCase() === "sent"),

    };

  }, [connectionsQuery.data]);



  async function onAccept(requestId: string) {

    setActionError(null);

    try {

      await acceptMutation.mutateAsync(requestId);

    } catch (err) {

      setActionError(err instanceof Error ? err.message : t("error.body"));

    }

  }



  async function onDecline(requestId: string) {

    setActionError(null);

    try {

      await declineMutation.mutateAsync(requestId);

    } catch (err) {

      setActionError(err instanceof Error ? err.message : t("error.body"));

    }

  }



  if (connectionsQuery.isLoading) {

    return <LoadingState label={t("loading.label")} />;

  }



  if (connectionsQuery.error) {

    const err = connectionsQuery.error;

    return (

      <ErrorState
        title={t("error.title")}
        detail={err instanceof PlatformApiError ? err.message : t("error.body")}
        error={err}
      />

    );

  }



  return (

    <section className="flex flex-col gap-4">

      <PageHeader title={t("invitations.title")} />



      <div className="flex flex-col gap-3">

        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">

          {t("invitations.connectionRequests")}

        </h2>

        {received.length === 0 ? (

          <EmptyState title={t("invitations.emptyTitle")} detail={t("invitations.emptyBody")} />

        ) : (

          received.map((request) => (

            <Card key={request.id} className="flex flex-col gap-2">

              <p className="m-0 font-semibold">{request.requesterDisplayName}</p>

              {request.requesterPublicUserId ? (

                <p className="m-0 text-muted">{request.requesterPublicUserId}</p>

              ) : null}

              <p className="m-0 text-muted">{t("invitations.connectionRequestBody")}</p>

              <StatusChip tone="warning">{t("people.status.requestPending")}</StatusChip>

              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">

                {formatShortDate(request.createdAtUtc)}

              </p>

              <div className="flex flex-wrap gap-2">

                <Button

                  type="button"

                  variant="outline"

                  disabled={declineMutation.isPending}

                  onClick={() => void onDecline(request.id)}

                >

                  {t("invitations.decline")}

                </Button>

                <Button

                  type="button"

                  disabled={acceptMutation.isPending}

                  onClick={() => void onAccept(request.id)}

                >

                  {t("invitations.accept")}

                </Button>

              </div>

            </Card>

          ))

        )}

      </div>



      <div className="flex flex-col gap-3">

        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">{t("invitations.sent")}</h2>

        {sent.length === 0 ? (

          <p className="m-0 text-muted">{t("invitations.sentEmpty")}</p>

        ) : (

          sent.map((request) => (

            <Card key={request.id} className="flex flex-col gap-2">

              <p className="m-0 font-semibold">

                {request.targetPublicUserId ?? t("invitations.someone")}

              </p>

              <p className="m-0 text-muted">{t("invitations.waitingResponse")}</p>

              <Button

                type="button"

                variant="outline"

                disabled={revokeMutation.isPending}

                onClick={() => void revokeMutation.mutateAsync(request.id)}

              >

                {t("people.detail.cancelRequest")}

              </Button>

            </Card>

          ))

        )}

      </div>



      {actionError ? (

        <p className="m-0 text-destructive" role="alert">

          {actionError}

        </p>

      ) : null}



      <Button asChild variant="ghost">

        <Link to="/personal/people">{t("shell.back")}</Link>

      </Button>

    </section>

  );

}

