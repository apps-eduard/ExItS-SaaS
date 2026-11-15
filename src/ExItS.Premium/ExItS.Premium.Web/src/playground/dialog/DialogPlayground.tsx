import { useState } from "react";
import { Button } from "../../components/button/Button";
import { ConfirmDialog } from "../../components/dialog/ConfirmDialog";
import { Dialog, type DialogSize, type DialogTransition } from "../../components/dialog/Dialog";
import { DIALOG_TRANSITION_DEFAULT } from "../../components/dialog/dialog-transition";
import { Input } from "../../components/input/Input";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import { useToast } from "../../components/toast/ToastProvider";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import { dialogCursorCommand } from "./dialog-cursor-command";
import styles from "./DialogPlayground.module.scss";

type SampleId = "confirm" | "form" | "delete" | "alert" | "scroll" | "persistent";

const DEFAULTS = {
  size: "md" as DialogSize,
  transition: DIALOG_TRANSITION_DEFAULT as DialogTransition,
  title: "Save changes?",
  confirmLabel: "Continue",
  description: true,
  footer: true,
  dismissible: true,
  icon: true,
  lineTop: true,
  lineBottom: true,
  grayFooter: true,
};

const SAMPLES: ReadonlyArray<{ id: SampleId; label: string; caption: string }> = [
  { id: "confirm", label: "Confirmation", caption: "Save · Cancel · Save" },
  { id: "form", label: "Form", caption: "Edit · Add · Update · mobile sheet" },
  { id: "delete", label: "Destructive", caption: "Delete or Remove" },
  { id: "alert", label: "Information", caption: "Notices and explanations" },
  { id: "scroll", label: "Long content", caption: "Scrollable body" },
  { id: "persistent", label: "Persistent", caption: "No backdrop / Esc close" },
];

