import { useState } from "react";
import { Badge } from "../../components/badge/Badge";
import { Button } from "../../components/button/Button";
import { ConfirmDialog } from "../../components/dialog/ConfirmDialog";
import { EmptyState } from "../../components/empty/EmptyState";
import { Input } from "../../components/input/Input";
import { Skeleton } from "../../components/skeleton/Skeleton";
import { useToast } from "../../components/toast/ToastProvider";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

type ConfirmKind = "delete" | "unsaved" | "cancel" | "session" | null;
type Access = "required" | "denied" | "restricted";
type Link = "online" | "offline" | "failed";
type Sync = "idle" | "pending" | "done" | "failed";

const ACCESS_COPY: Record<Access, { title: string; description: string; action: string }> = {
  required: {
    title: "Permission required",
    description: "Recording a payment needs access to this personal list. This sample does not change a device permission.",
    action: "Review access",
  },
  denied: {
    title: "Permission denied",
    description: "This personal list stayed closed. Choose another sample account, or stay on this page.",
    action: "Use a sample account",
  },
  restricted: {
    title: "Restricted feature",
    description: "Exports stay off in this sample. You can still review the payments that are already here.",
    action: "Review payments",
  },
};

export function SystemExperiencesReview() {
  const { push } = useToast();
  const [confirm, setConfirm] = useState<ConfirmKind>(null);
  const [notice, setNotice] = useState("No confirmation yet.");
  const [removed, setRemoved] = useState(false);
  const [dirty, setDirty] = useState(true);
  const [transfer, setTransfer] = useState("Maya transfer · ₱500.00");
  const [access, setAccess] = useState<Access>("required");
  const [note, setNote] = useState("Neighborhood market");
  const [link, setLink] = useState<Link>("online");
  const [draft, setDraft] = useState("Weekend market · ₱240.00");
  const [session, setSession] = useState<"active" | "expired">("active");
  const [sync, setSync] = useState<Sync>("idle");

  const accessCopy = ACCESS_COPY[access];

  function closeConfirm() {
    setConfirm(null);
  }

  function sendNote() {
    if (link !== "online") {
      setNotice(link === "offline" ? "You are offline. The note stays in the field." : "The send failed. The note is still in the field.");
      return;
    }
    push({ tone: "success", title: "Note kept on this device", description: note.trim() || "Empty note." });
    setNotice("The note was kept on this device.");
  }

  function retrySend() {
    setLink("online");
    push({ tone: "success", title: "Note kept on this device", description: note.trim() || "Empty note." });
    setNotice("Retried the send. The note is still in the field.");
  }

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">System experiences</h1>
        <p className="type-secondary">
          Confirmations, access, connection, and session recovery use the dialogs, toasts, and empty states already in this playground. Nothing is sent to a server, and no device permission changes.
        </p>
      </header>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Confirmation and destructive actions</h2>
        <p className="type-secondary">Delete is danger. Leaving and canceling stay on the safer confirm button. Cancel on the dialog always returns without that action.</p>
        <div className={review.actions}>
          <Button variant="danger" onClick={() => setConfirm("delete")} disabled={removed}>
            Delete transaction
          </Button>
          <Button variant="secondary" onClick={() => setConfirm("unsaved")} disabled={!dirty}>
            Leave with unsaved changes
          </Button>
          <Button variant="secondary" onClick={() => setConfirm("cancel")}>
            Cancel transfer
          </Button>
        </div>
        <p className="type-secondary" role="status">
          {notice}
        </p>
        {removed ? <p className="type-secondary">The Maya transfer sample was removed from this list.</p> : null}
        {!removed ? <p className={review.note}>Open transfer: {transfer}</p> : null}
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Permissions and access</h2>
        <p className="type-secondary">These are sample states. The next action explains what to do. It does not request a real permission.</p>
        <div className={review.actions}>
          <Button aria-pressed={access === "required"} variant={access === "required" ? "primary" : "secondary"} onClick={() => setAccess("required")}>
            Permission required
          </Button>
          <Button aria-pressed={access === "denied"} variant={access === "denied" ? "primary" : "secondary"} onClick={() => setAccess("denied")}>
            Permission denied
          </Button>
          <Button aria-pressed={access === "restricted"} variant={access === "restricted" ? "primary" : "secondary"} onClick={() => setAccess("restricted")}>
            Restricted feature
          </Button>
        </div>
        <EmptyState
          icon={access === "denied" ? "error" : "info"}
          title={accessCopy.title}
          description={accessCopy.description}
          action={
            <Button
              variant="secondary"
              onClick={() => setNotice(`${accessCopy.action}. No device permission was changed.`)}
            >
              {accessCopy.action}
            </Button>
          }
        />
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Connection and recovery</h2>
        <p className="type-secondary">A failed or offline send leaves the note in the field.</p>
        <div className={review.form}>
          <Input label="Payment note" value={note} onChange={(event) => setNote(event.target.value)} />
          <div className={review.row}>
            <Badge tone={link === "online" ? "success" : link === "offline" ? "warning" : "danger"} indicator>
              {link === "online" ? "Online" : link === "offline" ? "Offline" : "Request failed"}
            </Badge>
          </div>
          <div className={review.actions}>
            <Button variant="secondary" onClick={() => { setLink("offline"); setNotice("Connection lost. The note stays in the field."); }}>
              Go offline
            </Button>
            <Button
              variant="secondary"
              onClick={() => {
                setLink("online");
                push({ tone: "info", title: "Connection restored", description: "You can send the note on this device." });
                setNotice("Connection restored. The note is still in the field.");
              }}
            >
              Restore connection
            </Button>
            <Button variant="secondary" onClick={() => { setLink("failed"); setNotice("The request failed. The note is still in the field."); }}>
              Fail this request
            </Button>
            <Button onClick={link === "failed" ? retrySend : sendNote} disabled={link === "offline"}>
              {link === "failed" ? "Retry send" : "Send note"}
            </Button>
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Session and interruption</h2>
        <p className="type-secondary">
          {session === "active" ? "The session sample is active." : "The session sample expired. Recover keeps the draft. Discard removes it."}
        </p>
        <div className={review.form}>
          <Input label="Local draft" value={draft} onChange={(event) => { setDraft(event.target.value); setDirty(true); }} />
          <div className={review.actions}>
            <Button variant="secondary" onClick={() => setSession("expired")}>
              Session expired
            </Button>
            <Button onClick={() => { setSession("active"); setNotice("Recovered the local draft."); }} disabled={session === "active" || draft.length === 0}>
              Recover draft
            </Button>
            <Button variant="danger" onClick={() => setConfirm("session")} disabled={draft.length === 0}>
              Discard draft
            </Button>
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">System feedback</h2>
        <p className="type-secondary">A local sync sample. Pending uses the badge and skeleton. Finish and fail use the existing toast.</p>
        <div className={review.actions}>
          <Button onClick={() => { setSync("pending"); setNotice("Sync is pending on this device."); }} disabled={sync === "pending"}>
            Start sync
          </Button>
          <Button variant="secondary" onClick={() => { setSync("done"); push({ tone: "success", title: "Sync finished", description: "The sample payments are up to date on this device." }); setNotice("Sync finished."); }} disabled={sync !== "pending"}>
            Finish sync
          </Button>
          <Button variant="danger" onClick={() => { setSync("failed"); push({ tone: "danger", title: "Sync failed", description: "Nothing left this device. Start sync to try the sample again." }); setNotice("Sync failed. Nothing left this device."); }} disabled={sync !== "pending"}>
            Fail sync
          </Button>
        </div>
        <div className={review.row}>
          <Badge tone={sync === "done" ? "success" : sync === "failed" ? "danger" : sync === "pending" ? "warning" : "neutral"} indicator>
            {sync === "done" ? "Completed" : sync === "failed" ? "Failed" : sync === "pending" ? "Pending" : "Idle"}
          </Badge>
        </div>
        {sync === "pending" ? (
          <div className={review.preview} aria-busy="true">
            <Skeleton width="14rem" label="Loading sync status" />
          </div>
        ) : null}
        {sync === "failed" ? (
          <EmptyState
            icon="error"
            title="Sync did not finish"
            description="The payments are still only on this device."
            action={<Button onClick={() => setSync("pending")}>Try sync again</Button>}
          />
        ) : null}
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Review status</h2>
        <p className="type-secondary">Locked. Confirmations, access, connection, and session recovery stay as reviewed. The Components category stays unlocked.</p>
      </section>

      <ConfirmDialog
        open={confirm === "delete"}
        tone="danger"
        title="Delete this transaction?"
        description="The Maya transfer sample leaves this list. This does not contact a server."
        confirmLabel="Delete"
        onClose={closeConfirm}
        onConfirm={() => {
          setRemoved(true);
          setNotice("Deleted the Maya transfer sample.");
          closeConfirm();
        }}
      />
      <ConfirmDialog
        open={confirm === "unsaved"}
        title="Leave with unsaved changes?"
        description="The local draft can stay, or you can discard it. Nothing is saved to a server."
        confirmLabel="Discard changes"
        onClose={() => {
          setNotice("Stayed on the page. The draft is still here.");
          closeConfirm();
        }}
        onConfirm={() => {
          setDirty(false);
          setDraft("");
          setNotice("Discarded the unsaved changes.");
          closeConfirm();
        }}
      />
      <ConfirmDialog
        open={confirm === "cancel"}
        title="Cancel this transfer?"
        description="Canceling stops the sample. The amount stays visible until you delete it."
        confirmLabel="Cancel transfer"
        onClose={() => {
          setNotice("The transfer is still open.");
          closeConfirm();
        }}
        onConfirm={() => {
          setTransfer("Canceled · Maya transfer · ₱500.00");
          setNotice("Canceled the transfer. The amount is still on this page.");
          closeConfirm();
        }}
      />
      <ConfirmDialog
        open={confirm === "session"}
        tone="danger"
        title="Discard the local draft?"
        description="Recover keeps the words in the field. Discard removes them from this page only."
        confirmLabel="Discard draft"
        onClose={() => {
          setSession("active");
          setNotice("Recovered the local draft.");
          closeConfirm();
        }}
        onConfirm={() => {
          setDraft("");
          setSession("active");
          setDirty(false);
          setNotice("Discarded the local draft.");
          closeConfirm();
        }}
      />
    </>
  );
}
