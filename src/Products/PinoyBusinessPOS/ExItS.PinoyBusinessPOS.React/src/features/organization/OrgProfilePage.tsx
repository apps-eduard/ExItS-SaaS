import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Copy, Mail, MapPin, Pencil, Phone } from "lucide-react";
import {
  getOrganization,
  updateOrganizationBranding,
  updateOrganizationProfile,
  type PlatformOrganizationDto,
} from "@/api/platform/organization-profile-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function hasText(value: string | null | undefined): boolean {
  return Boolean(value?.trim());
}

function orgInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
  }
  return name.trim().slice(0, 2).toUpperCase() || "?";
}

function formatAddress(org: PlatformOrganizationDto): string | null {
  const p = org.profile;
  const locality = [p.city, p.region].filter((x) => hasText(x)).join(", ") || null;
  const parts = [p.addressLine1, p.addressLine2, locality, p.postalCode, p.countryCode]
    .map((x) => x?.trim())
    .filter(Boolean);
  return parts.length > 0 ? parts.join(", ") : null;
}

/** True when address is only a bare country code (common incomplete state). */
function hasMeaningfulAddress(org: PlatformOrganizationDto): boolean {
  const p = org.profile;
  return (
    hasText(p.addressLine1) ||
    hasText(p.addressLine2) ||
    hasText(p.city) ||
    hasText(p.region) ||
    hasText(p.postalCode)
  );
}

function profileGaps(org: PlatformOrganizationDto): Array<"phone" | "email" | "address"> {
  const gaps: Array<"phone" | "email" | "address"> = [];
  if (!hasText(org.profile.contactPhone)) gaps.push("phone");
  if (!hasText(org.profile.contactEmail)) gaps.push("email");
  if (!hasMeaningfulAddress(org)) gaps.push("address");
  return gaps;
}

function FieldLabel({ children }: { children: string }) {
  return <span className="org-profile-edit-group__label">{children}</span>;
}

