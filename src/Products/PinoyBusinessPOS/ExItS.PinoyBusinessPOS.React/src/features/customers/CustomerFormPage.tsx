import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { CircleCheck, Contact, IdCard, Loader2, Save, UserRound, Users } from "lucide-react";
import {
  createBusinessCustomerWithPersonalLink,
  type ResolvedPublicUserDto,
} from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  createCustomer,
  getCustomer,
  updateCustomer,
  type CheckoutCustomerSearchItem,
} from "@/api/pos/pos-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { findExistingCheckoutCustomerForPersonalId } from "@/features/checkout/find-existing-checkout-customer";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  CustomerPersonalLinkPanel,
  type SelectedPersonalIdentity,
} from "@/features/customers/CustomerPersonalLinkPanel";
import { useI18n } from "@/i18n/I18nProvider";
import { getCachedCustomer } from "@/offline/customer-cache";
import { useOrganizationOfflineContext } from "@/offline/organization-offline-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

type Mode = "create" | "edit";
/** Create path: walk-in (no ExItS ID) vs Personal ExItS link. */
type CreateKind = "walkin" | "exits";

export function CustomerCreatePage() {
  return <CustomerFormPage mode="create" />;
}

export function CustomerEditPage() {
  return <CustomerFormPage mode="edit" />;
}

