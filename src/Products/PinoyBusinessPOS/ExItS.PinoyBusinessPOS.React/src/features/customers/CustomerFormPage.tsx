import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { IdCard, Loader2, Save, UserRound } from "lucide-react";
import { createBusinessCustomerWithPersonalLink } from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { createCustomer, getCustomer, updateCustomer } from "@/api/pos/pos-customers-client";
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
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { getCachedCustomer } from "@/offline/customer-cache";
import {
  enqueueOfflineCustomerCreate,
  enqueueOfflineCustomerUpdate,
  OfflineCustomerRejectedError,
} from "@/offline/customer-offline";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
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
  const { refreshCounts } = useOfflineSync();

  const [displayName, setDisplayName] = useState("");
  const [mobileNumber, setMobileNumber] = useState("");
  const [address, setAddress] = useState("");
  const [notes, setNotes] = useState("");
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedIdentity, setSelectedIdentity] = useState<SelectedPersonalIdentity | null>(null);
  const [createKind, setCreateKind] = useState<CreateKind | null>(() =>
    linkPublicId?.trim() ? "exits" : null,
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
    });
    if (returnTo && returnTo.startsWith("/") && !returnTo.startsWith("//")) {
      navigate(returnTo, { replace: true });
      return;
    }
    navigate(`/customers/${created.customerId}?pendingLink=1`, { replace: true });
  }

  async function onSubmit(options?: { localOnly?: boolean }) {
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
        await saveOffline(name);
        return;
      }
      if (mode === "create") {
        const wantLink = createKind === "exits" && !options?.localOnly;
        if (wantLink) {
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
        err instanceof PlatformApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : t("error.detail"),
      );
    } finally {
      setSaving(false);
    }
  }

  async function saveOffline(name: string) {
    if (!offlineContext) {
      setError(t("offline.customerEnqueueFailed"));
      return;
    }
    const generated = createSecureMutationId();
    if (!generated.ok) {
      setError(t("offline.customerEnqueueFailed"));
      return;
    }

    try {
      if (mode === "create") {
        await enqueueOfflineCustomerCreate({
          ...offlineContext,
          customerId: generated.id,
          customer: { displayName: name, mobileNumber, address, notes },
        });
        await refreshCounts();
        navigate(`/customers/${generated.id}`, { replace: true });
        return;
      }
      await enqueueOfflineCustomerUpdate({
        ...offlineContext,
        customerId: customerId!,
        operationId: generated.id,
        customer: { displayName: name, mobileNumber, address, notes, expectedUpdatedAtUtc },
      });
      await refreshCounts();
      navigate(`/customers/${customerId}`, { replace: true });
    } catch (err) {
      setError(
        err instanceof OfflineCustomerRejectedError
          ? err.message
          : t("offline.customerEnqueueFailed"),
      );
    }
  }

  const primarySaveLabel =
    mode === "create" && createKind === "exits" && selectedIdentity
      ? t("customers.saveAndSendLink")
      : t("customers.save");

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
              : createKind === "walkin"
                ? t("customers.formLedeWalkIn")
                : t("customers.createKindLede")
            : t("customers.formLede")
        }
        backTo={
          mode === "edit" && customerId ? `/customers/${customerId}` : pageBackNav.customers.to
        }
        backLabel={
          mode === "edit" && customerId ? t("customers.backDetail") : t(pageBackNav.customers.labelKey)
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
        <div className="exits-alert exits-alert--error" data-testid="customer-form-error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{error}</p>
        </div>
      ) : null}

      {mode === "create" && online && createKind === null ? (
        <section
          className="catalog-form-section exits-animate-panel customer-create-kind"
          data-testid="customer-create-kind"
        >
          <h2 className="catalog-form-section__title">{t("customers.createKindTitle")}</h2>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.createKindLede")}
          </p>
          <div className="customer-create-kind__grid" role="group" aria-label={t("customers.createKindTitle")}>
            <button
              type="button"
              className="customer-create-kind__card"
              data-testid="customer-create-kind-walkin"
              onClick={() => {
                setCreateKind("walkin");
                setSelectedIdentity(null);
              }}
            >
              <span className="customer-create-kind__icon" aria-hidden>
                <UserRound className="size-5" />
              </span>
              <span className="customer-create-kind__label">{t("customers.createKindWalkIn")}</span>
              <span className="customer-create-kind__hint">{t("customers.createKindWalkInHint")}</span>
            </button>
            <button
              type="button"
              className="customer-create-kind__card"
              data-testid="customer-create-kind-exits"
              onClick={() => setCreateKind("exits")}
            >
              <span className="customer-create-kind__icon" aria-hidden>
                <IdCard className="size-5" />
              </span>
              <span className="customer-create-kind__label">{t("customers.createKindExits")}</span>
              <span className="customer-create-kind__hint">{t("customers.createKindExitsHint")}</span>
            </button>
          </div>
        </section>
      ) : null}

      {mode === "edit" || createKind !== null ? (
        <>
          {mode === "create" && online && createKind !== null ? (
            <div className="customer-create-kind__chosen exits-animate-toolbar">
              <p className="m-0 min-w-0 text-[length:var(--exits-text-sm)]">
                <span className="font-semibold">
                  {createKind === "exits"
                    ? t("customers.createKindExits")
                    : t("customers.createKindWalkIn")}
                </span>
              </p>
              <Button
                type="button"
                variant="ghost"
                className="min-h-9 shrink-0"
                data-testid="customer-create-kind-change"
                disabled={saving || Boolean(linkPublicId?.trim())}
                onClick={() => {
                  setCreateKind(null);
                  setSelectedIdentity(null);
                }}
              >
                {t("customers.createKindChange")}
              </Button>
            </div>
          ) : null}

          <section className="catalog-form-section exits-animate-panel">
            <h2 className="catalog-form-section__title">{t("customers.sectionBasics")}</h2>
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
            </div>
          </section>

          <section className="catalog-form-section exits-animate-panel">
            <h2 className="catalog-form-section__title">{t("customers.sectionDetails")}</h2>
            <div className="catalog-form-section__grid">
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
              <label className="catalog-form-field--full flex min-w-0 flex-col gap-1.5" htmlFor="customer-notes">
                <span className="text-[length:var(--exits-text-sm)] font-semibold">{t("customers.notes")}</span>
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

          {mode === "create" && online && workspace && createKind === "exits" ? (
            <CustomerPersonalLinkPanel
              disabled={saving}
              initialSubject={linkPublicId}
              selected={selectedIdentity}
              onResolved={(user) => {
                if (!displayName.trim() && user.displayName.trim()) {
                  setDisplayName(user.displayName.trim());
                }
              }}
              onSelected={(identity) => {
                setSelectedIdentity(identity);
                if (!displayName.trim() && identity.displayName.trim()) {
                  setDisplayName(identity.displayName.trim());
                }
              }}
              onCleared={() => setSelectedIdentity(null)}
            />
          ) : null}
          {mode === "create" && !online ? (
            <section
              className="catalog-form-section exits-animate-panel"
              data-testid="customer-personal-link-offline"
            >
              <h2 className="catalog-form-section__title">{t("customers.personalLink.title")}</h2>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("customers.personalLink.requiresOnline")}
              </p>
            </section>
          ) : null}

          <div className={cn("catalog-form-actions", "customer-form-actions")}>
            <div className="catalog-form-actions__primary flex flex-col gap-2 sm:flex-row">
              <Button
                type="submit"
                className="catalog-form-actions__save"
                data-testid="customer-save"
                disabled={saving}
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
              {mode === "create" && createKind === "exits" && online ? (
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11"
                  data-testid="customer-save-local-instead"
                  disabled={saving}
                  onClick={() => void onSubmit({ localOnly: true })}
                >
                  {t("customers.saveAsLocalInstead")}
                </Button>
              ) : null}
            </div>
          </div>
        </>
      ) : null}
    </form>
  );
}
