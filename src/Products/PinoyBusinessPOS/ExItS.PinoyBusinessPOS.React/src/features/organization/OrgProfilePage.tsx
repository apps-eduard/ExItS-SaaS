import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil } from "lucide-react";
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
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function formatAddress(org: PlatformOrganizationDto): string | null {
  const p = org.profile;
  const parts = [
    p.addressLine1,
    p.addressLine2,
    [p.city, p.region].filter(Boolean).join(", ") || null,
    p.postalCode,
    p.countryCode,
  ]
    .map((x) => x?.trim())
    .filter(Boolean);
  return parts.length > 0 ? parts.join(", ") : null;
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
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("orgProfile.fields.logoUrl")}</span>
        <Input value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} autoComplete="off" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("orgProfile.fields.businessName")}</span>
        <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("orgProfile.fields.businessPhone")}</span>
        <Input value={phone} onChange={(e) => setPhone(e.target.value)} inputMode="tel" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("orgProfile.fields.businessEmail")}</span>
        <Input value={email} onChange={(e) => setEmail(e.target.value)} type="email" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("orgProfile.fields.addressLine1")}</span>
        <Input value={addressLine1} onChange={(e) => setAddressLine1(e.target.value)} />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        <span>{t("orgProfile.fields.addressLine2")}</span>
        <Input value={addressLine2} onChange={(e) => setAddressLine2(e.target.value)} />
      </label>
      <div className="grid grid-cols-2 gap-2">
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("orgProfile.fields.city")}</span>
          <Input value={city} onChange={(e) => setCity(e.target.value)} />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("orgProfile.fields.region")}</span>
          <Input value={region} onChange={(e) => setRegion(e.target.value)} />
        </label>
      </div>
      <div className="grid grid-cols-2 gap-2">
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("orgProfile.fields.postalCode")}</span>
          <Input value={postalCode} onChange={(e) => setPostalCode(e.target.value)} />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span>{t("orgProfile.fields.countryCode")}</span>
          <Input value={countryCode} onChange={(e) => setCountryCode(e.target.value)} />
        </label>
      </div>
    </FormDrawer>
  );
}

export function OrgProfilePage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const organizationId = sessionGrant?.organizationId ?? null;
  const canEdit = hasOrganizationManagementAuthority(sessionGrant);
  const [editOpen, setEditOpen] = useState(false);

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

  return (
    <div className="flex flex-col gap-4 p-4" data-testid="org-profile-page">
      <PageHeader
        title={t("orgProfile.title")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        actions={
          canEdit ? (
            <Button type="button" variant="outline" data-testid="org-profile-edit" onClick={() => setEditOpen(true)}>
              <Pencil className="size-4" aria-hidden />
              {t("orgProfile.edit")}
            </Button>
          ) : null
        }
      />

      <section className="flex flex-col gap-3" aria-labelledby="org-profile-heading">
        <div className="flex items-start gap-3">
          {org.branding.logoUrl ? (
            <img src={org.branding.logoUrl} alt="" className="size-16 rounded-lg object-cover" />
          ) : (
            <div className="flex size-16 items-center justify-center rounded-lg bg-muted text-lg font-semibold">
              {org.displayName.slice(0, 2).toUpperCase()}
            </div>
          )}
          <div className="min-w-0">
            <h2 id="org-profile-heading" className="m-0 text-lg font-semibold">
              {org.displayName}
            </h2>
            <p className="m-0 text-sm text-muted-foreground" data-testid="org-profile-public-id">
              {org.publicOrganizationId ?? t("orgProfile.publicIdPending")}
            </p>
          </div>
        </div>

        <dl className="m-0 grid gap-2 text-sm">
          <div>
            <dt className="text-muted-foreground">{t("orgProfile.fields.businessPhone")}</dt>
            <dd className="m-0">{org.profile.contactPhone?.trim() || t("common.notAvailable")}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("orgProfile.fields.businessEmail")}</dt>
            <dd className="m-0">{org.profile.contactEmail?.trim() || t("common.notAvailable")}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">{t("orgProfile.fields.address")}</dt>
            <dd className="m-0">{address ?? t("common.notAvailable")}</dd>
          </div>
        </dl>
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
