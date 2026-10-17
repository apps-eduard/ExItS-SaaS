import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { BatteryFull, Signal, Wifi, WifiOff, X } from "lucide-react";

import { PlaygroundTextField } from "@/features/ui-standards/UiStandardsPlaygrounds";
import { useBodyScrollLock } from "@/lib/use-body-scroll-lock";
import { cn } from "@/lib/cn";

/**
 * Quasar QDialog "Basic" and "Styling" examples.
 * Showcase only — product dialogs use `ExitsModal`.
 * @see https://quasar.dev/vue-components/dialog
 */
const LOREM =
  "Lorem ipsum dolor sit amet consectetur adipisicing elit. Rerum repellendus sit voluptate voluptas eveniet porro. Rerum blanditiis perferendis totam, ea at omnis vel numquam exercitationem aut, natus minima, porro labore.";

type QdialogTransition = "fade" | "flip";

function QdialogShell({
  open,
  onClose,
  persistent = false,
  transition = "fade",
  labelledBy,
  autofocusSelector,
  cardClassName,
  testId,
  children,
}: {
  open: boolean;
  onClose: () => void;
  persistent?: boolean;
  transition?: QdialogTransition;
  labelledBy?: string;
  autofocusSelector?: string;
  cardClassName?: string;
  testId: string;
  children: ReactNode;
}) {
  const panelRef = useRef<HTMLDivElement | null>(null);
  const restoreRef = useRef<HTMLElement | null>(null);
  const [shaking, setShaking] = useState(false);

  useBodyScrollLock(open);

  function dismissOrShake() {
    if (!persistent) {
      onClose();
      return;
    }
    setShaking(false);
    window.requestAnimationFrame(() => setShaking(true));
  }

  useEffect(() => {
    if (!open) return;
    restoreRef.current = document.activeElement as HTMLElement | null;
    const frame = window.requestAnimationFrame(() => {
      const panel = panelRef.current;
      if (!panel) return;
      const target = autofocusSelector ? panel.querySelector<HTMLElement>(autofocusSelector) : null;
      (target ?? panel).focus();
    });
    return () => {
      window.cancelAnimationFrame(frame);
      restoreRef.current?.focus?.();
      restoreRef.current = null;
    };
  }, [open, autofocusSelector]);

  useEffect(() => {
    if (!open) return;
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        dismissOrShake();
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  });

  if (!open || typeof document === "undefined") return null;

  return createPortal(
    <div
      className="ui-standards-qdialog"
      role="presentation"
      data-testid={`${testId}-backdrop`}
      onClick={dismissOrShake}
    >
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelledBy}
        tabIndex={-1}
        className={cn(
          "ui-standards-qdialog__card",
          `ui-standards-qdialog__card--${transition}`,
          shaking && "ui-standards-qdialog__card--shake",
          cardClassName,
        )}
        data-testid={testId}
        onAnimationEnd={(event) => {
          if (event.animationName === "ui-standards-qdialog-shake") setShaking(false);
        }}
        onClick={(event) => event.stopPropagation()}
      >
        {children}
      </div>
    </div>,
    document.body,
  );
}

function QBtn({
  label,
  onClick,
  flat = false,
  testId,
}: {
  label: string;
  onClick: () => void;
  flat?: boolean;
  testId?: string;
}) {
  return (
    <button
      type="button"
      className={cn("ui-standards-qdialog-btn", flat && "ui-standards-qdialog-btn--flat")}
      onClick={onClick}
      data-testid={testId}
    >
      {label}
    </button>
  );
}

function QCloseBtn({ onClick, testId, round = false }: { onClick: () => void; testId: string; round?: boolean }) {
  return (
    <button
      type="button"
      className={cn("ui-standards-qdialog-icon-btn", round && "ui-standards-qdialog-icon-btn--round")}
      aria-label="Close"
      title="Close"
      onClick={onClick}
      data-testid={testId}
    >
      <X className="size-5" aria-hidden />
    </button>
  );
}

function QBar({ onClose, testId }: { onClose: () => void; testId: string }) {
  return (
    <div className="ui-standards-qdialog-bar">
      <Wifi className="size-[1.125rem]" aria-hidden />
      <Signal className="size-[1.125rem]" aria-hidden />
      <BatteryFull className="size-[1.125rem]" aria-hidden />
      <div>9:34</div>
      <span className="flex-1" />
      <QCloseBtn onClick={onClose} testId={testId} />
    </div>
  );
}

type BasicId = "alert" | "confirm" | "prompt";
type StylingId = "icon" | "bar" | "bar2" | "toolbar";

