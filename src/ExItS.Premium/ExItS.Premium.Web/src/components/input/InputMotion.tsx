import { useEffect, useState, type FocusEvent, type ReactNode } from "react";
import { FocusDraw } from "./FocusDraw";
import type { InputFocusMotion } from "./focus-motion";
import styles from "./InputMotion.module.scss";

type InputMotionProps = {
  motion: InputFocusMotion;
  disabled?: boolean;
  readOnly?: boolean;
  /** Keep the focus treatment active while a portaled menu or sheet holds interaction. */
  held?: boolean;
  state?: "error" | "success";
  children: ReactNode;
};

function prefersReducedMotion() {
  return window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
}

export function InputMotion({ motion, disabled = false, readOnly = false, held = false, state, children }: InputMotionProps) {
  const [session, setSession] = useState({ id: 0, active: false });
  const [reduced, setReduced] = useState(prefersReducedMotion);
  const draw = motion === "focus-draw" || motion === "border-reveal";
  const drawn = draw && (session.active || held);

  useEffect(() => {
    const media = window.matchMedia?.("(prefers-reduced-motion: reduce)");
    if (!media) return;
    const onChange = () => setReduced(media.matches);
    media.addEventListener?.("change", onChange);
    return () => media.removeEventListener?.("change", onChange);
  }, []);

  if (motion === "none" || disabled) return children;

  function onFocus() {
    if (readOnly || !draw) return;
    setSession((current) => (current.active ? current : { id: current.id + 1, active: true }));
  }

  function onBlur(event: FocusEvent<HTMLDivElement>) {
    if (!draw || readOnly) return;
    const next = event.relatedTarget;
    if (next instanceof Node && event.currentTarget.contains(next)) return;
    setSession((current) => ({ ...current, active: false }));
  }

  return (
    <div
      className={styles.shell}
      data-motion={motion}
      data-state={state}
      data-quiet={readOnly ? "true" : undefined}
      data-held={held ? "true" : undefined}
      data-drawn={drawn ? "true" : undefined}
      onFocus={onFocus}
      onBlur={onBlur}
    >
      {motion === "soft-glow" ? <span className={styles.glow} aria-hidden="true" /> : null}
      {draw ? null : <span className={styles.steady} aria-hidden="true" />}
      {motion === "running-accent" ? (
        <span className={styles.trace} aria-hidden="true">
          <span className={styles.traceSpin} />
        </span>
      ) : null}
      {children}
      {draw && !readOnly ? (
        <FocusDraw
          active={drawn}
          generation={session.id || (held ? 1 : 0)}
          reduced={reduced}
          variant={motion === "border-reveal" ? "border-reveal" : "focus-draw"}
        />
      ) : null}
    </div>
  );
}
