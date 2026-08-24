import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Save } from "lucide-react";
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
  type ConfirmedPersonalLink,
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

type Mode = "create" | "edit";

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
  const [personalLink, setPersonalLink] = useState<ConfirmedPersonalLink | null>(null);

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

  // Editing offline starts from the cached row so a queued edit still carries the fields the
  // cashier last saw, including the concurrency token the server will check.
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

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (mode === "edit" && existing.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (mode === "edit" && existing.isError && online) {
    return <ErrorState title={t("error.title")} detail={(existing.error as Error).message} />;
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
        await saveOffline(name);
        return;
      }
      if (mode === "create") {
        const created = await createCustomer(workspace, {
          displayName: name,
          mobileNumber,
          address,
          notes: personalLink
            ? notes.trim()
              ? `${notes.trim()}\nexits-id:${personalLink.publicUserId}`
              : `exits-id:${personalLink.publicUserId}`
            : notes,
          platformBusinessCustomerId: personalLink?.platformBusinessCustomerId ?? null,
        });
        if (returnTo && returnTo.startsWith("/") && !returnTo.startsWith("//")) {
          navigate(returnTo, { replace: true });
          return;
        }
        navigate(
          personalLink
            ? `/customers/${created.customerId}?pendingLink=1`
            : `/customers/${created.customerId}`,
          { replace: true },
        );
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
      setError(err instanceof Error ? err.message : t("error.detail"));
    } finally {
      setSaving(false);
    }
  }

  /**
   * Queue the customer for the server instead of pretending it was accepted. A create picks the
   * id here so the server adopts it, which keeps the queued row and the eventual server row the
   * same customer.
   */
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
        description={t("customers.formLede")}
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

      {mode === "create" && online && workspace ? (
        <CustomerPersonalLinkPanel
          organizationId={workspace.organizationId}
          displayName={displayName}
          phone={mobileNumber}
          notes={notes}
          disabled={saving}
          initialSubject={linkPublicId}
          onLinked={(link) => {
            setPersonalLink(link);
            if (!displayName.trim()) {
              setDisplayName(link.displayName);
            }
          }}
          onCleared={() => setPersonalLink(null)}
        />
      ) : null}
      {mode === "create" && !online ? (
        <section className="catalog-form-section exits-animate-panel" data-testid="customer-personal-link-offline">
          <h2 className="catalog-form-section__title">{t("customers.personalLink.title")}</h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.personalLink.requiresOnline")}
          </p>
        </section>
      ) : null}

      <div className="catalog-form-actions customer-form-actions">
        <div className="catalog-form-actions__primary">
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
                {t("customers.save")}
              </>
            )}
          </Button>
        </div>
      </div>
    </form>
  );
}
