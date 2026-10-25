import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatPhp, formatPhpCompact } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-format";

const ACTIVITY = [
  { when: "10:42 AM", text: "Stock request SR-1042 approved — Main Warehouse → Quiapo Branch" },
  { when: "10:18 AM", text: "Customer order CO-8891 marked Ready for pickup" },
  { when: "9:55 AM", text: "Manual GCash payment recorded for CO-8887 (Pending verification)" },
  { when: "9:20 AM", text: "Low stock alert: Copra Oil 1L (Available 4)" },
];

const LOW_STOCK = [
  { name: "Copra Oil 1L", sku: "OIL-COP-1L", available: 4, reorder: 12 },
  { name: "Sardines 155g", sku: "CAN-SAR-155", available: 8, reorder: 24 },
  { name: "Instant Coffee 50s", sku: "BEV-COF-50", available: 3, reorder: 10 },
];

const PENDING = [
  { ref: "CO-8891", customer: "Ana Reyes", amount: 485.5, status: "Preparing" as const },
  { ref: "CO-8890", customer: "Ben Santos", amount: 1290, status: "Scheduled" as const },
  { ref: "CO-8888", customer: "Carla Lim", amount: 320, status: "Submitted" as const },
];

/**
 * Organization dashboard visual prototype — Enterprise Hybrid Glass.
 * Fictional PH business data; no mutations.
 */
export function OrgEhgOverviewSample() {
  return (
    <div className="org-ehg" data-org-ehg data-testid="org-ehg-overview">
      <p className="org-ehg__banner">
        <strong>Prototype only.</strong> Organization Enterprise Hybrid Glass — does not change
        Personal Soft Glass or live Organization pages.
      </p>

      <div className="org-ehg__shell" data-testid="org-ehg-shell">
        <header className="org-ehg__topbar">
          <span className="org-ehg__brand">ABC Sari-Sari Store</span>
          <div className="org-ehg__selectors">
            <select id="org-ehg-org" className="org-ehg__select" defaultValue="abc" aria-label="Organization">
              <option value="abc">ABC Sari-Sari Store</option>
              <option value="xyz">XYZ Mini Grocery</option>
            </select>
            <select id="org-ehg-branch" className="org-ehg__select" defaultValue="quiapo" aria-label="Branch">
              <option value="quiapo">Quiapo Branch</option>
              <option value="main">Main Warehouse</option>
              <option value="makati">Makati Branch</option>
            </select>
          </div>
          <Button type="button" intent="primary" appearance="glossy" size="sm">
            Open sell floor
          </Button>
        </header>

        <nav className="org-ehg__sidenav" aria-label="Organization demo sidenav">
          <button type="button" className="org-ehg__nav-item" data-active="true">
            Dashboard
          </button>
          <button type="button" className="org-ehg__nav-item">
            Inventory
          </button>
          <button type="button" className="org-ehg__nav-item">
            Customer orders
          </button>
          <button type="button" className="org-ehg__nav-item">
            Branches
          </button>
          <button type="button" className="org-ehg__nav-item">
            Settings
          </button>
        </nav>

        <div className="org-ehg__main">
          <div className="org-ehg__page-header">
            <div>
              <h3 className="org-ehg__page-title">Today at Quiapo Branch</h3>
              <p className="org-ehg__page-lede">Saturday · illustrative Local Validation sample data</p>
            </div>
            <div className="org-ehg__actions">
              <Button type="button" intent="neutral" appearance="outline" size="sm">
                Export
              </Button>
              <Button type="button" intent="primary" appearance="glossy" size="sm">
                New stock count
              </Button>
            </div>
          </div>

          <div className="org-ehg__kpi-grid" data-testid="org-ehg-kpis">
            <article className="org-ehg__kpi">
              <p className="org-ehg__kpi-label">Today&apos;s sales</p>
              <p className="org-ehg__kpi-value">{formatPhpCompact(42850)}</p>
              <p className="org-ehg__kpi-meta">142 tickets · POS + storefront</p>
            </article>
            <article className="org-ehg__kpi">
              <p className="org-ehg__kpi-label">Pending orders</p>
              <p className="org-ehg__kpi-value">7</p>
              <p className="org-ehg__kpi-meta">3 scheduled for later</p>
            </article>
            <article className="org-ehg__kpi">
              <p className="org-ehg__kpi-label">Low stock</p>
              <p className="org-ehg__kpi-value">12</p>
              <p className="org-ehg__kpi-meta">Below reorder point</p>
            </article>
            <article className="org-ehg__kpi">
              <p className="org-ehg__kpi-label">Receivables</p>
              <p className="org-ehg__kpi-value">{formatPhpCompact(18640)}</p>
              <p className="org-ehg__kpi-meta">Customer Utang outstanding</p>
            </article>
          </div>

          <div className="org-ehg__split">
            <section className="org-ehg__panel">
              <div className="org-ehg__panel-head">
                <h4 className="org-ehg__panel-title">Pending customer orders</h4>
                <StatusChip tone="warning" appearance="soft" shape="soft">
                  7 open
                </StatusChip>
              </div>
              <div className="org-ehg__panel-body" style={{ padding: 0 }}>
                <table className="org-ehg__table org-ehg__table--compact">
                  <thead>
                    <tr>
                      <th>Reference</th>
                      <th>Customer</th>
                      <th className="org-ehg__num">Amount</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {PENDING.map((row) => (
                      <tr key={row.ref}>
                        <td>{row.ref}</td>
                        <td>{row.customer}</td>
                        <td className="org-ehg__num">{formatPhp(row.amount)}</td>
                        <td>
                          <StatusChip tone="info" appearance="soft" shape="soft">
                            {row.status}
                          </StatusChip>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <section className="org-ehg__panel">
              <div className="org-ehg__panel-head">
                <h4 className="org-ehg__panel-title">Low stock products</h4>
              </div>
              <div className="org-ehg__panel-body">
                <ul className="org-ehg__list">
                  {LOW_STOCK.map((p) => (
                    <li key={p.sku} className="org-ehg__list-item">
                      <span>
                        <strong>{p.name}</strong>
                        <br />
                        <span className="org-ehg__hint">{p.sku}</span>
                      </span>
                      <span className="org-ehg__num">
                        Avail {p.available}
                        <br />
                        <span className="org-ehg__hint">Reorder {p.reorder}</span>
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </section>
          </div>

          <section className="org-ehg__panel">
            <div className="org-ehg__panel-head">
              <h4 className="org-ehg__panel-title">Recent business activity</h4>
            </div>
            <div className="org-ehg__panel-body">
              <ul className="org-ehg__list">
                {ACTIVITY.map((a) => (
                  <li key={a.when + a.text} className="org-ehg__list-item">
                    <span>{a.text}</span>
                    <span className="org-ehg__hint">{a.when}</span>
                  </li>
                ))}
              </ul>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
