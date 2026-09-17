import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, Loader2, Save } from "lucide-react";
import { createCustomer } from "@/api/pos/pos-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Notice } from "@/components/exits/Notice";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

type FormState = {
  name: string;
  taxOrRegistrationNumber: string;
  contactPerson: string;
  mobileNumber: string;
  telephoneNumber: string;
  email: string;
  addressLine1: string;
  cityMunicipality: string;
  province: string;
  postalCode: string;
  notes: string;
};

const emptyForm: FormState = {
  name: "",
  taxOrRegistrationNumber: "",
  contactPerson: "",
  mobileNumber: "",
  telephoneNumber: "",
  email: "",
  addressLine1: "",
  cityMunicipality: "",
  province: "",
  postalCode: "",
  notes: "",
};

type FieldDef = {
  key: keyof FormState;
  labelKey: MessageKey;
  testId: string;
  multiline?: boolean;
  span?: "full" | "half";
};

function composeBusinessAddress(form: FormState): string | null {
  const parts = [
    form.addressLine1.trim(),
    form.cityMunicipality.trim(),
    form.province.trim(),
    form.postalCode.trim(),
  ].filter(Boolean);
  return parts.length > 0 ? parts.join(", ") : null;
}

function composeBusinessNotes(form: FormState): string | null {
  const parts: string[] = [];
  const contact = form.contactPerson.trim();
  const telephone = form.telephoneNumber.trim();
  const email = form.email.trim();
  const tax = form.taxOrRegistrationNumber.trim();
  const notes = form.notes.trim();
  if (contact) parts.push(`Contact: ${contact}`);
  if (telephone) parts.push(`Telephone: ${telephone}`);
  if (email) parts.push(`Email: ${email}`);
  if (tax) parts.push(`Tax / registration: ${tax}`);
  if (notes) parts.push(notes);
  return parts.length > 0 ? parts.join("\n") : null;
}

/**
 * Local business customer (not on ExItS) — POS PartyKind=Business, no org identity.
 * UI matches the supplier manual-entry form; extras are stored in address/notes.
 */
export function CustomerBusinessLocalFormPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const [form, setForm] = useState<FormState>(emptyForm);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const cancelTo = "/customers/new/business";

  function setField(key: keyof FormState, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  const basics: FieldDef[] = [
    { key: "name", labelKey: "suppliers.name", testId: "customer-business-name" },
    {
      key: "taxOrRegistrationNumber",
      labelKey: "suppliers.taxNumber",
      testId: "customer-business-tax",
    },
  ];
  const contact: FieldDef[] = [
    {
      key: "contactPerson",
      labelKey: "suppliers.contactPerson",
      testId: "customer-business-contact",
    },
    { key: "mobileNumber", labelKey: "suppliers.mobile", testId: "customer-business-mobile" },
    {
      key: "telephoneNumber",
      labelKey: "suppliers.telephone",
      testId: "customer-business-telephone",
    },
    { key: "email", labelKey: "suppliers.email", testId: "customer-business-email" },
  ];
  const address: FieldDef[] = [
    {
      key: "addressLine1",
      labelKey: "suppliers.addressLine1",
      testId: "customer-business-address",
      span: "full",
    },
    { key: "cityMunicipality", labelKey: "suppliers.city", testId: "customer-business-city" },
    { key: "province", labelKey: "suppliers.province", testId: "customer-business-province" },
    { key: "postalCode", labelKey: "suppliers.postalCode", testId: "customer-business-postal" },
  ];
  const notes: FieldDef[] = [
    {
      key: "notes",
      labelKey: "suppliers.notes",
      testId: "customer-business-notes",
      multiline: true,
      span: "full",
    },
  ];

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  async function onSubmit() {
    const name = form.name.trim();
    if (!name) {
      setError(t("customers.businessNameRequired"));
      return;
    }
    if (!online) {
      setError(t("staffInvite.onlineRequired"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const created = await createCustomer(workspace!, {
        displayName: name,
        mobileNumber: form.mobileNumber.trim() || null,
        address: composeBusinessAddress(form),
        notes: composeBusinessNotes(form),
        partyKind: "Business",
      });
      navigate(`/customers/${created.customerId}`, { replace: true });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : t("error.detail"),
      );
    } finally {
      setSaving(false);
    }
  }

  function renderFields(fields: FieldDef[]) {
    return fields.map((field) => (
      <label
        key={field.key}
        className={
          field.span === "full" || field.multiline
            ? "supplier-form-field supplier-form-field--full flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            : "supplier-form-field flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
        }
        htmlFor={field.testId}
      >
        {t(field.labelKey)}
        {field.multiline ? (
          <textarea
            id={field.testId}
            data-testid={field.testId}
            className="supplier-form-control supplier-form-control--area rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            rows={2}
            value={form[field.key]}
            disabled={saving}
            onChange={(event) => setField(field.key, event.target.value)}
          />
        ) : (
          <input
            id={field.testId}
            data-testid={field.testId}
            className="supplier-form-control rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={form[field.key]}
            disabled={saving}
            onChange={(event) => setField(field.key, event.target.value)}
          />
        )}
      </label>
    ));
  }

  return (
    <div
      className="supplier-form-page flex min-w-0 flex-col gap-3"
      data-testid="customer-business-local-form"
    >
      <PageHeader
        title={t("customers.addBusinessLocal")}
        description={t("customers.addBusinessLocalLede")}
        backTo={cancelTo}
        backLabel={t("customers.addBusiness")}
        backTestId="page-header-back-customer-business"
      />

      {error ? (
        <Notice tone="danger" testId="customer-business-form-error">{error}</Notice>
      ) : null}

      <Card className="supplier-form-card flex min-w-0 flex-col gap-0 p-0" data-testid="customer-business-form-card">
        <div className="supplier-form-card__body flex min-w-0 flex-col gap-0 p-3 sm:p-4">
          <section className="supplier-form-section">
            <h2 className="supplier-form-section__title">{t("suppliers.sectionBasics")}</h2>
            <div className="supplier-form-section__grid">{renderFields(basics)}</div>
          </section>

          <section className="supplier-form-section">
            <h2 className="supplier-form-section__title">{t("suppliers.sectionContact")}</h2>
            <div className="supplier-form-section__grid">{renderFields(contact)}</div>
          </section>

          <section className="supplier-form-section">
            <h2 className="supplier-form-section__title">{t("suppliers.sectionAddress")}</h2>
            <div className="supplier-form-section__grid supplier-form-section__grid--address">
              {renderFields(address)}
            </div>
          </section>

          <section className="supplier-form-section">
            <div className="supplier-form-section__grid">{renderFields(notes)}</div>
          </section>
        </div>

        <div className="supplier-form-actions flex flex-wrap items-center justify-end gap-2 border-t border-border px-3 py-3 sm:px-4">
          <Button
            asChild
            variant="outline"
            className="supplier-form-cancel-btn w-fit"
            disabled={saving}
          >
            <Link to={cancelTo} data-testid="customer-business-cancel">
              <ArrowLeft className="size-4 shrink-0" aria-hidden />
              {t("customers.backBusinessChooser")}
            </Link>
          </Button>
          <Button
            type="button"
            className="supplier-form-save-btn w-fit"
            data-testid="customer-business-local-save"
            disabled={saving || !form.name.trim()}
            onClick={() => void onSubmit()}
          >
            {saving ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Save className="size-4 shrink-0" aria-hidden />
            )}
            {saving ? t("suppliers.saving") : t("customers.saveBusiness")}
          </Button>
        </div>
      </Card>
    </div>
  );
}