function CustomerFormPage({ mode }: { mode: Mode }) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { customerId } = useParams<{ customerId: string }>();
  const [searchParams] = useSearchParams();
  const linkPublicId = searchParams.get("linkPublicId");
  const returnTo = searchParams.get("returnTo");
  const { boundWorkspace } = useWorkspace();
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const lockedToExits = Boolean(linkPublicId?.trim());

  const [displayName, setDisplayName] = useState("");
  const [mobileNumber, setMobileNumber] = useState("");
  const [address, setAddress] = useState("");
  const [notes, setNotes] = useState("");
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedIdentity, setSelectedIdentity] = useState<SelectedPersonalIdentity | null>(null);
  const [foundIdentity, setFoundIdentity] = useState<ResolvedPublicUserDto | null>(null);
  const [existingContact, setExistingContact] = useState<CheckoutCustomerSearchItem | null>(null);
  const [checkingExisting, setCheckingExisting] = useState(false);
  const [createKind, setCreateKind] = useState<CreateKind>(() =>
    linkPublicId?.trim() ? "exits" : "walkin",
  );

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const existing = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: mode === "edit" && Boolean(workspace) && Boolean(customerId) && online,
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  useEffect(() => {
    if (!existing.data) {
      return;
    }
    setDisplayName(existing.data.displayName);
    setMobileNumber(existing.data.mobileNumber ?? "");
    setAddress(existing.data.address ?? "");
    setNotes(existing.data.notes ?? "");
    setExpectedUpdatedAtUtc(existing.data.updatedAtUtc);
  }, [existing.data]);

  useEffect(() => {
    if (mode !== "edit" || !customerId || !offlineContext || existing.data || online) {
      return;
    }
    let cancelled = false;
    void getCachedCustomer(offlineContext.db, offlineContext.scopeBinding, customerId).then(
      (cachedCustomer) => {
        if (cancelled) {
          return;
        }
        if (!cachedCustomer) {
          setError(t("offline.customerNotCached"));
          return;
        }
        setDisplayName(cachedCustomer.displayName);
        setMobileNumber(cachedCustomer.mobileNumber ?? "");
        setAddress(cachedCustomer.address ?? "");
        setNotes(cachedCustomer.notes ?? "");
        setExpectedUpdatedAtUtc(cachedCustomer.updatedAtUtc);
      },
    );
    return () => {
      cancelled = true;
    };
  }, [customerId, existing.data, mode, offlineContext, online, t]);

  useEffect(() => {
    if (mode !== "create") {
      return;
    }
    if (!online) {
      setCreateKind("walkin");
      setSelectedIdentity(null);
      setFoundIdentity(null);
      return;
    }
    if (linkPublicId?.trim()) {
      setCreateKind("exits");
    }
  }, [linkPublicId, mode, online]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (mode === "edit" && existing.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (mode === "edit" && existing.isError && online) {
    return <ErrorState title={t("error.title")} detail={(existing.error as Error).message} />;
  }

  async function createLocalOnly(name: string) {
    const created = await createCustomer(workspace!, {
      displayName: name,
      mobileNumber,
      address,
      notes,
      platformBusinessCustomerId: null,
    });
    if (returnTo && returnTo.startsWith("/") && !returnTo.startsWith("//")) {
      navigate(returnTo, { replace: true });
      return;
    }
    navigate(`/customers/${created.customerId}`, { replace: true });
  }

  async function createWithLinkRequest(name: string, identity: SelectedPersonalIdentity) {
    const existing = await findExistingCheckoutCustomerForPersonalId(
      workspace!,
      identity.publicUserId,
    );
    if (existing) {
      setExistingContact(existing);
      setError(t("customers.alreadyInContacts").replace("{name}", existing.displayName));
      return;
    }
    const taggedNotes = notes.trim()
      ? `${notes.trim()}\nexits-id:${identity.publicUserId}`
      : `exits-id:${identity.publicUserId}`;
    const linkResult = await createBusinessCustomerWithPersonalLink(workspace!.organizationId, {
      displayName: name,
      phone: mobileNumber.trim() || null,
      notes: taggedNotes,
      owningProductCode: "PinoyBusinessPOS",
      publicUserId: identity.publicUserId,
      targetUserIdentityId: identity.userIdentityId,
    });
    const created = await createCustomer(workspace!, {
      displayName: name,
      mobileNumber,
      address,
      notes: taggedNotes,
      platformBusinessCustomerId: linkResult.customerId,
      linkedPersonalPublicUserId: identity.publicUserId,
    });
    if (returnTo && returnTo.startsWith("/") && !returnTo.startsWith("//")) {
      navigate(returnTo, { replace: true });
      return;
    }
    navigate(`/customers/${created.customerId}?pendingLink=1`, { replace: true });
  }

  async function onSubmit() {
    if (!workspace) {
      return;
    }
    const name = displayName.trim();
    if (!name) {
      setError(t("customers.displayNameRequired"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      if (!online) {
        setError(t("connectivity.actionRequiresInternet"));
        return;
      }
      if (mode === "create") {
        if (createKind === "exits") {
          if (!selectedIdentity) {
            setError(t("customers.personalLink.selectRequired"));
            return;
          }
          await createWithLinkRequest(name, selectedIdentity);
          return;
        }
        await createLocalOnly(name);
        return;
      }
      const updated = await updateCustomer(workspace, customerId!, {
        displayName: name,
        mobileNumber,
        address,
        notes,
        expectedUpdatedAtUtc,
      });
      navigate(`/customers/${updated.customerId}`, { replace: true });
    } catch (err) {
      setError(
        err instanceof PlatformApiError || err instanceof PosApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : t("error.detail"),
      );
    } finally {
      setSaving(false);
    }
  }

  const showCustomerInfo =
    mode === "edit" ||
    createKind === "walkin" ||
    (Boolean(foundIdentity) && !existingContact) ||
    (Boolean(selectedIdentity) && !existingContact);

  const showSave =
    mode === "edit" ||
    createKind === "walkin" ||
    ((Boolean(foundIdentity) || Boolean(selectedIdentity)) && !existingContact);

  const primarySaveLabel =
    mode === "create" && createKind === "exits" && foundIdentity
      ? t("customers.saveAndSendLink")
      : t("customers.save");

  function fillFromFoundIdentity(user: ResolvedPublicUserDto) {
    if (user.displayName.trim()) {
      setDisplayName(user.displayName.trim());
    }
  }

  function applyFoundIdentity(user: ResolvedPublicUserDto) {
    setFoundIdentity(user);
    setSelectedIdentity({
      publicUserId: user.publicUserId,
      userIdentityId: user.userIdentityId,
      displayName: user.displayName.trim(),
      maskedEmail: user.maskedEmail ?? null,
    });
    fillFromFoundIdentity(user);
  }

  function resetExitsLookup() {
    setSelectedIdentity(null);
    setFoundIdentity(null);
    setExistingContact(null);
    setCheckingExisting(false);
    setError(null);
  }

  return (
    <form
      className="customer-form-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="customer-form-page"
      onSubmit={(event) => {
        event.preventDefault();
        void onSubmit();
      }}
    >
      <PageHeader
        title={mode === "create" ? t("customers.newTitle") : t("customers.editTitle")}
        description={
          mode === "create"
            ? createKind === "exits"
              ? t("customers.formLedeExits")
              : t("customers.formLedeWalkIn")
            : t("customers.formLede")
        }
        backTo={
          mode === "edit" && customerId ? `/customers/${customerId}` : pageBackNav.customers.to
        }
        backLabel={
          mode === "edit" && customerId
            ? t("customers.backDetail")
            : t(pageBackNav.customers.labelKey)
        }
        backTestId="page-header-back-customers"
      />
      {!online ? (
        <div className="exits-alert" data-testid="customer-form-offline-notice" role="status">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.customerWillQueue")}
          </p>
        </div>
      ) : null}
      {error ? (
        <div
          className="exits-alert exits-alert--error"
          data-testid="customer-form-error"
          role="alert"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)]">{error}</p>
        </div>
      ) : null}

      {mode === "create" ? (
        <section
          className="catalog-form-section exits-animate-panel customer-create-kind customer-create-kind--compact"
          data-testid="customer-create-kind"
        >
          <h2 className="catalog-form-section__title catalog-form-section__heading">
            <span className="catalog-form-section__icon" aria-hidden>
              <Users className="size-4" />
            </span>
            {t("customers.createKindTitle")}
          </h2>
          <div
            className="customer-create-kind__grid"
            role="group"
            aria-label={t("customers.createKindTitle")}
          >
            <button
              type="button"
              className="customer-create-kind__card"
              data-testid="customer-create-kind-walkin"
              aria-pressed={createKind === "walkin"}
              disabled={saving || !online || lockedToExits}
              onClick={() => {
                setCreateKind("walkin");
                resetExitsLookup();
              }}
            >
              <span className="customer-create-kind__header">
                <span className="customer-create-kind__icon" aria-hidden>
                  <UserRound className="size-4" />
                </span>
                <span className="customer-create-kind__label">{t("customers.createKindWalkIn")}</span>
              </span>
            </button>
            <button
              type="button"
              className="customer-create-kind__card"
              data-testid="customer-create-kind-exits"
              aria-pressed={createKind === "exits"}
              disabled={saving || !online}
              onClick={() => {
                if (createKind !== "exits") {
                  resetExitsLookup();
                  setDisplayName("");
                  setMobileNumber("");
                  setAddress("");
                  setNotes("");
                }
                setCreateKind("exits");
              }}
            >
              <span className="customer-create-kind__header">
                <span className="customer-create-kind__icon" aria-hidden>
                  <IdCard className="size-4" />
                </span>
                <span className="customer-create-kind__label">{t("customers.createKindExits")}</span>
              </span>
            </button>
          </div>
        </section>
      ) : null}

      {mode === "create" && online && workspace && createKind === "exits" ? (
        <CustomerPersonalLinkPanel
          disabled={saving}
          initialSubject={linkPublicId}
          existingMatch={
            existingContact
              ? {
                  customerId: existingContact.customerId,
                  displayName: existingContact.displayName,
                }
              : null
          }
          checkingExisting={checkingExisting}
          onResolved={(user) => {
            setCheckingExisting(true);
            setExistingContact(null);
            setFoundIdentity(null);
            setSelectedIdentity(null);
            void findExistingCheckoutCustomerForPersonalId(workspace, user.publicUserId)
              .then((existing) => {
                setCheckingExisting(false);
                setExistingContact(existing);
                if (existing) {
                  return;
                }
                applyFoundIdentity(user);
              })
              .catch(() => {
                setCheckingExisting(false);
                setExistingContact(null);
                applyFoundIdentity(user);
              });
          }}
          onCleared={() => {
            resetExitsLookup();
            setDisplayName("");
            setMobileNumber("");
            setAddress("");
            setNotes("");
          }}
        />
      ) : null}
      {mode === "create" && !online ? (
        <section
          className="catalog-form-section exits-animate-panel"
          data-testid="customer-personal-link-offline"
        >
          <h2 className="catalog-form-section__title catalog-form-section__heading">
            <span className="catalog-form-section__icon" aria-hidden>
              <IdCard className="size-4" />
            </span>
            {t("customers.personalLink.title")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.personalLink.requiresOnline")}
          </p>
        </section>
      ) : null}

      {showCustomerInfo ? (
      <section
        className="catalog-form-section exits-animate-panel"
        data-testid="customer-info-section"
      >
        <h2 className="catalog-form-section__title catalog-form-section__heading">
          <span className="catalog-form-section__icon" aria-hidden>
            <Contact className="size-4" />
          </span>
          {t("customers.sectionInfo")}
        </h2>
        {createKind === "exits" && foundIdentity ? (
          <div
            className="exits-alert exits-alert--success"
            data-testid="customer-exits-invite-hint"
            role="status"
          >
            <CircleCheck className="exits-alert__icon size-5 shrink-0 text-[var(--exits-success)]" aria-hidden />
            <p className="exits-alert__content m-0 text-[length:var(--exits-text-sm)]">
              {t("customers.personalLink.confirmHint").replace(
                "{name}",
                displayName.trim() || foundIdentity.displayName,
              )}
            </p>
          </div>
        ) : null}
        <div className="catalog-form-section__grid">
          <Input
            label={t("customers.displayName")}
            id="customer-display-name"
            name="customerDisplayName"
            data-testid="customer-display-name"
            autoComplete="name"
            value={displayName}
            disabled={saving}
            onChange={(event) => setDisplayName(event.target.value)}
          />
          {createKind === "exits" && foundIdentity ? (
            <>
              <Input
                label={t("customers.exItsIdLabel")}
                id="customer-exits-id"
                name="customerExitsId"
                data-testid="customer-exits-id"
                value={foundIdentity.publicUserId}
                readOnly
                className="bg-[var(--exits-surface-muted)]"
              />
              <Input
                label={t("customers.email")}
                id="customer-email"
                name="customerEmail"
                data-testid="customer-email"
                value={foundIdentity.maskedEmail?.trim() || t("customers.exItsIdNone")}
                readOnly
                className="bg-[var(--exits-surface-muted)]"
              />
            </>
          ) : null}
          <Input
            label={t("customers.mobile")}
            id="customer-mobile"
            name="customerMobile"
            data-testid="customer-mobile"
            inputMode="tel"
            autoComplete="tel"
            value={mobileNumber}
            disabled={saving}
            onChange={(event) => setMobileNumber(event.target.value)}
          />
          <Input
            label={t("customers.address")}
            id="customer-address"
            name="customerAddress"
            data-testid="customer-address"
            autoComplete="street-address"
            value={address}
            disabled={saving}
            onChange={(event) => setAddress(event.target.value)}
          />
          <label
            className="catalog-form-field--full flex min-w-0 flex-col gap-1.5"
            htmlFor="customer-notes"
          >
            <span className="text-[length:var(--exits-text-sm)] font-semibold">
              {t("customers.notes")}
            </span>
            <textarea
              id="customer-notes"
              name="customerNotes"
              data-testid="customer-notes"
              className="customer-form-notes min-h-24 w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
              value={notes}
              disabled={saving}
              onChange={(event) => setNotes(event.target.value)}
            />
          </label>
        </div>
      </section>
      ) : null}

      {showSave ? (
      <div className={cn("catalog-form-actions", "customer-form-actions")}>
        <div className="catalog-form-actions__primary flex flex-col gap-2 sm:flex-row">
          <Button
            type="submit"
            className="catalog-form-actions__save"
            data-testid="customer-save"
            disabled={saving || Boolean(existingContact)}
          >
            {saving ? (
              <>
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                {t("customers.saving")}
              </>
            ) : (
              <>
                <Save className="size-4 shrink-0" aria-hidden />
                {primarySaveLabel}
              </>
            )}
          </Button>
        </div>
      </div>
      ) : null}
    </form>
  );
}
