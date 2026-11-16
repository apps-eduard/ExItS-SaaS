import { useRef, useState, type ReactNode, type RefObject } from "react";
import { Badge } from "../../components/badge/Badge";
import { Button } from "../../components/button/Button";
import { Card } from "../../components/card/Card";
import { ConfirmDialog } from "../../components/dialog/ConfirmDialog";
import { Dialog } from "../../components/dialog/Dialog";
import { Input } from "../../components/input/Input";
import { EmptyState } from "../../components/empty/EmptyState";
import { Skeleton } from "../../components/skeleton/Skeleton";
import { AnchoredPopover } from "../../components/time/AnchoredPopover";
import { Tooltip } from "../../components/tooltip/Tooltip";
import { ExIcon } from "../../icons/ExIcon";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

type Preview = "records" | "empty" | "loading";

const TRANSACTIONS = [
  {
    id: "market",
    when: "12 Apr",
    name: "Neighborhood market",
    note: "Groceries",
    amount: "−₱860.00",
    status: "Paid",
    tone: "success",
  },
  {
    id: "rent",
    when: "1 Apr",
    name: "April rent",
    note: "Home",
    amount: "−₱12,000.00",
    status: "Pending",
    tone: "warning",
  },
  {
    id: "maya",
    when: "28 Mar",
    name: "Maya transfer",
    note: "Sent",
    amount: "−₱500.00",
    status: "Failed",
    tone: "danger",
  },
] as const;

