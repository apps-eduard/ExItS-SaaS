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
  buildUpdateOrganizationBody,
  organizationProfileFormValues,
  type OrganizationProfileFormValues,
} from "@/features/organizations/organization-admin-mapping";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import { useUpdateOrganizationMutation } from "@/features/organizations/use-organization-mutations";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

const PROFILE_FIELDS: Array<{ key: keyof OrganizationProfileFormValues; label: MessageKey }> = [
  { key: "legalName", label: "organization.field.legalName" },
  { key: "contactEmail", label: "organization.field.contactEmail" },
  { key: "contactPhone", label: "organization.field.contactPhone" },
  { key: "addressLine1", label: "organization.field.addressLine1" },
  { key: "addressLine2", label: "organization.field.addressLine2" },
  { key: "city", label: "organization.field.city" },
  { key: "region", label: "organization.field.region" },
  { key: "postalCode", label: "organization.field.postalCode" },
  { key: "countryCode", label: "organization.field.countryCode" },
  { key: "timeZoneId", label: "organization.field.timeZoneId" },
  { key: "locale", label: "organization.field.locale" },
  { key: "currencyCode", label: "organization.field.currencyCode" },
];

export function OrganizationProfileEditor({ organization }: { organization: OrganizationDetail }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageOrganizations);
  const mutation = useUpdateOrganizationMutation();
  const [values, setValues] = useState(() => organizationProfileFormValues(organization));
  const [feedback, setFeedback] = useState<{ tone: "danger" | "info"; title: string; detail: string } | null>(
    null,
  );

  const readOnly = !canManage || organization.status === "Closed";

  function updateField<K extends keyof OrganizationProfileFormValues>(key: K, value: OrganizationProfileFormValues[K]) {
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
        body: buildUpdateOrganizationBody(values, organization, { includeSlug: canManage }),
      });
      setFeedback({
        tone: "info",
        title: t("organization.profile.save.success"),
        detail: "",
      });
    } catch (error) {
      const copy = organizationMutationFailureCopy(error, t);
      setFeedback({ tone: "danger", title: copy.title, detail: copy.detail });
      if (classifyCommercialMutationFailure(error).kind === "conflict") {
        setValues(organizationProfileFormValues(organization));
      }
    }
  }

  return (
    <DashboardSection
      title={t("organization.overview.profile")}
      description={readOnly ? t("organization.profile.readOnly") : undefined}
    >
      {feedback ? (
        <Alert title={feedback.title} tone={feedback.tone === "danger" ? "danger" : "info"}>
          {feedback.detail}
        </Alert>
      ) : null}
      <div className="grid gap-3">
        <div className="grid gap-1">
          <Label htmlFor="org-display-name">{t("organizations.column.organization")}</Label>
          <Input
            id="org-display-name"
            value={values.displayName}
            disabled={readOnly || mutation.isPending}
            onChange={(event) => updateField("displayName", event.target.value)}
          />
        </div>
        {canManage ? (
          <div className="grid gap-1">
            <Label htmlFor="org-slug">{t("organizations.column.identifier")}</Label>
            <Input
              id="org-slug"
              value={values.slug}
              disabled={readOnly || mutation.isPending}
              onChange={(event) => updateField("slug", event.target.value)}
            />
          </div>
        ) : null}
        <div className="grid gap-3 sm:grid-cols-2">
          {PROFILE_FIELDS.map((field) => (
            <div key={field.key} className="grid gap-1">
              <Label htmlFor={`org-profile-${field.key}`}>{t(field.label)}</Label>
              <Input
                id={`org-profile-${field.key}`}
                value={values[field.key]}
                disabled={readOnly || mutation.isPending}
                onChange={(event) => updateField(field.key, event.target.value)}
              />
            </div>
          ))}
        </div>
        {!readOnly ? (
          <div>
            <Button type="button" size="sm" disabled={mutation.isPending} onClick={() => void save()}>
              <Save aria-hidden className="mr-2 size-4" />
              {t("organization.profile.save")}
            </Button>
          </div>
        ) : null}
      </div>
    </DashboardSection>
  );
}
