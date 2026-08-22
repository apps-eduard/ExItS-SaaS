import { useState } from "react";
import { Save } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import type { OrganizationDetail } from "@/api/organizations/organization-types";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  buildUpdateOrganizationBrandingBody,
  organizationBrandingFormValues,
  type OrganizationBrandingFormValues,
} from "@/features/organizations/organization-admin-mapping";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import { useUpdateOrganizationBrandingMutation } from "@/features/organizations/use-organization-mutations";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";

export function OrganizationBrandingEditor({ organization }: { organization: OrganizationDetail }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageOrganizations);
  const mutation = useUpdateOrganizationBrandingMutation();
  const [values, setValues] = useState(() => organizationBrandingFormValues(organization));
  const [feedback, setFeedback] = useState<{ tone: "danger" | "info"; title: string; detail: string } | null>(
    null,
  );

  const readOnly = !canManage || organization.status === "Closed";
  const previewName = values.brandDisplayName.trim() || organization.displayName;
  const primary = values.primaryColor.trim() || "#1677FF";
  const accent = values.accentColor.trim() || "#08979C";

  function updateField<K extends keyof OrganizationBrandingFormValues>(
    key: K,
    value: OrganizationBrandingFormValues[K],
  ) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  async function save() {
    if (readOnly || mutation.isPending) {
      return;
    }
    setFeedback(null);
    try {
      await mutation.mutateAsync({
        organizationId: organization.id,
        body: buildUpdateOrganizationBrandingBody(values, organization),
      });
      setFeedback({ tone: "info", title: t("organization.branding.save.success"), detail: "" });
    } catch (error) {
      const copy = organizationMutationFailureCopy(error, t);
      setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
      if (classifyCommercialMutationFailure(error).kind === "conflict") {
        setValues(organizationBrandingFormValues(organization));
      }
    }
  }

  return (
    <DashboardSection
      title={t("organization.overview.branding")}
      description={readOnly ? t("organization.branding.readOnly") : undefined}
    >
      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone === "danger" ? "danger" : "info"}>
          {feedback.detail}
        </Alert>
      ) : null}
      <div className="grid gap-4 lg:grid-cols-2">
        <div className="grid gap-3">
          <div className="grid gap-1">
            <Label htmlFor="org-brand-name">{t("organization.field.brandDisplayName")}</Label>
            <Input
              id="org-brand-name"
              value={values.brandDisplayName}
              disabled={readOnly || mutation.isPending}
              onChange={(event) => updateField("brandDisplayName", event.target.value)}
            />
          </div>
          <div className="grid gap-1">
            <Label htmlFor="org-brand-logo">{t("organization.field.logoUrl")}</Label>
            <Input
              id="org-brand-logo"
              value={values.logoUrl}
              disabled={readOnly || mutation.isPending}
              onChange={(event) => updateField("logoUrl", event.target.value)}
            />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="grid gap-1">
              <Label htmlFor="org-brand-primary">{t("organization.field.primaryColor")}</Label>
              <Input
                id="org-brand-primary"
                value={values.primaryColor}
                disabled={readOnly || mutation.isPending}
                onChange={(event) => updateField("primaryColor", event.target.value)}
              />
            </div>
            <div className="grid gap-1">
              <Label htmlFor="org-brand-accent">{t("organization.field.accentColor")}</Label>
              <Input
                id="org-brand-accent"
                value={values.accentColor}
                disabled={readOnly || mutation.isPending}
                onChange={(event) => updateField("accentColor", event.target.value)}
              />
            </div>
          </div>
          {!readOnly ? (
            <div>
              <Button type="button" size="sm" disabled={mutation.isPending} onClick={() => void save()}>
                <Save aria-hidden className="mr-2 size-4" />
                {t("organization.branding.save")}
              </Button>
            </div>
          ) : null}
        </div>
        <div className="min-w-0">
          <p className="mb-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
            {t("organization.branding.preview")}
          </p>
          <div className="overflow-hidden rounded-[var(--exits-density-radius)] border border-border">
            <div className="flex items-center gap-3 bg-surface-muted px-4 py-3">
              {values.logoUrl.startsWith("https://") ? (
                <img
                  src={values.logoUrl}
                  alt=""
                  className="h-7 max-w-[120px] rounded bg-white object-contain p-0.5"
                />
              ) : (
                <span
                  aria-hidden="true"
                  className="size-8 shrink-0 rounded-[var(--exits-density-radius)] border border-border"
                  style={{ backgroundColor: primary }}
                />
              )}
              <strong className="text-foreground">{previewName}</strong>
            </div>
            <div className="flex flex-wrap gap-3 bg-surface p-4 text-[length:var(--exits-text-sm)] text-foreground">
              <span className="inline-flex items-center gap-2">
                <span
                  aria-hidden="true"
                  className="size-4 rounded-sm border border-border"
                  style={{ backgroundColor: primary }}
                />
                {t("organization.branding.preview.primary")}
              </span>
              <span className="inline-flex items-center gap-2">
                <span
                  aria-hidden="true"
                  className="size-4 rounded-sm border border-border"
                  style={{ backgroundColor: accent }}
                />
                {t("organization.branding.preview.accent")}
              </span>
            </div>
          </div>
        </div>
      </div>
    </DashboardSection>
  );
}
