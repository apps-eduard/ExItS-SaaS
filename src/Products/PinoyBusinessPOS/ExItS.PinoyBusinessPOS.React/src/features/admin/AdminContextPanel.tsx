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
      <ul className="m-0 mt-3 list-none space-y-2 p-0">
        {branchCapacityQuery.data ? (
          <li className="admin-context-panel__row" data-testid="admin-context-branches">
            <span>{t("admin.context.branches")}</span>
            <strong>
              {branchCapacityQuery.data.used}/{branchCapacityQuery.data.allowed}
            </strong>
          </li>
        ) : null}
        {areasQuery.data && areasQuery.data.maxAreas > 0 ? (
          <li className="admin-context-panel__row" data-testid="admin-context-areas">
            <span>{t("admin.context.areas")}</span>
            <strong>
              {areasQuery.data.activeAreaCount}/{areasQuery.data.maxAreas}
            </strong>
          </li>
        ) : null}
        {deviceCapacityQuery.data ? (
          <li className="admin-context-panel__row" data-testid="admin-context-devices">
            <span>{t("admin.context.devices")}</span>
            <strong>
              {deviceCapacityQuery.data.used}/{deviceCapacityQuery.data.allowed}
            </strong>
          </li>
        ) : null}
      </ul>
    </aside>
  );
}
