import { useLayoutEffect, useState, type MouseEvent } from "react";
import { copyText, inkForHex } from "./color";
import styles from "./colors-page.module.scss";

export type ReferenceCopyMode = "hex" | "name";

type ReferenceShadeButtonProps = {
  name: string;
  hex: string;
  copyMode: ReferenceCopyMode;
};

export function ReferenceShadeButton({ name, hex, copyMode }: ReferenceShadeButtonProps) {
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

  async function onCopy(event: MouseEvent<HTMLButtonElement>) {
    const preferName = copyMode === "name" || event.altKey || event.shiftKey;
    const value = preferName ? name : normalizedHex;
    const ok = await copyText(value);
    setCopied(ok);
    setAnnounce(ok ? `Copied ${value}` : `Could not copy ${name}`);
  }

  return (
    <button
      type="button"
      className={styles.shade}
      style={{ background: normalizedHex, color: ink }}
      title={`${name} · ${normalizedHex}`}
      aria-label={`Copy ${name} ${normalizedHex}`}
      data-copied={copied ? "true" : undefined}
      data-name={name}
      data-hex={normalizedHex}
      onClick={onCopy}
    >
      <span className={styles.shadeLabel}>{name}</span>
      <span className={styles.shadeMeta}>{normalizedHex}</span>
      {copied ? <span className={styles.copied}>Copied</span> : null}
      <span className={styles.live} aria-live="polite">
        {announce}
      </span>
    </button>
  );
}
