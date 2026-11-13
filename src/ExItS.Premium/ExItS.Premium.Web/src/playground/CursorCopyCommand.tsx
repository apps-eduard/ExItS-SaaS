import { useEffect, useState } from "react";
import { copyText } from "./color";
import styles from "./CursorCopyCommand.module.scss";

export function formatCursorClipboard(standard: string, command: string) {
  const apply = command.trim().replace(/\s+/g, " ");
  return [
    `Use the locked ExItS ${standard} Standard.`,
    `Apply: ${apply}.`,
    "Preserve existing business behavior, domain rules, permissions, data flow, and API behavior unless explicitly instructed otherwise.",
  ].join("\n");
}

function CopyGlyph({ done }: { done: boolean }) {
  if (done) {
    return (
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M5 13l4 4L19 7" stroke="currentColor" strokeWidth="2.25" strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    );
  }
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <rect x="9" y="9" width="11" height="11" rx="2" stroke="currentColor" strokeWidth="2" />
      <path d="M5 15V5a2 2 0 0 1 2-2h10" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  );
}

export function CursorCopyCommand({
  standard,
  command,
  className,
}: {
  standard: string;
  command: string;
  className?: string;
}) {
  const [status, setStatus] = useState<"idle" | "copied" | "failed">("idle");
  const fullText = formatCursorClipboard(standard, command);

  useEffect(() => {
    if (status === "idle") return;
    const timer = window.setTimeout(() => setStatus("idle"), 1500);
    return () => window.clearTimeout(timer);
  }, [status]);

  async function copy() {
    const ok = await copyText(fullText);
    setStatus(ok ? "copied" : "failed");
  }

  return (
    <div
      className={[styles.bar, className].filter(Boolean).join(" ")}
      data-testid="cursor-copy-command"
      data-command={command}
      data-standard={standard}
    >
      <div className={styles.body}>
        <p className={styles.label}>Cursor</p>
        <code className={styles.command}>{command}</code>
        {status === "failed" ? (
          <p className={styles.fail} role="status">
            Could not copy — select the command and copy manually.
          </p>
        ) : null}
      </div>
      <button
        type="button"
        className={styles.copy}
        data-status={status}
        aria-label={status === "copied" ? "Copied Cursor command" : "Copy Cursor command"}
        onClick={() => void copy()}
      >
        <CopyGlyph done={status === "copied"} />
      </button>
      <span className={styles.live} aria-live="polite">
        {status === "copied" ? "Copied" : ""}
      </span>
    </div>
  );
}
