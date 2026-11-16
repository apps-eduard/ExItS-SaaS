import { useRef, useState, type RefObject } from "react";
import { AnchoredPopover } from "../../components/time/AnchoredPopover";
import { BottomSheet } from "../../components/sheet/BottomSheet";
import { Button } from "../../components/button/Button";
import { Dialog } from "../../components/dialog/Dialog";
import { DropdownMenu } from "../../components/menu/DropdownMenu";
import { Input } from "../../components/input/Input";
import { SegmentedControl } from "../../components/segmented/SegmentedControl";
import { Select } from "../../components/select/Select";
import { Skeleton } from "../../components/skeleton/Skeleton";
import { Switch } from "../../components/switch/Switch";
import { Tooltip } from "../../components/tooltip/Tooltip";
import { useToast } from "../../components/toast/ToastProvider";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

export function MotionReview() {
  const { push } = useToast();
  const [noteOpen, setNoteOpen] = useState(false);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [menuChoice, setMenuChoice] = useState("No menu choice yet.");
  const [alerts, setAlerts] = useState(true);
  const [span, setSpan] = useState("week");
  const [saving, setSaving] = useState(false);
  const [showSkeleton, setShowSkeleton] = useState(false);

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Motion</h1>
        <p className="type-secondary">
          These samples use the components already in the playground. The review does not add a second animation. Foundations Motion stays at #motion.
        </p>
      </header>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Enter and exit</h2>
        <p className="type-secondary">Dialog, popover, tooltip, menu, and sheet keep their current motion. Open one, then close it.</p>
        <div className={review.actions}>
          <Button onClick={() => setNoteOpen(true)}>Open note</Button>
          <BalancePopover />
          <Tooltip content="The receipt stays on this device.">
            <Button variant="secondary">Receipt hint</Button>
          </Tooltip>
          <DropdownMenu
            label="Record"
            items={[
              { id: "edit", label: "Edit" },
              { id: "keep", label: "Keep" },
            ]}
            onSelect={(id) => setMenuChoice(id === "edit" ? "Menu: edit." : "Menu: keep.")}
          />
          <Button variant="secondary" onClick={() => setSheetOpen(true)}>
            Open sheet
          </Button>
        </div>
        <p className="type-secondary" role="status">
          {menuChoice}
        </p>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Microinteractions</h2>
        <p className="type-secondary">Press, float, menu, thumb, and switch use the motion already on each control.</p>
        <div className={review.form}>
          <div className={review.actions}>
            <Button>Save payment</Button>
          </div>
          <Input label="Soft note" appearance="soft" defaultValue="" />
          <Select label="Category" defaultValue="groceries">
            <option value="groceries">Groceries</option>
            <option value="rent">Rent</option>
          </Select>
          <SegmentedControl
            label="Summary"
            name="motion-review-span"
            value={span}
            onChange={setSpan}
            options={[
              { value: "week", label: "Week" },
              { value: "month", label: "Month" },
            ]}
          />
          <Switch label="Payment alerts" checked={alerts} onChange={(event) => setAlerts(event.target.checked)} />
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Feedback</h2>
        <p className="type-secondary">
          Toast, the button spinner, and the skeleton are the existing feedback motion. Show saved toast uses the real toast. Its right edge starts full and shrinks toward the top until the toast dismisses. Hover or keyboard focus pauses the clock. Reduced motion hides the indicator and still dismisses on time.
        </p>
        <div className={review.actions}>
          <Button
            onClick={() => push({ tone: "success", title: "Payment saved", description: "The sample stayed on this device." })}
          >
            Show saved toast
          </Button>
          <Button loading={saving} onClick={() => setSaving(true)}>
            Save receipt
          </Button>
          <Button variant="secondary" onClick={() => setSaving(false)} disabled={!saving}>
            Stop save
          </Button>
          <Button aria-pressed={showSkeleton} variant="secondary" onClick={() => setShowSkeleton((current) => !current)}>
            {showSkeleton ? "Hide payment row" : "Show payment row"}
          </Button>
        </div>
        {showSkeleton ? <Skeleton width="14rem" label="Loading payment row" /> : null}
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Navigation</h2>
        <p className="type-secondary">
          Changing section is instant. There is no page transition to demonstrate. The sidebar, breadcrumb, and hash update without an entrance animation.
        </p>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">What stays as built</h2>
        <ul className={review.notes}>
          <li>Shared tokens stay 120ms, 180ms, and 280ms, with the existing ease. Reduced motion sets those tokens to 1ms.</li>
          <li>The dialog keeps its spring. The sheet uses the slow token in and the base token out. They are not the same motion.</li>
          <li>The select menu stays 300ms with its own ease. The toast entrance stays 200ms. The locked right-edge countdown stays 4000ms. The soft label stays 360ms. Running accent stays one 960ms lap when it is chosen.</li>
          <li>Motion for React follows the system setting. This page does not override it.</li>
        </ul>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Review status</h2>
        <p className="type-secondary">Not locked. Waiting for visual approval before motion is locked.</p>
      </section>

      <Dialog
        open={noteOpen}
        title="Payment note"
        description="A local note. Closing uses the existing dialog exit."
        onClose={() => setNoteOpen(false)}
        footer={
          <Button variant="secondary" onClick={() => setNoteOpen(false)}>
            Close note
          </Button>
        }
      >
        <p className="type-secondary">Nothing is sent to a server.</p>
      </Dialog>
      <BottomSheet open={sheetOpen} title="Payment note" description="The existing sheet." onClose={() => setSheetOpen(false)}>
        <Button variant="secondary" onClick={() => setSheetOpen(false)}>
          Close sheet
        </Button>
      </BottomSheet>
    </>
  );
}

function BalancePopover() {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);

  return (
    <>
      <Button
        ref={anchorRef}
        variant="secondary"
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => setOpen((current) => !current)}
      >
        Balance details
      </Button>
      <AnchoredPopover open={open} anchorRef={anchorRef as RefObject<HTMLElement | null>} label="Balance details" onCancel={() => setOpen(false)}>
        <PopoverBody onClose={() => setOpen(false)} />
      </AnchoredPopover>
    </>
  );
}

function PopoverBody({ onClose }: { onClose: () => void }) {
  return (
    <>
      <p className={review.note}>Available after pending payments.</p>
      <Button variant="secondary" onClick={onClose}>
        Close balance details
      </Button>
    </>
  );
}
