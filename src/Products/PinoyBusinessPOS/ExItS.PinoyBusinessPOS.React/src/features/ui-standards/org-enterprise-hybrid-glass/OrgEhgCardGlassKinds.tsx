import { useId } from "react";
import {
  ArrowRight,
  Boxes,
  Building2,
  CheckCircle2,
  Package,
  ShoppingCart,
  TrendingDown,
  TrendingUp,
  Truck,
  Wallet,
} from "lucide-react";
import { Switch } from "@/components/ui/switch";

export type OrgEhgCardGlassKind =
  | "menu"
  | "soft-kpi"
  | "hybrid-kpi"
  | "solid"
  | "timeline"
  | "table"
  | "settings";

export const ORG_EHG_CARD_GLASS_KINDS: readonly OrgEhgCardGlassKind[] = [
  "menu",
  "soft-kpi",
  "hybrid-kpi",
  "solid",
  "timeline",
  "table",
  "settings",
];

export function parseOrgEhgCardGlassKind(
  category: string,
): OrgEhgCardGlassKind | null {
  const match = category.match(/(?:^|-)card-glass-(.+)$/);
  if (!match) return null;
  const kind = match[1] as OrgEhgCardGlassKind;
  return ORG_EHG_CARD_GLASS_KINDS.includes(kind) ? kind : null;
}

export function OrgEhgCardGlassSoftKpi({
  variant = "sales",
}: {
  variant?: "sales" | "fulfillment" | "stock";
}) {
  const copy =
    variant === "sales"
      ? {
          icon: Wallet,
          label: "Weekly sales",
          metric: "₱186,400",
          delta: "+12.4%",
          up: true,
          hint: "Target ₱220,000 · 68% complete",
          fill: 68,
        }
      : variant === "fulfillment"
        ? {
            icon: Truck,
            label: "Same-day fulfillment",
            metric: "92%",
            delta: "+3.1%",
            up: true,
            hint: "148 of 161 orders packed today",
            fill: 92,
          }
        : {
            icon: Boxes,
            label: "Shelf availability",
            metric: "87%",
            delta: "−1.8%",
            up: false,
            hint: "Low stock on 14 SKUs",
            fill: 87,
          };

  const Icon = copy.icon;
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--soft-kpi">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Icon className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">{copy.label}</h3>
            <p className="org-ehg-card-glass__subtitle">Last 7 days</p>
          </div>
        </div>
        <span
          className={
            "org-ehg-card-glass__badge " +
            (copy.up ? "org-ehg-card-glass__badge--up" : "org-ehg-card-glass__badge--down")
          }
        >
          {copy.up ? (
            <TrendingUp className="size-3.5" aria-hidden />
          ) : (
            <TrendingDown className="size-3.5" aria-hidden />
          )}
          {copy.delta}
        </span>
      </header>
      <p className="org-ehg-card-glass__metric">{copy.metric}</p>
      <p className="org-ehg-card-glass__hint">{copy.hint}</p>
      <div
        className="org-ehg-card-glass__progress"
        role="progressbar"
        aria-valuenow={copy.fill}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={copy.label}
      >
        <span style={{ width: `${copy.fill}%` }} />
      </div>
    </article>
  );
}

