import { useId } from "react";
import {
  ArrowRight,
  Bell,
  Boxes,
  Building2,
  CalendarDays,
  CheckCircle2,
  CircleAlert,
  CreditCard,
  ImageIcon,
  Inbox,
  ListTodo,
  Mail,
  MapPin,
  Package,
  Pencil,
  Phone,
  Receipt,
  ShoppingCart,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Truck,
  UserPlus,
  UserRound,
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
  | "settings"
  | "empty"
  | "edit"
  | "confirm"
  | "profile"
  | "checklist"
  | "notification"
  | "pricing"
  | "invoice"
  | "invite"
  | "schedule"
  | "media"
  | "comparison"
  | "loading";

export const ORG_EHG_CARD_GLASS_KINDS: readonly OrgEhgCardGlassKind[] = [
  "menu",
  "soft-kpi",
  "hybrid-kpi",
  "solid",
  "timeline",
  "table",
  "settings",
  "empty",
  "edit",
  "confirm",
  "profile",
  "checklist",
  "notification",
  "pricing",
  "invoice",
  "invite",
  "schedule",
  "media",
  "comparison",
  "loading",
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
          <Switch
            checked
            disabled
            aria-label="Enable low stock alerts"
            onCheckedChange={() => {}}
          />
        </label>
        <label className="org-ehg-card-glass__switch-row">
          <span>
            <strong>Send email notifications</strong>
            <small>Also push to contact email on the branch profile</small>
          </span>
          <Switch
            checked
            disabled
            aria-label="Send email notifications"
            onCheckedChange={() => {}}
          />
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

export function OrgEhgCardGlassEmpty() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--empty">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Inbox className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">No activity yet</h3>
            <p className="org-ehg-card-glass__subtitle">This card is waiting for data</p>
          </div>
        </div>
      </header>
      <div className="org-ehg-card-glass__empty-body">
        <p className="org-ehg-card-glass__empty-title">Nothing to show</p>
        <p className="org-ehg-card-glass__empty-hint">
          Add a record or connect a source to fill this glass card.
        </p>
      </div>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Add first item
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassEditForm() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--edit">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Pencil className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Edit product</h3>
            <p className="org-ehg-card-glass__subtitle">SKU · RICE-25KG-WHT</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__pill">Draft</span>
      </header>
      <div className="org-ehg-card-glass__fields">
        <label className="org-ehg-card-glass__field">
          <span>Product name</span>
          <input defaultValue="Rice 25kg · White" readOnly />
        </label>
        <div className="org-ehg-card-glass__field-row">
          <label className="org-ehg-card-glass__field">
            <span>Category</span>
            <select defaultValue="grocery" disabled>
              <option value="grocery">Grocery</option>
              <option value="produce">Produce</option>
            </select>
          </label>
          <label className="org-ehg-card-glass__field">
            <span>Unit price</span>
            <div className="org-ehg-card-glass__input-affix">
              <em>₱</em>
              <input defaultValue="1,250.00" readOnly />
            </div>
          </label>
        </div>
        <label className="org-ehg-card-glass__field">
          <span>Notes</span>
          <input defaultValue="Keep near dry goods aisle" readOnly />
        </label>
      </div>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Cancel
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Save product
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassConfirm() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--confirm">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span
            className="org-ehg-card-glass__icon-chip org-ehg-card-glass__icon-chip--danger"
            aria-hidden
          >
            <CircleAlert className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Archive transfer TR-918?</h3>
            <p className="org-ehg-card-glass__subtitle">This removes it from the live ops board</p>
          </div>
        </div>
      </header>
      <p className="org-ehg-card-glass__confirm-body">
        Makati → Quezon City hub will stay in history. Staff on shift will no longer see this transfer
        in Active moves.
      </p>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Keep active
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--danger">
          Archive transfer
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassProfile() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--profile">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__avatar org-ehg-card-glass__avatar--lg" aria-hidden>
            AR
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Ana Reyes</h3>
            <p className="org-ehg-card-glass__subtitle">Floor lead · Main branch</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__pill">
          <CheckCircle2 className="size-3.5" aria-hidden />
          On shift
        </span>
      </header>
      <ul className="org-ehg-card-glass__profile-facts">
        <li>
          <UserRound className="size-3.5" aria-hidden />
          <span>Staff ID · ST-1042</span>
        </li>
        <li>
          <Mail className="size-3.5" aria-hidden />
          <span>ana.reyes@store.local</span>
        </li>
        <li>
          <Phone className="size-3.5" aria-hidden />
          <span>+63 917 555 0142</span>
        </li>
      </ul>
      <footer className="org-ehg-card-glass__actions" style={{ justifyContent: "start" }}>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Message
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          View schedule
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassChecklist() {
  const items = [
    { label: "Count dry goods aisle A", done: true },
    { label: "Confirm PO #4421 put-away", done: true },
    { label: "Restock Rice 25kg · White", done: false },
    { label: "Post end-of-shift notes", done: false },
  ] as const;

  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--checklist">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <ListTodo className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Closing checklist</h3>
            <p className="org-ehg-card-glass__subtitle">Main branch · today</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__pill">2 of 4</span>
      </header>
      <ul className="org-ehg-card-glass__checklist">
        {items.map((item) => (
          <li key={item.label} data-done={item.done ? "true" : "false"}>
            <span className="org-ehg-card-glass__check" aria-hidden>
              {item.done ? <CheckCircle2 className="size-4" /> : <span />}
            </span>
            <span>{item.label}</span>
          </li>
        ))}
      </ul>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Mark next done
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassNotification() {
  const notes = [
    {
      tone: "warn",
      title: "Low stock: Rice 25kg",
      detail: "Shelf A-12 · 6 bags left",
      time: "Now",
    },
    {
      tone: "info",
      title: "Shift handoff ready",
      detail: "Ana Reyes posted notes",
      time: "12m",
    },
    {
      tone: "success",
      title: "PO #4421 matched",
      detail: "48 lines confirmed",
      time: "1h",
    },
  ] as const;

  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--notification">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Bell className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Notifications</h3>
            <p className="org-ehg-card-glass__subtitle">3 unread · Main branch</p>
          </div>
        </div>
        <button type="button" className="org-ehg-card-glass__link">
          Mark all read
        </button>
      </header>
      <ul className="org-ehg-card-glass__notify-list">
        {notes.map((note) => (
          <li key={note.title} data-tone={note.tone}>
            <span className="org-ehg-card-glass__dot" aria-hidden />
            <div>
              <div className="org-ehg-card-glass__timeline-top">
                <strong>{note.title}</strong>
                <span className="org-ehg-card-glass__time">{note.time}</span>
              </div>
              <p className="org-ehg-card-glass__timeline-detail">{note.detail}</p>
            </div>
          </li>
        ))}
      </ul>
    </article>
  );
}

export function OrgEhgCardGlassPricing() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--pricing">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Sparkles className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Growth plan</h3>
            <p className="org-ehg-card-glass__subtitle">Multi-branch · billed monthly</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__pill">Popular</span>
      </header>
      <p className="org-ehg-card-glass__metric">
        ₱2,490
        <span className="org-ehg-card-glass__metric-unit">/ mo</span>
      </p>
      <ul className="org-ehg-card-glass__feature-list">
        <li>Up to 5 branches</li>
        <li>Shift + inventory alerts</li>
        <li>CSV / Excel export</li>
        <li>Priority support</li>
      </ul>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Compare plans
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Start trial
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassInvoice() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--invoice">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <Receipt className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Invoice INV-2084</h3>
            <p className="org-ehg-card-glass__subtitle">Due Oct 31 · Ana Reyes</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__status" data-tone="warn">
          Unpaid
        </span>
      </header>
      <ul className="org-ehg-card-glass__invoice-rows">
        <li>
          <span>Subtotal</span>
          <strong>₱4,120.00</strong>
        </li>
        <li>
          <span>VAT (12%)</span>
          <strong>₱494.40</strong>
        </li>
        <li data-total="true">
          <span>Total due</span>
          <strong>₱4,614.40</strong>
        </li>
      </ul>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Download PDF
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          <CreditCard className="size-3.5" aria-hidden />
          Record payment
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassInvite() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--invite">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <UserPlus className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Invite staff</h3>
            <p className="org-ehg-card-glass__subtitle">Main branch · cashier role</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__pill">Pending</span>
      </header>
      <div className="org-ehg-card-glass__fields">
        <label className="org-ehg-card-glass__field">
          <span>Contact email</span>
          <input defaultValue="carlo.lim@example.com" readOnly />
        </label>
        <label className="org-ehg-card-glass__field">
          <span>Display name</span>
          <input defaultValue="Carlo Lim" readOnly />
        </label>
      </div>
      <p className="org-ehg-card-glass__hint">Invite expires in 7 days. Login is org-scoped.</p>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Cancel
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Send invite
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassSchedule() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--schedule">
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__icon-chip" aria-hidden>
            <CalendarDays className="size-4" strokeWidth={2.25} />
          </span>
          <div>
            <h3 className="org-ehg-card-glass__title">Stock count</h3>
            <p className="org-ehg-card-glass__subtitle">Thu · Oct 31</p>
          </div>
        </div>
        <span className="org-ehg-card-glass__status" data-tone="info">
          Scheduled
        </span>
      </header>
      <ul className="org-ehg-card-glass__schedule-facts">
        <li>
          <strong>2:00 PM – 4:30 PM</strong>
          <span>Warehouse B</span>
        </li>
        <li>
          <MapPin className="size-3.5" aria-hidden />
          <span>Aisle A–C · dry goods</span>
        </li>
        <li>
          <UserRound className="size-3.5" aria-hidden />
          <span>Lead · Ana Reyes + 2</span>
        </li>
      </ul>
      <footer className="org-ehg-card-glass__actions" style={{ justifyContent: "start" }}>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Reschedule
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Open brief
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassMedia() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--media">
      <div className="org-ehg-card-glass__media-frame" aria-hidden>
        <ImageIcon className="size-8" strokeWidth={1.75} />
        <span>Product photo</span>
      </div>
      <header className="org-ehg-card-glass__header">
        <div>
          <h3 className="org-ehg-card-glass__title">Rice 25kg · White</h3>
          <p className="org-ehg-card-glass__subtitle">SKU · RICE-25KG-WHT</p>
        </div>
        <span className="org-ehg-card-glass__pill">In catalog</span>
      </header>
      <p className="org-ehg-card-glass__hint">Last updated today · 1.2 MB JPEG</p>
      <footer className="org-ehg-card-glass__actions">
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--ghost">
          Replace
        </button>
        <button type="button" className="org-ehg-card-glass__btn org-ehg-card-glass__btn--primary">
          Use photo
        </button>
      </footer>
    </article>
  );
}

