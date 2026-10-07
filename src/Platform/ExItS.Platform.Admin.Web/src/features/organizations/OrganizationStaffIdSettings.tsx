import { useState } from "react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { platformRequest } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { useQuery } from "@tanstack/react-query";

type StaffIdSettings = {
  prefix: string;
  nextNumber: number;
  padDigits: number;
};

export function OrganizationStaffIdSettings({ organizationId }: { organizationId: string }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageOrganizations);
  const query = useQuery({
    queryKey: ["organization-staff-id-settings", organizationId],
    queryFn: () =>
      platformRequest<StaffIdSettings>({
        path: `/api/v1/platform/organizations/${organizationId}/staff-id-settings`,
      }),
  });
  const [draft, setDraft] = useState<StaffIdSettings | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const values = draft ?? (query.data
    ? {
        prefix: query.data.prefix,
        nextNumber: query.data.nextNumber,
        padDigits: query.data.padDigits,
      }
    : null);

  async function save() {
    if (!values || !canManage || pending) {
      return;
    }
    setPending(true);
    setError(null);
    setMessage(null);
    try {
      const saved = await platformRequest<StaffIdSettings>({
        method: "PUT",
        path: `/api/v1/platform/organizations/${organizationId}/staff-id-settings`,
        body: {
          prefix: values.prefix,
          nextNumber: Number(values.nextNumber),
          padDigits: Number(values.padDigits),
        },
      });
      setDraft({
        prefix: saved.prefix,
        nextNumber: saved.nextNumber,
        padDigits: saved.padDigits,
      });
      setMessage(t("organization.staffId.saved"));
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : t("organization.staffId.title"));
    } finally {
      setPending(false);
    }
  }

  return (
    <DashboardSection title={t("organization.staffId.title")} description={t("organization.staffId.description")}>
      {error ? <Alert tone="danger" title={error} /> : null}
      {message ? <Alert tone="info" title={message} /> : null}
      {values ? (
        <div className="grid gap-3 sm:grid-cols-3">
          <div className="flex flex-col gap-1">
            <Label htmlFor="staff-id-prefix">{t("organization.staffId.prefix")}</Label>
            <Input
              id="staff-id-prefix"
              value={values.prefix}
              disabled={!canManage || pending}
              onChange={(event) => setDraft({ ...values, prefix: event.target.value })}
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label htmlFor="staff-id-next">{t("organization.staffId.next")}</Label>
            <Input
              id="staff-id-next"
              type="number"
              min={1}
              value={values.nextNumber}
              disabled={!canManage || pending}
              onChange={(event) => setDraft({ ...values, nextNumber: Number(event.target.value) })}
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label htmlFor="staff-id-pad">{t("organization.staffId.pad")}</Label>
            <Input
              id="staff-id-pad"
              type="number"
              min={1}
              max={8}
              value={values.padDigits}
              disabled={!canManage || pending}
              onChange={(event) => setDraft({ ...values, padDigits: Number(event.target.value) })}
            />
          </div>
          {canManage ? (
            <div className="sm:col-span-3">
              <Button type="button" onClick={() => void save()} disabled={pending}>
                {t("organization.staffId.save")}
              </Button>
            </div>
          ) : null}
        </div>
      ) : null}
    </DashboardSection>
  );
}