export function PresentationReview() {
  const [preview, setPreview] = useState<Preview>("records");
  const [notice, setNotice] = useState("Showing this month’s payments.");
  const [paymentNoteOpen, setPaymentNoteOpen] = useState(false);
  const [savedDescription, setSavedDescription] = useState("Neighborhood market");
  const [savedAmount, setSavedAmount] = useState("860.00");
  const [draftDescription, setDraftDescription] = useState("Neighborhood market");
  const [draftAmount, setDraftAmount] = useState("860.00");
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [dialogResult, setDialogResult] = useState("");

  function openEdit() {
    setDraftDescription(savedDescription);
    setDraftAmount(savedAmount);
    setEditOpen(true);
  }

  function saveEdit() {
    setSavedDescription(draftDescription);
    setSavedAmount(draftAmount);
    setEditOpen(false);
    setDialogResult(`Saved ${draftDescription} for ₱${draftAmount}.`);
  }

  function confirmDelete() {
    setDeleteOpen(false);
    setDialogResult("Deleted the Neighborhood market sample.");
  }

  function show(next: Preview, message: string) {
    setPreview(next);
    setNotice(message);
  }

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Presentation</h1>
        <p className="type-secondary">
          Cards, badges, popovers, tooltips, dialogs, empty states, and skeletons use the components already in this playground. This page does not add a second presentation system.
        </p>
      </header>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Interactive preview</h2>
        <p className="type-secondary">Personal payments for this month. Nothing is sent to a server.</p>
        <div className={styles.stage}>
          <div className={review.actions}>
            <Button aria-pressed={preview === "records"} variant={preview === "records" ? "primary" : "secondary"} onClick={() => show("records", "Showing this month’s payments.")}>
              Show records
            </Button>
            <Button aria-pressed={preview === "empty"} variant={preview === "empty" ? "primary" : "secondary"} onClick={() => show("empty", "The list is empty.")}>
              Show empty list
            </Button>
            <Button aria-pressed={preview === "loading"} variant={preview === "loading" ? "primary" : "secondary"} onClick={() => show("loading", "Loading this month’s payments.")}>
              Show loading
            </Button>
          </div>
          {preview === "records" ? <Records onOpen={(name) => setNotice(`Opened the receipt for ${name}.`)} /> : null}
          {preview === "empty" ? (
            <EmptyState
              icon="payments"
              title="No payments yet"
              description="Record a payment to see it in this month’s list."
              action={
                <Button onClick={() => show("records", "Recorded a sample payment.")}>Record a payment</Button>
              }
            />
          ) : null}
          {preview === "loading" ? <LoadingPayments /> : null}
        </div>
        <p className="type-secondary" role="status">
          {notice}
        </p>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">States and examples</h2>
        <p className="type-secondary">Default elevation is 1. The recent list uses elevation 2. Badges name the status in text.</p>
        <div className={styles.stage}>
          <Card eyebrow="This month" title="Balance">
            <p className={`type-numeric ${review.amount}`}>₱4,280.00</p>
            <p className={`type-secondary ${review.note}`}>Available after pending payments.</p>
          </Card>
          <Card elevation={2} eyebrow="Recent" title="Neighborhood market">
            <p className={`type-secondary ${review.note}`}>Groceries · 12 Apr</p>
            <div className={review.row}>
              <Badge tone="success" indicator>
                Paid
              </Badge>
              <p className={review.lineAmount}>−₱860.00</p>
            </div>
          </Card>
          <div className={review.actions}>
            <Badge tone="success" indicator>
              Paid
            </Badge>
            <Badge tone="warning" indicator>
              Pending
            </Badge>
            <Badge tone="danger" indicator>
              Failed
            </Badge>
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Popover</h2>
        <p className="type-secondary">
          An anchored popover is interactive. It uses the existing panel, stays inside the viewport, and closes on an outside click or Escape. Focus returns to the trigger.
        </p>
        <div className={styles.stage}>
          <div className={review.actions}>
            <SamplePopover label="Balance details" dialogLabel="Balance details">
              {(close) => (
                <>
                  <p className={review.note}>Available after pending payments. This panel is anchored to Balance details.</p>
                  <Button variant="secondary" onClick={close}>
                    Close balance details
                  </Button>
                </>
              )}
            </SamplePopover>
            <SamplePopover label="Filter payments" dialogLabel="Filter payments">
              {(close) => (
                <>
                  <p className={review.note}>Choose what this month’s list shows.</p>
                  <div className={review.actions}>
                    <Button
                      variant="secondary"
                      appearance="outline"
                      onClick={() => {
                        setNotice("Showing this month’s payments.");
                        close();
                      }}
                    >
                      This month
                    </Button>
                    <Button
                      variant="secondary"
                      appearance="outline"
                      onClick={() => {
                        setNotice("Showing pending payments.");
                        close();
                      }}
                    >
                      Pending only
                    </Button>
                    <Button
                      variant="secondary"
                      appearance="outline"
                      onClick={() => {
                        setNotice("Showing every payment.");
                        close();
                      }}
                    >
                      All payments
                    </Button>
                  </div>
                </>
              )}
            </SamplePopover>
          </div>
          <div className={review.edgeRow}>
            <SamplePopover label="More actions" dialogLabel="More actions">
              {(close) => (
                <>
                  <p className={review.note}>Placed at the row end so the panel shifts inward.</p>
                  <div className={review.actions}>
                    <Button
                      variant="secondary"
                      appearance="outline"
                      onClick={() => {
                        setNotice("Opened the receipt.");
                        close();
                      }}
                    >
                      View receipt
                    </Button>
                    <Button
                      variant="secondary"
                      appearance="outline"
                      onClick={() => {
                        setNotice("Copied the amount.");
                        close();
                      }}
                    >
                      Copy amount
                    </Button>
                  </div>
                </>
              )}
            </SamplePopover>
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Tooltip</h2>
        <p className="type-secondary">
          A tooltip is not interactive. It opens on hover after 300ms and on keyboard focus after 40ms. The tip prefers the top, flips below when the top edge is too close, and keeps a small tail on the trigger. Its radius follows the control radius and stops at 12px in Pill.
        </p>
        <div className={styles.stage}>
          <div className={review.tooltipSamples}>
            <Tooltip content="Opens the receipt.">
              <Button variant="secondary">Receipt hint</Button>
            </Tooltip>
            <Tooltip content="Available after pending payments clear.">
              <Button variant="ghost" iconOnly aria-label="About the available balance" icon={<ExIcon name="info" />} />
            </Tooltip>
            <Tooltip content="This note stays with the payment. It explains who was paid, when the money left, and why the transfer did not complete.">
              <Button variant="secondary">Long payment note</Button>
            </Tooltip>
          </div>
          <div className={review.actions}>
            <Tooltip content="Kept inside the start edge.">
              <Button variant="secondary">Start edge hint</Button>
            </Tooltip>
          </div>
          <div className={review.edgeRow}>
            <Tooltip content="Kept inside the end edge.">
              <Button variant="secondary">End edge hint</Button>
            </Tooltip>
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Dialogs</h2>
        <p className="type-secondary">
          These samples use the existing dialog. Edit keeps a draft until Save changes. Delete waits for confirmation. Transaction details scrolls while the footer stays in view. The sheet sample below uses the same dialog.
        </p>
        <div className={review.actions}>
          <Button variant="secondary" onClick={openEdit}>
            Edit transaction
          </Button>
          <Button variant="secondary" onClick={() => setDeleteOpen(true)}>
            Delete transaction
          </Button>
          <Button variant="secondary" onClick={() => setDetailsOpen(true)}>
            View transaction details
          </Button>
        </div>
        <p className="type-secondary" aria-live="polite">
          {dialogResult || "No dialog action yet."}
        </p>
        <Dialog
          open={editOpen}
          title="Edit transaction"
          description="Update this transaction's details."
          onClose={() => setEditOpen(false)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setEditOpen(false)}>
                Cancel
              </Button>
              <Button onClick={saveEdit}>Save changes</Button>
            </>
          }
        >
          <Input label="Description" value={draftDescription} onChange={(event) => setDraftDescription(event.target.value)} data-autofocus />
          <Input label="Amount" inputMode="decimal" value={draftAmount} onChange={(event) => setDraftAmount(event.target.value)} />
        </Dialog>
        <ConfirmDialog
          open={deleteOpen}
          title="Delete this transaction?"
          description="This removes the Neighborhood market sample from this page. Nothing is sent to a server, and the list itself stays."
          confirmLabel="Delete"
          tone="danger"
          onClose={() => setDeleteOpen(false)}
          onConfirm={confirmDelete}
        />
        <Dialog
          open={detailsOpen}
          title="Transaction details"
          description="A longer record. Scroll the details. The actions stay at the foot of the dialog."
          size="lg"
          onClose={() => setDetailsOpen(false)}
          footer={
            <Button variant="secondary" onClick={() => setDetailsOpen(false)}>
              Close details
            </Button>
          }
        >
          <dl className={review.detailList}>
            <div>
              <dt>Merchant</dt>
              <dd>Neighborhood market</dd>
            </div>
            <div>
              <dt>Date</dt>
              <dd>12 April 2026, 09:14</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>Paid</dd>
            </div>
            <div>
              <dt>Amount</dt>
              <dd>−₱860.00</dd>
            </div>
            <div>
              <dt>Category</dt>
              <dd>Groceries</dd>
            </div>
            <div>
              <dt>Method</dt>
              <dd>Visa debit ···· 4242</dd>
            </div>
            <div>
              <dt>Reference</dt>
              <dd>TX-2026-0412-0914</dd>
            </div>
            <div>
              <dt>Location</dt>
              <dd>Quezon City</dd>
            </div>
            <div>
              <dt>Note</dt>
              <dd>Weekly groceries for the household. Rice, vegetables, and household soap. The receipt was saved with this payment.</dd>
            </div>
            <div>
              <dt>Earlier activity</dt>
              <dd>11 April, 18:02. The payment was authorized. 12 April, 09:14. The payment was marked paid. No refund has been requested.</dd>
            </div>
          </dl>
        </Dialog>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Sheet</h2>
        <p className="type-secondary">
          On a narrow screen this payment note docks as the existing sheet. On a wide screen the same dialog stays centered.
        </p>
        <div className={review.actions}>
          <Button variant="secondary" onClick={() => setPaymentNoteOpen(true)}>
            Open payment note
          </Button>
        </div>
        <Dialog
          open={paymentNoteOpen}
          title="Payment note"
          description="Existing dialog. On a narrow screen this docks as the existing sheet."
          onClose={() => setPaymentNoteOpen(false)}
          mobilePresentation="sheet"
          footer={
            <Button variant="secondary" onClick={() => setPaymentNoteOpen(false)}>
              Close payment note
            </Button>
          }
        />
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Accessibility and behavior</h2>
        <ul className={review.notes}>
          <li>A popover anchors to its trigger, stays inside the viewport, closes on an outside click or Escape, and returns focus to the trigger.</li>
          <li>A tooltip is not interactive. It opens on hover after 300ms and on keyboard focus after 40ms. Escape hides it.</li>
          <li>A disabled Refund control stays out of the tab order. The Details control remains available.</li>
          <li>Paid, Pending, and Failed use success, warning, and danger, and the word is in the badge.</li>
          <li>The empty state names the next action. Record a payment returns the sample list.</li>
          <li>Each skeleton is a status with its own loading name. Reduced motion removes the sweep.</li>
          <li>A dialog keeps Tab inside, closes on Escape, and returns focus to the trigger. Cancel discards the draft. Delete runs only after confirmation.</li>
        </ul>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Review status</h2>
        <p className="type-secondary">Locked. Card, tooltip, popover, dialog, and sheet decisions are in the design system. The Components category stays unlocked.</p>
      </section>
    </>
  );
}