function OrgProfileEditDrawer({
  open,
  onClose,
  organizationId,
  organization,
}: {
  open: boolean;
  onClose: () => void;
  organizationId: string;
  organization: PlatformOrganizationDto;
}) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const [displayName, setDisplayName] = useState(organization.displayName);
  const [logoUrl, setLogoUrl] = useState(organization.branding.logoUrl ?? "");
  const [phone, setPhone] = useState(organization.profile.contactPhone ?? "");
  const [email, setEmail] = useState(organization.profile.contactEmail ?? "");
  const [addressLine1, setAddressLine1] = useState(organization.profile.addressLine1 ?? "");
  const [addressLine2, setAddressLine2] = useState(organization.profile.addressLine2 ?? "");
  const [city, setCity] = useState(organization.profile.city ?? "");
  const [region, setRegion] = useState(organization.profile.region ?? "");
  const [postalCode, setPostalCode] = useState(organization.profile.postalCode ?? "");
  const [countryCode, setCountryCode] = useState(organization.profile.countryCode ?? "");
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setDisplayName(organization.displayName);
    setLogoUrl(organization.branding.logoUrl ?? "");
    setPhone(organization.profile.contactPhone ?? "");
    setEmail(organization.profile.contactEmail ?? "");
    setAddressLine1(organization.profile.addressLine1 ?? "");
    setAddressLine2(organization.profile.addressLine2 ?? "");
    setCity(organization.profile.city ?? "");
    setRegion(organization.profile.region ?? "");
    setPostalCode(organization.profile.postalCode ?? "");
    setCountryCode(organization.profile.countryCode ?? "");
    setFormError(null);
  }, [open, organization]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const name = displayName.trim();
      if (!name) {
        throw new Error(t("orgProfile.nameRequired"));
      }
      let latest = await updateOrganizationProfile(organizationId, {
        displayName: name,
        contactPhone: phone.trim() || null,
        contactEmail: email.trim() || null,
        addressLine1: addressLine1.trim() || null,
        addressLine2: addressLine2.trim() || null,
        city: city.trim() || null,
        region: region.trim() || null,
        postalCode: postalCode.trim() || null,
        countryCode: countryCode.trim() || null,
        expectedUpdatedAtUtc: organization.updatedAtUtc,
      });
      const nextLogo = logoUrl.trim() || null;
      if ((latest.branding.logoUrl ?? null) !== nextLogo) {
        latest = await updateOrganizationBranding(organizationId, {
          logoUrl: nextLogo,
          brandDisplayName: latest.branding.brandDisplayName,
          primaryColor: latest.branding.primaryColor,
          accentColor: latest.branding.accentColor,
          expectedUpdatedAtUtc: latest.updatedAtUtc,
        });
      }
      return latest;
    },
    onSuccess: async () => {
      setFormError(null);
      showToast(t("orgProfile.saved"), "success");
      await queryClient.invalidateQueries({ queryKey: ["organization-profile", organizationId] });
      onClose();
    },
    onError: (error) => {
      const message =
        error instanceof PlatformApiError
          ? error.message
          : error instanceof Error
            ? error.message
            : t("orgProfile.saveFailed");
      setFormError(message);
      showToast(message, "error");
    },
  });

  return (
    <FormDrawer
      open={open}
      onOpenChange={(next) => {
        if (!next && !saveMutation.isPending) onClose();
      }}
      title={t("orgProfile.editTitle")}
      testId="org-profile-edit-drawer"
      saveTestId="org-profile-save"
      cancelLabel={t("orgProfile.cancel")}
      saveLabel={saveMutation.isPending ? t("loading.label") : t("orgProfile.save")}
      saving={saveMutation.isPending}
      onSave={() => {
        if (!saveMutation.isPending) saveMutation.mutate();
      }}
    >
      {formError ? (
        <p className="m-0 text-sm text-destructive" role="alert">
          {formError}
        </p>
      ) : null}

      <div className="org-profile-edit-group">
        <p className="org-profile-edit-group__title m-0">{t("orgProfile.section.basics")}</p>
        <label className="flex flex-col gap-1 text-sm">
          <FieldLabel>{t("orgProfile.fields.businessName")}</FieldLabel>
          <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <FieldLabel>{t("orgProfile.fields.logoUrl")}</FieldLabel>
          <Input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} autoComplete="off" />
        </label>
      </div>

      <div className="org-profile-edit-group">
        <p className="org-profile-edit-group__title m-0">{t("orgProfile.section.contact")}</p>
        <label className="flex flex-col gap-1 text-sm">
          <FieldLabel>{t("orgProfile.fields.businessPhone")}</FieldLabel>
          <Input value={phone} onChange={(e) => setPhone(e.target.value)} inputMode="tel" />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <FieldLabel>{t("orgProfile.fields.businessEmail")}</FieldLabel>
          <Input value={email} onChange={(e) => setEmail(e.target.value)} type="email" />
        </label>
      </div>

      <div className="org-profile-edit-group">
        <p className="org-profile-edit-group__title m-0">{t("orgProfile.section.address")}</p>
        <label className="flex flex-col gap-1 text-sm">
          <FieldLabel>{t("orgProfile.fields.addressLine1")}</FieldLabel>
          <Input value={addressLine1} onChange={(e) => setAddressLine1(e.target.value)} />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <FieldLabel>{t("orgProfile.fields.addressLine2")}</FieldLabel>
          <Input value={addressLine2} onChange={(e) => setAddressLine2(e.target.value)} />
        </label>
        <div className="grid grid-cols-2 gap-2">
          <label className="flex flex-col gap-1 text-sm">
            <FieldLabel>{t("orgProfile.fields.city")}</FieldLabel>
            <Input value={city} onChange={(e) => setCity(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <FieldLabel>{t("orgProfile.fields.region")}</FieldLabel>
            <Input value={region} onChange={(e) => setRegion(e.target.value)} />
          </label>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <label className="flex flex-col gap-1 text-sm">
            <FieldLabel>{t("orgProfile.fields.postalCode")}</FieldLabel>
            <Input value={postalCode} onChange={(e) => setPostalCode(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <FieldLabel>{t("orgProfile.fields.countryCode")}</FieldLabel>
            <Input value={countryCode} onChange={(e) => setCountryCode(e.target.value)} />
          </label>
        </div>
      </div>
    </FormDrawer>
  );
}

function ContactRow({
  icon: Icon,
  label,
  value,
  empty,
  testId,
}: {
  icon: typeof Phone;
  label: string;
  value: string | null;
  empty: string;
  testId: string;
}) {
  const filled = hasText(value);
  return (
    <div className="org-profile-contact__row" data-testid={testId} data-empty={filled ? "false" : "true"}>
      <span className="org-profile-contact__icon" aria-hidden>
        <Icon className="size-4" strokeWidth={1.75} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="org-profile-contact__label m-0">{label}</p>
        <p
          className={cn(
            "org-profile-contact__value m-0",
            !filled && "org-profile-contact__value--empty",
          )}
        >
          {filled ? value : empty}
        </p>
      </div>
    </div>
  );
}

export function OrgProfilePage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const { sessionGrant } = useWorkspace();
  const organizationId = sessionGrant?.organizationId ?? null;
  const canEdit = hasOrganizationManagementAuthority(sessionGrant);
  const [editOpen, setEditOpen] = useState(false);
  const [idCopied, setIdCopied] = useState(false);

  const query = useQuery({
    queryKey: ["organization-profile", organizationId],
    enabled: Boolean(organizationId),
    queryFn: ({ signal }) => getOrganization(organizationId!, signal),
  });

  if (!organizationId) {
    return (
      <ErrorState title={t("orgProfile.orgRequired")} detail={t("orgProfile.orgRequiredDetail")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState
        title={t("orgProfile.loadFailed")}
        detail={t("orgProfile.loadFailedDetail")}
        error={query.error}
        operation="getOrganization"
      />
    );
  }

  const org = query.data;
  const address = formatAddress(org);
  const gaps = profileGaps(org);
  const publicId = org.publicOrganizationId?.trim() || null;
  const phone = org.profile.contactPhone?.trim() || null;
  const email = org.profile.contactEmail?.trim() || null;

  async function copyPublicId() {
    if (!publicId) return;
    try {
      await navigator.clipboard.writeText(publicId);
      setIdCopied(true);
      showToast(t("qr.copied"), "success");
      window.setTimeout(() => setIdCopied(false), 1600);
    } catch {
      showToast(t("orgProfile.copyFailed"), "error");
    }
  }

  return (
    <div className="org-profile-page flex flex-col gap-4 p-4" data-testid="org-profile-page">
      <PageHeader
        title={t("orgProfile.title")}
        description={t("orgProfile.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        actions={
          canEdit ? (
            <Button
              type="button"
              variant="outline"
              data-testid="org-profile-edit"
              aria-label={t("orgProfile.edit")}
              onClick={() => setEditOpen(true)}
            >
              <Pencil className="size-4" aria-hidden />
              <span className="org-profile-edit-label">{t("orgProfile.editShort")}</span>
            </Button>
          ) : null
        }
      />

      {gaps.length > 0 ? (
        <Notice
          tone="info"
          title={t("orgProfile.incompleteTitle")}
          testId="org-profile-incomplete"
          action={
            canEdit ? (
              <Button
                type="button"
                variant="outline"
                size="sm"
                data-testid="org-profile-incomplete-edit"
                onClick={() => setEditOpen(true)}
              >
                {t("orgProfile.incompleteAction")}
              </Button>
            ) : null
          }
        >
          {t("orgProfile.incompleteBody")}
        </Notice>
      ) : null}

      <section
        className="catalog-form-section exits-animate-panel org-profile-identity"
        aria-labelledby="org-profile-heading"
        data-testid="org-profile-identity"
      >
        <div className="org-profile-identity__row">
          {org.branding.logoUrl ? (
            <img
              src={org.branding.logoUrl}
              alt=""
              className="org-profile-identity__avatar org-profile-identity__avatar--image"
            />
          ) : (
            <div
              className="org-profile-identity__avatar org-profile-identity__avatar--initials"
              aria-hidden
              data-testid="org-profile-initials"
            >
              {orgInitials(org.displayName)}
            </div>
          )}
          <div className="org-profile-identity__copy min-w-0">
            <h2 id="org-profile-heading" className="org-profile-identity__name m-0">
              {org.displayName}
            </h2>
            <div className="org-profile-identity__id-row">
              <p className="org-profile-identity__id m-0" data-testid="org-profile-public-id">
                {publicId ?? t("orgProfile.publicIdPending")}
              </p>
              {publicId ? (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="org-profile-identity__copy-btn"
                  data-testid="org-profile-copy-id"
                  aria-label={t("orgProfile.copyId")}
                  onClick={() => void copyPublicId()}
                >
                  {idCopied ? (
                    <Check className="size-3.5" aria-hidden />
                  ) : (
                    <Copy className="size-3.5" aria-hidden />
                  )}
                  <span>{idCopied ? t("qr.copied") : t("orgProfile.copyId")}</span>
                </Button>
              ) : null}
            </div>
          </div>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel org-profile-contact"
        aria-labelledby="org-profile-contact-heading"
        data-testid="org-profile-contact"
      >
        <h2 id="org-profile-contact-heading" className="catalog-form-section__title m-0">
          {t("orgProfile.contactTitle")}
        </h2>
        <div className="org-profile-contact__list">
          <ContactRow
            icon={Phone}
            label={t("orgProfile.fields.businessPhone")}
            value={phone}
            empty={t("common.notAvailable")}
            testId="org-profile-phone"
          />
          <ContactRow
            icon={Mail}
            label={t("orgProfile.fields.businessEmail")}
            value={email}
            empty={t("common.notAvailable")}
            testId="org-profile-email"
          />
          <ContactRow
            icon={MapPin}
            label={t("orgProfile.fields.address")}
            value={hasMeaningfulAddress(org) ? address : null}
            empty={
              hasText(org.profile.countryCode) && !hasMeaningfulAddress(org)
                ? t("orgProfile.addressCountryOnly").replace(
                    "{country}",
                    org.profile.countryCode!.trim(),
                  )
                : t("common.notAvailable")
            }
            testId="org-profile-address"
          />
        </div>
      </section>

      <OrgProfileEditDrawer
        open={editOpen}
        onClose={() => setEditOpen(false)}
        organizationId={organizationId}
        organization={org}
      />
    </div>
  );
}

/** Exported for unit tests. */
export const __orgProfileTestUtils = {
  orgInitials,
  formatAddress,
  hasMeaningfulAddress,
  profileGaps,
};