export function DialogPlayground() {
  const toast = useToast();
  const [size, setSize] = useState(DEFAULTS.size);
  const [transition, setTransition] = useState(DEFAULTS.transition);
  const [title, setTitle] = useState(DEFAULTS.title);
  const [confirmLabel, setConfirmLabel] = useState(DEFAULTS.confirmLabel);
  const [description, setDescription] = useState(DEFAULTS.description);
  const [footer, setFooter] = useState(DEFAULTS.footer);
  const [dismissible, setDismissible] = useState(DEFAULTS.dismissible);
  const [showIcon, setShowIcon] = useState(DEFAULTS.icon);
  const [lineTop, setLineTop] = useState(DEFAULTS.lineTop);
  const [lineBottom, setLineBottom] = useState(DEFAULTS.lineBottom);
  const [grayFooter, setGrayFooter] = useState(DEFAULTS.grayFooter);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [sample, setSample] = useState<SampleId | null>(null);
  const [note, setNote] = useState("");

  const shownTitle = title.trim() || DEFAULTS.title;
  const shownConfirm = confirmLabel.trim() || DEFAULTS.confirmLabel;
  const cursorCommand = dialogCursorCommand({
    size,
    transition,
    description,
    footer,
    dismissible,
    icon: showIcon,
    lineTop,
    lineBottom,
    grayFooter,
  });

  function reset() {
    setSize(DEFAULTS.size);
    setTransition(DEFAULTS.transition);
    setTitle(DEFAULTS.title);
    setConfirmLabel(DEFAULTS.confirmLabel);
    setDescription(DEFAULTS.description);
    setFooter(DEFAULTS.footer);
    setDismissible(DEFAULTS.dismissible);
    setShowIcon(DEFAULTS.icon);
    setLineTop(DEFAULTS.lineTop);
    setLineBottom(DEFAULTS.lineBottom);
    setGrayFooter(DEFAULTS.grayFooter);
  }

  function closeSample() {
    setSample(null);
  }

  return (
    <>
      <PlaygroundShell
        title="Dialogs"
        description="Premium modal shell for short confirmations, forms, alerts, and destructive actions. Forms use a bottom sheet under 640px; confirmations stay centered. Focus traps, Escape, backdrop dismiss, and focus restore stay on the Dialog API."
        panes="equal"
        propertiesColumns={2}
        preview={
          <div className={styles.previewStack}>
            <div className={styles.previewField}>
              <Button type="button" onClick={() => setPreviewOpen(true)}>
                Open dialog
              </Button>
            </div>
            <CursorCopyCommand standard="Dialog" command={cursorCommand} />
          </div>
        }
        properties={
          <>
            <Select label="Size" value={size} onChange={(event) => setSize(event.target.value as DialogSize)}>
              <option value="sm">Small</option>
              <option value="md">Medium</option>
              <option value="lg">Large</option>
            </Select>
            <Select
              label="Transition"
              value={transition}
              onChange={(event) => setTransition(event.target.value as DialogTransition)}
            >
              <option value="spring">Spring</option>
              <option value="fade">Fade</option>
              <option value="scale">Scale</option>
              <option value="slide">Slide</option>
            </Select>
            <Input label="Title" value={title} onChange={(event) => setTitle(event.target.value)} />
            <Input label="Confirm label" value={confirmLabel} onChange={(event) => setConfirmLabel(event.target.value)} />
            <Switch
              label="Description"
              checked={description}
              onChange={(event) => setDescription(event.target.checked)}
            />
            <Switch label="Icon" checked={showIcon} onChange={(event) => setShowIcon(event.target.checked)} />
            <Switch label="Footer" checked={footer} onChange={(event) => setFooter(event.target.checked)} />
            <Switch label="Line top" checked={lineTop} onChange={(event) => setLineTop(event.target.checked)} />
            <Switch
              label="Line bottom"
              checked={lineBottom}
              onChange={(event) => setLineBottom(event.target.checked)}
            />
            <Switch
              label="Gray bottom"
              checked={grayFooter}
              onChange={(event) => setGrayFooter(event.target.checked)}
            />
            <Switch
              label="Close on backdrop / Esc"
              checked={dismissible}
              onChange={(event) => setDismissible(event.target.checked)}
            />
            <div className={styles.resetRow}>
              <Button type="button" variant="ghost" onClick={reset}>
                Default global
              </Button>
            </div>
          </>
        }
        examples={
          <div className={styles.examples}>
            <div className={styles.exampleCard}>
              <h3 className={styles.exampleTitle}>Use cases</h3>
              <div className={styles.sampleGrid}>
                {SAMPLES.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    className={styles.sampleButton}
                    onClick={() => setSample(item.id)}
                  >
                    <span className={styles.sampleLabel}>{item.label}</span>
                    <span className={styles.sampleCaption}>{item.caption}</span>
                  </button>
                ))}
              </div>
            </div>
          </div>
        }
      />

      <Dialog
        open={previewOpen}
        title={shownTitle}
        description={description ? "Review before you continue." : undefined}
        size={size}
        transition={transition}
        dismissible={dismissible}
        icon={showIcon ? "check" : undefined}
        iconTone="primary"
        lineTop={lineTop}
        lineBottom={lineBottom}
        grayFooter={grayFooter}
        onClose={() => setPreviewOpen(false)}
        footer={
          footer ? (
            <>
              <Button variant="secondary" onClick={() => setPreviewOpen(false)}>
                Cancel
              </Button>
              <Button data-autofocus onClick={() => setPreviewOpen(false)}>
                {shownConfirm}
              </Button>
            </>
          ) : undefined
        }
      >
        <p className={styles.message}>Your edits will be saved to this record. You can change them again later.</p>
      </Dialog>

      <ConfirmDialog
        open={sample === "confirm"}
        title="Save changes?"
        description="Review before you continue."
        confirmLabel="Save"
        onConfirm={() => {
          closeSample();
          toast.push({ tone: "success", title: "Saved", description: "Local preview only." });
        }}
        onClose={closeSample}
      >
        <p className={styles.message}>Your edits will be saved to this record. You can change them again later.</p>
      </ConfirmDialog>

      <Dialog
        open={sample === "form"}
        title="Add a note"
        description="Optional note for this preview."
        icon="edit_note"
        iconTone="primary"
        mobilePresentation="sheet"
        onClose={closeSample}
        footer={
          <>
            <Button variant="secondary" onClick={closeSample}>
              Cancel
            </Button>
            <Button
              data-autofocus
              onClick={() => {
                closeSample();
                toast.push({ tone: "success", title: "Note saved" });
              }}
            >
              Save
            </Button>
          </>
        }
      >
        <Input label="Note" value={note} onChange={(event) => setNote(event.target.value)} placeholder="Optional" />
      </Dialog>

      <ConfirmDialog
        open={sample === "delete"}
        title="Delete this record?"
        description="This action cannot be undone."
        confirmLabel="Delete"
        tone="danger"
        onConfirm={() => {
          closeSample();
          toast.push({ tone: "danger", title: "Record removed", description: "Visual feedback only." });
        }}
        onClose={closeSample}
      >
        <p className={styles.message}>The record and its history will be removed from this preview.</p>
      </ConfirmDialog>

      <Dialog
        open={sample === "alert"}
        title="Backup complete"
        description="Your records are up to date."
        size="sm"
        icon="info"
        iconTone="info"
        onClose={closeSample}
        footer={
          <Button data-autofocus variant="secondary" onClick={closeSample}>
            Got it
          </Button>
        }
      >
        <p className={styles.message}>Your records were saved to this device.</p>
      </Dialog>

      <Dialog
        open={sample === "scroll"}
        title="Terms of use"
        size="lg"
        icon="inbox"
        iconTone="info"
        mobilePresentation="sheet"
        onClose={closeSample}
        footer={
          <Button data-autofocus onClick={closeSample}>
            I agree
          </Button>
        }
      >
        <div className={styles.scrollBody}>
          {Array.from({ length: 12 }, (_, index) => (
            <p key={index} className={styles.message}>
              {index + 1}. Records stay on your account. Shared records are visible to the person you share them with,
              and either of you can review changes before they count.
            </p>
          ))}
        </div>
      </Dialog>

      <Dialog
        open={sample === "persistent"}
        title="Finish setup"
        dismissible={false}
        icon="warning"
        iconTone="warning"
        onClose={closeSample}
        footer={
          <Button data-autofocus variant="secondary" onClick={closeSample}>
            Done
          </Button>
        }
      >
        <p className={styles.message}>
          Clicking outside or pressing Esc does not close this dialog. Use Done or the close button.
        </p>
      </Dialog>
    </>
  );
}
