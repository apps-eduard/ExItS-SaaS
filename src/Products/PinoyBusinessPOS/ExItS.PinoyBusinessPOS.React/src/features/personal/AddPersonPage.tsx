import { useState } from "react";

import { Link, useNavigate } from "react-router-dom";

import { ApiClientError } from "@/api/http";

import { PageHeader } from "@/components/exits/PageHeader";

import { Button } from "@/components/ui/button";

import { Card } from "@/components/ui/card";

import {

  useCreateContactMutation,

  useResolvePublicUserMutation,

} from "@/features/personal/people-queries";

import { useI18n } from "@/i18n/I18nProvider";

import type { ResolvedPublicUserDto } from "@/api/platform/personal-types";



export function AddPersonPage() {

  const { t } = useI18n();

  const navigate = useNavigate();

  const [exitsId, setExitsId] = useState("");

  const [qrMode, setQrMode] = useState(false);

  const [resolved, setResolved] = useState<ResolvedPublicUserDto | null>(null);

  const [error, setError] = useState<string | null>(null);

  const resolveMutation = useResolvePublicUserMutation();

  const createMutation = useCreateContactMutation();



  async function onFind() {

    setError(null);

    setResolved(null);

    const value = exitsId.trim();

    if (!value) {

      setError(t("people.add.requiredId"));

      return;

    }

    try {

      const result = await resolveMutation.mutateAsync(value);

      if (result.isSelf) {

        setError(t("people.add.cannotAddSelf"));

        return;

      }

      setResolved(result);

    } catch (err) {

      if (err instanceof ApiClientError && (err.status === 404 || err.status === 400)) {

        setError(t("people.add.notFound"));

        return;

      }

      setError(err instanceof Error ? err.message : t("people.add.notFound"));

    }

  }



  async function onConfirmAdd() {

    if (!resolved) {

      return;

    }

    setError(null);

    try {

      const contact = await createMutation.mutateAsync({

        displayName: resolved.displayName,

        phone: null,

        email: null,

        resolvedUserIdentityId: resolved.userIdentityId,

        resolvedPublicUserId: resolved.publicUserId,

      });

      void navigate(`/personal/people/${contact.id}`, { replace: true });

    } catch (err) {

      setError(err instanceof Error ? err.message : t("error.body"));

    }

  }



  return (

    <section className="mx-auto flex w-full max-w-lg flex-col gap-4">

      <PageHeader title={t("people.howToAdd.withId")} subtitle={t("people.add.lede")} />



      <div className="flex flex-col gap-3">

        <Button type="button" variant="outline" onClick={() => setQrMode((value) => !value)}>

          {t("people.add.scanQr")}

        </Button>

        {qrMode ? (

          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("people.add.scanHint")}</p>

        ) : null}



        <label className="flex flex-col gap-1">

          <span className="font-semibold">{t("people.add.exitsId")}</span>

          <input

            value={exitsId}

            onChange={(event) => setExitsId(event.target.value)}

            placeholder={t("people.add.exitsIdPlaceholder")}

            autoComplete="off"

            spellCheck={false}

            className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"

          />

        </label>



        <Button

          type="button"

          onClick={() => void onFind()}

          disabled={resolveMutation.isPending || !exitsId.trim()}

        >

          {resolveMutation.isPending ? t("loading.label") : t("people.add.find")}

        </Button>

      </div>



      {error ? (

        <p className="m-0 rounded-[var(--exits-radius-md)] bg-[var(--exits-danger-bg)] px-3 py-2 text-destructive" role="alert">

          {error}

        </p>

      ) : null}



      {resolved ? (

        <Card className="flex flex-col gap-3" data-testid="identity-confirmation">

          <div>

            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">

              {t("people.add.identityFound")}

            </h2>

            <p className="m-0 mt-2 font-semibold">{resolved.displayName}</p>

            <p className="m-0 text-muted">{resolved.publicUserId}</p>

            {resolved.maskedEmail ? (

              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{resolved.maskedEmail}</p>

            ) : null}

          </div>

          <div className="flex flex-wrap gap-2">

            <Button type="button" variant="outline" onClick={() => setResolved(null)}>

              {t("people.add.cancel")}

            </Button>

            <Button

              type="button"

              onClick={() => void onConfirmAdd()}

              disabled={createMutation.isPending}

            >

              {createMutation.isPending ? t("loading.label") : t("people.add.confirm")}

            </Button>

          </div>

        </Card>

      ) : null}



      <Button asChild variant="ghost">

        <Link to="/personal/people">{t("shell.back")}</Link>

      </Button>

    </section>

  );

}

