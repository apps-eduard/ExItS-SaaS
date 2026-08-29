import type { MessageKey } from "@/i18n/messages";

/** Canonical back destinations for org child pages (explicit routes, not history). */
export const pageBackNav = {
  managerHome: { to: "/role/manager", labelKey: "nav.backToManagerHome" as MessageKey },
  more: { to: "/more", labelKey: "org.more.back" as MessageKey },
  org: { to: "/org", labelKey: "devices.backOrg" as MessageKey },
  orgStaff: { to: "/org/staff", labelKey: "staffManage.back" as MessageKey },
  shifts: { to: "/shifts", labelKey: "shift.backToShifts" as MessageKey },
  registers: { to: "/registers", labelKey: "registers.back" as MessageKey },
  customers: { to: "/customers", labelKey: "customers.back" as MessageKey },
  suppliers: { to: "/suppliers", labelKey: "suppliers.back" as MessageKey },
  purchasing: { to: "/purchasing", labelKey: "purchasing.backHub" as MessageKey },
  inventory: { to: "/inventory", labelKey: "inventory.backList" as MessageKey },
  catalog: { to: "/catalog", labelKey: "catalog.back" as MessageKey },
  returns: { to: "/returns", labelKey: "returns.back" as MessageKey },
  expenses: { to: "/expenses", labelKey: "expense.backList" as MessageKey },
  orders: { to: "/orders", labelKey: "orders.backToQueue" as MessageKey },
  reports: { to: "/reports", labelKey: "reports.back" as MessageKey },
  orgDevices: { to: "/org/devices", labelKey: "devices.backDevices" as MessageKey },
  orgBranches: { to: "/org/branches", labelKey: "branches.backList" as MessageKey },
  connectedBuyers: { to: "/suppliers/connected/buyers", labelKey: "connected.backToBuyers" as MessageKey },
} as const;

/**
 * Canonical back destinations for Personal child pages (explicit routes, not history).
 * Omit on bottom-nav roots except when product UX needs an explicit parent
 * (Utang / Todo / Orders hubs → Home). Still omit on: Home, More.
 */
export const personalPageBackNav = {
  home: { to: "/personal", labelKey: "personal.nav.home" as MessageKey },
  more: { to: "/personal/more", labelKey: "personal.more.back" as MessageKey },
  utang: { to: "/personal/utang", labelKey: "personal.nav.utang" as MessageKey },
  utangLent: { to: "/personal/utang/lent", labelKey: "personal.utang.lent" as MessageKey },
  utangOwe: { to: "/personal/utang/owe", labelKey: "personal.utang.owe" as MessageKey },
  todo: { to: "/personal/todo", labelKey: "personal.todo.back" as MessageKey },
  orders: { to: "/personal/orders", labelKey: "personal.nav.orders" as MessageKey },
  merchants: {
    to: "/personal/linked-merchants",
    labelKey: "personal.backToMerchants" as MessageKey,
  },
  explore: {
    to: "/personal/explore-pos",
    labelKey: "personal.startBusiness.changePlan" as MessageKey,
  },
} as const;

export type PageBackNavTarget = (typeof pageBackNav)[keyof typeof pageBackNav];
export type PersonalPageBackNavTarget =
  (typeof personalPageBackNav)[keyof typeof personalPageBackNav];
