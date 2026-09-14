import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  updateCustomer,
  type PosCustomerDetail,
} from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { useToast } from "@/components/exits/ToastProvider";
import { Input } from "@/components/ui/input";
import {
  composeSellerLocalNotes,
  extractDeliveryInstructions,
} from "@/features/customers/customer-store-details";
import { extractPersonalExItsIdFromNotes } from "@/features/customers/customer-link-status";
import { useI18n } from "@/i18n/I18nProvider";

type CustomerStoreDetailsEditDrawerProps = {
  open: boolean;
  onClose: () => void;
  workspace: PosWorkspaceScope;
  customer: PosCustomerDetail;
  /** Linked Personal display context for header. */
  contextLabel?: string | null;
  isLinked: boolean;
};

export function CustomerStoreDetailsEditDrawer({
  open,
  onClose,
  workspace,
  customer,
  contextLabel,
  isLinked,
}: CustomerStoreDetailsEditDrawerProps) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const personalExItsId =
    customer.linkedPersonalPublicUserId?.trim() ||
    extractPersonalExItsIdFromNotes(customer.notes).exItsId;

  const [preferredName, setPreferredName] = useState(customer.displayName);
  const [contactPhone, setContactPhone] = useState(customer.mobileNumber ?? "");
  const [deliveryAddress, setDeliveryAddress] = useState(customer.address ?? "");
  const [deliveryInstructions, setDeliveryInstructions] = useState("");
  const [internalNotes, setInternalNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    const parsed = extractDeliveryInstructions(customer.notes);
    setPreferredName(customer.displayName);
    setContactPhone(customer.mobileNumber ?? "");
    setDeliveryAddress(customer.address ?? "");
    setDeliveryInstructions(parsed.deliveryInstructions);
    setInternalNotes(parsed.internalNotes);
    setFormError(null);
  }, [open, customer]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const name = preferredName.trim();
      if (!name) {
        throw new Error(t("customers.storeDetails.nameRequired"));
      }
      return updateCustomer(workspace, customer.customerId, {
        displayName: name,
        mobileNumber: contactPhone.trim() || null,
        address: deliveryAddress.trim() || null,
        notes: composeSellerLocalNotes({
          internalNotes,
          deliveryInstructions,
          personalExItsId,
        }),
        expectedUpdatedAtUtc: customer.updatedAtUtc,
      });
    },
    onSuccess: async () => {
      showToast(t("customers.storeDetails.saved"), "success");
      await queryClient.invalidateQueries({
        queryKey: ["customers", "detail", workspace.organizationId, customer.customerId],
      });
      await queryClient.invalidateQueries({
        queryKey: ["customers", "list", workspace.organizationId],
      });
      onClose();
    },
    onError: (error) => {
      setFormError(
        error instanceof PosApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("customers.storeDetails.saveFailed"),
      );
    },
  });

  return (
    <FormDrawer
      open={open}
      onOpenChange={(next) => {
        if (!next && !saveMutation.isPending) {
          onClose();
        }
      }}
      title={t("customers.storeDetails.editTitle")}
      description={contextLabel?.trim() || customer.displayName}
      testId="customer-store-details-drawer"
      closeLabel={t("customers.storeDetails.cancel")}
      cancelLabel={t("customers.storeDetails.cancel")}
      cancelTestId="store-details-cancel"
      saveTestId="store-details-save"
      saveLabel={
        saveMutation.isPending
          ? t("customers.storeDetails.saving")
          : t("customers.storeDetails.save")
      }
      saving={saveMutation.isPending}
      onSave={() => {
        if (saveMutation.isPending) return;
        setFormError(null);
        saveMutation.mutate();
      }}
    >
      {isLinked ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-2 text-[length:var(--exits-text-xs)] text-muted"
          data-testid="customer-store-details-linked-context"
        >
          <span className="font-medium text-foreground">
            {t("customers.storeDetails.linkedProfile")}
          </span>
          <span className="mx-1.5">·</span>
          {contextLabel?.trim() || customer.displayName}
          {personalExItsId ? ` · ${personalExItsId}` : ""}
          <span className="mt-1 block">{t("customers.storeDetails.readOnlyIdentity")}</span>
        </p>
      ) : null}

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="store-preferred-name">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {isLinked ? t("customers.storeDetails.preferredName") : t("customers.displayName")}
        </span>
        <Input
          id="store-preferred-name"
          data-testid="store-preferred-name"
          value={preferredName}
          onChange={(event) => setPreferredName(event.target.value)}
          autoComplete="off"
          required
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="store-contact-phone">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.storeDetails.contactPhone")}
        </span>
        <Input
          id="store-contact-phone"
          data-testid="store-contact-phone"
          value={contactPhone}
          onChange={(event) => setContactPhone(event.target.value)}
          inputMode="tel"
          autoComplete="tel"
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="store-delivery-address">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.storeDetails.deliveryAddress")}
        </span>
        <textarea
          id="store-delivery-address"
          data-testid="store-delivery-address"
          className="customer-form-notes min-h-[4.25rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
          value={deliveryAddress}
          onChange={(event) => setDeliveryAddress(event.target.value)}
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="store-delivery-instructions">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.storeDetails.deliveryInstructions")}
        </span>
        <textarea
          id="store-delivery-instructions"
          data-testid="store-delivery-instructions"
          className="customer-form-notes min-h-[3.5rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
          value={deliveryInstructions}
          onChange={(event) => setDeliveryInstructions(event.target.value)}
        />
      </label>

      <label className="flex min-w-0 flex-col gap-1.5" htmlFor="store-internal-notes">
        <span className="text-[length:var(--exits-text-sm)] font-medium">
          {t("customers.storeDetails.internalNotes")}
        </span>
        <textarea
          id="store-internal-notes"
          data-testid="store-internal-notes"
          className="customer-form-notes min-h-[4.25rem] w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-md)] text-foreground"
          value={internalNotes}
          onChange={(event) => setInternalNotes(event.target.value)}
        />
      </label>

      {formError ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="store-details-error"
        >
          {formError}
        </p>
      ) : null}
    </FormDrawer>
  );
}
