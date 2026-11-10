import { useEffect, useId, useRef, type ReactNode } from "react";
import { ExIcon } from "../../icons/ExIcon";
import { Button } from "../button/Button";
import styles from "./Dialog.module.scss";

type DialogProps = {
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  children?: ReactNode;
  footer?: ReactNode;
};

function showDialog(dialog: HTMLDialogElement) {
  try {
    if (!dialog.open) dialog.showModal();
  } catch {
    dialog.setAttribute("open", "");
  }
}

function hideDialog(dialog: HTMLDialogElement) {
  if (dialog.open && typeof dialog.close === "function") dialog.close();
  dialog.removeAttribute("open");
  delete dialog.dataset.closing;
}

export function Dialog({ open, title, description, onClose, children, footer }: DialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const restoreRef = useRef<HTMLElement | null>(null);
  const requestedRef = useRef(false);
  const titleId = useId();
  const descriptionId = useId();

  function requestClose() {
    if (requestedRef.current) return;
    requestedRef.current = true;
    onClose();
    window.setTimeout(() => {
      if (dialogRef.current?.open) requestedRef.current = false;
    }, 0);
  }

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open) {
      requestedRef.current = false;
      if (document.activeElement instanceof HTMLElement && !dialog.contains(document.activeElement)) {
        restoreRef.current = document.activeElement;
      }
      showDialog(dialog);
      const target = dialog.querySelector<HTMLElement>("[data-autofocus]") ?? dialog.querySelector<HTMLElement>("button, input, select, textarea, a[href]");
      target?.focus();
      return;
    }
    if (!dialog.open && !dialog.hasAttribute("open")) return;
    const duration = Number.parseFloat(getComputedStyle(dialog).animationDuration) || 0;
    if (duration < 0.05) {
      hideDialog(dialog);
      restoreRef.current?.focus();
      return;
    }
    dialog.dataset.closing = "true";
    const finish = (event: AnimationEvent) => {
      if (event.target !== dialog) return;
      dialog.removeEventListener("animationend", finish);
      hideDialog(dialog);
      restoreRef.current?.focus();
    };
    dialog.addEventListener("animationend", finish);
    return () => dialog.removeEventListener("animationend", finish);
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      className={styles.dialog}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        event.preventDefault();
        requestClose();
      }}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.preventDefault();
          requestClose();
        }
      }}
      onClick={(event) => {
        if (event.target !== event.currentTarget) return;
        const box = event.currentTarget.getBoundingClientRect();
        const inside = event.clientX >= box.left && event.clientX <= box.right && event.clientY >= box.top && event.clientY <= box.bottom;
        if (!inside) requestClose();
      }}
    >
      <header className={styles.header}>
        <div className={styles.copy}>
          <h2 id={titleId} className="type-section">
            {title}
          </h2>
          {description ? (
            <p id={descriptionId} className="type-secondary">
              {description}
            </p>
          ) : null}
        </div>
        <Button variant="ghost" iconOnly aria-label="Close" onClick={requestClose} icon={<ExIcon name="close" />} />
      </header>
      {children ? <div className={styles.body}>{children}</div> : null}
      {footer ? <div className={styles.footer}>{footer}</div> : null}
    </dialog>
  );
}
