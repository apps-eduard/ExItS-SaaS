import { useLayoutEffect, useRef, useState } from "react";
import { useAppearance } from "../theme/AppearanceProvider";
import { copyText, inkForHex, toColorText } from "./color";
import styles from "./colors-page.module.scss";

type ColorShadeButtonProps = {
  label: string;
  token: string;
  variant?: "shade" | "semantic";
};

export function ColorShadeButton({ label, token, variant = "shade" }: ColorShadeButtonProps) {
  const { resolvedTheme, palette } = useAppearance();
  const fillRef = useRef<HTMLButtonElement>(null);
  const [hex, setHex] = useState("");
  const [copied, setCopied] = useState(false);
  const [announce, setAnnounce] = useState("");

  useLayoutEffect(() => {
    const node = fillRef.current;
    if (!node) return;
    const computed = getComputedStyle(node).backgroundColor;
    const fromPaint = toColorText(computed);
    if (fromPaint && !/^rgba?\(\s*0[,\s]+0[,\s]+0(?:[,\s/]+0(?:\.0+)?)?\s*\)$/i.test(fromPaint)) {
      setHex(fromPaint);
      return;
    }
    const fromToken = toColorText(getComputedStyle(document.documentElement).getPropertyValue(token).trim());
    setHex(fromToken || fromPaint || token);
  }, [token, resolvedTheme, palette]);

  useLayoutEffect(() => {
    if (!copied) return;
    const timer = window.setTimeout(() => {
      setCopied(false);
      setAnnounce("");
    }, 1400);
    return () => window.clearTimeout(timer);
  }, [copied]);

  const ink = hex.startsWith("#") ? inkForHex(hex) : "#111827";
  const title = hex ? `${token} · ${hex}` : token;

  async function onCopy() {
    const value = hex || token;
    const ok = await copyText(value);
    setCopied(ok);
    setAnnounce(ok ? `Copied ${value}` : `Could not copy ${label}`);
  }

  return (
    <button
      ref={fillRef}
      type="button"
      className={variant === "semantic" ? styles.semantic : styles.shade}
      style={{ background: `var(${token})`, color: ink }}
      title={title}
      aria-label={`Copy ${label} ${hex || token}`}
      data-copied={copied ? "true" : undefined}
      onClick={onCopy}
    >
      {variant === "semantic" ? (
        <>
          <span className={styles.semanticName}>{label}</span>
          <span className={styles.semanticMeta}>{copied ? "Copied" : hex || token}</span>
        </>
      ) : (
        <>
          <span className={styles.shadeLabel}>{label}</span>
          <span className={styles.shadeMeta}>{hex || token}</span>
        </>
      )}
      {copied && variant === "shade" ? <span className={styles.copied}>Copied</span> : null}
      <span className={styles.live} aria-live="polite">
        {announce}
      </span>
    </button>
  );
}
