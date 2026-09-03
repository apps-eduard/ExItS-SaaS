import { cn } from "@/lib/utils";

export type PricingComparisonRow = {
  feature: string;
  availability: string;
};

export function ExItsPricingComparisonTable({
  caption,
  rows,
}: {
  caption: string;
  rows: PricingComparisonRow[];
}) {
  return (
    <div className="overflow-x-auto rounded-xl border border-borderDefault">
      <table className="min-w-full border-collapse text-left text-sm">
        <caption className="sr-only">{caption}</caption>
        <thead className="bg-surface">
          <tr className="border-b border-borderDefault">
            <th scope="col" className="px-4 py-4 font-semibold text-primary sm:px-6">
              Capability
            </th>
            <th scope="col" className="px-4 py-4 font-semibold text-primary sm:px-6">
              In Pinoy Business POS
            </th>
            <th scope="col" className="px-4 py-4 font-semibold text-primary sm:px-6">
              Plan packaging
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr
              key={row.feature}
              className={cn(
                "border-b border-borderDefault last:border-b-0",
                index % 2 === 1 && "bg-surface/40",
              )}
            >
              <th scope="row" className="px-4 py-4 font-medium text-primary sm:px-6">
                {row.feature}
              </th>
              <td className="px-4 py-4 text-muted sm:px-6">{row.availability}</td>
              <td className="px-4 py-4 text-muted sm:px-6">TBD</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
