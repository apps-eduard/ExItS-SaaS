/**
 * Isolated fixtures for design-system / offline demos only.
 * Live Home rendering must not import these values.
 */
export type MetricCard = {
  id: string;
  labelKey:
    | "metricSales"
    | "metricOrders"
    | "metricReceivables"
    | "metricReminders";
  value: number;
  kind: "currency" | "count";
  delta: string;
  deltaTone: "positive" | "negative" | "neutral" | "warning";
};

export type ActivityItem = {
  id: string;
  title: string;
  detail: string;
  at: string;
  tone: "primary" | "personal" | "success" | "warning";
};

export type ReminderItem = {
  id: string;
  title: string;
  dueAt: string;
  priority: "high" | "medium" | "low";
};

export type QuickAction = {
  id: string;
  labelKey: "qaNewSale" | "qaAddPerson" | "qaRecordPayment" | "qaViewInventory";
  tone: "primary" | "personal" | "secondary" | "muted";
};

export const mockProfile = {
  name: "Alex Santos",
  email: "alex@example.com",
  initials: "AS",
};

export const mockMetrics: MetricCard[] = [
  {
    id: "sales",
    labelKey: "metricSales",
    value: 128_450,
    kind: "currency",
    delta: "+12%",
    deltaTone: "positive",
  },
  {
    id: "orders",
    labelKey: "metricOrders",
    value: 18,
    kind: "count",
    delta: "+3",
    deltaTone: "positive",
  },
  {
    id: "receivables",
    labelKey: "metricReceivables",
    value: 42_780,
    kind: "currency",
    delta: "-4%",
    deltaTone: "negative",
  },
  {
    id: "reminders",
    labelKey: "metricReminders",
    value: 5,
    kind: "count",
    delta: "2 due",
    deltaTone: "warning",
  },
];

export const mockQuickActions: QuickAction[] = [
  { id: "sale", labelKey: "qaNewSale", tone: "primary" },
  { id: "person", labelKey: "qaAddPerson", tone: "personal" },
  { id: "payment", labelKey: "qaRecordPayment", tone: "secondary" },
  { id: "inventory", labelKey: "qaViewInventory", tone: "muted" },
];

export const mockActivity: ActivityItem[] = [
  {
    id: "a1",
    title: "Sale completed · Branch Main",
    detail: "Walk-in · Cash",
    at: new Date(Date.now() - 18 * 60_000).toISOString(),
    tone: "success",
  },
  {
    id: "a2",
    title: "Payment recorded · Maria Cruz",
    detail: "Utang · Partial",
    at: new Date(Date.now() - 2 * 60 * 60_000).toISOString(),
    tone: "personal",
  },
  {
    id: "a3",
    title: "Low stock · Premium coffee 1kg",
    detail: "Inventory alert",
    at: new Date(Date.now() - 5 * 60 * 60_000).toISOString(),
    tone: "warning",
  },
  {
    id: "a4",
    title: "Shift opened · Cashier 2",
    detail: "Downtown branch",
    at: new Date(Date.now() - 8 * 60 * 60_000).toISOString(),
    tone: "primary",
  },
];

export const mockReminders: ReminderItem[] = [
  {
    id: "r1",
    title: "Collect payment from Juan Dela Cruz",
    dueAt: new Date(Date.now() + 3 * 60 * 60_000).toISOString(),
    priority: "high",
  },
  {
    id: "r2",
    title: "Restock cooking oil (5L)",
    dueAt: new Date(Date.now() + 26 * 60 * 60_000).toISOString(),
    priority: "medium",
  },
  {
    id: "r3",
    title: "Review weekend shift schedule",
    dueAt: new Date(Date.now() + 48 * 60 * 60_000).toISOString(),
    priority: "low",
  },
];