function QdialogBasic() {
  const [open, setOpen] = useState<BasicId | null>(null);
  const [address, setAddress] = useState("");
  const close = () => setOpen(null);
  const alertTitle = useId();
  const promptTitle = useId();

  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-qdialog-basic">
      <div className="ui-standards-q-example__label">Basic</div>
      <div className="ui-standards-q-example__row">
        <QBtn label="Alert" onClick={() => setOpen("alert")} testId="ui-standard-qdialog-open-alert" />
        <QBtn label="Confirm" onClick={() => setOpen("confirm")} testId="ui-standard-qdialog-open-confirm" />
        <QBtn label="Prompt" onClick={() => setOpen("prompt")} testId="ui-standard-qdialog-open-prompt" />
      </div>

      <QdialogShell open={open === "alert"} onClose={close} labelledBy={alertTitle} testId="ui-standard-qdialog-alert">
        <div className="ui-standards-qdialog-section">
          <div id={alertTitle} className="ui-standards-qdialog-h6">
            Alert
          </div>
        </div>
        <div className="ui-standards-qdialog-section ui-standards-qdialog-section--pt-none">{LOREM}</div>
        <div className="ui-standards-qdialog-actions">
          <QBtn flat label="OK" onClick={close} testId="ui-standard-qdialog-alert-ok" />
        </div>
      </QdialogShell>

      <QdialogShell open={open === "confirm"} onClose={close} persistent testId="ui-standard-qdialog-confirm">
        <div className="ui-standards-qdialog-section ui-standards-qdialog-section--row">
          <span className="ui-standards-qdialog-avatar" aria-hidden>
            <WifiOff className="size-6" />
          </span>
          <span className="ml-2">You are currently not connected to any network.</span>
        </div>
        <div className="ui-standards-qdialog-actions">
          <QBtn flat label="Cancel" onClick={close} testId="ui-standard-qdialog-confirm-cancel" />
          <QBtn flat label="Turn on Wifi" onClick={close} />
        </div>
      </QdialogShell>

      <QdialogShell
        open={open === "prompt"}
        onClose={close}
        persistent
        labelledBy={promptTitle}
        autofocusSelector="input"
        cardClassName="ui-standards-qdialog__card--min-350"
        testId="ui-standard-qdialog-prompt"
      >
        <div className="ui-standards-qdialog-section">
          <div id={promptTitle} className="ui-standards-qdialog-h6">
            Your address
          </div>
        </div>
        <div
          className="ui-standards-qdialog-section ui-standards-qdialog-section--pt-none"
          onKeyUp={(event) => {
            if (event.key === "Enter") close();
          }}
        >
          <PlaygroundTextField
            label="Address"
            value={address}
            onChange={setAddress}
            testId="ui-standard-qdialog-prompt-input"
          />
        </div>
        <div className="ui-standards-qdialog-actions">
          <QBtn flat label="Cancel" onClick={close} />
          <QBtn flat label="Add address" onClick={close} />
        </div>
      </QdialogShell>
    </div>
  );
}

function QdialogStyling() {
  const [open, setOpen] = useState<StylingId | null>(null);
  const close = () => setOpen(null);
  const iconTitle = useId();
  const bar2Title = useId();

  return (
    <div className="ui-standards-q-example" data-testid="ui-standard-qdialog-styling">
      <div className="ui-standards-q-example__label">Styling</div>
      <div className="ui-standards-q-example__row">
        <QBtn label="Close Icon" onClick={() => setOpen("icon")} testId="ui-standard-qdialog-open-icon" />
        <QBtn label="With QBar" onClick={() => setOpen("bar")} testId="ui-standard-qdialog-open-bar" />
        <QBtn label="Another QBar" onClick={() => setOpen("bar2")} testId="ui-standard-qdialog-open-bar2" />
        <QBtn label="With QToolbar" onClick={() => setOpen("toolbar")} testId="ui-standard-qdialog-open-toolbar" />
      </div>

      <QdialogShell open={open === "icon"} onClose={close} labelledBy={iconTitle} testId="ui-standard-qdialog-icon">
        <div className="ui-standards-qdialog-section ui-standards-qdialog-section--row ui-standards-qdialog-section--pb-none">
          <div id={iconTitle} className="ui-standards-qdialog-h6">
            Close icon
          </div>
          <span className="flex-1" />
          <QCloseBtn round onClick={close} testId="ui-standard-qdialog-icon-close" />
        </div>
        <div className="ui-standards-qdialog-section">{LOREM}</div>
      </QdialogShell>

      <QdialogShell open={open === "bar"} onClose={close} persistent testId="ui-standard-qdialog-bar">
        <QBar onClose={close} testId="ui-standard-qdialog-bar-close" />
        <div className="ui-standards-qdialog-section">{LOREM}</div>
        <div className="ui-standards-qdialog-section ui-standards-qdialog-section--pt-none">{LOREM}</div>
      </QdialogShell>

      <QdialogShell
        open={open === "bar2"}
        onClose={close}
        persistent
        transition="flip"
        labelledBy={bar2Title}
        cardClassName="ui-standards-qdialog__card--primary"
        testId="ui-standard-qdialog-bar2"
      >
        <QBar onClose={close} testId="ui-standard-qdialog-bar2-close" />
        <div className="ui-standards-qdialog-section">
          <div id={bar2Title} className="ui-standards-qdialog-h6">
            Alert
          </div>
        </div>
        <div className="ui-standards-qdialog-section ui-standards-qdialog-section--pt-none">{LOREM}</div>
      </QdialogShell>

      <QdialogShell open={open === "toolbar"} onClose={close} testId="ui-standard-qdialog-toolbar">
        <div className="ui-standards-qdialog-toolbar">
          <span className="ui-standards-qdialog-toolbar__avatar" aria-hidden>
            Q
          </span>
          <div className="ui-standards-qdialog-toolbar__title">
            <span className="font-bold">Quasar</span> Framework
          </div>
          <QCloseBtn round onClick={close} testId="ui-standard-qdialog-toolbar-close" />
        </div>
        <div className="ui-standards-qdialog-section">{LOREM}</div>
      </QdialogShell>
    </div>
  );
}

export function UiStandardsQdialogGallery(): ReactNode {
  return (
    <div className="flex flex-col gap-4" data-testid="ui-standard-qdialog-gallery">
      <QdialogBasic />
      <QdialogStyling />
    </div>
  );
}