function SamplePopover({
  label,
  dialogLabel,
  children,
}: {
  label: string;
  dialogLabel: string;
  children: (close: () => void) => ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);

  function close() {
    setOpen(false);
  }

  return (
    <>
      <Button
        ref={anchorRef}
        variant="secondary"
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => setOpen((current) => !current)}
      >
        {label}
      </Button>
      <AnchoredPopover open={open} anchorRef={anchorRef as RefObject<HTMLElement | null>} label={dialogLabel} onCancel={close}>
        {children(close)}
      </AnchoredPopover>
    </>
  );
}

function Records({ onOpen }: { onOpen: (name: string) => void }) {
  return (
    <div className={review.preview}>
      <Card eyebrow="This month" title="Balance">
        <p className={`type-numeric ${review.amount}`}>₱4,280.00</p>
        <p className={`type-secondary ${review.note}`}>Available after pending payments.</p>
      </Card>
      <Card elevation={2} eyebrow="Recent" title="Transactions">
        <ul className={review.list}>
          {TRANSACTIONS.map((item) => (
            <li key={item.id} className={review.transaction}>
              <div className={review.meta}>
                <p className={review.name}>{item.name}</p>
                <p className={`type-secondary ${review.note}`}>
                  {item.note} · {item.when}
                </p>
              </div>
              <div className={review.row}>
                <Badge tone={item.tone} indicator>
                  {item.status}
                </Badge>
                <p className={review.lineAmount}>{item.amount}</p>
              </div>
              <div className={review.actions}>
                <Tooltip content={`Opens the receipt for ${item.name}.`}>
                  <Button variant="secondary" onClick={() => onOpen(item.name)}>
                    Details for {item.name}
                  </Button>
                </Tooltip>
                <Button variant="secondary" disabled>
                  Refund {item.name}
                </Button>
              </div>
            </li>
          ))}
        </ul>
      </Card>
    </div>
  );
}

function LoadingPayments() {
  return (
    <div className={review.preview}>
      <Card eyebrow="This month" title="Balance">
        <Skeleton width="9rem" height="2rem" label="Loading balance" />
        <Skeleton width="14rem" label="Loading balance note" />
      </Card>
      <Card elevation={2} eyebrow="Recent" title="Transactions">
        <Skeleton label="Loading Neighborhood market" />
        <Skeleton label="Loading April rent" />
        <Skeleton label="Loading Maya transfer" />
      </Card>
    </div>
  );
}
