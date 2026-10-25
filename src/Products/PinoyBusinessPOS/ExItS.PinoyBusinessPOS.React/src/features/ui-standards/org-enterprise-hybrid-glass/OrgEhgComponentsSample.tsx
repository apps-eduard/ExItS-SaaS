import { useState } from "react";
import { Button } from "@/components/ui/button";
import { StatusChip } from "@/components/exits/StatusChip";
import { Switch } from "@/components/ui/switch";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { Notice } from "@/components/exits/Notice";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { FormDrawer } from "@/components/exits/FormDrawer";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { OrgEhgDateGlassField } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgDateGlassField";
import { Info } from "lucide-react";

/**
 * Organization-scoped presentation of shared ExItS components.
 * Behavior reused; chrome sits inside `.org-ehg` only.
 */
export function OrgEhgComponentsSample() {
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [branch, setBranch] = useState("quiapo");
  const [tab, setTab] = useState("active");
  const [enabled, setEnabled] = useState(true);

  return (
    <div className="org-ehg" data-org-ehg data-testid="org-ehg-components">
      <p className="org-ehg__banner">
        <strong>Components &amp; states</strong> — shared ExItS primitives with Organization Hybrid
        Glass surfaces. Personal Soft Glass unchanged.
      </p>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Buttons</h4>
        </div>
        <div className="org-ehg__panel-body org-ehg__actions">
          <Button type="button" intent="primary" appearance="glossy">
            Primary
          </Button>
          <Button type="button" intent="neutral" appearance="outline">
            Secondary
          </Button>
          <Button type="button" intent="danger" appearance="glossy">
            Destructive
          </Button>
          <Button type="button" intent="primary" appearance="glossy" disabled>
            Disabled
          </Button>
        </div>
      </section>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Fields</h4>
        </div>
        <div className="org-ehg__panel-body org-ehg__field-grid">
          <div className="org-ehg__field">
            <label className="org-ehg__label" htmlFor="org-ehg-text">
              Product name
            </label>
            <input id="org-ehg-text" className="org-ehg__input" defaultValue="Copra Oil 1L" />
          </div>
          <div className="org-ehg__field">
            <label className="org-ehg__label" htmlFor="org-ehg-search">
              Search SKU
            </label>
            <input
              id="org-ehg-search"
              className="org-ehg__input"
              type="search"
              placeholder="SKU or barcode"
            />
          </div>
          <div className="org-ehg__field">
            <label className="org-ehg__label" htmlFor="org-ehg-qty">
              Quantity
            </label>
            <input id="org-ehg-qty" className="org-ehg__input" type="number" defaultValue={12} />
          </div>
          <div className="org-ehg__field">
            <label className="org-ehg__label" htmlFor="org-ehg-php">
              Unit price (PHP)
            </label>
            <input id="org-ehg-php" className="org-ehg__input" inputMode="decimal" defaultValue="89.50" />
          </div>
          <div className="org-ehg__field">
            <label className="org-ehg__label" htmlFor="org-ehg-invalid">
              Invalid sample
            </label>
            <input
              id="org-ehg-invalid"
              className="org-ehg__input"
              aria-invalid="true"
              defaultValue=""
              placeholder="Required"
            />
            <p className="org-ehg__error">Branch code is required.</p>
          </div>
          <div className="org-ehg__field">
            <label className="org-ehg__label" htmlFor="org-ehg-disabled">
              Disabled
            </label>
            <input id="org-ehg-disabled" className="org-ehg__input" disabled defaultValue="Read only" />
          </div>
          <div className="org-ehg__field">
            <span className="org-ehg__label">Branch (ExitsSelect)</span>
            <ExitsSelect
              aria-label="Branch"
              value={branch}
              onChange={setBranch}
              searchable
              options={[
                { value: "quiapo", label: "Quiapo Branch" },
                { value: "main", label: "Main Warehouse" },
                { value: "makati", label: "Makati Branch" },
              ]}
            />
          </div>
          <div className="org-ehg__field">
            <OrgEhgDateGlassField
              kind="datetime"
              variety="android"
              intensity="medium"
              label="Scheduled pickup"
              value="Oct 24, 2026  4:30 PM"
              showPopover="none"
              testId="org-ehg-components-datetime-glass"
            />
          </div>
          <div className="org-ehg__field">
            <span className="org-ehg__label">Accept scheduled while closed</span>
            <div className="flex items-center gap-2">
              <Switch checked={enabled} onCheckedChange={setEnabled} aria-label="Toggle sample" />
            </div>
          </div>
          <div className="org-ehg__field">
            <label className="org-ehg__label">
              <input type="checkbox" defaultChecked className="me-2" />
              Show on Connected Commerce storefront
            </label>
          </div>
        </div>
      </section>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Status, tabs, feedback</h4>
        </div>
        <div className="org-ehg__panel-body flex flex-col gap-3">
          <div className="org-ehg__chip-row">
            <StatusChip tone="success">Paid</StatusChip>
            <StatusChip tone="warning">Pending payment</StatusChip>
            <StatusChip tone="danger">Rejected</StatusChip>
            <StatusChip tone="info">In transit</StatusChip>
            <StatusChip tone="neutral">Draft</StatusChip>
          </div>
          <ExitsTabs
            items={[
              { key: "active", label: "Active" },
              { key: "archived", label: "Archived" },
            ]}
            value={tab}
            onValueChange={setTab}
            ariaLabel="Sample tabs"
          />
          <Notice tone="info">Hybrid Glass keeps work surfaces solid for dense ops screens.</Notice>
          <Notice tone="warning">Unsaved changes on this demo form are local only.</Notice>
          <ExitsTooltip content="Organization Hybrid Glass tooltip" placement="top">
            <Button type="button" intent="neutral" appearance="outline" size="sm" aria-label="Info tip">
              <Info className="size-4" aria-hidden />
            </Button>
          </ExitsTooltip>
          <nav className="org-ehg__hint" aria-label="Breadcrumb sample">
            Organization / Inventory / Quiapo Branch / Copra Oil 1L
          </nav>
        </div>
      </section>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">States &amp; overlays</h4>
        </div>
        <div className="org-ehg__panel-body org-ehg__state-grid">
          <div className="org-ehg__state-card">
            <h4>Loading</h4>
            <LoadingState label="Loading inventory…" />
          </div>
          <div className="org-ehg__state-card">
            <h4>Empty</h4>
            <EmptyState title="No stock requests" detail="Create a request from warehouse." size="compact" />
          </div>
          <div className="org-ehg__state-card">
            <h4>Error</h4>
            <ErrorState title="Could not load orders" detail="Demo error region." />
          </div>
          <div className="org-ehg__state-card">
            <h4>Dialogs</h4>
            <div className="org-ehg__actions">
              <Button
                type="button"
                intent="danger"
                appearance="outline"
                size="sm"
                onClick={() => setConfirmOpen(true)}
              >
                Confirm
              </Button>
              <Button
                type="button"
                intent="primary"
                appearance="outline"
                size="sm"
                onClick={() => setDrawerOpen(true)}
              >
                Drawer
              </Button>
              <Button
                type="button"
                intent="neutral"
                appearance="outline"
                size="sm"
                onClick={() => setModalOpen(true)}
              >
                Modal
              </Button>
            </div>
          </div>
        </div>
      </section>

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Compact table vs mobile card</h4>
        </div>
        <div className="org-ehg__panel-body org-ehg__split">
          <table className="org-ehg__table org-ehg__table--compact">
            <thead>
              <tr>
                <th>SKU</th>
                <th className="org-ehg__num">On hand</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>OIL-COP-1L</td>
                <td className="org-ehg__num">18</td>
                <td>
                  <StatusChip tone="warning" shape="soft">
                    Reorder
                  </StatusChip>
                </td>
              </tr>
              <tr>
                <td>CAN-SAR-155</td>
                <td className="org-ehg__num">64</td>
                <td>
                  <StatusChip tone="success" shape="soft">
                    OK
                  </StatusChip>
                </td>
              </tr>
            </tbody>
          </table>
          <div className="org-ehg__mobile-card">
            <strong>Copra Oil 1L</strong>
            <span className="org-ehg__hint">OIL-COP-1L · Quiapo</span>
            <span>
              On hand 18 · Reserved 2 · Request committed 4 · <strong>Available 12</strong>
            </span>
            <StatusChip tone="warning" shape="soft">
              Below reorder
            </StatusChip>
          </div>
        </div>
      </section>

      <ConfirmActionDialog
        open={confirmOpen}
        variant="danger"
        title="Reject stock request?"
        description="Demo confirm only — no inventory change."
        confirmLabel="Reject"
        onConfirm={() => setConfirmOpen(false)}
        onCancel={() => setConfirmOpen(false)}
      />
      <FormDrawer
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
        title="Edit product (demo)"
        onSave={() => setDrawerOpen(false)}
      >
        <p className="org-ehg__hint">FormDrawer shell reused — Organization presentation wrapper only.</p>
      </FormDrawer>
      <ExitsModal
        open={modalOpen}
        onOpenChange={setModalOpen}
        title="Short dialog"
        footer={
          <Button type="button" intent="primary" appearance="glossy" onClick={() => setModalOpen(false)}>
            Close
          </Button>
        }
      >
        <p className="m-0 text-sm">ExitsModal sample inside Organization Hybrid Glass docs context.</p>
      </ExitsModal>
    </div>
  );
}
