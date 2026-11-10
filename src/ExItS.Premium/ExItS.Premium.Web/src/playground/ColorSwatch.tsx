import { useLayoutEffect, useRef, useState } from "react";
import { useAppearance } from "../theme/AppearanceProvider";
import { toColorText } from "./color";
import { CopyControl } from "./CopyControl";
import styles from "./catalog.module.scss";

export function ColorSwatch({ name, token }: { name: string; token: string }) {
  const { resolvedTheme, palette } = useAppearance();
  const fillRef = useRef<HTMLSpanElement>(null);
  const [hex, setHex] = useState("");

  useLayoutEffect(() => {
    const node = fillRef.current;
    if (!node) return;
    setHex(toColorText(getComputedStyle(node).backgroundColor));
  }, [token, resolvedTheme, palette]);

  return (
    <article className={styles.swatch}>
      <span ref={fillRef} className={styles.fill} style={{ background: `var(${token})` }} />
      <p className={styles.name}>{name}</p>
      <p className={styles.token}>{token}</p>
      <p className={styles.hex}>{hex}</p>
      <CopyControl label={`${name}, ${token}`} value={hex || token} />
    </article>
  );
}
