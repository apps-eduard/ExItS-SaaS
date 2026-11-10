import type { ButtonHTMLAttributes, ReactNode, Ref } from "react";
import styles from "./Button.module.scss";

export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger" | "success";
export type ButtonAppearance = "solid" | "soft" | "outline" | "raise" | "solid-raise";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  ref?: Ref<HTMLButtonElement>;
  variant?: ButtonVariant;
  appearance?: ButtonAppearance;
  loading?: boolean;
  icon?: ReactNode;
  iconOnly?: boolean;
  fullWidth?: boolean;
};

function cx(...parts: Array<string | false | undefined>) {
  return parts.filter(Boolean).join(" ");
}

export function Button({
  ref,
  variant = "primary",
  appearance = "solid",
  loading = false,
  icon,
  iconOnly = false,
  fullWidth = false,
  children,
  className,
  disabled,
  type = "button",
  onClick,
  ...props
}: ButtonProps) {
  return (
    <button
      ref={ref}
      {...props}
      type={type}
      className={cx(styles.button, styles[variant], iconOnly && styles.iconOnly, fullWidth && styles.fullWidth, loading && styles.loading, className)}
      disabled={disabled}
      data-appearance={appearance}
      data-loading={loading ? "true" : undefined}
      aria-busy={loading || undefined}
      onClick={(event) => {
        if (loading) {
          event.preventDefault();
          return;
        }
        onClick?.(event);
      }}
    >
      {loading ? <span className={styles.spinner} aria-hidden="true" /> : icon}
      {children}
    </button>
  );
}
