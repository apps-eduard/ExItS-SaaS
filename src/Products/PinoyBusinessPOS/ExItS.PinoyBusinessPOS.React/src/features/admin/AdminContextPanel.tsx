import { useQuery } from "@tanstack/react-query";
import { getBranchCapacity } from "@/api/platform/organization-branches-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import { getPosDeviceCapacity } from "@/api/platform/pos-devices-client";
import {
  canInviteOrganizationStaff,
  canManageStoreAreas,
  hasOrganizationManagementAuthority,
} from "@/access/pos-capabilities";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

function UsageMeter({
  label,
  used,
  allowed,
  testId,
}: {
  label: string;
  used: number;
  allowed: number;
  testId: string;
}) {
  const pct = allowed > 0 ? Math.min(100, Math.round((used / allowed) * 100)) : 0;
  const atLimit = allowed > 0 && used >= allowed;
  return (
    <li className="admin-context-panel__meter" data-testid={testId}>
      <div className="admin-context-panel__row">
        <span>{label}</span>
        <strong>
          {used} / {allowed}
        </strong>
      </div>
      {allowed > 0 ? (
        <div
          className="admin-context-panel__track"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={allowed}
          aria-valuenow={used}
          aria-label={label}
        >
          <span
            className={cn(
              "admin-context-panel__fill",
              atLimit && "admin-context-panel__fill--limit",
            )}
            style={{ width: `${pct}%` }}
          />
        </div>
      ) : null}
    </li>
  );
}

/**
 * XL-only contextual panel. Shows real capacity data only — no invented metrics.
 */
export function AdminContextPanel() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canAdmin = hasOrganizationManagementAuthority(sessionGrant);
  const canInvite = canInviteOrganizationStaff(sessionGrant);
  const areasEntitled = canManageStoreAreas(sessionGrant);

  const branchCapacityQuery = useQuery({
    queryKey: ["admin-context-branch-capacity", organizationId],
    enabled: Boolean(organizationId && canInvite),
    queryFn: async ({ signal }) => {
      const result = await getBranchCapacity(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "capacity");
      return result.value;
    },
  });

  const areasQuery = useQuery({
    queryKey: ["admin-context-areas", organizationId],
    enabled: Boolean(organizationId && canInvite && areasEntitled),
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "areas");
      return result.value;
    },
  });

  const deviceCapacityQuery = useQuery({
    queryKey: ["admin-context-device-capacity", organizationId],
    enabled: Boolean(organizationId && canAdmin),
    queryFn: async ({ signal }) => {
      const result = await getPosDeviceCapacity(organizationId!, signal);
      if (!result.ok) throw new Error(result.body?.detail ?? "devices");
      return result.value;
    },
  });

  const hasAny =
    branchCapacityQuery.data ||
    areasQuery.data ||
    deviceCapacityQuery.data;

  if (!hasAny && !branchCapacityQuery.isLoading && !areasQuery.isLoading && !deviceCapacityQuery.isLoading) {
    return null;
  }

  return (
    <aside
      className="admin-context-panel"
      data-testid="admin-context-panel"
      aria-label={t("admin.context.aria")}
    >
      <h2 className="admin-context-panel__title m-0">{t("admin.context.usageTitle")}</h2>
      <ul className="m-0 mt-3 list-none space-y-3 p-0">
        {branchCapacityQuery.data ? (
          <UsageMeter
            label={t("admin.context.branches")}
            used={branchCapacityQuery.data.used}
            allowed={branchCapacityQuery.data.allowed}
            testId="admin-context-branches"
          />
        ) : null}
        {areasQuery.data && areasQuery.data.maxAreas > 0 ? (
          <UsageMeter
            label={t("admin.context.areas")}
            used={areasQuery.data.activeAreaCount}
            allowed={areasQuery.data.maxAreas}
            testId="admin-context-areas"
          />
        ) : null}
        {deviceCapacityQuery.data ? (
          <UsageMeter
            label={t("admin.context.devices")}
            used={deviceCapacityQuery.data.used}
            allowed={deviceCapacityQuery.data.allowed}
            testId="admin-context-devices"
          />
        ) : null}
      </ul>
    </aside>
  );
}
