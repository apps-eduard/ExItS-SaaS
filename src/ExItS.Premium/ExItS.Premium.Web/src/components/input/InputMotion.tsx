import type { ReactNode } from "react";
import type { InputFocusMotion } from "./focus-motion";
import styles from "./InputMotion.module.scss";

type InputMotionProps = {
  motion: InputFocusMotion;
  disabled?: boolean;
  readOnly?: boolean;
  state?: "error" | "success";
  children: ReactNode;
};

export function InputMotion({ motion, disabled = false, readOnly = false, state, children }: InputMotionProps) {
  if (motion === "none" || disabled) return children;

  return (
    <div className={styles.shell} data-motion={motion} data-state={state} data-quiet={readOnly ? "true" : undefined}>
      {motion === "soft-glow" ? <span className={styles.glow} aria-hidden="true" /> : null}
      <span className={styles.steady} aria-hidden="true" />
      {motion === "running-accent" ? (
        <span className={styles.trace} aria-hidden="true">
          <span className={styles.traceSpin} />
        </span>
      ) : null}
      {children}
    </div>
  );
}
