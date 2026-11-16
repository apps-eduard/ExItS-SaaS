export type TransactionStatus = "Posted" | "Pending" | "Failed" | "Refunded";

export type Transaction = {
  id: string;
  reference: string;
  customer: string;
  description: string;
  date: string;
  category: string;
  amount: number;
  status: TransactionStatus;
};

export const TRANSACTION_CATEGORIES = ["Sales", "Supplies", "Utilities", "Refund", "Payroll"] as const;

export const TRANSACTION_STATUSES: readonly TransactionStatus[] = ["Posted", "Pending", "Failed", "Refunded"];

const CUSTOMERS = [
  "Amina Cruz",
  "Jonah Reyes",
  "Liza Mercado",
  "Paolo Santos",
  "Rina Bautista",
  "Marco Dela Cruz",
  "Elena Villanueva",
  "Theo Navarro",
] as const;

const DESCRIPTIONS = [
  "Counter sale",
  "Inventory restock",
  "Power and water",
  "Customer refund",
  "Staff payroll",
  "Weekend market",
  "Packaging supplies",
  "Delivery fee",
] as const;

function mulberry32(seed: number) {
  let value = seed >>> 0;
  return () => {
    value = (value + 0x6d2b79f5) >>> 0;
    let next = value;
    next = Math.imul(next ^ (next >>> 15), next | 1);
    next ^= next + Math.imul(next ^ (next >>> 7), next | 61);
    return ((next ^ (next >>> 14)) >>> 0) / 4294967296;
  };
}

export function createTransactions(count: number, seed = 11): Transaction[] {
  const random = mulberry32(seed);
  const rows: Transaction[] = [];
  for (let index = 0; index < count; index += 1) {
    const reference = `TX-${String(index + 1).padStart(4, "0")}`;
    const day = String((index % 27) + 1).padStart(2, "0");
    const month = String((index % 12) + 1).padStart(2, "0");
    rows.push({
      id: reference,
      reference,
      customer: CUSTOMERS[Math.floor(random() * CUSTOMERS.length)] ?? CUSTOMERS[0],
      description: DESCRIPTIONS[Math.floor(random() * DESCRIPTIONS.length)] ?? DESCRIPTIONS[0],
      date: `2026-${month}-${day}`,
      category: TRANSACTION_CATEGORIES[Math.floor(random() * TRANSACTION_CATEGORIES.length)] ?? "Sales",
      amount: Math.round((random() * 9800 + 40) * 100) / 100,
      status: TRANSACTION_STATUSES[Math.floor(random() * TRANSACTION_STATUSES.length)] ?? "Posted",
    });
  }
  return rows;
}

export function formatPeso(amount: number) {
  return new Intl.NumberFormat("en-PH", { style: "currency", currency: "PHP" }).format(amount);
}

export function formatLedgerDate(iso: string) {
  const [year, month, day] = iso.split("-").map(Number);
  if (!year || !month || !day) return iso;
  return new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric", year: "numeric", timeZone: "UTC" }).format(
    new Date(Date.UTC(year, month - 1, day)),
  );
}
