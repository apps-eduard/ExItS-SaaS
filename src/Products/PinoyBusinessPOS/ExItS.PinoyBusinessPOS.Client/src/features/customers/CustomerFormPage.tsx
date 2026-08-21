import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { createCustomer, getCustomer, updateCustomer } from "@/api/pos/pos-customers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
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
  const { boundWorkspace } = useWorkspace();

  const [displayName, setDisplayName] = useState("");
  const [mobileNumber, setMobileNumber] = useState("");
  const [address, setAddress] = useState("");
  const [notes, setNotes] = useState("");
  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const existing = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: mode === "edit" && Boolean(workspace) && Boolean(customerId),
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

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (mode === "edit" && existing.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (mode === "edit" && existing.isError) {
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
      if (mode === "create") {
        const created = await createCustomer(workspace, {
          displayName: name,
          mobileNumber,
          address,
          notes,
        });
        navigate(`/customers/${created.customerId}`, { replace: true });
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

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customer-form-page">
      <PageHeader
        title={mode === "create" ? t("customers.newTitle") : t("customers.editTitle")}
        description={t("customers.formLede")}
      />
      {error ? (
        <Card data-testid="customer-form-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {error}
          </p>
        </Card>
      ) : null}
      <Card className="flex flex-col gap-3">
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="customer-display-name"
        >
          {t("customers.displayName")}
          <input
            id="customer-display-name"
            data-testid="customer-display-name"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={displayName}
            disabled={saving}
            onChange={(event) => setDisplayName(event.target.value)}
          />
        </label>
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="customer-mobile"
        >
          {t("customers.mobile")}
          <input
            id="customer-mobile"
            data-testid="customer-mobile"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={mobileNumber}
            disabled={saving}
            onChange={(event) => setMobileNumber(event.target.value)}
          />
        </label>
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="customer-address"
        >
          {t("customers.address")}
          <input
            id="customer-address"
            data-testid="customer-address"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={address}
            disabled={saving}
            onChange={(event) => setAddress(event.target.value)}
          />
        </label>
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="customer-notes"
        >
          {t("customers.notes")}
          <textarea
            id="customer-notes"
            data-testid="customer-notes"
            className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
            value={notes}
            disabled={saving}
            onChange={(event) => setNotes(event.target.value)}
          />
        </label>
      </Card>
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          className="min-h-11"
          data-testid="customer-save"
          disabled={saving}
          onClick={() => void onSubmit()}
        >
          {saving ? t("customers.saving") : t("customers.save")}
        </Button>
        <Button asChild variant="ghost" className="min-h-11" disabled={saving}>
          <Link to={mode === "edit" && customerId ? `/customers/${customerId}` : "/customers"}>
            {t("customers.back")}
          </Link>
        </Button>
      </div>
    </div>
  );
}
