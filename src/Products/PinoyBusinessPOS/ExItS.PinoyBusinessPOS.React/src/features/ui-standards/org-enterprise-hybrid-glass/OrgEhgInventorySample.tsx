import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { MoreHorizontal } from "lucide-react";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";

type StockRow = {
  name: string;
  sku: string;
  branch: string;
  onHand: number;
  reserved: number;
  requestCommitted: number;
  available: number;
  reorder: number;
  status: "OK" | "Reorder" | "Critical";
};

/** Available = On Hand − Reserved − Stock Request Committed (illustrative semantics). */
const PRODUCTS: StockRow[] = [
  {
    name: "Copra Oil 1L",
    sku: "OIL-COP-1L",
    branch: "Quiapo",
    onHand: 18,
    reserved: 2,
    requestCommitted: 4,
    available: 12,
    reorder: 12,
    status: "Reorder",
  },
  {
    name: "Sardines 155g",
    sku: "CAN-SAR-155",
    branch: "Quiapo",
    onHand: 80,
    reserved: 6,
    requestCommitted: 10,
    available: 64,
    reorder: 24,
    status: "OK",
  },
  {
    name: "Instant Coffee 50s",
    sku: "BEV-COF-50",
    branch: "Makati",
    onHand: 9,
    reserved: 1,
    requestCommitted: 5,
    available: 3,
    reorder: 10,
    status: "Critical",
  },
];

const REQUEST_STATES = [
  "Pending",
  "Approved",
  "Preparing",
  "In Transit",
  "Partially Fulfilled",
  "Fulfilled",
  "Rejected",
  "Cancelled",
] as const;

const REQUEST_TONE: Record<(typeof REQUEST_STATES)[number], "neutral" | "info" | "warning" | "success" | "danger"> = {
  Pending: "warning",
  Approved: "info",
  Preparing: "info",
  "In Transit": "info",
  "Partially Fulfilled": "warning",
  Fulfilled: "success",
  Rejected: "danger",
  Cancelled: "neutral",
};

export function OrgEhgInventorySample() {
  return (
    <div className="org-ehg" data-org-ehg data-testid="org-ehg-inventory">
      <p className="org-ehg__banner">
        <strong>Inventory &amp; warehouse</strong> — terminology matches ExItS semantics.{" "}
        <em>Stock Request Committed</em> is distinct from <em>Reserved</em>. No mutations.
      </p>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Branch stock positions</h4>
          <Button type="button" intent="primary" appearance="outline" size="sm">
            New stock request
          </Button>
        </div>
        <div className="org-ehg__panel-body" style={{ padding: 0, overflowX: "auto" }}>
          <table className="org-ehg__table" data-testid="org-ehg-inventory-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>SKU</th>
                <th>Branch</th>
                <th className="org-ehg__num">On hand</th>
                <th className="org-ehg__num">Reserved</th>
                <th className="org-ehg__num">Request committed</th>
                <th className="org-ehg__num">Available</th>
                <th className="org-ehg__num">Reorder</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {PRODUCTS.map((p) => (
                <tr key={p.sku + p.branch}>
                  <td>
                    <strong>{p.name}</strong>
                  </td>
                  <td>{p.sku}</td>
                  <td>{p.branch}</td>
                  <td className="org-ehg__num">{p.onHand}</td>
                  <td className="org-ehg__num">{p.reserved}</td>
                  <td className="org-ehg__num">{p.requestCommitted}</td>
                  <td className="org-ehg__num">
                    <strong>{p.available}</strong>
                  </td>
                  <td className="org-ehg__num">{p.reorder}</td>
                  <td>
                    <StatusChip
                      tone={p.status === "OK" ? "success" : p.status === "Critical" ? "danger" : "warning"}
                      shape="soft"
                    >
                      {p.status === "Reorder" ? "Reorder alert" : p.status}
                    </StatusChip>
                  </td>
                  <td>
                    <ExitsTooltip content="Product actions (demo)" placement="top">
                      <Button
                        type="button"
                        intent="neutral"
                        appearance="ghost"
                        size="sm"
                        aria-label={`Actions for ${p.name}`}
                      >
                        <MoreHorizontal className="size-4" aria-hidden />
                      </Button>
                    </ExitsTooltip>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Stock request lifecycle (sample states)</h4>
        </div>
        <div className="org-ehg__panel-body org-ehg__chip-row" data-testid="org-ehg-stock-request-states">
          {REQUEST_STATES.map((state) => (
            <StatusChip key={state} tone={REQUEST_TONE[state]} shape="soft">
              {state}
            </StatusChip>
          ))}
        </div>
        <div className="org-ehg__panel-body">
          <p className="org-ehg__hint m-0">
            Example: SR-1042 Quiapo ← Main Warehouse · Approved · Preparing 12 of 20 units · Request
            committed reduces Available at the source branch until fulfill or cancel.
          </p>
        </div>
      </section>
    </div>
  );
}
