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
  const [editor, setEditor] = useState<EditorState | null>(null);
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

  return (
    <div className="personal-profile-field--wide flex flex-col gap-3" data-testid="personal-address-list">
      {error ? <Notice tone="danger">{error}</Notice> : null}
      {addresses.length === 0 ? <p className="m-0 text-[length:var(--exits-text-sm)]">{t("personal.profile.noAddresses")}</p> : null}
      {addresses.map((address) => (
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
            <div className="mt-2 flex flex-wrap gap-2">
              <Button type="button" onClick={() => setEditor({ id: address.id, draft: draftFromAddress(address) })}>
                {t("personal.profile.editAddress")}
              </Button>
              <Button type="button" onClick={() => deleteMutation.mutate(address.id)} disabled={deleteMutation.isPending}>
                {t("personal.profile.deleteAddress")}
              </Button>
              {address.isPrimary ? null : (
                <Button type="button" onClick={() => primaryMutation.mutate(address.id)} disabled={primaryMutation.isPending}>
                  {t("personal.profile.setPrimary")}
                </Button>
              )}
            </div>
          )}
        </article>
      ))}
      {readOnly ? null : editor ? (
        <div className="flex flex-col gap-3">
          <AddressForm
            value={editor.draft}
            onChange={(draft) => setEditor((current) => (current ? { ...current, draft } : current))}
            showAddressType
            showPrimary
            allowMultiple
            requireAddress={false}
            testIdPrefix="personal-address"
          />
          <div className="flex flex-wrap gap-2">
            <Button type="button" onClick={save} disabled={saveMutation.isPending} data-testid="personal-address-save">
              {t("personal.profile.saveAddress")}
            </Button>
            <Button type="button" onClick={() => setEditor(null)}>{t("personal.profile.cancelAddress")}</Button>
          </div>
        </div>
      ) : (
        <Button type="button" className="personal-address-add" onClick={() => setEditor({ id: null, draft: emptySharedAddress(addresses.length === 0) })} data-testid="personal-address-add">
          {t("personal.profile.addAddress")}
        </Button>
      )}
    </div>
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
