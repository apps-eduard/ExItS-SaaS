import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Loader2, Save } from "lucide-react";
import { createCustomer } from "@/api/pos/pos-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import { LoadingState } from "@/components/exits/LoadingState";

function composeBusinessNotes(input: {
  contactPerson: string;
  email: string;
  notes: string;
}): string | null {
  const parts: string[] = [];
  const contact = input.contactPerson.trim();
  const email = input.email.trim();
  const notes = input.notes.trim();
  if (contact) parts.push(`Contact: ${contact}`);
  if (email) parts.push(`Email: ${email}`);
  if (notes) parts.push(notes);
  return parts.length > 0 ? parts.join("\n") : null;
}

/**
 * Local business customer (not on ExItS) — POS PartyKind=Business, no org identity.
 */
export function CustomerBusinessLocalFormPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const [businessName, setBusinessName] = useState("");
  const [contactPerson, setContactPerson] = useState("");
  const [mobileNumber, setMobileNumber] = useState("");
  const [email, setEmail] = useState("");
  const [address, setAddress] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  async function onSubmit() {
    const name = businessName.trim();
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
        mobileNumber: mobileNumber.trim() || null,
        address: address.trim() || null,
        notes: composeBusinessNotes({ contactPerson, email, notes }),
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

  return (
    <div
      className="flex min-w-0 flex-col gap-4"
      data-testid="customer-business-local-form"
    >
      <PageHeader
        title={t("customers.addBusinessLocal")}
        description={t("customers.addBusinessLocalLede")}
        backTo="/customers/new/business"
        backLabel={t("customers.addBusiness")}
        backTestId="page-header-back-customer-business"
      />

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.businessName")} *
        </span>
        <Input
          value={businessName}
          onChange={(e) => setBusinessName(e.target.value)}
          data-testid="customer-business-name"
          autoComplete="organization"
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.contactPerson")}
        </span>
        <Input
          value={contactPerson}
          onChange={(e) => setContactPerson(e.target.value)}
          data-testid="customer-business-contact"
          autoComplete="name"
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-sm)] font-medium">{t("customers.mobile")}</span>
        <Input
          value={mobileNumber}
          onChange={(e) => setMobileNumber(e.target.value)}
          data-testid="customer-business-mobile"
          inputMode="tel"
          autoComplete="tel"
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-sm)] font-medium">{t("customers.email")}</span>
        <Input
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          data-testid="customer-business-email"
          type="email"
          autoComplete="email"
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-sm)] font-medium">{t("customers.address")}</span>
        <Input
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          data-testid="customer-business-address"
          autoComplete="street-address"
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5">
        <span className="text-[length:var(--exits-text-sm)] font-medium">{t("customers.notes")}</span>
        <textarea
          className="min-h-24 w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-sm)]"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          data-testid="customer-business-notes"
        />
      </label>

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
          {error}
        </p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          data-testid="customer-business-local-save"
          disabled={saving || !businessName.trim()}
          onClick={() => void onSubmit()}
        >
          {saving ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Save className="size-4" aria-hidden />}
          {t("customers.save")}
        </Button>
        <Button asChild variant="ghost">
          <Link to="/customers/new/business">{t("preferences.close")}</Link>
        </Button>
      </div>
    </div>
  );
}
