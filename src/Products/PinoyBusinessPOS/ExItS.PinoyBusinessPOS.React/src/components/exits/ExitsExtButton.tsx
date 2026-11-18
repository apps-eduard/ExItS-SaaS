import {
  Children,
  cloneElement,
  isValidElement,
  useEffect,
  useRef,
  useState,
  type ButtonHTMLAttributes,
  type CSSProperties,
  type MouseEvent,
  type ReactElement,
  type ReactNode,
} from "react";
import {
  INTENT_GLOBAL_AMBER,
  INTENT_GLOBAL_BLACK,
  INTENT_GLOBAL_DANGER,
  INTENT_GLOBAL_HELP,
  INTENT_GLOBAL_INFO,
  INTENT_GLOBAL_PRIMARY,
  INTENT_GLOBAL_SECONDARY,
  INTENT_GLOBAL_STANDARD,
  INTENT_GLOBAL_SUCCESS,
  INTENT_GLOBAL_WARN,
  INTENT_GLOBAL_WARN_FOREGROUND,
} from "@/features/ui-standards/intent-color-global-default";
import { cn } from "@/lib/cn";

/** Product default: ext btn secondary glossy-raise ripple */
export const EXITS_EXT_BTN_DEFAULT_COMMAND = "ext btn secondary glossy-raise ripple" as const;

export type ExitsExtButtonIntent =
  | "standard"
  | "primary"
  | "secondary"
  | "success"
  | "info"
  | "amber"
  | "danger"
  | "warn"
  | "help"
  | "black";

export type ExitsExtButtonAppearance = "standard" | "glossy" | "glossy-raise";

const INTENT_FILL: Record<ExitsExtButtonIntent, { bg: string; fg: string }> = {
  standard: { bg: INTENT_GLOBAL_STANDARD, fg: "#000000" },
  primary: { bg: INTENT_GLOBAL_PRIMARY, fg: "#ffffff" },
  secondary: { bg: INTENT_GLOBAL_SECONDARY, fg: "#ffffff" },
  success: { bg: INTENT_GLOBAL_SUCCESS, fg: "#ffffff" },
  info: { bg: INTENT_GLOBAL_INFO, fg: "#ffffff" },
  amber: { bg: INTENT_GLOBAL_AMBER, fg: "#000000" },
  danger: { bg: INTENT_GLOBAL_DANGER, fg: "#ffffff" },
  warn: { bg: INTENT_GLOBAL_WARN, fg: INTENT_GLOBAL_WARN_FOREGROUND },
  help: { bg: INTENT_GLOBAL_HELP, fg: "#ffffff" },
  black: { bg: INTENT_GLOBAL_BLACK, fg: "#ffffff" },
};

type Wave = { id: number; diameter: number; x: number; y: number };

function useExtBtnRipple(enabled: boolean) {
  const [waves, setWaves] = useState<readonly Wave[]>([]);
  const timers = useRef<number[]>([]);
  useEffect(() => {
    const pending = timers.current;
    return () => {
      for (const timer of pending) window.clearTimeout(timer);
    };
  }, []);

  function showRipple(event: MouseEvent<HTMLElement>) {
    if (!enabled) return;
    const target = event.currentTarget;
    if ("disabled" in target && (target as HTMLButtonElement).disabled) return;
    const rect = target.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    const farthest = Math.max(
      Math.hypot(x, y),
      Math.hypot(rect.width - x, y),
      Math.hypot(x, rect.height - y),
      Math.hypot(rect.width - x, rect.height - y),
    );
    const diameter = farthest * 2;
    const id = Date.now() + Math.random();
    setWaves((current) => [
      ...current,
      { id, diameter, x: x - diameter / 2, y: y - diameter / 2 },
    ]);
    const timer = window.setTimeout(() => {
      setWaves((current) => current.filter((wave) => wave.id !== id));
    }, 750);
    timers.current.push(timer);
  }

  const layer: ReactNode = enabled ? (
    <span className="exits-ext-btn__ripple-host" aria-hidden>
      {waves.map((wave) => (
        <span
          key={wave.id}
          className="exits-ext-btn__ripple"
          style={{ width: wave.diameter, height: wave.diameter, left: wave.x, top: wave.y }}
        />
      ))}
    </span>
  ) : null;

  return { showRipple, layer };
}

