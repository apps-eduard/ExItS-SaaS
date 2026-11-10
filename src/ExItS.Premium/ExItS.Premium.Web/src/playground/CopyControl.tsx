import { useEffect, useState } from "react";
import styles from "./catalog.module.scss";

export function CopyControl({ label, value }: { label: string; value: string }) {
  const [status, setStatus] = useState<"idle" | "copied" | "failed">("idle");

  useEffect(() => {
    if (status === "idle") return;
    const timer = window.setTimeout(() => setStatus("idle"), 1600);
    return () => window.clearTimeout(timer);
  }, [status]);

  async function copy(event: { currentTarget: HTMLButtonElement }) {
    const trigger = event.currentTarget;
    try {
      await navigator.clipboard.writeText(value);
      setStatus("copied");
      return;
    } catch {
      const field = document.createElement("textarea");
      field.value = value;
      field.setAttribute("readonly", "");
      field.tabIndex = -1;
      field.style.position = "fixed";
      field.style.opacity = "0";
      document.body.append(field);
      field.focus();
      field.select();
      const ok = document.execCommand("copy");
      field.remove();
      trigger.focus();
      setStatus(ok ? "copied" : "failed");
    }
  }

  const visible = status === "copied" ? "Copied" : status === "failed" ? "Copy failed" : "Copy";
  const announced = status === "copied" ? `Copied ${value}` : status === "failed" ? `Could not copy ${label}` : "";

  return (
    <>
      <button type="button" className={styles.copy} aria-label={status === "idle" ? `Copy ${label}` : `${visible} ${label}`} onClick={copy}>
        {visible}
      </button>
      <span className={styles.live} aria-live="polite">
        {announced}
      </span>
    </>
  );
}
