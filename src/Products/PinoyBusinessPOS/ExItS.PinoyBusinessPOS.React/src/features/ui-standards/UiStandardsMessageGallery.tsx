import { useCallback, useState, type ReactNode } from "react";
import {
  AlertTriangle,
  Check,
  CircleX,
  Loader2,
  Receipt,
  Sparkles,
  Wifi,
  X,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";
import { PlaygroundLabel } from "@/features/ui-standards/UiStandardsSnippetBlock";

/**
 * Full Diamond PrimeNG Message UI Kit sample gallery.
 * @see https://diamond.primeng.dev/uikit/message
 * Showcase-only — does not change product Notice / Toast APIs.
 */

type MessageSeverity = "success" | "info" | "warn" | "error" | "secondary" | "contrast";
type MessageVariant = "filled" | "outlined" | "simple";

type GalleryMessageDef = {
  severity: MessageSeverity;
  text: ReactNode;
  Icon: LucideIcon;
};

const MESSAGE_FILLED_COPY: Record<MessageSeverity, string> = {
  success: "Your account is now ready.",
  info: "Upgrade now and save %5.",
  warn: "Your subscription is about to expire.",
  error: "Something went wrong.",
  secondary: "Processing may take a few moments.",
  contrast: "You're currently in offline mode.",
};

const MESSAGE_SAMPLES: ReadonlyArray<GalleryMessageDef> = [
  {
    severity: "success",
    text: "Your account is now ready.",
    Icon: Check,
  },
  {
    severity: "info",
    text: (
      <>
        Upgrade now and save %5.{" "}
        <a
          href="#upgrade"
          className="font-semibold underline underline-offset-2"
          onClick={(e) => e.preventDefault()}
        >
          Upgrade
        </a>
      </>
    ),
    Icon: Sparkles,
  },
  {
    severity: "warn",
    text: (
      <>
        Your subscription is about to expire.{" "}
        <a
          href="#renew"
          className="font-semibold underline underline-offset-2"
          onClick={(e) => e.preventDefault()}
        >
          Renew
        </a>
      </>
    ),
    Icon: Receipt,
  },
  {
    severity: "error",
    text: (
      <>
        Something went wrong. Please{" "}
        <a
          href="#retry"
          className="font-semibold underline underline-offset-2"
          onClick={(e) => e.preventDefault()}
        >
          try again
        </a>
        .
      </>
    ),
    Icon: AlertTriangle,
  },
  {
    severity: "secondary",
    text: "Processing may take a few moments.",
    Icon: Loader2,
  },
  {
    severity: "contrast",
    text: "You're currently in offline mode.",
    Icon: Wifi,
  },
];

/** Filled Message (Diamond): soft tint fill + pastel outline — not a hard Outlined stroke. */
const FILLED_CLASS: Record<MessageSeverity, string> = {
  success:
    "border-0 bg-[color-mix(in_srgb,#f0fdf4_95%,transparent)] text-[#16a34a] outline outline-[0.8px] outline-[#bbf7d0] [box-shadow:0_4px_8px_color-mix(in_srgb,#22c55e_4%,transparent)]",
  info:
    "border-0 bg-[color-mix(in_srgb,#eff6ff_95%,transparent)] text-[#2563eb] outline outline-[0.8px] outline-[#bfdbfe] [box-shadow:0_4px_8px_color-mix(in_srgb,#3b82f6_4%,transparent)]",
  warn:
    "border-0 bg-[color-mix(in_srgb,#fefce8_95%,transparent)] text-[#ca8a04] outline outline-[0.8px] outline-[#fef08a] [box-shadow:0_4px_8px_color-mix(in_srgb,#eab308_4%,transparent)]",
  error:
    "border-0 bg-[color-mix(in_srgb,#fef2f2_95%,transparent)] text-[#dc2626] outline outline-[0.8px] outline-[#fecaca] [box-shadow:0_4px_8px_color-mix(in_srgb,#ef4444_4%,transparent)]",
  secondary:
    "border-0 bg-[#f1f5f9] text-[#475569] outline outline-[0.8px] outline-[#e2e8f0] [box-shadow:0_4px_8px_color-mix(in_srgb,#64748b_4%,transparent)]",
  contrast:
    "border-0 bg-[#0f172a] text-[#f8fafc] outline outline-[0.8px] outline-[#020617] [box-shadow:0_4px_8px_color-mix(in_srgb,#020617_4%,transparent)]",
};

/** Outlined Message (Diamond): transparent fill + strong severity outline. */
const OUTLINED_CLASS: Record<MessageSeverity, string> = {
  success:
    "border-0 bg-transparent text-[#16a34a] outline outline-[0.8px] outline-[#16a34a] [box-shadow:0_4px_8px_color-mix(in_srgb,#22c55e_4%,transparent)]",
  info:
    "border-0 bg-transparent text-[#2563eb] outline outline-[0.8px] outline-[#2563eb] [box-shadow:0_4px_8px_color-mix(in_srgb,#3b82f6_4%,transparent)]",
  warn:
    "border-0 bg-transparent text-[#ca8a04] outline outline-[0.8px] outline-[#ca8a04] [box-shadow:0_4px_8px_color-mix(in_srgb,#eab308_4%,transparent)]",
  error:
    "border-0 bg-transparent text-[#dc2626] outline outline-[0.8px] outline-[#dc2626] [box-shadow:0_4px_8px_color-mix(in_srgb,#ef4444_4%,transparent)]",
  secondary:
    "border-0 bg-transparent text-[#64748b] outline outline-[0.8px] outline-[#94a3b8] [box-shadow:0_4px_8px_color-mix(in_srgb,#64748b_4%,transparent)]",
  contrast:
    "border-0 bg-transparent text-[#020617] outline outline-[0.8px] outline-[#020617] [box-shadow:0_4px_8px_color-mix(in_srgb,#020617_4%,transparent)]",
};

const SIMPLE_CLASS: Record<MessageSeverity, string> = {
  success: "border-0 bg-transparent text-[#16a34a] outline-none shadow-none",
  info: "border-0 bg-transparent text-[#2563eb] outline-none shadow-none",
  warn: "border-0 bg-transparent text-[#ca8a04] outline-none shadow-none",
  error: "border-0 bg-transparent text-[#dc2626] outline-none shadow-none",
  secondary: "border-0 bg-transparent text-[#64748b] outline-none shadow-none",
  contrast: "border-0 bg-transparent text-[#020617] outline-none shadow-none",
};

function variantClass(severity: MessageSeverity, variant: MessageVariant): string {
  if (variant === "outlined") return OUTLINED_CLASS[severity];
  if (variant === "simple") return SIMPLE_CLASS[severity];
  return FILLED_CLASS[severity];
}

function GallerySection({
  title,
  children,
  testId,
}: {
  title: string;
  children: ReactNode;
  testId: string;
}) {
  return (
    <div
      className="flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface)] p-3 shadow-[var(--exits-shadow-sm)]"
      data-testid={testId}
    >
      <h3 className="m-0 text-[length:var(--exits-text-sm)] font-bold text-foreground">{title}</h3>
      {children}
    </div>
  );
}

function GalleryMessage({
  severity,
  variant = "filled",
  closable = true,
  icon: Icon,
  children,
  size = "default",
  className,
  testId,
  onClose,
}: {
  severity: MessageSeverity;
  variant?: MessageVariant;
  closable?: boolean;
  icon?: LucideIcon | null;
  children: ReactNode;
  size?: "default" | "small";
  className?: string;
  testId?: string;
  onClose?: () => void;
}) {
  const [open, setOpen] = useState(true);
  if (!open) return null;

  const spin = severity === "secondary" && Icon === Loader2;

  return (
    <div
      role={severity === "error" ? "alert" : "status"}
      aria-live="polite"
      data-gallery-message-severity={severity}
      data-gallery-message-variant={variant}
      data-testid={testId}
      className={cn(
        "exits-gallery-message flex w-full min-w-0 items-center gap-[7px] rounded-[6px] px-[10.5px] text-[14px] leading-snug",
        size === "small" ? "py-1 text-[12px]" : "py-[7px]",
        variantClass(severity, variant),
        className,
      )}
    >
      {Icon ? (
        <Icon
          className={cn("size-5 shrink-0", spin && "animate-spin")}
          aria-hidden
          strokeWidth={2}
        />
      ) : null}
      <div className="min-w-0 flex-1">{children}</div>
      {closable ? (
        <button
          type="button"
          className="inline-flex size-6 shrink-0 items-center justify-center rounded-[4px] text-inherit opacity-80 hover:opacity-100"
          aria-label="Close"
          data-testid={testId ? `${testId}-close` : undefined}
          onClick={() => {
            setOpen(false);
            onClose?.();
          }}
        >
          <X className="size-4" aria-hidden strokeWidth={2} />
        </button>
      ) : null}
    </div>
  );
}

type DemoToast = {
  id: string;
  severity: MessageSeverity;
  title: string;
  detail?: string;
  actionLabel?: string;
};

const SEVERITY_TOAST_META: Record<
  MessageSeverity,
  { label: string; title: string; detail: string }
> = {
  info: {
    label: "Info",
    title: "Info Message",
    detail: "Message Detail",
  },
  success: {
    label: "Success",
    title: "Success Message",
    detail: "Message Detail",
  },
  warn: {
    label: "Warn",
    title: "Warn Message",
    detail: "Message Detail",
  },
  error: {
    label: "Error",
    title: "Error Message",
    detail: "Message Detail",
  },
  secondary: {
    label: "Secondary",
    title: "Secondary Message",
    detail: "Message Detail",
  },
  contrast: {
    label: "Contrast",
    title: "Contrast Message",
    detail: "Message Detail",
  },
};

function DemoToastStack({
  toasts,
  onDismiss,
}: {
  toasts: DemoToast[];
  onDismiss: (id: string) => void;
}) {
  if (toasts.length === 0) return null;
  return (
    <div
      className="pointer-events-none fixed right-4 top-4 z-[80] flex w-[min(100%-2rem,22rem)] flex-col gap-2"
      data-testid="ui-standard-msg-toast-region"
      aria-live="polite"
    >
      {toasts.map((toast) => (
        <div key={toast.id} className="pointer-events-auto shadow-[var(--exits-shadow-md)]">
          <GalleryMessage
            severity={toast.severity}
            Icon={
              toast.severity === "success"
                ? Check
                : toast.severity === "info"
                  ? Sparkles
                  : toast.severity === "warn"
                    ? Receipt
                    : toast.severity === "error"
                      ? AlertTriangle
                      : toast.severity === "secondary"
                        ? Loader2
                        : Wifi
            }
            testId={`ui-standard-msg-toast-${toast.id}`}
            onClose={() => onDismiss(toast.id)}
          >
            <div className="flex flex-col gap-0.5">
              <span className="font-semibold">{toast.title}</span>
              {toast.detail ? <span className="opacity-90">{toast.detail}</span> : null}
              {toast.actionLabel ? (
                <a
                  href="#toast-action"
                  className="mt-1 font-semibold underline underline-offset-2"
                  onClick={(e) => e.preventDefault()}
                >
                  {toast.actionLabel}
                </a>
              ) : null}
            </div>
          </GalleryMessage>
        </div>
      ))}
    </div>
  );
}

/**
 * Button gallery matching the full Diamond Message sample.
 * @see https://diamond.primeng.dev/uikit/message
 */
export function UiStandardsMessageGallery() {
  const [toasts, setToasts] = useState<DemoToast[]>([]);

  const pushToast = useCallback((toast: Omit<DemoToast, "id">) => {
    const id =
      typeof crypto !== "undefined" && "randomUUID" in crypto
        ? crypto.randomUUID()
        : `msg-toast-${Date.now()}`;
    setToasts((current) => [...current, { ...toast, id }]);
    window.setTimeout(() => {
      setToasts((current) => current.filter((item) => item.id !== id));
    }, 4200);
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts((current) => current.filter((item) => item.id !== id));
  }, []);

  const outlineBtn =
    "!h-[32px] !min-h-[32px] !rounded-[6px] !border !border-[color-mix(in_srgb,var(--exits-severity-info)_45%,var(--exits-border))] !bg-[var(--exits-surface)] !px-3 !text-[14px] !font-medium !text-[var(--exits-severity-info)] hover:!bg-[color-mix(in_srgb,var(--exits-severity-info)_8%,var(--exits-surface))]";

  return (
    <div className="flex flex-col gap-3" data-testid="ui-standard-message-gallery">
      <DemoToastStack toasts={toasts} onDismiss={dismissToast} />

      <div className="flex flex-col gap-1">
        <PlaygroundLabel>Gallery</PlaygroundLabel>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          Full sample cards from{" "}
          <a
            href="https://diamond.primeng.dev/uikit/message"
            target="_blank"
            rel="noreferrer"
            className="font-medium text-primary underline-offset-2 hover:underline"
          >
            Diamond PrimeNG → Message
          </a>
          . Showcase-only; product Notice / Toast keep ExItS APIs.
        </p>
      </div>

      <div className="grid min-w-0 gap-3 md:grid-cols-2">
        <GallerySection title="Toast" testId="ui-standard-msg-gallery-toast">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              appearance="outline"
              className={outlineBtn}
              data-testid="ui-standard-msg-create-toast"
              onClick={() =>
                pushToast({
                  severity: "info",
                  title: "Message Summary",
                  detail: "Message Detail",
                })
              }
            >
              Create toast
            </Button>
          </div>
        </GallerySection>

        <GallerySection title="Severity" testId="ui-standard-msg-gallery-severity">
          <div className="flex flex-wrap gap-2">
            {(Object.keys(SEVERITY_TOAST_META) as MessageSeverity[]).map((severity) => (
              <Button
                key={severity}
                type="button"
                appearance="outline"
                className={cn(
                  "!h-[32px] !min-h-[32px] !rounded-[6px] !px-3 !text-[14px] !font-medium",
                  severity === "info" &&
                    "!border-[#93c5fd] !bg-[#eff6ff] !text-[#2563eb] hover:!bg-[#dbeafe]",
                  severity === "success" &&
                    "!border-[#86efac] !bg-[#f0fdf4] !text-[#16a34a] hover:!bg-[#dcfce7]",
                  severity === "warn" &&
                    "!border-[#fde047] !bg-[#fefce8] !text-[#ca8a04] hover:!bg-[#fef9c3]",
                  severity === "error" &&
                    "!border-[#fca5a5] !bg-[#fef2f2] !text-[#dc2626] hover:!bg-[#fee2e2]",
                  severity === "secondary" &&
                    "!border-[#cbd5e1] !bg-[#f8fafc] !text-[#475569] hover:!bg-[#f1f5f9]",
                  severity === "contrast" &&
                    "!border-[#0f172a] !bg-[#0f172a] !text-[#f8fafc] hover:!bg-[#1e293b]",
                )}
                data-testid={`ui-standard-msg-severity-${severity}`}
                onClick={() =>
                  pushToast({
                    severity,
                    title: SEVERITY_TOAST_META[severity].title,
                    detail: SEVERITY_TOAST_META[severity].detail,
                  })
                }
              >
                {SEVERITY_TOAST_META[severity].label}
              </Button>
            ))}
          </div>
        </GallerySection>

        <GallerySection title="Custom" testId="ui-standard-msg-gallery-custom">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              appearance="outline"
              className={outlineBtn}
              data-testid="ui-standard-msg-custom-toast"
              onClick={() =>
                pushToast({
                  severity: "success",
                  title: "Custom toast",
                  detail: "This toast uses a custom severity title.",
                })
              }
            >
              Custom toast
            </Button>
          </div>
        </GallerySection>

        <GallerySection title="Expanded Mode" testId="ui-standard-msg-gallery-expanded">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              appearance="outline"
              className={outlineBtn}
              data-testid="ui-standard-msg-expanded-toast"
              onClick={() =>
                pushToast({
                  severity: "info",
                  title: "Expanded toast",
                  detail:
                    "This expanded toast includes a longer detail line so operators can read context without opening another surface.",
                })
              }
            >
              Create toast
            </Button>
          </div>
        </GallerySection>

        <GallerySection title="Action" testId="ui-standard-msg-gallery-action">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              appearance="outline"
              className={outlineBtn}
              data-testid="ui-standard-msg-action-toast"
              onClick={() =>
                pushToast({
                  severity: "warn",
                  title: "Action required",
                  detail: "Review the pending subscription renewal.",
                  actionLabel: "Take action",
                })
              }
            >
              Create toast with action
            </Button>
          </div>
        </GallerySection>

        <GallerySection title="Inline" testId="ui-standard-msg-gallery-inline">
          <div className="flex w-full max-w-md flex-col gap-2">
            <GalleryMessage
              severity="error"
              closable={false}
              Icon={CircleX}
              testId="ui-standard-msg-inline-banner"
            >
              Validation Failed
            </GalleryMessage>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="sr-only">Username</span>
              <input
                className="h-9 rounded-[6px] border border-[var(--exits-danger)] bg-[var(--exits-surface)] px-3 text-foreground outline-none"
                placeholder="Username"
                aria-invalid
                aria-describedby="ui-standard-msg-inline-username-error"
                data-testid="ui-standard-msg-inline-username"
                defaultValue=""
              />
              <GalleryMessage
                severity="error"
                variant="simple"
                size="small"
                closable={false}
                icon={null}
                className="!px-0 !py-0"
                testId="ui-standard-msg-inline-username-error"
              >
                <span id="ui-standard-msg-inline-username-error">Username is required</span>
              </GalleryMessage>
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="sr-only">Phone</span>
              <input
                className="h-9 rounded-[6px] border border-[var(--exits-danger)] bg-[var(--exits-surface)] px-3 text-foreground outline-none"
                placeholder="Phone"
                aria-invalid
                aria-describedby="ui-standard-msg-inline-phone-error"
                data-testid="ui-standard-msg-inline-phone"
                defaultValue=""
              />
              <GalleryMessage
                severity="error"
                variant="simple"
                size="small"
                closable={false}
                icon={null}
                className="!px-0 !py-0"
                testId="ui-standard-msg-inline-phone-error"
              >
                <span id="ui-standard-msg-inline-phone-error">Phone number is required</span>
              </GalleryMessage>
            </label>
          </div>
        </GallerySection>

        <GallerySection title="Message" testId="ui-standard-msg-gallery-message">
          <div className="flex w-full flex-col gap-2">
            {MESSAGE_SAMPLES.map(({ severity, Icon }) => (
              <GalleryMessage
                key={`filled-${severity}`}
                severity={severity}
                variant="filled"
                Icon={Icon}
                testId={`ui-standard-msg-filled-${severity}`}
              >
                {MESSAGE_FILLED_COPY[severity]}
              </GalleryMessage>
            ))}
          </div>
        </GallerySection>

        <GallerySection title="Outlined" testId="ui-standard-msg-gallery-outlined">
          <div className="flex w-full flex-col gap-2">
            {MESSAGE_SAMPLES.map(({ severity, text, Icon }) => (
              <GalleryMessage
                key={`outlined-${severity}`}
                severity={severity}
                variant="outlined"
                Icon={Icon}
                testId={`ui-standard-msg-outlined-${severity}`}
              >
                {text}
              </GalleryMessage>
            ))}
          </div>
        </GallerySection>

        <GallerySection title="Simple" testId="ui-standard-msg-gallery-simple">
          <div className="flex w-full flex-col gap-2">
            {MESSAGE_SAMPLES.map(({ severity, text, Icon }) => (
              <GalleryMessage
                key={`simple-${severity}`}
                severity={severity}
                variant="simple"
                Icon={Icon}
                testId={`ui-standard-msg-simple-${severity}`}
              >
                {text}
              </GalleryMessage>
            ))}
          </div>
        </GallerySection>
      </div>
    </div>
  );
}
