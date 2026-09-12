import { useEffect, useState } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { CircleCheck, Contact, IdCard, Loader2, ArrowLeft, Save, UserRound, Users } from "lucide-react";
import {
  createBusinessCustomerWithPersonalLink,
  evaluateCustomerLinkEligibility,
  type CustomerLinkEligibilityDto,
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
import { Notice } from "@/components/exits/Notice";
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
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

type Mode = "create" | "edit";
/** Create path: walk-in (no ExItS ID) vs Personal ExItS link. */
type CreateKind = "walkin" | "exits";

function composeCustomerAddress(parts: {
  addressLine1: string;
  cityMunicipality: string;
  province: string;
  postalCode: string;
}): string | null {
  const composed = [
    parts.addressLine1.trim(),
    parts.cityMunicipality.trim(),
    parts.province.trim(),
    parts.postalCode.trim(),
  ].filter(Boolean);
  return composed.length > 0 ? composed.join(", ") : null;
}

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
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const lockedToExits = Boolean(linkPublicId?.trim());

  const [displayName, setDisplayName] = useState("");
  const [mobileNumber, setMobileNumber] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [cityMunicipality, setCityMunicipality] = useState("");
  const [province, setProvince] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [notes, setNotes] = useState("");
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedIdentity, setSelectedIdentity] = useState<SelectedPersonalIdentity | null>(null);
  const [foundIdentity, setFoundIdentity] = useState<ResolvedPublicUserDto | null>(null);
  const [existingContact, setExistingContact] = useState<CheckoutCustomerSearchItem | null>(null);
  const [checkingExisting, setCheckingExisting] = useState(false);
  const [linkEligibility, setLinkEligibility] = useState<CustomerLinkEligibilityDto | null>(null);
  const [eligibilityLoading, setEligibilityLoading] = useState(false);
  const [eligibilityFailed, setEligibilityFailed] = useState(false);
  const [createKind, setCreateKind] = useState<CreateKind>(() =>
    linkPublicId?.trim() ? "exits" : "walkin",
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
    // Backend stores a single address string; keep prior text in street line on edit.
    setAddressLine1(existing.data.address ?? "");
    setCityMunicipality("");
    setProvince("");
    setPostalCode("");
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
        setAddressLine1(cachedCustomer.address ?? "");
        setCityMunicipality("");
        setProvince("");
        setPostalCode("");
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
      address: composeCustomerAddress({
        addressLine1,
        cityMunicipality,
        province,
        postalCode,
      }),
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
      address: composeCustomerAddress({
        addressLine1,
        cityMunicipality,
        province,
        postalCode,
      }),
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
          if (!selectedIdentity || !linkEligible) {
            setError(
              eligibilityFailed
                ? t("customers.linkElig.failed")
                : t("customers.personalLink.selectRequired"),
            );
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
        address: composeCustomerAddress({
          addressLine1,
          cityMunicipality,
          province,
          postalCode,
        }),
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

  const linkEligible =
    linkEligibility?.status === "Eligible" && !eligibilityLoading && !eligibilityFailed;

  const showCustomerInfo =
    mode === "edit" ||
    createKind === "walkin" ||
    (Boolean(foundIdentity) && !existingContact && linkEligible) ||
    (Boolean(selectedIdentity) && !existingContact && linkEligible);

  const showSave =
    mode === "edit" ||
    createKind === "walkin" ||
    (linkEligible && (Boolean(foundIdentity) || Boolean(selectedIdentity)) && !existingContact);

  const primarySaveLabel =
    mode === "create" && createKind === "exits" && foundIdentity && linkEligible
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
    setLinkEligibility(null);
    setEligibilityLoading(false);
    setEligibilityFailed(false);
    setError(null);
  }

  function eligibilityMessage(status: string): string {
    switch (status) {
      case "OwnerOfOrganization":
        return t("customers.linkElig.ownerSelf");
      case "OrganizationStaff":
        return t("customers.linkElig.organizationStaff");
      case "AlreadyLinked":
        return t("customers.linkElig.alreadyLinked");
      case "PendingInvitation":
        return t("customers.linkElig.pendingInvitation");
      case "BlockedOrUnavailable":
      case "InvalidTarget":
        return t("customers.linkElig.unavailable");
      default:
        return t("customers.linkElig.unavailable");
    }
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
          mode === "edit" && customerId ? `/customers/${customerId}` : "/customers/new"
        }
        backLabel={
          mode === "edit" && customerId
            ? t("customers.backDetail")
            : t("customers.add")
        }
        backTestId="page-header-back-customers"
      />
      {!online ? (
        <Notice tone="info" testId="customer-form-offline-notice">
          {t("offline.customerWillQueue")}
        </Notice>
      ) : null}
      {error ? (
        <Notice tone="danger" testId="customer-form-error">
          {error}
        </Notice>
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
                  setAddressLine1("");
                  setCityMunicipality("");
                  setProvince("");
                  setPostalCode("");
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
            setEligibilityLoading(true);
            setEligibilityFailed(false);
            setLinkEligibility(null);
            setExistingContact(null);
            setFoundIdentity(null);
            setSelectedIdentity(null);
            void (async () => {
              try {
                const existing = await findExistingCheckoutCustomerForPersonalId(
                  workspace,
                  user.publicUserId,
                );
                setExistingContact(existing);
                if (existing) {
                  setCheckingExisting(false);
                  setEligibilityLoading(false);
                  setLinkEligibility({
                    status: "AlreadyLinked",
                    message: t("customers.linkElig.alreadyLinked"),
                    publicUserId: user.publicUserId,
                    displayName: user.displayName,
                    userIdentityId: user.userIdentityId,
                    existingBusinessCustomerId: null,
                    existingPendingRequestId: null,
                  });
                  return;
                }

                const eligibility = await evaluateCustomerLinkEligibility(
                  workspace.organizationId,
                  { publicUserIdOrQrPayload: user.publicUserId },
                );
                setLinkEligibility(eligibility);
                setCheckingExisting(false);
                setEligibilityLoading(false);
                if (eligibility.status === "Eligible") {
                  applyFoundIdentity(user);
                } else {
                  setFoundIdentity(user);
                  setSelectedIdentity(null);
                }
              } catch {
                setCheckingExisting(false);
                setEligibilityLoading(false);
                setEligibilityFailed(true);
                setLinkEligibility(null);
                setExistingContact(null);
                setFoundIdentity(null);
                setSelectedIdentity(null);
              }
            })();
          }}
          onCleared={() => {
            resetExitsLookup();
            setDisplayName("");
            setMobileNumber("");
            setAddressLine1("");
            setCityMunicipality("");
            setProvince("");
            setPostalCode("");
            setNotes("");
          }}
        />
      ) : null}

      {mode === "create" &&
      createKind === "exits" &&
      (eligibilityLoading || checkingExisting) &&
      !existingContact ? (
        <p
          className="m-0 inline-flex items-center gap-2 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="customer-link-eligibility-loading"
        >
          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          {t("customers.linkElig.checking")}
        </p>
      ) : null}

      {mode === "create" && createKind === "exits" && eligibilityFailed ? (
        <div
          className="exits-alert exits-alert--error"
          data-testid="customer-link-eligibility-failed"
          role="alert"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)]">{t("customers.linkElig.failed")}</p>
        </div>
      ) : null}

      {mode === "create" &&
      createKind === "exits" &&
      linkEligibility &&
      linkEligibility.status !== "Eligible" &&
      !eligibilityLoading ? (
        <div
          className="exits-alert exits-alert--warning"
          data-testid={`customer-link-eligibility-${linkEligibility.status}`}
          role="alert"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {eligibilityMessage(linkEligibility.status)}
          </p>
          {existingContact ? (
            <Button asChild className="mt-2 w-full sm:w-auto">
              <Link
                to={`/customers/${existingContact.customerId}`}
                data-testid="customer-link-view-existing"
              >
                {t("customers.openExisting")}
              </Link>
            </Button>
          ) : null}
        </div>
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
          <label className="flex min-w-0 flex-col gap-1.5" htmlFor="customer-address">
            <span className="text-[length:var(--exits-text-sm)] font-semibold">
              {t("suppliers.addressLine1")}
            </span>
            <textarea
              id="customer-address"
              name="customerAddress"
              data-testid="customer-address"
              className="customer-form-notes min-h-[4.25rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
              rows={2}
              autoComplete="street-address"
              value={addressLine1}
              disabled={saving}
              onChange={(event) => setAddressLine1(event.target.value)}
            />
          </label>
          <label className="flex min-w-0 flex-col gap-1.5" htmlFor="customer-notes">
            <span className="text-[length:var(--exits-text-sm)] font-semibold">
              {t("customers.notes")}
            </span>
            <textarea
              id="customer-notes"
              name="customerNotes"
              data-testid="customer-notes"
              className="customer-form-notes min-h-[4.25rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
              rows={2}
              value={notes}
              disabled={saving}
              onChange={(event) => setNotes(event.target.value)}
            />
          </label>
          <div className="catalog-form-field--full grid gap-3 sm:grid-cols-3">
            <Input
              label={t("suppliers.city")}
              id="customer-city"
              name="customerCity"
              data-testid="customer-city"
              autoComplete="address-level2"
              value={cityMunicipality}
              disabled={saving}
              onChange={(event) => setCityMunicipality(event.target.value)}
            />
            <Input
              label={t("suppliers.province")}
              id="customer-province"
              name="customerProvince"
              data-testid="customer-province"
              autoComplete="address-level1"
              value={province}
              disabled={saving}
              onChange={(event) => setProvince(event.target.value)}
            />
            <Input
              label={t("suppliers.postalCode")}
              id="customer-postal"
              name="customerPostal"
              data-testid="customer-postal"
              autoComplete="postal-code"
              value={postalCode}
              disabled={saving}
              onChange={(event) => setPostalCode(event.target.value)}
            />
          </div>
        </div>
      </section>
      ) : null}

      {showSave ? (
      <div className="supplier-form-actions flex flex-wrap items-center justify-end gap-2 border-t border-border px-3 py-3 sm:px-4">
        {mode === "create" ? (
          <Button
            asChild
            variant="outline"
            className="supplier-form-cancel-btn w-fit"
            disabled={saving}
          >
            <Link to="/customers/new" data-testid="customer-back-options">
              <ArrowLeft className="size-4 shrink-0" aria-hidden />
              {t("customers.backCustomerChooser")}
            </Link>
          </Button>
        ) : null}
        <Button
          type="submit"
          className="supplier-form-save-btn w-fit"
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
      ) : null}
    </form>
  );
}
