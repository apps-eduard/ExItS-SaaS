import { useLayoutEffect, useRef, useState } from "react";
import { useAppearance } from "../theme/AppearanceProvider";
import { copyText, inkForColor, resolveSwatchValue } from "./color";
import styles from "./colors-page.module.scss";

type ColorShadeButtonProps = {
  label: string;
  token: string;
  variant?: "shade" | "semantic";
};

function readTokenValue(token: string) {
  return getComputedStyle(document.documentElement).getPropertyValue(token).trim();
}

export function ColorShadeButton({ label, token, variant = "shade" }: ColorShadeButtonProps) {
  const { resolvedTheme, palette } = useAppearance();
  const fillRef = useRef<HTMLButtonElement>(null);
  const [value, setValue] = useState("");
  const [copied, setCopied] = useState(false);
  const [announce, setAnnounce] = useState("");

  useLayoutEffect(() => {
    const node = fillRef.current;
    if (!node) return;
    const authored = readTokenValue(token);
    const resolved = resolveSwatchValue(authored, getComputedStyle(node).backgroundColor);
    setValue(resolved || token);
  }, [token, resolvedTheme, palette]);

  useLayoutEffect(() => {
    if (!copied) return;
    const timer = window.setTimeout(() => {
      setCopied(false);
      setAnnounce("");
    }, 1400);
    return () => window.clearTimeout(timer);
  }, [copied]);

  const ink = value ? inkForColor(value) : "#111827";
  const title = value ? `${token} · ${value}` : token;

  async function onCopy() {
    const next = value || token;
    const ok = await copyText(next);
    setCopied(ok);
    setAnnounce(ok ? `Copied ${next}` : `Could not copy ${label}`);
  }

  return (
    <button
      ref={fillRef}
      type="button"
      className={variant === "semantic" ? styles.semantic : styles.shade}
      style={{ background: `var(${token})`, color: ink }}
      title={title}
      aria-label={`Copy ${label} ${value || token}`}
      data-copied={copied ? "true" : undefined}
      data-token={token}
      data-value={value || undefined}
      onClick={onCopy}
    >
      {variant === "semantic" ? (
        <>
          <span className={styles.semanticName}>{label}</span>
          <span className={styles.semanticMeta}>{copied ? "Copied" : value || token}</span>
        </>
      ) : (
        <>
          <span className={styles.shadeLabel}>{label}</span>
          <span className={styles.shadeMeta}>{value || token}</span>
        </>
      )}
      {copied && variant === "shade" ? <span className={styles.copied}>Copied</span> : null}
      <span className={styles.live} aria-live="polite">
        {announce}
      </span>
    </button>
  );
}
