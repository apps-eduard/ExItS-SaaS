import { ChevronDown, Pencil, Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  deletePersonalAddress,
  savePersonalAddress,
  setPersonalAddressPrimary,
  type PersonalAddressDto,
  type PersonalProfileDto,
  type SavePersonalAddressRequest,
} from "@/api/platform/start-business-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { Notice } from "@/components/exits/Notice";
import { useI18n } from "@/i18n/I18nProvider";
import { AddressForm, emptySharedAddress, type SharedAddressValue } from "@/platform/geography";

type EditorState = {
  id: string | null;
  draft: SharedAddressValue;
};

function draftFromAddress(address: PersonalAddressDto): SharedAddressValue {
  return {
    addressType: address.addressType,
    countryCode: address.countryCode ?? "",
    countryName: address.country ?? "",
    administrativeAreaCode: "",
    administrativeAreaName: address.provinceState ?? "",
    cityCode: "",
    cityName: address.cityMunicipality ?? "",
    barangay: address.barangay ?? "",
    addressLine1: address.addressLine1 ?? "",
    addressLine2: address.addressLine2 ?? "",
    postalCode: address.postalCode ?? "",
    isPrimary: address.isPrimary,
  };
}

export function PersonalAddressesSection({ profile, readOnly = false }: { profile: PersonalProfileDto; readOnly?: boolean }) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const addresses = profile.addresses ?? [];
  const [showAll, setShowAll] = useState(false);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const saveMutation = useMutation({
    mutationFn: (input: { id: string | null; body: SavePersonalAddressRequest }) =>
      savePersonalAddress(input.body, input.id ?? undefined),
    onSuccess: (data) => {
      queryClient.setQueryData(["personal", "profile"], data);
      setEditor(null);
      setError(null);
    },
    onError: (caught) => setError(messageFrom(caught, t("personal.profile.saveFailed"))),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deletePersonalAddress(id),
    onSuccess: (data) => {
      queryClient.setQueryData(["personal", "profile"], data);
      setPendingDeleteId(null);
      setError(null);
    },
    onError: (caught) => setError(messageFrom(caught, t("personal.profile.saveFailed"))),
  });

  const primaryMutation = useMutation({
    mutationFn: (id: string) => setPersonalAddressPrimary(id),
    onSuccess: (data) => {
      queryClient.setQueryData(["personal", "profile"], data);
      setError(null);
    },
    onError: (caught) => setError(messageFrom(caught, t("personal.profile.saveFailed"))),
  });

  function save() {
    if (!editor || saveMutation.isPending) {
      return;
    }
    const draft = editor.draft;
    saveMutation.mutate({
      id: editor.id,
      body: {
        addressType: draft.addressType,
        country: draft.countryName || draft.countryCode,
        addressLine1: draft.addressLine1,
        addressLine2: draft.addressLine2 || null,
        barangay: draft.barangay || null,
        cityMunicipality: draft.cityName,
        provinceState: draft.administrativeAreaName,
        postalCode: draft.postalCode || null,
        isPrimary: draft.isPrimary,
      },
    });
  }

  const primaryAddress = addresses.find((address) => address.isPrimary) ?? addresses[0];
  const visibleAddresses = readOnly && !showAll && addresses.length > 1 && primaryAddress ? [primaryAddress] : addresses;

  return (
    <div className="personal-profile-field--wide flex flex-col gap-3" data-testid="personal-address-list">
      {error && !editor ? <Notice tone="danger">{error}</Notice> : null}
      {addresses.length === 0 ? <p className="m-0 text-[length:var(--exits-text-sm)]">{t("personal.profile.noAddresses")}</p> : null}
      {readOnly && addresses.length > 1 ? (
        <Button
          type="button"
          intent="primary"
          appearance="glossy"
          className="personal-address-show-all"
          data-testid="personal-address-show-all"
          aria-expanded={showAll}
          onClick={() => setShowAll((current) => !current)}
        >
          {showAll ? t("personal.profile.showPrimaryOnly") : t("personal.profile.showAllAddresses")}
          <ChevronDown
            className={`personal-address-show-all__chevron size-4 shrink-0${showAll ? " personal-address-show-all__chevron--open" : ""}`}
            aria-hidden="true"
          />
          <StatusChip tone="contrast" shape="pill">{addresses.length}</StatusChip>
        </Button>
      ) : null}
      {visibleAddresses.map((address) => (
        <article key={address.id} className="flex flex-col gap-1 rounded-md border border-[var(--exits-border)] p-3" data-testid={`personal-address-${address.id}`}>
          <p className="m-0 font-semibold">
            {address.isPrimary ? <span data-testid="personal-address-primary-badge">[{t("personal.profile.primaryBadge")}] </span> : null}
            {t(address.addressType === "Office" ? "personal.profile.typeOffice" : address.addressType === "Other" ? "personal.profile.typeOther" : "personal.profile.typeHome")}
          </p>
          <div className="personal-address-saved">
            <AddressValue label={t("personal.profile.country")} value={address.country} />
            <AddressValue label={t("personal.profile.province")} value={address.provinceState} />
            <AddressValue label={t("personal.profile.city")} value={address.cityMunicipality} />
            <AddressValue label={t("personal.profile.barangay")} value={address.barangay} />
            <AddressValue label={t("personal.profile.address1")} value={address.addressLine1} />
            <AddressValue label={t("personal.profile.address2")} value={address.addressLine2} />
            <AddressValue label={t("personal.profile.postal")} value={address.postalCode} />
          </div>
          {readOnly ? null : (
            <div className="mt-2 flex flex-wrap items-center justify-end gap-2">
              {address.isPrimary ? null : (
                <Button
                  type="button"
                  intent="primary"
                  appearance="glossy"
                  onClick={() => primaryMutation.mutate(address.id)}
                  disabled={primaryMutation.isPending}
                >
                  {t("personal.profile.setPrimary")}
                </Button>
              )}
              <Button
                type="button"
                intent="primary"
                appearance="glossy"
                onClick={() => setEditor({ id: address.id, draft: draftFromAddress(address) })}
              >
                <Pencil className="size-4 shrink-0" aria-hidden="true" />
                {t("personal.profile.editAddress")}
              </Button>
              <Button
                type="button"
                intent="danger"
                appearance="glossy"
                emphasis="strong"
                onClick={() => setPendingDeleteId(address.id)}
                disabled={deleteMutation.isPending}
              >
                <Trash2 className="size-4 shrink-0" aria-hidden="true" />
                {t("personal.profile.deleteAddress")}
              </Button>
            </div>
          )}
        </article>
      ))}
      {readOnly ? null : (
        <Button
          type="button"
          intent="primary"
          appearance="glossy"
          className="personal-address-add"
          onClick={() => setEditor({ id: null, draft: emptySharedAddress(addresses.length === 0) })}
          data-testid="personal-address-add"
        >
          <Plus className="size-4 shrink-0" aria-hidden="true" />
          {t("personal.profile.addAddress")}
        </Button>
      )}
      <ExitsModal
        open={editor !== null}
        onOpenChange={(open) => {
          if (!open) setEditor(null);
        }}
        title={editor?.id ? t("personal.profile.editAddressTitle") : t("personal.profile.addAddress")}
        ribbon
        ribbonIcon={editor?.id ? Pencil : Plus}
        size="lg"
        className="personal-address-dialog"
        testId="personal-address-dialog"
        closeLabel={t("personal.profile.cancelAddress")}
        busy={saveMutation.isPending}
        footer={
          <>
            <Button
              type="button"
              intent="danger"
              appearance="glossy"
              emphasis="strong"
              disabled={saveMutation.isPending}
              onClick={() => setEditor(null)}
            >
              {t("personal.profile.cancelAddress")}
            </Button>
            <Button type="button" intent="primary" appearance="glossy" disabled={saveMutation.isPending} onClick={save} data-testid="personal-address-save">
              {editor?.id ? <SavePenIcon className="size-4 shrink-0" /> : <SavePlusIcon className="size-4 shrink-0" />}
              {t("personal.profile.saveAddress")}
            </Button>
          </>
        }
      >
        {editor ? (
          <div className="flex flex-col gap-3">
            {error ? <Notice tone="danger">{error}</Notice> : null}
            <AddressForm
              value={editor.draft}
              onChange={(draft) => setEditor((current) => (current ? { ...current, draft } : current))}
              showAddressType
              showPrimary
              allowMultiple
              requireAddress={false}
              lockedFields
              testIdPrefix="personal-address"
            />
          </div>
        ) : null}
      </ExitsModal>
      <ConfirmActionDialog
        open={pendingDeleteId !== null}
        variant="danger"
        title={t("personal.profile.deleteAddressTitle")}
        description={t("personal.profile.deleteAddressDetail")}
        confirmLabel={t("personal.profile.deleteAddress")}
        cancelLabel={t("personal.profile.cancelAddress")}
        pending={deleteMutation.isPending}
        testId="personal-address-delete-confirm"
        onCancel={() => {
          if (!deleteMutation.isPending) {
            setPendingDeleteId(null);
          }
        }}
        onConfirm={() => {
          if (pendingDeleteId && !deleteMutation.isPending) {
            deleteMutation.mutate(pendingDeleteId);
          }
        }}
      />
    </div>
  );
}

function SavePlusIcon({ className }: { className?: string }) {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4V12" />
      <path d="M16 13H8a1 1 0 0 0-1 1v7" />
      <path d="M19 22v-6" />
      <path d="M22 19h-6" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function SavePenIcon({ className }: { className?: string }) {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className={className} aria-hidden="true">
      <path d="M13.33 13H8a1 1 0 0 0-1 1v7" />
      <path d="M14.363 17.634a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506l4.013-4.009a1 1 0 1 0-3.004-3.004z" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v.3" />
    </svg>
  );
}

function AddressValue({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
      <span className="font-semibold">{label}</span>
      <span>{value?.trim() || "—"}</span>
    </div>
  );
}

function messageFrom(error: unknown, fallback: string) {
  if (error instanceof PlatformApiError) {
    return error.problem.detail ?? error.message;
  }
  return error instanceof Error ? error.message : fallback;
}
