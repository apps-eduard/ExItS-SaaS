import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { Notice } from "@/components/exits/Notice";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { RealtimeDraftConflictNotice } from "@/realtime/RealtimeDraftConflictNotice";

/**
 * Branch settings / forms prototype — local demo state only.
 */
export function OrgEhgBranchFormsSample() {
  const [dirty, setDirty] = useState(true);
  const [remoteConflict, setRemoteConflict] = useState(true);
  const [pickup, setPickup] = useState(true);
  const [scheduledClosed, setScheduledClosed] = useState(true);
  const [commerce, setCommerce] = useState(true);
  const [leadMinutes, setLeadMinutes] = useState("45");
  const [leadError, setLeadError] = useState(false);
  const [unsavedOpen, setUnsavedOpen] = useState(false);

  function markDirty() {
    setDirty(true);
  }

  function tryCancel() {
    if (dirty) {
      setUnsavedOpen(true);
      return;
    }
  }

  function saveDemo() {
    const n = Number(leadMinutes);
    if (!Number.isFinite(n) || n < 15) {
      setLeadError(true);
      return;
    }
    setLeadError(false);
    setDirty(false);
    setRemoteConflict(false);
  }

  return (
    <div className="org-ehg" data-org-ehg data-testid="org-ehg-forms">
      <p className="org-ehg__banner">
        <strong>Forms &amp; branch settings</strong> — domain concepts only; does not call Platform
        APIs or mutate settings.
      </p>

      <RealtimeDraftConflictNotice
        visible={remoteConflict}
        onReload={() => {
          setRemoteConflict(false);
          setDirty(false);
        }}
        onDismiss={() => setRemoteConflict(false)}
        testId="org-ehg-remote-conflict"
      />

      {dirty ? (
        <Notice tone="warning" testId="org-ehg-unsaved-banner">
          You have unsaved changes on this demo form.
        </Notice>
      ) : null}

      <section className="org-ehg__panel">
        <div className="org-ehg__panel-head">
          <h4 className="org-ehg__panel-title">Quiapo Branch · fulfillment</h4>
        </div>
        <div className="org-ehg__panel-body">
          <div className="org-ehg__section">
            <h5 className="org-ehg__section-title">Branch details</h5>
            <div className="org-ehg__field-grid">
              <div className="org-ehg__field">
                <label className="org-ehg__label" htmlFor="org-ehg-branch-name">
                  Display name
                </label>
                <input
                  id="org-ehg-branch-name"
                  className="org-ehg__input"
                  defaultValue="Quiapo Branch"
                  onChange={markDirty}
                />
              </div>
              <div className="org-ehg__field">
                <label className="org-ehg__label" htmlFor="org-ehg-tz">
                  Time zone
                </label>
                <input
                  id="org-ehg-tz"
                  className="org-ehg__input"
                  defaultValue="Asia/Manila"
                  onChange={markDirty}
                />
                <p className="org-ehg__hint">Used for opening hours and scheduled order windows.</p>
              </div>
            </div>
          </div>

          <div className="org-ehg__section">
            <h5 className="org-ehg__section-title">Opening hours</h5>
            <div className="org-ehg__field-grid">
              <div className="org-ehg__field">
                <label className="org-ehg__label" htmlFor="org-ehg-open">
                  Weekday open
                </label>
                <input
                  id="org-ehg-open"
                  className="org-ehg__input"
                  type="time"
                  defaultValue="08:00"
                  onChange={markDirty}
                />
              </div>
              <div className="org-ehg__field">
                <label className="org-ehg__label" htmlFor="org-ehg-close">
                  Weekday close
                </label>
                <input
                  id="org-ehg-close"
                  className="org-ehg__input"
                  type="time"
                  defaultValue="20:00"
                  onChange={markDirty}
                />
              </div>
            </div>
          </div>

          <div className="org-ehg__section">
            <h5 className="org-ehg__section-title">Pickup &amp; scheduled orders</h5>
            <div className="org-ehg__field-grid">
              <div className="org-ehg__field">
                <span className="org-ehg__label">Pickup availability</span>
                <Switch checked={pickup} onCheckedChange={(v) => { setPickup(v); markDirty(); }} />
              </div>
              <div className="org-ehg__field">
                <span className="org-ehg__label">Accept scheduled orders while closed</span>
                <Switch
                  checked={scheduledClosed}
                  onCheckedChange={(v) => {
                    setScheduledClosed(v);
                    markDirty();
                  }}
                />
                <p className="org-ehg__hint">Hours closed ≠ pause; pause is a separate control in product.</p>
              </div>
              <div className="org-ehg__field">
                <label className="org-ehg__label" htmlFor="org-ehg-lead">
                  Preparation lead time (minutes)
                </label>
                <input
                  id="org-ehg-lead"
                  className="org-ehg__input"
                  inputMode="numeric"
                  value={leadMinutes}
                  aria-invalid={leadError}
                  onChange={(e) => {
                    setLeadMinutes(e.target.value);
                    setLeadError(false);
                    markDirty();
                  }}
                />
                {leadError ? <p className="org-ehg__error">Minimum 15 minutes.</p> : null}
              </div>
            </div>
          </div>

          <div className="org-ehg__section">
            <h5 className="org-ehg__section-title">Delivery areas</h5>
            <p className="org-ehg__hint">Quiapo · San Miguel · Intramuros (sample service areas).</p>
            <Button type="button" intent="neutral" appearance="outline" size="sm" onClick={markDirty}>
              Add delivery area (demo)
            </Button>
          </div>

          <div className="org-ehg__section">
            <h5 className="org-ehg__section-title">Connected Commerce &amp; payments</h5>
            <div className="org-ehg__field-grid">
              <div className="org-ehg__field">
                <span className="org-ehg__label">Customer ordering visibility</span>
                <Switch checked={commerce} onCheckedChange={(v) => { setCommerce(v); markDirty(); }} />
              </div>
              <div className="org-ehg__field org-ehg__field--full">
                <span className="org-ehg__label">Payment methods (illustrative)</span>
                <div className="org-ehg__chip-row mt-1">
                  <label className="org-ehg__hint">
                    <input type="checkbox" defaultChecked className="me-1" onChange={markDirty} /> Cash
                  </label>
                  <label className="org-ehg__hint">
                    <input type="checkbox" defaultChecked className="me-1" onChange={markDirty} /> Manual
                    GCash
                  </label>
                  <label className="org-ehg__hint">
                    <input type="checkbox" defaultChecked className="me-1" onChange={markDirty} />{" "}
                    Authorized Utang
                  </label>
                </div>
              </div>
            </div>
          </div>

          <div className="org-ehg__actions">
            <Button type="button" intent="danger" appearance="glossy" onClick={tryCancel}>
              Cancel
            </Button>
            <Button type="button" intent="primary" appearance="glossy" onClick={saveDemo}>
              Save
            </Button>
          </div>
        </div>
      </section>

      <ConfirmActionDialog
        open={unsavedOpen}
        variant="warning"
        title="Discard unsaved changes?"
        description="Local demo only — nothing was saved to the server."
        confirmLabel="Discard"
        onConfirm={() => {
          setDirty(false);
          setUnsavedOpen(false);
        }}
        onCancel={() => setUnsavedOpen(false)}
      />
    </div>
  );
}