export type ExitsExtButtonProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children"> & {
  children: ReactNode;
  asChild?: boolean;
  intent?: ExitsExtButtonIntent;
  appearance?: ExitsExtButtonAppearance;
  ripple?: boolean;
  testId?: string;
};

export function exitsExtButtonCommand({
  intent = "secondary",
  appearance = "glossy-raise",
  ripple = true,
}: Pick<ExitsExtButtonProps, "intent" | "appearance" | "ripple"> = {}): string {
  const parts = ["ext btn", intent, appearance];
  if (ripple) parts.push("ripple");
  return parts.join(" ");
}

function exitsExtButtonClassName({
  appearance = "glossy-raise",
  ripple = true,
  className,
}: Pick<ExitsExtButtonProps, "appearance" | "ripple" | "className">): string {
  return cn(
    "exits-ext-btn",
    appearance === "glossy" && "exits-ext-btn--glossy",
    appearance === "glossy-raise" && "exits-ext-btn--glossy-raise",
    ripple && "exits-ext-btn--ripple",
    className,
  );
}

/**
 * Product ExItS Button playground recipe (`ext btn`).
 * Default: ext btn secondary glossy-raise ripple
 */
export function ExitsExtButton({
  children,
  asChild = false,
  intent = "secondary",
  appearance = "glossy-raise",
  ripple = true,
  className,
  testId,
  type = "button",
  style,
  onClick,
  ...props
}: ExitsExtButtonProps) {
  const fill = INTENT_FILL[intent];
  const { showRipple, layer } = useExtBtnRipple(ripple);
  const visualStyle: CSSProperties = {
    backgroundColor: fill.bg,
    color: fill.fg,
    ...style,
  };
  const classNames = exitsExtButtonClassName({ appearance, ripple, className });
  const recipe = exitsExtButtonCommand({ intent, appearance, ripple });

  if (asChild) {
    const child = Children.only(children);
    if (!isValidElement(child)) {
      throw new Error("ExitsExtButton asChild requires a single React element child.");
    }
    const childEl = child as ReactElement<{
      className?: string;
      style?: CSSProperties;
      onClick?: (event: MouseEvent<HTMLElement>) => void;
      children?: ReactNode;
    }>;
    return cloneElement(childEl, {
      className: cn(classNames, childEl.props.className),
      style: { ...visualStyle, ...childEl.props.style },
      "data-intent": intent,
      "data-appearance": appearance,
      "data-ripple": ripple ? "ripple" : "no-ripple",
      "data-recipe": recipe,
      "data-testid": testId ?? (childEl.props as { "data-testid"?: string })["data-testid"],
      onClick: (event: MouseEvent<HTMLElement>) => {
        showRipple(event);
        childEl.props.onClick?.(event);
        onClick?.(event as MouseEvent<HTMLButtonElement>);
      },
      children: (
        <>
          <span className="exits-ext-btn__content">{childEl.props.children}</span>
          {layer}
        </>
      ),
    } as never);
  }

  return (
    <button
      type={type}
      className={classNames}
      style={visualStyle}
      data-intent={intent}
      data-appearance={appearance}
      data-ripple={ripple ? "ripple" : "no-ripple"}
      data-recipe={recipe}
      data-testid={testId}
      onClick={(event) => {
        showRipple(event);
        onClick?.(event);
      }}
      {...props}
    >
      <span className="exits-ext-btn__content">{children}</span>
      {layer}
    </button>
  );
}