export function OrgEhgCardGlassComparison() {
  return (
    <article className="org-ehg-card-glass org-ehg-card-glass--comparison">
      <header className="org-ehg-card-glass__header">
        <div>
          <h3 className="org-ehg-card-glass__title">Branch comparison</h3>
          <p className="org-ehg-card-glass__subtitle">Sales · last 7 days</p>
        </div>
      </header>
      <div className="org-ehg-card-glass__compare-grid">
        <div>
          <span className="org-ehg-card-glass__compare-label">Main</span>
          <p className="org-ehg-card-glass__metric org-ehg-card-glass__metric--sm">₱186k</p>
          <span className="org-ehg-card-glass__badge org-ehg-card-glass__badge--up">
            <TrendingUp className="size-3.5" aria-hidden />
            +12.4%
          </span>
        </div>
        <div>
          <span className="org-ehg-card-glass__compare-label">Quezon</span>
          <p className="org-ehg-card-glass__metric org-ehg-card-glass__metric--sm">₱142k</p>
          <span className="org-ehg-card-glass__badge org-ehg-card-glass__badge--down">
            <TrendingDown className="size-3.5" aria-hidden />
            −3.1%
          </span>
        </div>
      </div>
      <p className="org-ehg-card-glass__hint">Main leads by ₱44,200 this week</p>
    </article>
  );
}

export function OrgEhgCardGlassLoading() {
  return (
    <article
      className="org-ehg-card-glass org-ehg-card-glass--loading"
      aria-busy="true"
      aria-label="Loading card"
    >
      <header className="org-ehg-card-glass__header">
        <div className="org-ehg-card-glass__lead">
          <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--chip" />
          <div className="org-ehg-card-glass__skeleton-stack">
            <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--title" />
            <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--sub" />
          </div>
        </div>
        <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--pill" />
      </header>
      <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--metric" />
      <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--hint" />
      <span className="org-ehg-card-glass__skeleton org-ehg-card-glass__skeleton--bar" />
    </article>
  );
}
