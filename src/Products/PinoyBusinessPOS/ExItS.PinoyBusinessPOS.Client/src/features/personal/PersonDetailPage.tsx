import { useMemo, useState } from "react";

import { Link, useParams } from "react-router-dom";

import { ApiClientError } from "@/api/http";

import { useSession } from "@/session/SessionProvider";

import { EmptyState } from "@/components/exits/EmptyState";

import { ErrorState } from "@/components/exits/ErrorState";

import { PageHeader } from "@/components/exits/PageHeader";

import { StatusChip } from "@/components/ui/badge";

import { Button } from "@/components/ui/button";

import { Card } from "@/components/ui/card";

import { LoadingState } from "@/components/ui/skeleton";

import {

  useBlockContactMutation,

  useCreateUtangMutation,

  usePersonalConnectionRequestsQuery,

  usePersonalContactsQuery,

  usePersonalUtangSummariesQuery,

  useRequestConnectionMutation,

  useRevokeConnectionMutation,

  useUnblockContactMutation,

  useUnlinkContactMutation,

} from "@/features/personal/people-queries";

import { deriveConnectionStatus, formatShortDate } from "@/features/personal/people-status";

import { useI18n } from "@/i18n/I18nProvider";



function formatMoney(amount: number, currencyCode: string): string {

  try {

    return new Intl.NumberFormat(undefined, {

      style: "currency",

      currency: currencyCode || "PHP",

      maximumFractionDigits: 2,

    }).format(amount);

  } catch {

    return `${currencyCode} ${amount.toFixed(2)}`;

  }

}



