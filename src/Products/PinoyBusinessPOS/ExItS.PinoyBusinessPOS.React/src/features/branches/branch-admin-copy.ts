import { isWarehouseBranch, type OrganizationBranchType } from "@/features/branches/branch-type";
import type { MessageKey } from "@/i18n/messages";

/** Visible Admin copy for Retail vs Warehouse — domain entity names stay Branch. */
export function branchAdminCopy(branchType: OrganizationBranchType | string | null | undefined) {
  const warehouse = isWarehouseBranch(branchType);
  return {
    warehouse,
    overviewTab: (warehouse
      ? "branches.detail.overview.warehouse"
      : "branches.detail.overview") as MessageKey,
    detailsTab: (warehouse
      ? "branches.detail.details.warehouse"
      : "branches.detail.details") as MessageKey,
    nameLabel: (warehouse ? "branches.create.name.warehouse" : "branches.create.name") as MessageKey,
    codeLabel: (warehouse
      ? "branches.detail.codeReadonly.warehouse"
      : "branches.detail.codeReadonly") as MessageKey,
    detailsTitle: (warehouse
      ? "branches.detailsTitle.warehouse"
      : "branches.detailsTitle") as MessageKey,
    typeLabel: (warehouse ? "branches.type.warehouseLabel" : "branches.type") as MessageKey,
    devicesLabel: (warehouse ? "branches.mgmt.devicesShort" : "branches.mgmt.devices") as MessageKey,
    openLabel: (warehouse ? "branches.mgmt.openWarehouse" : "branches.mgmt.open") as MessageKey,
    lifecycleTitle: (warehouse
      ? "branches.detail.lifecycleTitle.warehouse"
      : "branches.detail.lifecycleTitle") as MessageKey,
    updatedMessage: (warehouse
      ? "branches.detail.updated.warehouse"
      : "branches.detail.updated") as MessageKey,
  };
}