export function OrgEhgCardGlassHybridKpi({
  variant = "conversion",
}: {
  variant?: "conversion" | "aov" | "visits";
}) {
  const sparkId = useId().replace(/:/g, "");
  const copy =
    variant === "conversion"
      ? {
          label: "Checkout conversion",
          metric: "3.8%",
          delta: "+0.4 pts",
          period: "Today",
          path: "M0 30 C18 28, 28 22, 42 24 S68 34, 88 18 S118 10, 160 14",
          fillPath:
            "M0 40 L0 30 C18 28, 28 22, 42 24 S68 34, 88 18 S118 10, 160 14 L160 40 Z",
        }
      : variant === "aov"
        ? {
            label: "Average order value",
            metric: "₱842",
            delta: "+₱56",
            period: "This week",
            path: "M0 26 C22 24, 36 30, 52 22 S84 12, 108 18 S138 28, 160 16",
            fillPath:
              "M0 40 L0 26 C22 24, 36 30, 52 22 S84 12, 108 18 S138 28, 160 16 L160 40 Z",
          }
        : {
            label: "Store visits",
            metric: "4,280",
            delta: "+9.2%",
            period: "This month",
            path: "M0 32 C24 30, 40 20, 58 22 S90 34, 112 14 S140 10, 160 12",
            fillPath:
              "M0 40 L0 32 C24 30, 40 20, 58 22 S90 34, 112 14 S140 10, 160 12 L160 40 Z",
          };

  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--hybrid-kpi">
      <header className="org-ehg-card-glass__header">
        <div>
          <h3 className="org-ehg-card-glass__title">{copy.label}</h3>
          <p className="org-ehg-card-glass__subtitle">{copy.period}</p>
        </div>
        <span className="org-ehg-card-glass__badge org-ehg-card-glass__badge--up">
          <TrendingUp className="size-3.5" aria-hidden />
          {copy.delta}
        </span>
      </header>
      <p className="org-ehg-card-glass__metric">{copy.metric}</p>
      <svg
        className="org-ehg-card-glass__spark"
        viewBox="0 0 160 40"
        aria-hidden
        preserveAspectRatio="none"
      >
        <defs>
          <linearGradient id={`org-ehg-card-spark-${sparkId}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="currentColor" stopOpacity="0.35" />
            <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
          </linearGradient>
        </defs>
        <path d={copy.fillPath} fill={`url(#org-ehg-card-spark-${sparkId})`} />
        <path
          d={copy.path}
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
          strokeLinecap="round"
        />
      </svg>
      <div className="org-ehg-card-glass__foot-stats">
        <span>
          Peak <strong>11–1 PM</strong>
        </span>
        <span>
          Channel <strong>POS + online</strong>
        </span>
      </div>
    </article>
  );
}

export function OrgEhgCardGlassSolid() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--solid">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip org-ehg-card-glass__icon-chip--solid" aria-hidden>
            <Package className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Active SKUs</h3>
            <p className="org-ehg-card-glass__subtitle">Across 3 warehouses</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__badge org-ehg-card-glass__badge--down">
          <TrendingDown className="size-3.5" aria-hidden />
          −2.1%
        </span>
      </header>
      <p className="org-ehg-card-glass__metric">1,240</p>
      <ul className="org-ehg-card-glass__stats">
        <li>
          <span>In stock</span>
          <div className="org-ehg-card-glass__stat-bar" aria-hidden>
            <i style={{ width: "89%" }} />
          </div>
          <strong>1,102</strong>
        </li>
        <li>
          <span>Low stock</span>
          <div className="org-ehg-card-glass__stat-bar" aria-hidden>
            <i style={{ width: "8%" }} data-tone="warn" />
          </div>
          <strong>98</strong>
        </li>
        <li>
          <span>Out of stock</span>
          <div className="org-ehg-card-glass__stat-bar" aria-hidden>
            <i style={{ width: "3%" }} data-tone="danger" />
          </div>
          <strong>40</strong>
        </li>
      </ul>
    </article>
  );
}

