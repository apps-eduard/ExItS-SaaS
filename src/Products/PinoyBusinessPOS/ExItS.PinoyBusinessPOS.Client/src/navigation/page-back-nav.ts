import type { MessageKey } from "@/i18n/messages";

/** Canonical back destinations for org child pages (explicit routes, not history). */
export const pageBackNav = {
  managerHome: { to: "/role/manager", labelKey: "nav.backToManagerHome" as MessageKey },
  more: { to: "/more", labelKey: "org.more.back" as MessageKey },
  org: { to: "/org", labelKey: "devices.backOrg" as MessageKey },
  shifts: { to: "/shifts", labelKey: "shift.backToShifts" as MessageKey },
  registers: { to: "/registers", labelKey: "registers.back" as MessageKey },
  customers: { to: "/customers", labelKey: "customers.back" as MessageKey },
  suppliers: { to: "/suppliers", labelKey: "suppliers.back" as MessageKey },
  purchasing: { to: "/purchasing", labelKey: "purchasing.backHub" as MessageKey },
  inventory: { to: "/inventory", labelKey: "inventory.backList" as MessageKey },
  catalog: { to: "/catalog", labelKey: "catalog.back" as MessageKey },
  returns: { to: "/returns", labelKey: "returns.back" as MessageKey },
  orders: { to: "/orders", labelKey: "orders.backToQueue" as MessageKey },
  reports: { to: "/reports", labelKey: "reports.back" as MessageKey },
  orgDevices: { to: "/org/devices", labelKey: "devices.backDevices" as MessageKey },
  orgBranches: { to: "/org/branches", labelKey: "branches.backList" as MessageKey },
  connectedBuyers: { to: "/suppliers/connected/buyers", labelKey: "connected.backToBuyers" as MessageKey },
} as const;

export type PageBackNavTarget = (typeof pageBackNav)[keyof typeof pageBackNav];
