import { useMemo, useState } from "react";
import { Badge } from "../../components/badge/Badge";
import { Button } from "../../components/button/Button";
import { EmptyState } from "../../components/empty/EmptyState";
import { SearchField } from "../../components/search/SearchField";
import { Skeleton } from "../../components/skeleton/Skeleton";
import { useToast } from "../../components/toast/ToastProvider";
import { ExIcon } from "../../icons/ExIcon";
import type { IconName } from "../../icons/registry";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

const PAYMENTS = [
  { name: "Neighborhood market", note: "Groceries · 12 Apr", amount: "−₱860.00" },
  { name: "April rent", note: "Home · 1 Apr", amount: "−₱12,000.00" },
  { name: "Maya transfer", note: "Sent · 28 Mar", amount: "−₱500.00" },
] as const;

const STATUSES = [
  { label: "Paid", tone: "success", icon: "check" },
  { label: "Pending", tone: "warning", icon: "warning" },
  { label: "Overdue", tone: "danger", icon: "error" },
  { label: "Scheduled", tone: "info", icon: "info" },
  { label: "Draft", tone: "neutral", icon: "inbox" },
] as const;

export function StatusReview() {
  const { push } = useToast();
  const [saving, setSaving] = useState(false);
  const [showRows, setShowRows] = useState(false);
  const [records, setRecords] = useState<"ready" | "empty" | "failed">("ready");
  const [query, setQuery] = useState("");

  const matches = useMemo(() => {
    const needle = query.trim().toLowerCase();
    if (needle.length === 0) return PAYMENTS;
    return PAYMENTS.filter((payment) => `${payment.name} ${payment.note}`.toLowerCase().includes(needle));
  }, [query]);

  function toast(tone: "success" | "warning" | "danger" | "info", title: string, description: string) {
    push({ tone, title, description });
  }

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Status</h1>
        <p className="type-secondary">
          Badges, loading, toasts, and empty states use the components already in this playground. This page does not add a second status system.
        </p>
      </header>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Status and badges</h2>
        <p className="type-secondary">Each status keeps a word. The pip and icon repeat that word. Color is not the only signal.</p>
        <ul className={review.list}>
          {STATUSES.map((status) => (
            <li key={status.label} className={review.transaction}>
              <div className={review.row}>
                <ExIcon name={status.icon as IconName} size={18} label="" />
                <Badge tone={status.tone} indicator>
                  {status.label}
                </Badge>
              </div>
            </li>
          ))}
        </ul>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Progress and loading</h2>
        <p className="type-secondary">
          There is no progress bar. A percentage here would look like a real transfer, so this sample stays indeterminate. The button spinner and the skeleton are the existing loading states.
        </p>
        <div className={review.actions}>
          <Button loading={saving} aria-describedby="status-save-note" onClick={() => setSaving(true)}>
            Save receipt
          </Button>
          <Button variant="secondary" onClick={() => setSaving(false)} disabled={!saving}>
            Stop save
          </Button>
          <Button aria-pressed={showRows} variant={showRows ? "primary" : "secondary"} onClick={() => setShowRows((current) => !current)}>
            {showRows ? "Hide payment rows" : "Show payment rows"}
          </Button>
        </div>
        <p id="status-save-note" className="type-secondary">
          {saving ? "Saving the receipt sample. Stop save ends it." : "Save receipt shows the button spinner."}
        </p>
        {showRows ? (
          <div className={review.preview} aria-busy="true">
            <Skeleton width="12rem" label="Loading payment name" />
            <Skeleton width="8rem" label="Loading payment amount" />
            <Skeleton width="100%" height="2.5rem" label="Loading payment row" />
          </div>
        ) : (
          <p className="type-secondary" role="status">
            Nothing is loading.
          </p>
        )}
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Notifications and feedback</h2>
        <p className="type-secondary">
          Each button opens the existing toast. Dismiss closes it. A danger toast is an alert. The others are status. The right edge counts down with the auto-dismiss time, in that toast’s tone. Hover or keyboard focus pauses it.
        </p>
        <div className={review.actions}>
          <Button onClick={() => toast("success", "Payment saved", "Neighborhood market was recorded on this device.")}>Payment saved</Button>
          <Button variant="secondary" onClick={() => toast("warning", "Transfer still pending", "Maya has not confirmed ₱500.00 yet.")}>
            Transfer pending
          </Button>
          <Button variant="danger" onClick={() => toast("danger", "Payment failed", "April rent could not be recorded.")}>
            Payment failed
          </Button>
          <Button variant="secondary" onClick={() => toast("info", "Statement ready", "March statement is available to review.")}>
            Statement ready
          </Button>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Empty and error states</h2>
        <p className="type-secondary">Personal payment samples. Nothing is sent to a server.</p>
        <div className={review.actions}>
          <Button aria-pressed={records === "ready"} variant={records === "ready" ? "primary" : "secondary"} onClick={() => setRecords("ready")}>
            Show records
          </Button>
          <Button aria-pressed={records === "empty"} variant={records === "empty" ? "primary" : "secondary"} onClick={() => setRecords("empty")}>
            No records
          </Button>
          <Button aria-pressed={records === "failed"} variant={records === "failed" ? "primary" : "secondary"} onClick={() => setRecords("failed")}>
            Failed loading
          </Button>
        </div>
        {records === "ready" ? (
          <ul className={review.list}>
            {PAYMENTS.map((payment) => (
              <li key={payment.name} className={review.transaction}>
                <p className={review.name}>{payment.name}</p>
                <p className={`type-secondary ${review.note}`}>{payment.note}</p>
                <p className={review.lineAmount}>{payment.amount}</p>
              </li>
            ))}
          </ul>
        ) : null}
        {records === "empty" ? (
          <EmptyState
            icon="inbox"
            title="No payments yet"
            description="Record a payment to see it in this month’s list."
            action={<Button onClick={() => setRecords("ready")}>Record a payment</Button>}
          />
        ) : null}
        {records === "failed" ? (
          <EmptyState
            icon="error"
            title="Payments could not load"
            description="The sample list did not arrive. Try again when you are ready."
            action={<Button onClick={() => setRecords("ready")}>Try again</Button>}
          />
        ) : null}

        <SearchField label="Find a payment" value={query} onChange={setQuery} placeholder="Market, rent, transfer" />
        {matches.length > 0 ? (
          <ul className={review.list}>
            {matches.map((payment) => (
              <li key={payment.name} className={review.result}>
                <span className={review.resultName}>{payment.name}</span>
                <span className={`type-secondary ${review.resultNote}`}>{payment.note}</span>
              </li>
            ))}
          </ul>
        ) : (
          <EmptyState
            icon="search"
            title="No matching payments"
            description={`Nothing in this month matches “${query.trim()}”.`}
            action={<Button variant="secondary" onClick={() => setQuery("")}>Show every payment</Button>}
          />
        )}
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Gaps</h2>
        <ul className={review.notes}>
          <li>No determinate progress bar. Do not invent one for this review.</li>
          <li>The spinner lives on Button while it is saving. There is no separate spinner component.</li>
          <li>A snackbar is the existing toast. There is no second feedback surface.</li>
        </ul>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Review status</h2>
        <p className="type-secondary">Locked. Badges, loading, toasts, and empty states stay as reviewed. The Components category stays unlocked.</p>
        <p className="type-secondary">The toast right-edge countdown is locked.</p>
      </section>
    </>
  );
}