export function PersonDetailPage() {

  const { contactId = "" } = useParams();

  const { t } = useI18n();

  const { session } = useSession();

  const contactsQuery = usePersonalContactsQuery();

  const connectionsQuery = usePersonalConnectionRequestsQuery();

  const utangQuery = usePersonalUtangSummariesQuery();

  const requestConnection = useRequestConnectionMutation();

  const revokeConnection = useRevokeConnectionMutation();

  const unlinkContact = useUnlinkContactMutation();

  const blockContact = useBlockContactMutation();

  const unblockContact = useUnblockContactMutation();

  const createUtang = useCreateUtangMutation();

  const [amount, setAmount] = useState("1000");

  const [mode, setMode] = useState<"lent" | "borrowed" | null>(null);

  const [confirmUnlink, setConfirmUnlink] = useState(false);

  const [confirmBlock, setConfirmBlock] = useState(false);

  const [actionError, setActionError] = useState<string | null>(null);



  const contact = contactsQuery.data?.find((item) => item.id === contactId);

  const connections = connectionsQuery.data ?? [];

  const connection = contact ? deriveConnectionStatus(contact, connections) : null;

  const publicUserId = contact?.resolvedPublicUserId ?? undefined;



  const related = useMemo(() => {

    if (!contact || !utangQuery.data) {

      return [];

    }

    return [...utangQuery.data.lent, ...utangQuery.data.borrowed].filter(

      (rel) =>

        rel.creditorContactId === contact.id ||

        rel.debtorContactId === contact.id ||

        (contact.linkedUserIdentityId &&

          (rel.creditorUserIdentityId === contact.linkedUserIdentityId ||

            rel.debtorUserIdentityId === contact.linkedUserIdentityId)),

    );

  }, [contact, utangQuery.data]);



  const activeRel = related.find((rel) => rel.status.toLowerCase() === "active");



  async function submitUtang(kind: "lent" | "borrowed") {

    if (!contact || !session) {

      return;

    }

    setActionError(null);

    const parsed = Number(amount);

    if (!Number.isFinite(parsed) || parsed <= 0) {

      setActionError(t("people.detail.amountInvalid"));

      return;

    }



    const relationship =

      kind === "lent"

        ? {

            creditorUserIdentityId: session.userId,

            debtorContactId: contact.id,

            currencyCode: "PHP",

            initialLoanAmount: parsed,

          }

        : {

            debtorUserIdentityId: session.userId,

            creditorContactId: contact.id,

            currencyCode: "PHP",

            initialLoanAmount: parsed,

          };



    try {

      await createUtang.mutateAsync(relationship);

      setMode(null);

    } catch (err) {

      setActionError(err instanceof Error ? err.message : t("error.body"));

    }

  }



  function statusLabel(status: NonNullable<typeof connection>["status"]): string {

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



  function statusTone(

    status: NonNullable<typeof connection>["status"],

  ): "neutral" | "success" | "warning" | "info" {

    if (status === "connected") {

      return "success";

    }

    if (status === "request_pending") {

      return "warning";

    }

    if (status === "blocked") {

      return "warning";

    }

    return "neutral";

  }



  if (contactsQuery.isLoading || connectionsQuery.isLoading || utangQuery.isLoading) {

    return <LoadingState label={t("loading.label")} />;

  }



  const loadError = contactsQuery.error ?? connectionsQuery.error ?? utangQuery.error;

  if (loadError) {

    return (

      <ErrorState
        title={t("error.title")}
        detail={loadError instanceof ApiClientError ? loadError.message : t("error.body")}
        error={loadError}
      />

    );

  }



  if (!contact || !connection) {

    return (

      <EmptyState title={t("people.detail.notFoundTitle")} detail={t("people.detail.notFoundBody")} />

    );

  }



  return (

    <section className="mx-auto flex w-full max-w-lg flex-col gap-4">

      <PageHeader title={contact.displayName} />

      <div>

        <p className="m-0 text-muted">

          {publicUserId ? publicUserId : t("people.localContact")}

        </p>

        <div className="mt-2">

          <StatusChip tone={statusTone(connection.status)}>{statusLabel(connection.status)}</StatusChip>

        </div>

      </div>



      {activeRel ? (

        <Card>

          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold uppercase tracking-wide">

            {t("people.detail.utang")}

          </h2>

          <p className="m-0 mt-2 text-muted">

            {activeRel.perspective} · {formatMoney(activeRel.currentBalance, activeRel.currencyCode)}

          </p>

        </Card>

      ) : null}



      {connection.status !== "request_pending" && connection.status !== "blocked" ? (

        <div className="flex flex-col gap-2">

          <Button type="button" onClick={() => setMode("lent")}>

            {t("people.detail.iLent")}

          </Button>

          <Button type="button" variant="outline" onClick={() => setMode("borrowed")}>

            {t("people.detail.iBorrowed")}

          </Button>

        </div>

      ) : null}



      {mode ? (

        <Card className="flex flex-col gap-3">

          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">

            {mode === "lent" ? t("people.detail.iLent") : t("people.detail.iBorrowed")}

          </h2>

          <label className="flex flex-col gap-1">

            <span className="font-semibold">{t("people.detail.amount")}</span>

            <input

              inputMode="decimal"

              value={amount}

              onChange={(event) => setAmount(event.target.value)}

              className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"

            />

          </label>

          <div className="flex flex-wrap gap-2">

            <Button type="button" variant="outline" onClick={() => setMode(null)}>

              {t("people.add.cancel")}

            </Button>

            <Button

              type="button"

              disabled={createUtang.isPending}

              onClick={() => void submitUtang(mode)}

            >

              {createUtang.isPending ? t("loading.label") : t("people.detail.confirmUtang")}

            </Button>

          </div>

        </Card>

      ) : null}



      <Card className="flex flex-col gap-3">

        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold uppercase tracking-wide">

          {t("people.detail.connection")}

        </h2>



        {connection.status === "not_connected" ? (

          <>

            <p className="m-0 text-muted">{t("people.detail.connectionHelp")}</p>

            <Button

              type="button"

              disabled={requestConnection.isPending}

              onClick={() => {

                setActionError(null);

                void requestConnection.mutateAsync(contact.id).catch((err) => {

                  setActionError(err instanceof Error ? err.message : t("error.body"));

                });

              }}

            >

              {requestConnection.isPending ? t("loading.label") : t("people.detail.requestConnection")}

            </Button>

          </>

        ) : null}



        {connection.status === "request_pending" && connection.pendingConnectionRequest ? (

          <>

            <p className="m-0 text-muted">

              {t("people.detail.waitingBody").replace(

                "{name}",

                contact.displayName.split(" ")[0] ?? contact.displayName,

              )}

            </p>

            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">

              {t("people.detail.sentOn").replace(

                "{date}",

                formatShortDate(connection.pendingConnectionRequest.createdAtUtc),

              )}

            </p>

            <Button

              type="button"

              variant="outline"

              disabled={revokeConnection.isPending}

              onClick={() => {

                setActionError(null);

                void revokeConnection

                  .mutateAsync(connection.pendingConnectionRequest!.id)

                  .catch((err) => {

                    setActionError(err instanceof Error ? err.message : t("error.body"));

                  });

              }}

            >

              {t("people.detail.cancelRequest")}

            </Button>

          </>

        ) : null}



        {connection.status === "connected" ? (

          <>

            <p className="m-0 text-muted">

              {t("people.detail.connectedSince").replace(

                "{date}",

                formatShortDate(contact.connectedAtUtc ?? contact.createdAtUtc),

              )}

            </p>

            {!confirmUnlink ? (

              <Button type="button" variant="outline" onClick={() => setConfirmUnlink(true)}>

                {t("people.detail.unlink")}

              </Button>

            ) : (

              <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3">

                <p className="m-0 font-semibold">

                  {t("people.detail.unlinkConfirmTitle").replace("{name}", contact.displayName)}

                </p>

                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">

                  {t("people.detail.unlinkConfirmBody")}

                </p>

                <div className="flex flex-wrap gap-2">

                  <Button type="button" variant="outline" onClick={() => setConfirmUnlink(false)}>

                    {t("people.add.cancel")}

                  </Button>

                  <Button

                    type="button"

                    variant="destructive"

                    disabled={unlinkContact.isPending}

                    onClick={() => {

                      setActionError(null);

                      void unlinkContact

                        .mutateAsync(contact.id)

                        .then(() => setConfirmUnlink(false))

                        .catch((err) => {

                          setActionError(err instanceof Error ? err.message : t("error.body"));

                        });

                    }}

                  >

                    {t("people.detail.unlinkConfirmAction")}

                  </Button>

                </div>

              </div>

            )}

          </>

        ) : null}



        {connection.status === "blocked" ? (

          <>

            <p className="m-0 text-muted">{t("people.detail.blockedHelp")}</p>

            <Button

              type="button"

              disabled={unblockContact.isPending}

              onClick={() => {

                setActionError(null);

                void unblockContact.mutateAsync(contact.id).catch((err) => {

                  setActionError(err instanceof Error ? err.message : t("error.body"));

                });

              }}

            >

              {t("people.detail.unblock")}

            </Button>

          </>

        ) : null}

      </Card>



      {connection.status === "connected" ? (

        <Card className="flex flex-col gap-3">

          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold uppercase tracking-wide">

            {t("people.detail.safety")}

          </h2>

          {!confirmBlock ? (

            <Button type="button" variant="outline" onClick={() => setConfirmBlock(true)}>

              {t("people.detail.block")}

            </Button>

          ) : (

            <div className="flex flex-col gap-2">

              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">

                {t("people.detail.blockConfirmBody")}

              </p>

              <div className="flex flex-wrap gap-2">

                <Button type="button" variant="outline" onClick={() => setConfirmBlock(false)}>

                  {t("people.add.cancel")}

                </Button>

                <Button

                  type="button"

                  variant="destructive"

                  disabled={blockContact.isPending}

                  onClick={() => {

                    setActionError(null);

                    void blockContact

                      .mutateAsync(contact.id)

                      .then(() => setConfirmBlock(false))

                      .catch((err) => {

                        setActionError(err instanceof Error ? err.message : t("error.body"));

                      });

                  }}

                >

                  {t("people.detail.block")}

                </Button>

              </div>

            </div>

          )}

        </Card>

      ) : null}



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

