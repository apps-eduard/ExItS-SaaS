import { useLayoutEffect, useState, type KeyboardEvent, type MouseEvent } from "react";
import { copyText, inkForHex } from "./color";
import styles from "./colors-page.module.scss";

export type ReferenceCopyMode = "hex" | "name";

type ReferenceShadeButtonProps = {
  name: string;
  label: string;
  hex: string;
  copyMode: ReferenceCopyMode;
};

export function ReferenceShadeButton({ name, label, hex, copyMode }: ReferenceShadeButtonProps) {
  const [copied, setCopied] = useState(false);
  const [announce, setAnnounce] = useState("");
  const ink = inkForHex(hex);
  const normalizedHex = hex.startsWith("#") ? hex.toLowerCase() : `#${hex.toLowerCase()}`;

  useLayoutEffect(() => {
    if (!copied) return;
    const timer = window.setTimeout(() => {
      setCopied(false);
      setAnnounce("");
    }, 1400);
    return () => window.clearTimeout(timer);
  }, [copied]);

  async function copyValue(preferName: boolean) {
    const value = preferName ? name : normalizedHex;
    const ok = await copyText(value);
    setCopied(ok);
    setAnnounce(ok ? `Copied ${value}` : `Could not copy ${name}`);
  }

  async function onClick(event: MouseEvent<HTMLButtonElement>) {
    const preferName = copyMode === "name" || event.altKey || event.shiftKey;
    await copyValue(preferName);
  }

  async function onKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key === "Enter" || event.key === " ") {
      // Native button activation still fires click; only intercept Alt/Shift modifiers for name.
      if (event.altKey || event.shiftKey) {
        event.preventDefault();
        await copyValue(true);
      }
    }
  }

  async function onContextMenu(event: MouseEvent<HTMLButtonElement>) {
    event.preventDefault();
    await copyValue(true);
  }

  return (
    <button
      type="button"
      className={styles.shade}
      style={{ background: normalizedHex, color: ink }}
      title={`${label} · ${name} · ${normalizedHex}`}
      aria-label={`Copy ${name} ${normalizedHex}`}
      data-copied={copied ? "true" : undefined}
      data-name={name}
      data-hex={normalizedHex}
      onClick={onClick}
      onKeyDown={onKeyDown}
      onContextMenu={onContextMenu}
    >
      <span className={styles.shadeLabel}>{label}</span>
      <span className={styles.shadeMeta}>{normalizedHex}</span>
      {copied ? <span className={styles.copied}>Copied</span> : null}
      <span className={styles.live} aria-live="polite">
        {announce}
      </span>
    </button>
  );
}
