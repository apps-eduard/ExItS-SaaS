import { useLayoutEffect, useRef } from "react";
import { borderRevealGeometry, focusDrawGeometry } from "./focus-draw";
import { INPUT_FOCUS_DRAW_EASING, INPUT_FOCUS_DRAW_MS, INPUT_FOCUS_DRAW_RELEASE_MS, INPUT_FOCUS_DRAW_STROKE } from "./focus-motion";
import styles from "./InputMotion.module.scss";

type FocusDrawProps = {
  active: boolean;
  generation: number;
  reduced: boolean;
  variant?: "focus-draw" | "border-reveal";
};

function readGeometry(shell: HTMLElement, variant: "focus-draw" | "border-reveal") {
  const box = shell.getBoundingClientRect();
  if (box.width < 1 || box.height < 1) return null;
  const radius = Number.parseFloat(getComputedStyle(shell).borderTopLeftRadius) || 0;
  const geometry = variant === "border-reveal" ? borderRevealGeometry : focusDrawGeometry;
  return geometry(box.width, box.height, radius);
}

function cancelMotion(path: SVGPathElement) {
  path.getAnimations?.().forEach((animation) => animation.cancel());
}

function strokeLength(path: SVGPathElement) {
  try {
    const length = path.getTotalLength();
    return length > 0 ? length : 1;
  } catch {
    return 1;
  }
}

function showSolid(path: SVGPathElement) {
  cancelMotion(path);
  path.style.opacity = "1";
  path.style.strokeDasharray = "none";
  path.style.strokeDashoffset = "0";
}

function play(path: SVGPathElement) {
  cancelMotion(path);
  const length = strokeLength(path);
  path.style.opacity = "1";
  path.style.strokeDasharray = `${length}`;
  path.style.strokeDashoffset = `${length}`;
  if (typeof path.animate !== "function") {
    path.style.strokeDashoffset = "0";
    return;
  }
  path.animate([{ strokeDashoffset: length }, { strokeDashoffset: 0 }], {
    duration: INPUT_FOCUS_DRAW_MS,
    easing: INPUT_FOCUS_DRAW_EASING,
    fill: "forwards",
    delay: 0,
  });
}

function release(path: SVGPathElement, stamp: number, stamps: { current: number }, reduced: boolean) {
  const offset = getComputedStyle(path).strokeDashoffset;
  cancelMotion(path);
  if (offset) path.style.strokeDashoffset = offset;
  if (reduced || typeof path.animate !== "function") {
    path.style.opacity = "0";
    path.style.strokeDashoffset = "1";
    return;
  }
  const fade = path.animate([{ opacity: 1 }, { opacity: 0 }], {
    duration: INPUT_FOCUS_DRAW_RELEASE_MS,
    easing: "cubic-bezier(0.2, 0, 1, 1)",
    fill: "forwards",
    delay: 0,
  });
  fade.onfinish = () => {
    if (stamps.current !== stamp) return;
    path.style.opacity = "0";
    path.style.strokeDashoffset = "1";
    fade.cancel();
  };
}

export function FocusDraw({ active, generation, reduced, variant = "focus-draw" }: FocusDrawProps) {
  const svgRef = useRef<SVGSVGElement>(null);
  const leftRef = useRef<SVGPathElement>(null);
  const rightRef = useRef<SVGPathElement>(null);
  const stamp = useRef(0);
  const played = useRef(0);

  useLayoutEffect(() => {
    const shell = svgRef.current?.parentElement;
    if (!shell) return;
    const apply = () => {
      const geometry = readGeometry(shell, variant);
      const svg = svgRef.current;
      const left = leftRef.current;
      const right = rightRef.current;
      if (!geometry || !svg || !left || !right) return;
      svg.setAttribute("viewBox", `0 0 ${geometry.width} ${geometry.height}`);
      for (const path of [left, right]) {
        path.setAttribute("d", path === left ? geometry.left : geometry.right);
        const running = path.getAnimations?.().some((animation) => animation.playState === "running");
        const offset = Number.parseFloat(getComputedStyle(path).strokeDashoffset);
        if (!running && path.style.opacity === "1" && offset === 0) {
          path.style.strokeDasharray = "none";
          path.style.strokeDashoffset = "0";
        }
      }
    };
    apply();
    const observer = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(apply);
    observer?.observe(shell);
    const root = document.documentElement;
    const appearance = new MutationObserver(apply);
    appearance.observe(root, { attributes: true, attributeFilter: ["data-radius", "data-density"] });
    return () => {
      observer?.disconnect();
      appearance.disconnect();
    };
  }, [variant]);

  useLayoutEffect(() => {
    const shell = svgRef.current?.parentElement;
    const left = leftRef.current;
    const right = rightRef.current;
    if (!shell || !left || !right) return;

    if (!active) {
      if (played.current === 0) return;
      played.current = 0;
      const current = ++stamp.current;
      release(left, current, stamp, reduced);
      release(right, current, stamp, reduced);
      return;
    }

    const geometry = readGeometry(shell, variant);
    if (geometry && svgRef.current) {
      svgRef.current.setAttribute("viewBox", `0 0 ${geometry.width} ${geometry.height}`);
      left.setAttribute("d", geometry.left);
      right.setAttribute("d", geometry.right);
    }

    if (reduced) {
      showSolid(left);
      showSolid(right);
      played.current = generation;
      return;
    }

    if (played.current === generation) return;
    played.current = generation;
    stamp.current += 1;
    play(left);
    play(right);
  }, [active, generation, reduced, variant]);

  return (
    <svg ref={svgRef} className={styles.draw} data-focus-draw="true" aria-hidden="true" width="100%" height="100%">
      <path ref={leftRef} className={styles.drawPath} data-stroke={variant === "border-reveal" ? "up" : "left"} strokeWidth={INPUT_FOCUS_DRAW_STROKE} />
      <path ref={rightRef} className={styles.drawPath} data-stroke={variant === "border-reveal" ? "down" : "right"} strokeWidth={INPUT_FOCUS_DRAW_STROKE} />
    </svg>
  );
}