export function OrgEhgCardGlassTimeline() {
  const events = [
    {
      tone: "info",
      title: "PO #4421 received",
      detail: "Warehouse B · 48 lines matched",
      time: "2m ago",
      who: "JR",
    },
    {
      tone: "success",
      title: "Transfer TR-918 completed",
      detail: "Makati → Quezon City hub",
      time: "18m ago",
      who: "AL",
    },
    {
      tone: "warn",
      title: "Low stock: Rice 25kg",
      detail: "Only 6 bags left on shelf A-12",
      time: "1h ago",
      who: "SYS",
    },
    {
      tone: "success",
      title: "Order ORD-1021 packed",
      detail: "Courier booked for 4:30 PM",
      time: "2h ago",
      who: "MN",
    },
  ] as const;

  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--timeline">
      <header className="org-ehg-card-glass__header">
        <div>
          <h3 className="org-ehg-card-glass__title">Operations activity</h3>
          <p className="org-ehg-card-glass__subtitle">Live feed · today</p>
        </div>
        <button type="button" className="org-ehg-card-glass__link">
          View all
          <ArrowRight className="size-3.5" aria-hidden />
        </button>
      </header>
      <ul className="org-ehg-card-glass__timeline">
        {events.map((event) => (
          <li key={event.title} data-tone={event.tone}>
            <span className="org-ehg-card-glass__dot" aria-hidden />
            <div className="org-ehg-card-glass__timeline-body">
              <div className="org-ehg-card-glass__timeline-top">
                <strong>{event.title}</strong>
                <span className="org-ehg-card-glass__time">{event.time}</span>
              </div>
              <p className="org-ehg-card-glass__timeline-detail">{event.detail}</p>
              <span className="org-ehg-card-glass__avatar" aria-hidden>
                {event.who}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </article>
  );
}

export function OrgEhgCardGlassTable() {
  const rows = [
    {
      id: "ORD-1021",
      customer: "Ana Reyes",
      total: "₱2,480",
      when: "10:42 AM",
      status: "Completed",
      tone: "success",
    },
    {
      id: "ORD-1020",
      customer: "Carlo Lim",
      total: "₱890",
      when: "10:18 AM",
      status: "Processing",
      tone: "info",
    },
    {
      id: "ORD-1019",
      customer: "Bea Santos",
      total: "₱4,120",
      when: "9:55 AM",
      status: "Pending",
      tone: "warn",
    },
    {
      id: "ORD-1018",
      customer: "Diego Cruz",
      total: "₱310",
      when: "9:12 AM",
      status: "Cancelled",
      tone: "danger",
    },
    {
      id: "ORD-1017",
      customer: "Ella Go",
      total: "₱1,650",
      when: "Yesterday",
      status: "Completed",
      tone: "success",
    },
  ] as const;

  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--table">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <ShoppingCart className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Recent customer orders</h3>
            <p className="org-ehg-card-glass__subtitle">5 latest · Main branch</p>
          </div>
        </div>
        <button type="button" className="org-ehg-card-glass__link">
          View all
          <ArrowRight className="size-3.5" aria-hidden />
        </button>
      </header>
      <table className="org-ehg-card-glass__table">
        <thead>
          <tr>
            <th scope="col">Order</th>
            <th scope="col">Customer</th>
            <th scope="col">Total</th>
            <th scope="col">Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id}>
              <td>
                <strong>{row.id}</strong>
                <span className="org-ehg-card-glass__muted">{row.when}</span>
              </td>
              <td>{row.customer}</td>
              <td>{row.total}</td>
              <td>
                <span className="org-ehg-card-glass__status" data-tone={row.tone}>
                  {row.status}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </article>
  );
}

export function OrgEhgCardGlassSettings() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--settings">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Building2 className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Inventory alert settings</h3>
            <p className="org-ehg-card-glass__subtitle">Applies to all connected branches</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__pill">
          <CheckCircle2 className="size-3.5" aria-hidden />
          Saved
        </span>
      </header>
      <div className="org-ehg-card-glass__fields">
        <label className="org-ehg-card-glass__field">
          <span>Organization name</span>
          <input defaultValue="ExItS Retail Coop" readOnly />
        </label>
        <div className="org-ehg-card-glass__field-row">
          <label className="org-ehg-card-glass__field">
            <span>Default currency</span>
            <select defaultValue="PHP" disabled>
              <option value="PHP">PHP — Philippine Peso</option>
              <option value="USD">USD</option>
            </select>
          </label>
          <label className="org-ehg-card-glass__field">
            <span>Low stock threshold</span>
            <div className="org-ehg-card-glass__input-affix">
              <input defaultValue="12" readOnly />
              <em>units</em>
            </div>
          </label>
        </div>
        <label className="org-ehg-card-glass__switch-row">
          <span>
            <strong>Enable low stock alerts</strong>
            <small>Notify managers when SKUs fall below threshold</small>
          </span>
          <Switch checked disabled aria-label="Enable low stock alerts" />
        </label>
        <label className="org-ehg-card-glass__switch-row">
          <span>
            <strong>Send email notifications</strong>
            <small>Also push to contact email on the branch profile</small>
          </span>
          <Switch checked disabled aria-label="Send email notifications" />
        </label>
      </div>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Cancel
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Save changes
        </button>
      </footer>
    </article>
  );
}
