import { formatCurrency } from "@/lib/format";

export function MoneyDisplay({
  amount,
  currency,
}: {
  amount: number;
  currency?: string | null;
}) {
  return <span className="tabular-nums">{formatCurrency(amount, undefined, currency)}</span>;
}
