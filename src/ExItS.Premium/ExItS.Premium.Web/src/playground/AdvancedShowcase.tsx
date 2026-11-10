import { useState, type ReactNode } from "react";
import { Button } from "../components/button/Button";
import { BottomSheet } from "../components/sheet/BottomSheet";
import { ConfirmDialog } from "../components/dialog/ConfirmDialog";
import { Dialog } from "../components/dialog/Dialog";
import { EmptyState } from "../components/empty/EmptyState";
import { Input } from "../components/input/Input";
import { ActionMenu, DropdownMenu } from "../components/menu/DropdownMenu";
import { Skeleton } from "../components/skeleton/Skeleton";
import { Tabs } from "../components/tabs/Tabs";
import { useToast } from "../components/toast/ToastProvider";
import { Tooltip } from "../components/tooltip/Tooltip";
import { ExIcon } from "../icons/ExIcon";
import { FEATURE_ICONS, FEATURE_TONES } from "../icons/registry";
import { IconTile } from "../components/icon-tile/IconTile";
import styles from "./Playground.module.scss";

function Intro({ title, children }: { title: string; children: string }) {
  return (
    <header className={styles.intro}>
      <h1 className="type-page">{title}</h1>
      <p className="type-secondary">{children}</p>
    </header>
  );
}

function Stage({ label, children }: { label: string; children: ReactNode }) {
  return (
    <section className={styles.stageBlock}>
      <h2 className="type-section">{label}</h2>
      <div className={styles.stage}>{children}</div>
    </section>
  );
}

export function AdvancedShowcase() {
  const toast = useToast();
  const [dialog, setDialog] = useState(false);
  const [confirm, setConfirm] = useState(false);
  const [sheet, setSheet] = useState(false);
  const [choice, setChoice] = useState("None");
  const [tab, setTab] = useState<"paid" | "due">("paid");
  const [note, setNote] = useState("");

  return (
    <>
      <Intro title="Advanced components">
        Dialogs, sheets, menus, tabs, tooltips, and toasts use the same tokens as the rest of the system. Appearance changes apply immediately.
      </Intro>
      <Stage label="Dialog">
        <Button onClick={() => setDialog(true)}>Open dialog</Button>
        <Dialog
          open={dialog}
          title="Add a note"
          description="This dialog traps focus, closes from Escape or the backdrop, and returns focus to the button that opened it."
          onClose={() => setDialog(false)}
          footer={
            <>
              <Button variant="secondary" onClick={() => setDialog(false)}>
                Cancel
              </Button>
              <Button data-autofocus onClick={() => setDialog(false)}>
                Save note
              </Button>
            </>
          }
        >
          <Input label="Note" value={note} onChange={(event) => setNote(event.target.value)} placeholder="Optional" />
        </Dialog>
      </Stage>
      <Stage label="Confirm">
        <Button variant="danger" onClick={() => setConfirm(true)}>
          Remove sample
        </Button>
        <ConfirmDialog
          open={confirm}
          title="Remove this sample?"
          description="Nothing is deleted. This confirms the dialog action pattern."
          confirmLabel="Remove"
          tone="danger"
          onConfirm={() => {
            setConfirm(false);
            toast.push({ tone: "success", title: "Sample removed", description: "Visual feedback only." });
          }}
          onClose={() => setConfirm(false)}
        />
      </Stage>
      <Stage label="Bottom sheet">
        <Button variant="secondary" onClick={() => setSheet(true)}>
          Open sheet
        </Button>
        <BottomSheet
          open={sheet}
          title="Record a payment"
          description="Close, Escape, the backdrop, or the handle dismiss it. A swipe is optional."
          onClose={() => setSheet(false)}
        >
          <Input label="Amount" placeholder="0.00" />
          <Button
            fullWidth
            onClick={() => {
              setSheet(false);
              toast.push({ tone: "success", title: "Payment noted" });
            }}
          >
            Save
          </Button>
        </BottomSheet>
      </Stage>
      <Stage label="Menus">
        <DropdownMenu
          label="Move money"
          items={[
            { id: "add", label: "Add", icon: "add" },
            { id: "send", label: "Send", icon: "arrow_outward" },
            { id: "request", label: "Request", icon: "south_west", disabled: true },
          ]}
          onSelect={setChoice}
        />
        <ActionMenu
          label="Row actions"
          items={[
            { id: "edit", label: "Edit", icon: "edit_note" },
            { id: "remove", label: "Remove", icon: "close", tone: "danger" },
          ]}
          onSelect={setChoice}
        />
        <p className="type-caption">Last choice: {choice}</p>
      </Stage>
      <Stage label="Tabs">
        <Tabs
          label="Payments"
          value={tab}
          onChange={setTab}
          tabs={[
            { value: "paid", label: "Paid", content: <p>Paid items use the success color, not the primary palette.</p> },
            { value: "due", label: "Due", content: <p>Due items keep the warning color at #ff5722.</p> },
          ]}
        />
      </Stage>
      <Stage label="Tooltip, skeleton, and empty">
        <Tooltip content="Collapse is not available in this row">
          <Button variant="ghost" iconOnly aria-label="More information" icon={<ExIcon name="info" />} />
        </Tooltip>
        <Skeleton width="8rem" />
        <Skeleton width="12rem" height="2.75rem" />
        <EmptyState title="No payments yet" description="Sample empty state. It does not load account data." action={<Button variant="secondary">Add a payment</Button>} />
      </Stage>
      <Stage label="Toasts">
        <Button
          variant="success"
          onClick={() => toast.push({ tone: "success", title: "Saved", description: "The change is local to this preview." })}
        >
          Success
        </Button>
        <Button variant="danger" onClick={() => toast.push({ tone: "danger", title: "Could not save", description: "Try the action again." })}>
          Error
        </Button>
        <Button variant="secondary" onClick={() => toast.push({ tone: "info", title: "Reminder set" })}>
          Info
        </Button>
      </Stage>
      <Stage label="Icon tiles">
        <IconTile name="dashboard" variant="neutral" label="Neutral" />
        <IconTile name="palette" variant="primary" label="Primary" />
        <IconTile name="check" variant="solid" label="Solid primary" />
        {FEATURE_TONES.map((tone) => (
          <IconTile key={tone} name={FEATURE_ICONS[tone]} variant="feature" tone={tone} label={tone} />
        ))}
      </Stage>
    </>
  );
}
