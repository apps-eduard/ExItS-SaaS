import { useEffect, useRef, useState, type ButtonHTMLAttributes, type KeyboardEvent, type PointerEvent, type ReactNode, type Ref } from "react";
import styles from "./Button.module.scss";

export type ButtonVariant =
  | "primary"
  | "secondary"
  | "accent"
  | "success"
  | "info"
  | "warning"
  | "help"
  | "danger"
  | "contrast"
  | "ghost";
export type ButtonAppearance =
  | "solid"
  | "soft"
  | "outline"
  | "ghost"
  | "raise"
  | "soft-raise"
  | "solid-raise";

export const BUTTON_APPEARANCES = [
  "solid",
  "soft",
  "outline",
  "ghost",
  "raise",
  "soft-raise",
  "solid-raise",
] as const satisfies readonly ButtonAppearance[];

export const BUTTON_SEMANTIC_VARIANTS = [
  "primary",
  "secondary",
  "accent",
  "success",
  "info",
  "warning",
  "help",
  "danger",
  "contrast",
] as const satisfies readonly ButtonVariant[];

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  ref?: Ref<HTMLButtonElement>;
  variant?: ButtonVariant;
  appearance?: ButtonAppearance;
  loading?: boolean;
  /** Material-style press wave. Default on. */
  ripple?: boolean;
  icon?: ReactNode;
  iconOnly?: boolean;
  fullWidth?: boolean;
};

type Wave = { id: number; diameter: number; x: number; y: number };

function cx(...parts: Array<string | false | undefined>) {
  return parts.filter(Boolean).join(" ");
}

function useButtonRipple(enabled: boolean) {
  const [waves, setWaves] = useState<readonly Wave[]>([]);
  const timers = useRef<number[]>([]);

  useEffect(() => {
    const pending = timers.current;
    return () => {
      for (const timer of pending) window.clearTimeout(timer);
    };
  }, []);

  function spawn(target: HTMLElement, clientX: number, clientY: number) {
    if (!enabled) return;
    if ("disabled" in target && (target as HTMLButtonElement).disabled) return;
    const rect = target.getBoundingClientRect();
    const x = clientX - rect.left;
    const y = clientY - rect.top;
    const farthest = Math.max(
      Math.hypot(x, y),
      Math.hypot(rect.width - x, y),
      Math.hypot(x, rect.height - y),
      Math.hypot(rect.width - x, rect.height - y),
    );
    const diameter = farthest * 2;
    const id = Date.now() + Math.random();
    setWaves((current) => [...current, { id, diameter, x: x - diameter / 2, y: y - diameter / 2 }]);
    const timer = window.setTimeout(() => {
      setWaves((current) => current.filter((wave) => wave.id !== id));
    }, 700);
    timers.current.push(timer);
  }

  function onPointerDown(event: PointerEvent<HTMLButtonElement>) {
    if (event.button !== 0) return;
    spawn(event.currentTarget, event.clientX, event.clientY);
  }

  function onKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (event.key !== "Enter" && event.key !== " ") return;
    if (event.repeat) return;
    const rect = event.currentTarget.getBoundingClientRect();
    spawn(event.currentTarget, rect.left + rect.width / 2, rect.top + rect.height / 2);
  }

  const layer = enabled ? (
    <span className={styles.rippleHost} aria-hidden="true">
      {waves.map((wave) => (
        <span
          key={wave.id}
          className={styles.ripple}
          data-ripple-wave=""
          style={{ width: wave.diameter, height: wave.diameter, left: wave.x, top: wave.y }}
        />
      ))}
    </span>
  ) : null;

  return { layer, onPointerDown, onKeyDown };
}

export function Button({
  ref,
  variant = "primary",
  appearance = "solid",
  loading = false,
  ripple = true,
  icon,
  iconOnly = false,
  fullWidth = false,
  children,
  className,
  disabled,
  type = "button",
  onClick,
  onPointerDown,
  onKeyDown,
  ...props
}: ButtonProps) {
  const waves = useButtonRipple(ripple && !disabled && !loading);

  return (
    <button
      ref={ref}
      {...props}
      type={type}
      className={cx(
        styles.button,
        styles[variant],
        iconOnly && styles.iconOnly,
        fullWidth && styles.fullWidth,
        loading && styles.loading,
        className,
      )}
      disabled={disabled}
      data-appearance={appearance}
      data-loading={loading ? "true" : undefined}
      data-ripple={ripple ? "true" : "false"}
      aria-busy={loading || undefined}
      onPointerDown={(event) => {
        waves.onPointerDown(event);
        onPointerDown?.(event);
      }}
      onKeyDown={(event) => {
        waves.onKeyDown(event);
        onKeyDown?.(event);
      }}
      onClick={(event) => {
        if (loading) {
          event.preventDefault();
          return;
        }
        onClick?.(event);
      }}
    >
      {waves.layer}
      <span className={styles.content}>
        {loading ? <span className={styles.spinner} aria-hidden="true" /> : icon}
        {children}
      </span>
    </button>
  );
}
