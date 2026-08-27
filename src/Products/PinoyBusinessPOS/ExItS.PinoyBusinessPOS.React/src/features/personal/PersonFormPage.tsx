import { useCallback, useState } from "react";
import { IdCard, Loader2, Save, UserRound } from "lucide-react";
import { Link, Navigate, useNavigate, useSearchParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PersonalIdentityResolvePanel } from "@/features/personal/PersonalIdentityResolvePanel";
import {
  findExistingContact,
  isAlreadyAddedConflict,
  isPublicUserNotFound,
} from "@/features/personal/person-form-helpers";
import {
  useCreateContactMutation,
  usePersonalContactsQuery,
  useResolvePublicUserMutation,
} from "@/features/personal/people-queries";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import type { PersonalContactDto, ResolvedPublicUserDto } from "@/api/platform/personal-types";

export type PersonCreateKind = "walkin" | "exits";

export function parsePersonCreateKind(value: string | null): PersonCreateKind | null {
  if (value === "walkin" || value === "exits") {
    return value;
  }
  return null;
}

type PersonCreateFormProps = {
  embedded?: boolean;
  initialKind?: PersonCreateKind | null;
  linkPublicId?: string | null;
  onCancel?: () => void;
};

export function PersonCreateForm({
  embedded = false,
  initialKind = null,
  linkPublicId = null,
  onCancel,
}: PersonCreateFormProps) {
  const { t } = useI18n();
  const navigate = useNavigate();

  const [createKind, setCreateKind] = useState<PersonCreateKind | null>(() =>
    initialKind ?? (linkPublicId?.trim() ? "exits" : null),
  );
  const [displayName, setDisplayName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [resolved, setResolved] = useState<ResolvedPublicUserDto | null>(null);
  const [existingContact, setExistingContact] = useState<PersonalContactDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [resolving, setResolving] = useState(false);

  const contactsQuery = usePersonalContactsQuery();
  const resolveMutation = useResolvePublicUserMutation();
  const createMutation = useCreateContactMutation();

  const handleResolve = useCallback(
    async (subjectOrPayload: string) => {
      setError(null);
      setResolved(null);
      setExistingContact(null);
      setResolving(true);
      try {
        const result = await resolveMutation.mutateAsync(subjectOrPayload);
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
        if (!displayName.trim() && result.displayName.trim()) {
          setDisplayName(result.displayName.trim());
        }
      } catch (err) {
        if (isPublicUserNotFound(err)) {
          setError(t("people.add.notFound"));
          return;
        }
        setError(err instanceof Error ? err.message : t("people.add.notFound"));
      } finally {
        setResolving(false);
      }
    },
    [contactsQuery, displayName, resolveMutation, t],
  );

  function clearResolved() {
    setResolved(null);
    setExistingContact(null);
    setError(null);
  }

  function resetForm() {
    setCreateKind(initialKind ?? (linkPublicId?.trim() ? "exits" : null));
    clearResolved();
    setDisplayName("");
    setPhone("");
    setEmail("");
    setError(null);
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);

    if (createKind === "exits") {
      if (!resolved || existingContact) {
        setError(t("people.add.requiredId"));
        return;
      }
      setSaving(true);
      try {
        const contact = await createMutation.mutateAsync({
          displayName: displayName.trim() || resolved.displayName,
          phone: phone.trim() || null,
          email: email.trim() || null,
          resolvedUserIdentityId: resolved.userIdentityId,
          resolvedPublicUserId: resolved.publicUserId,
        });
        void navigate(`/personal/people/${contact.id}`, { replace: true });
      } catch (err) {
        if (isAlreadyAddedConflict(err)) {
          const contacts =
            contactsQuery.data ??
            (await contactsQuery.refetch().then((response) => response.data ?? []));
          const existing = resolved ? findExistingContact(contacts, resolved) : null;
          if (existing) {
            setExistingContact(existing);
          }
          setError(
            t("people.add.alreadyAdded").replace(
              "{name}",
              existing?.displayName || resolved?.displayName || "",
            ),
          );
          return;
        }
        setError(err instanceof Error ? err.message : t("error.body"));
      } finally {
        setSaving(false);
      }
      return;
    }

    const name = displayName.trim();
    if (!name) {
      setError(t("people.localAdd.nameRequired"));
      return;
    }

    setSaving(true);
    try {
      const contact = await createMutation.mutateAsync({
        displayName: name,
        phone: phone.trim() || null,
        email: email.trim() || null,
      });
      void navigate(`/personal/people/${contact.id}`, { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSaving(false);
    }
  }

  const formLede =
    createKind === "exits"
      ? t("people.formLedeExits")
      : createKind === "walkin"
        ? t("people.formLedeWalkIn")
        : t("people.createKindLede");

  return (
    <form
      className={cn(
        "person-form-page flex min-w-0 flex-col gap-3",
        !embedded && "exits-page mx-auto w-full max-w-3xl",
      )}
      data-testid="person-form-page"
      onSubmit={(event) => void onSubmit(event)}
    >
      {!embedded ? (
        <header className="flex min-w-0 flex-col gap-1">
          <h2 className="m-0 text-[length:var(--exits-text-xl)] font-bold">{t("people.newTitle")}</h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{formLede}</p>
        </header>
      ) : (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{formLede}</p>
      )}

      {error ? (
        <div className="exits-alert exits-alert--error" data-testid="people-add-error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{error}</p>
          {existingContact ? (
            <p className="m-0 mt-2">
              <Link
                to={`/personal/people/${existingContact.id}`}
                className="font-semibold text-primary"
                data-testid="people-add-open-existing"
              >
                {t("people.add.openExisting")}
              </Link>
            </p>
          ) : null}
        </div>
      ) : null}

      {createKind === null ? (
        <section
          className="catalog-form-section exits-animate-panel customer-create-kind"
          data-testid="person-create-kind"
        >
          <h2 className="catalog-form-section__title">{t("people.createKindTitle")}</h2>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
            {t("people.createKindLede")}
          </p>
          <div
            className="customer-create-kind__grid"
            role="group"
            aria-label={t("people.createKindTitle")}
          >
            <button
              type="button"
              className="customer-create-kind__card"
              data-testid="person-create-kind-walkin"
              onClick={() => {
                setCreateKind("walkin");
                clearResolved();
              }}
            >
              <span className="customer-create-kind__icon" aria-hidden>
                <UserRound className="size-5" />
              </span>
              <span className="customer-create-kind__label">{t("people.createKindWalkIn")}</span>
              <span className="customer-create-kind__hint">{t("people.createKindWalkInHint")}</span>
            </button>
            <button
              type="button"
              className="customer-create-kind__card"
              data-testid="person-create-kind-exits"
              onClick={() => setCreateKind("exits")}
            >
              <span className="customer-create-kind__icon" aria-hidden>
                <IdCard className="size-5" />
              </span>
              <span className="customer-create-kind__label">{t("people.createKindExits")}</span>
              <span className="customer-create-kind__hint">{t("people.createKindExitsHint")}</span>
            </button>
          </div>
        </section>
      ) : null}

      {createKind !== null ? (
        <>
          <div className="customer-create-kind__chosen exits-animate-toolbar">
            <p className="m-0 min-w-0 text-[length:var(--exits-text-sm)]">
              <span className="font-semibold">
                {createKind === "exits"
                  ? t("people.createKindExits")
                  : t("people.createKindWalkIn")}
              </span>
            </p>
            <Button
              type="button"
              variant="ghost"
              className="min-h-9 shrink-0"
              data-testid="person-create-kind-change"
              disabled={saving || resolving || Boolean(linkPublicId?.trim())}
              onClick={() => {
                setCreateKind(null);
                clearResolved();
                setDisplayName("");
                setPhone("");
                setEmail("");
              }}
            >
              {t("people.createKindChange")}
            </Button>
          </div>

          {createKind === "exits" ? (
            <PersonalIdentityResolvePanel
              disabled={saving}
              busy={resolving}
              initialSubject={linkPublicId}
              resolved={resolved}
              existingContact={existingContact}
              onResolve={(value) => void handleResolve(value)}
              onClear={clearResolved}
            />
          ) : null}

          {(createKind === "walkin" || (createKind === "exits" && resolved && !existingContact)) ? (
            <>
              <section className="catalog-form-section exits-animate-panel">
                <h2 className="catalog-form-section__title">{t("people.sectionBasics")}</h2>
                <div className="catalog-form-section__grid">
                  <Input
                    label={t("people.displayName")}
                    id="person-display-name"
                    name="personDisplayName"
                    data-testid="person-display-name"
                    autoComplete="name"
                    value={displayName}
                    disabled={saving}
                    onChange={(event) => setDisplayName(event.target.value)}
                  />
                </div>
              </section>

              <section className="catalog-form-section exits-animate-panel">
                <h2 className="catalog-form-section__title">{t("people.sectionDetails")}</h2>
                <div className="catalog-form-section__grid">
                  <Input
                    label={t("people.localAdd.phone")}
                    id="person-phone"
                    name="personPhone"
                    data-testid="person-phone"
                    inputMode="tel"
                    autoComplete="tel"
                    value={phone}
                    disabled={saving}
                    onChange={(event) => setPhone(event.target.value)}
                  />
                  <Input
                    label={t("people.localAdd.email")}
                    id="person-email"
                    name="personEmail"
                    data-testid="person-email"
                    type="email"
                    inputMode="email"
                    autoComplete="email"
                    value={email}
                    disabled={saving}
                    onChange={(event) => setEmail(event.target.value)}
                  />
                </div>
              </section>
            </>
          ) : null}

          {!existingContact ? (
            <div className={cn("catalog-form-actions", embedded && "person-form-actions--embedded")}>
              <div className="catalog-form-actions__primary flex flex-wrap gap-2">
                {embedded && onCancel ? (
                  <Button
                    type="button"
                    variant="outline"
                    className="min-h-11 flex-1 sm:flex-none"
                    data-testid="person-add-cancel"
                    disabled={saving || resolving}
                    onClick={() => {
                      resetForm();
                      onCancel();
                    }}
                  >
                    {t("people.add.cancel")}
                  </Button>
                ) : null}
                <Button
                  type="submit"
                  className={cn("catalog-form-actions__save", embedded && "flex-1 sm:min-w-[12rem]")}
                  data-testid="person-save"
                  disabled={saving || resolving || (createKind === "exits" && !resolved)}
                >
                  {saving ? (
                    <>
                      <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                      {t("people.saving")}
                    </>
                  ) : (
                    <>
                      <Save className="size-4 shrink-0" aria-hidden />
                      {t("people.save")}
                    </>
                  )}
                </Button>
              </div>
            </div>
          ) : null}
        </>
      ) : null}
    </form>
  );
}

/** @deprecated Add person is inline on PeoplePage — redirects with ?add=1 */
export function PersonCreatePage() {
  const [searchParams] = useSearchParams();
  const params = new URLSearchParams({ add: "1" });
  const kind = searchParams.get("kind");
  const linkPublicId = searchParams.get("linkPublicId");
  if (kind) {
    params.set("kind", kind);
  }
  if (linkPublicId) {
    params.set("linkPublicId", linkPublicId);
  }
  return <Navigate to={`/personal/people?${params.toString()}`} replace />;
}
