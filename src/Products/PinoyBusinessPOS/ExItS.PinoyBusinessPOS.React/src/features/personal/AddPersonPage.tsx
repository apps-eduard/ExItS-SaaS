import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  useCreateContactMutation,
  usePersonalContactsQuery,
  useResolvePublicUserMutation,
} from "@/features/personal/people-queries";
import { useI18n } from "@/i18n/I18nProvider";
import type { PersonalContactDto, ResolvedPublicUserDto } from "@/api/platform/personal-types";
import { PlatformApiError } from "@/api/platform/platform-http";

function isPublicUserNotFound(error: unknown): boolean {
  if (!(error instanceof PlatformApiError)) {
    return false;
  }
  if (error.status === 404) {
    return true;
  }
  const code = error.errorCode ?? "";
  return (
    code === "application.user.not_found" ||
    code === "platform.public_user_id.invalid"
  );
}

function isAlreadyAddedConflict(error: unknown): boolean {
  if (!(error instanceof PlatformApiError)) {
    return false;
  }
  const code = error.errorCode ?? "";
  return (
    code === "application.personal.contact.identity.conflict" ||
    // Legacy mapping before dedicated identity conflict code.
    (code === "application.personal.contact.email.conflict" &&
      /exits identity|already (exists|in your people)/i.test(error.message))
  );
}

function findExistingContact(
  contacts: PersonalContactDto[] | undefined,
  resolved: ResolvedPublicUserDto,
): PersonalContactDto | null {
  if (!contacts?.length) {
    return null;
  }
  const publicId = resolved.publicUserId.trim().toUpperCase();
  return (
    contacts.find((contact) => {
      if (contact.resolvedUserIdentityId === resolved.userIdentityId) {
        return true;
      }
      if (contact.linkedUserIdentityId === resolved.userIdentityId) {
        return true;
      }
      const contactPublic = contact.resolvedPublicUserId?.trim().toUpperCase() ?? "";
      return contactPublic.length > 0 && contactPublic === publicId;
    }) ?? null
  );
}

export function AddPersonPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [exitsId, setExitsId] = useState("");
  const [qrMode, setQrMode] = useState(false);
  const [resolved, setResolved] = useState<ResolvedPublicUserDto | null>(null);
  const [existingContact, setExistingContact] = useState<PersonalContactDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const contactsQuery = usePersonalContactsQuery();
  const resolveMutation = useResolvePublicUserMutation();
  const createMutation = useCreateContactMutation();

  async function onFind() {
    setError(null);
    setResolved(null);
    setExistingContact(null);
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

      const contacts =
        contactsQuery.data ??
        (await contactsQuery.refetch().then((response) => response.data ?? []));
      const existing = findExistingContact(contacts, result);
      if (existing) {
        setExistingContact(existing);
        setResolved(result);
        setError(
          t("people.add.alreadyAdded").replace(
            "{name}",
            existing.displayName || result.displayName,
          ),
        );
        return;
      }

      setResolved(result);
    } catch (err) {
      if (isPublicUserNotFound(err)) {
        setError(t("people.add.notFound"));
        return;
      }
      setError(err instanceof Error ? err.message : t("people.add.notFound"));
    }
  }

  async function onConfirmAdd() {
    if (!resolved || existingContact) {
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
      if (isAlreadyAddedConflict(err)) {
        const contacts =
          contactsQuery.data ??
          (await contactsQuery.refetch().then((response) => response.data ?? []));
        const existing = findExistingContact(contacts, resolved);
        if (existing) {
          setExistingContact(existing);
        }
        setError(
          t("people.add.alreadyAdded").replace(
            "{name}",
            existing?.displayName || resolved.displayName,
          ),
        );
        return;
      }
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
        <p
          className="m-0 rounded-[var(--exits-radius-md)] bg-[var(--exits-danger-bg)] px-3 py-2 text-destructive"
          role="alert"
          data-testid="people-add-error"
        >
          {error}
        </p>
      ) : null}

      {resolved ? (
        <Card className="flex flex-col gap-3" data-testid="identity-confirmation">
          <div>
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {existingContact
                ? t("people.add.alreadyAddedTitle")
                : t("people.add.identityFound")}
            </h2>
            <p className="m-0 mt-2 font-semibold">{resolved.displayName}</p>
            <p className="m-0 text-muted">{resolved.publicUserId}</p>
            {resolved.maskedEmail ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{resolved.maskedEmail}</p>
            ) : null}
          </div>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setResolved(null);
                setExistingContact(null);
                setError(null);
              }}
            >
              {t("people.add.cancel")}
            </Button>
            {existingContact ? (
              <Button asChild data-testid="people-add-open-existing">
                <Link to={`/personal/people/${existingContact.id}`}>
                  {t("people.add.openExisting")}
                </Link>
              </Button>
            ) : (
              <Button
                type="button"
                onClick={() => void onConfirmAdd()}
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? t("loading.label") : t("people.add.confirm")}
              </Button>
            )}
          </div>
        </Card>
      ) : null}

      <Button asChild variant="ghost">
        <Link to="/personal/people">{t("shell.back")}</Link>
      </Button>
    </section>
  );
}
