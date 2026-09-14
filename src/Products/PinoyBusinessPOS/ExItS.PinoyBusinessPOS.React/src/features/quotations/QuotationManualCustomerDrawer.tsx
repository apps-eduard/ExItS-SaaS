import { useMemo, useState } from "react";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { Input } from "@/components/ui/input";
import { createCustomer } from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { useI18n } from "@/i18n/I18nProvider";

/** Manual customer create for quotations — reuses canonical POS Customer fields only (no email). */
export function QuotationManualCustomerDrawer({
  open,
  onOpenChange,
  workspace,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  workspace: PosWorkspaceScope;
  onCreated: (customer: { customerId: string; displayName: string }) => void;
}) {
  const { t } = useI18n();
  const [displayName, setDisplayName] = useState("");
  const [mobileNumber, setMobileNumber] = useState("");
  const [address, setAddress] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const dirty = useMemo(
    () =>
      Boolean(displayName.trim() || mobileNumber.trim() || address.trim() || notes.trim()),
    [address, displayName, mobileNumber, notes],
  );

  async function save() {
    const name = displayName.trim();
    if (!name) {
      setError(t("quotations.customerNameRequired"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const created = await createCustomer(workspace, {
        displayName: name,
        mobileNumber: mobileNumber.trim() || null,
        address: address.trim() || null,
        notes: notes.trim() || null,
      });
      onCreated({ customerId: created.customerId, displayName: created.displayName });
      setDisplayName("");
      setMobileNumber("");
      setAddress("");
      setNotes("");
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : t("quotations.customerCreateFailed"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <FormDrawer
      open={open}
      onOpenChange={onOpenChange}
      title={t("quotations.addCustomerTitle")}
      description={t("quotations.addCustomerLede")}
      saveLabel={t("quotations.saveAndUseCustomer")}
      onSave={() => void save()}
      saving={saving}
      saveDisabled={!displayName.trim()}
      dirty={dirty}
      confirmUnsavedOnClose
      testId="quotation-manual-customer-drawer"
      saveTestId="quotation-manual-customer-save"
    >
      <div className="flex flex-col gap-3">
        {error ? (
          <p className="m-0 text-sm text-destructive" data-testid="quotation-manual-customer-error">
            {error}
          </p>
        ) : null}
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("quotations.customerName")}</span>
          <Input
            value={displayName}
            data-testid="quotation-customer-name"
            onChange={(e) => setDisplayName(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("quotations.customerPhone")}</span>
          <Input
            value={mobileNumber}
            data-testid="quotation-customer-phone"
            onChange={(e) => setMobileNumber(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("quotations.customerAddress")}</span>
          <Input
            value={address}
            data-testid="quotation-customer-address"
            onChange={(e) => setAddress(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("quotations.customerNotes")}</span>
          <textarea
            className="min-h-20 rounded-md border border-border bg-background px-3 py-2 text-sm"
            value={notes}
            data-testid="quotation-customer-notes"
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
      </div>
    </FormDrawer>
  );
}
