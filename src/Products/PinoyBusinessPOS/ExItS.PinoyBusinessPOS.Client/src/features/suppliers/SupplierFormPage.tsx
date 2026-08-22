import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  createSupplier,
  getSupplier,
  updateSupplier,
  type CreatePosSupplierInput,
} from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { describeSupplierError } from "@/features/suppliers/supplier-errors";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Mode = "create" | "edit";

type FormState = {
  name: string;
  contactPerson: string;
  mobileNumber: string;
  telephoneNumber: string;
  email: string;
  addressLine1: string;
  addressLine2: string;
  cityMunicipality: string;
  province: string;
  postalCode: string;
  taxOrRegistrationNumber: string;
  notes: string;
};

const emptyForm: FormState = {
  name: "",
  contactPerson: "",
  mobileNumber: "",
  telephoneNumber: "",
  email: "",
  addressLine1: "",
  addressLine2: "",
  cityMunicipality: "",
  province: "",
  postalCode: "",
  taxOrRegistrationNumber: "",
  notes: "",
};

export function SupplierCreatePage() {
  return <SupplierFormPage mode="create" />;
}

export function SupplierEditPage() {
  return <SupplierFormPage mode="edit" />;
}

function SupplierFormPage({ mode }: { mode: Mode }) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { supplierId } = useParams<{ supplierId: string }>();
  const { boundWorkspace } = useWorkspace();

  const [form, setForm] = useState<FormState>(emptyForm);
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
    queryKey: ["suppliers", "detail", workspace?.organizationId, supplierId],
    enabled: mode === "edit" && Boolean(workspace) && Boolean(supplierId),
    queryFn: ({ signal }) => getSupplier(workspace!, supplierId!, signal),
  });

  useEffect(() => {
    if (!existing.data) {
      return;
    }
    setForm({
      name: existing.data.name,
      contactPerson: existing.data.contactPerson ?? "",
      mobileNumber: existing.data.mobileNumber ?? "",
      telephoneNumber: existing.data.telephoneNumber ?? "",
      email: existing.data.email ?? "",
      addressLine1: existing.data.addressLine1 ?? "",
      addressLine2: existing.data.addressLine2 ?? "",
      cityMunicipality: existing.data.cityMunicipality ?? "",
      province: existing.data.province ?? "",
      postalCode: existing.data.postalCode ?? "",
      taxOrRegistrationNumber: existing.data.taxOrRegistrationNumber ?? "",
      notes: existing.data.notes ?? "",
    });
    setExpectedUpdatedAtUtc(existing.data.updatedAtUtc);
  }, [existing.data]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (mode === "edit" && existing.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (mode === "edit" && existing.isError) {
    return (
      <ErrorState title={t("error.title")} detail={describeSupplierError(existing.error, t)} />
    );
  }

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function toInput(): CreatePosSupplierInput {
    return {
      name: form.name,
      contactPerson: form.contactPerson,
      mobileNumber: form.mobileNumber,
      telephoneNumber: form.telephoneNumber,
      email: form.email,
      addressLine1: form.addressLine1,
      addressLine2: form.addressLine2,
      cityMunicipality: form.cityMunicipality,
      province: form.province,
      postalCode: form.postalCode,
      taxOrRegistrationNumber: form.taxOrRegistrationNumber,
      notes: form.notes,
    };
  }

  async function onSubmit() {
    if (!workspace) {
      return;
    }
    const name = form.name.trim();
    if (!name) {
      setError(t("suppliers.nameRequired"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      if (mode === "create") {
        const created = await createSupplier(workspace, toInput());
        navigate(`/suppliers/${created.supplierId}`, { replace: true });
        return;
      }
      if (!expectedUpdatedAtUtc) {
        setError(t("suppliers.errorConcurrency"));
        return;
      }
      const updated = await updateSupplier(workspace, supplierId!, {
        ...toInput(),
        expectedUpdatedAtUtc,
      });
      navigate(`/suppliers/${updated.supplierId}`, { replace: true });
    } catch (err) {
      setError(describeSupplierError(err, t));
    } finally {
      setSaving(false);
    }
  }

  const fields: Array<{
    key: keyof FormState;
    labelKey:
      | "suppliers.name"
      | "suppliers.contactPerson"
      | "suppliers.mobile"
      | "suppliers.telephone"
      | "suppliers.email"
      | "suppliers.addressLine1"
      | "suppliers.addressLine2"
      | "suppliers.city"
      | "suppliers.province"
      | "suppliers.postalCode"
      | "suppliers.taxNumber"
      | "suppliers.notes";
    testId: string;
    multiline?: boolean;
  }> = [
    { key: "name", labelKey: "suppliers.name", testId: "supplier-name" },
    { key: "contactPerson", labelKey: "suppliers.contactPerson", testId: "supplier-contact" },
    { key: "mobileNumber", labelKey: "suppliers.mobile", testId: "supplier-mobile" },
    { key: "telephoneNumber", labelKey: "suppliers.telephone", testId: "supplier-telephone" },
    { key: "email", labelKey: "suppliers.email", testId: "supplier-email" },
    { key: "addressLine1", labelKey: "suppliers.addressLine1", testId: "supplier-address1" },
    { key: "addressLine2", labelKey: "suppliers.addressLine2", testId: "supplier-address2" },
    { key: "cityMunicipality", labelKey: "suppliers.city", testId: "supplier-city" },
    { key: "province", labelKey: "suppliers.province", testId: "supplier-province" },
    { key: "postalCode", labelKey: "suppliers.postalCode", testId: "supplier-postal" },
    { key: "taxOrRegistrationNumber", labelKey: "suppliers.taxNumber", testId: "supplier-tax" },
    { key: "notes", labelKey: "suppliers.notes", testId: "supplier-notes", multiline: true },
  ];

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="supplier-form-page">
      <PageHeader
        title={mode === "create" ? t("suppliers.newTitle") : t("suppliers.editTitle")}
        description={t("suppliers.formLede")}
        backTo={
          mode === "edit" && supplierId ? `/suppliers/${supplierId}` : pageBackNav.suppliers.to
        }
        backLabel={t(pageBackNav.suppliers.labelKey)}
        backTestId="page-header-back-suppliers"
      />
      {error ? (
        <Card data-testid="supplier-form-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {error}
          </p>
        </Card>
      ) : null}
      <Card className="flex flex-col gap-3">
        {fields.map((field) => (
          <label
            key={field.key}
            className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor={field.testId}
          >
            {t(field.labelKey)}
            {field.multiline ? (
              <textarea
                id={field.testId}
                data-testid={field.testId}
                className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
                value={form[field.key]}
                disabled={saving}
                onChange={(event) => setField(field.key, event.target.value)}
              />
            ) : (
              <input
                id={field.testId}
                data-testid={field.testId}
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                value={form[field.key]}
                disabled={saving}
                onChange={(event) => setField(field.key, event.target.value)}
              />
            )}
          </label>
        ))}
      </Card>
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          className="min-h-11"
          data-testid="supplier-save"
          disabled={saving}
          onClick={() => void onSubmit()}
        >
          {saving ? t("suppliers.saving") : t("suppliers.save")}
        </Button>
        <Button asChild variant="ghost" className="min-h-11" disabled={saving}>
          <Link to={mode === "edit" && supplierId ? `/suppliers/${supplierId}` : "/suppliers"}>
            {t("suppliers.back")}
          </Link>
        </Button>
      </div>
    </div>
  );
}
