import { useState } from "react";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { formatPhp } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-format";

type OrderRow = {
  ref: string;
  customer: string;
  orderStatus: string;
  paymentMethod: string;
  paymentStatus: string;
  fulfillment: string;
  scheduled: string;
  amount: number;
};

/**
 * Payment status is authoritative — submitted/picked up does not imply Paid.
 */
const ORDERS: OrderRow[] = [
  {
    ref: "CO-8891",
    customer: "Ana Reyes",
    orderStatus: "Ready for pickup",
    paymentMethod: "Cash",
    paymentStatus: "Unpaid",
    fulfillment: "Pickup",
    scheduled: "Today 4:30 PM",
    amount: 485.5,
  },
  {
    ref: "CO-8890",
    customer: "Ben Santos",
    orderStatus: "Scheduled",
    paymentMethod: "Manual GCash",
    paymentStatus: "Pending verification",
    fulfillment: "Delivery",
    scheduled: "Sun 10:00 AM",
    amount: 1290,
  },
  {
    ref: "CO-8887",
    customer: "Diego Cruz",
    orderStatus: "Completed",
    paymentMethod: "Authorized Utang",
    paymentStatus: "Charged to Utang",
    fulfillment: "Pickup",
    scheduled: "—",
    amount: 760,
  },
  {
    ref: "CO-8885",
    customer: "Elena Tan",
    orderStatus: "Picked up",
    paymentMethod: "Cash",
    paymentStatus: "Paid",
    fulfillment: "Pickup",
    scheduled: "—",
    amount: 210,
  },
];

function paymentTone(status: string): "success" | "warning" | "info" | "neutral" {
  if (status === "Paid" || status === "Charged to Utang") return "success";
  if (status.includes("Pending")) return "warning";
  if (status === "Unpaid") return "warning";
  return "neutral";
}

export function OrgEhgOrdersSample() {
  const [detail, setDetail] = useState<OrderRow | null>(null);

  return (
    <div className="org-ehg" data-org-ehg data-testid="org-ehg-orders">
      <p className="org-ehg__banner">
        <strong>Sales &amp; customer orders</strong> — order lifecycle and payment status are separate.
        Picked up ≠ Paid unless payment status says so. Demo data only.
      </p>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Seller order board</h4>
        </div>
        <div className="org-ehg__panel-body" style={{ padding: 0, overflowX: "auto" }}>
          <table className="org-ehg__table" data-testid="org-ehg-orders-table">
            <thead>
              <tr>
                <th>Customer</th>
                <th>Reference</th>
                <th>Order status</th>
                <th>Payment method</th>
                <th>Payment status</th>
                <th>Fulfillment</th>
                <th>Scheduled</th>
                <th className="org-ehg__num">Amount</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {ORDERS.map((o) => (
                <tr key={o.ref}>
                  <td>{o.customer}</td>
                  <td>{o.ref}</td>
                  <td>
                    <StatusChip tone="info" shape="soft">
                      {o.orderStatus}
                    </StatusChip>
                  </td>
                  <td>{o.paymentMethod}</td>
                  <td>
                    <StatusChip tone={paymentTone(o.paymentStatus)} shape="soft">
                      {o.paymentStatus}
                    </StatusChip>
                  </td>
                  <td>{o.fulfillment}</td>
                  <td>{o.scheduled}</td>
                  <td className="org-ehg__num">{formatPhp(o.amount)}</td>
                  <td>
                    <Button
                      type="button"
                      intent="neutral"
                      appearance="outline"
                      size="sm"
                      onClick={() => setDetail(o)}
                    >
                      Open
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <FormDrawer
        open={detail !== null}
        onOpenChange={(open) => {
          if (!open) setDetail(null);
        }}
        title={detail ? `Order ${detail.ref}` : "Order"}
        description="Illustrative detail drawer — no checkout or payment calls."
        onSave={() => setDetail(null)}
        saveLabel="Close"
      >
        {detail ? (
          <div className="org-ehg__field-grid">
            <div className="org-ehg__field">
              <span className="org-ehg__label">Customer</span>
              <span>{detail.customer}</span>
            </div>
            <div className="org-ehg__field">
              <span className="org-ehg__label">Amount</span>
              <span>{formatPhp(detail.amount)}</span>
            </div>
            <div className="org-ehg__field">
              <span className="org-ehg__label">Order status</span>
              <StatusChip tone="info" shape="soft">
                {detail.orderStatus}
              </StatusChip>
            </div>
            <div className="org-ehg__field">
              <span className="org-ehg__label">Payment status</span>
              <StatusChip tone={paymentTone(detail.paymentStatus)} shape="soft">
                {detail.paymentStatus}
              </StatusChip>
            </div>
            <div className="org-ehg__field org-ehg__field--full">
              <p className="org-ehg__hint m-0">
                Backend owns payment state. UI must not mark Paid solely because the order was
                submitted or picked up.
              </p>
            </div>
          </div>
        ) : null}
      </FormDrawer>
    </div>
  );
}
