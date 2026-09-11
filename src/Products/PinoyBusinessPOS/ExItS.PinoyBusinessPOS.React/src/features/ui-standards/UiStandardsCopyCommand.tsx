import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { Check, ChevronDown, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/cn";

export type UiStandardsStandardName = "Table" | "Button" | "Chip" | "Tabs" | "Card" | (string & {});

export type UiStandardsCopyCommandProps = {
  /** Locked standard name(s), e.g. "Tabs" or ["Card", "Chip"]. */
  standard: UiStandardsStandardName | UiStandardsStandardName[];
  /** Compact semantic shorthand shown in the UI and used in Apply:. */
  command: string;
  className?: string;
  /**
   * Optional context (icon names, sample references, chip mappings).
   * Shown compactly under the command; included in clipboard as Context: …
   */
  context?: string;
};

function formatStandardLine(standard: UiStandardsStandardName | UiStandardsStandardName[]): string {
  const standards = (Array.isArray(standard) ? standard : [standard]).map((s) => s.trim()).filter(Boolean);
  if (standards.length === 0) {
    return "Use the locked ExItS Standard.";
  }
  if (standards.length === 1) {
    return `Use the locked ExItS ${standards[0]} Standard.`;
  }
  if (standards.length === 2) {
    return `Use the locked ExItS ${standards[0]} Standard and ExItS ${standards[1]} Standard.`;
  }
  const head = standards.slice(0, -1).map((s) => `ExItS ${s}`).join(", ");
  return `Use the locked ${head}, and ExItS ${standards[standards.length - 1]} Standard.`;
}

function formatContextBlock(context: string): string {
  const trimmed = context.trim();
  if (!trimmed) return "";
  if (/^context\s*:/i.test(trimmed)) {
    return trimmed;
  }
  // Multiline context → keep lines; single line → prefix Context:
  if (trimmed.includes("\n")) {
    return `Context:\n${trimmed}`;
  }
  return `Context: ${trimmed}`;
}

/** Full clipboard text — compact UI shows command (+ optional short context). */
export function formatUiStandardsCursorClipboard(
  standard: UiStandardsStandardName | UiStandardsStandardName[],
  command: string,
  context?: string,
): string {
  const apply = command.trim().replace(/\s+/g, " ");
  const lines = [
    formatStandardLine(standard),
    `Apply: ${apply}.`,
    "Preserve existing business behavior, domain rules, permissions, data flow, and API behavior unless explicitly instructed otherwise.",
  ];
  const ctx = context?.trim() ? formatContextBlock(context) : "";
  if (ctx) {
    lines.splice(2, 0, ctx);
  }
  return lines.join("\n");
}

type CopyStatus = "idle" | "copied" | "failed";

/** Clipboard write — isolated for tests / environments without Clipboard API. */
export async function writeUiStandardsClipboardText(text: string): Promise<void> {
  if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }
  const ta = document.createElement("textarea");
  ta.value = text;
  ta.setAttribute("readonly", "");
  ta.style.position = "fixed";
  ta.style.left = "-9999px";
  document.body.appendChild(ta);
  ta.select();
  const ok = document.execCommand("copy");
  document.body.removeChild(ta);
  if (!ok) {
    throw new Error("copy failed");
  }
}

/** Mutable writer so tests can spy without module-mock circularity. */
export const uiStandardsClipboardWriter = {
  write: writeUiStandardsClipboardText,
};

/**
 * Compact Cursor command footer for UI Standards samples.
 * Displays shorthand; clipboard receives the full safe prompt.
 */
export function UiStandardsCopyCommand({
  standard,
  command,
  className,
  context,
}: UiStandardsCopyCommandProps) {
  const [status, setStatus] = useState<CopyStatus>("idle");
  const [expanded, setExpanded] = useState(false);
  const resetTimer = useRef<number | null>(null);
  const detailsId = useId();
  const fullText = formatUiStandardsCursorClipboard(standard, command, context);
  const standardAttr = Array.isArray(standard) ? standard.join("+") : standard;

  useEffect(() => {
    return () => {
      if (resetTimer.current !== null) {
        window.clearTimeout(resetTimer.current);
      }
    };
  }, []);

  function scheduleReset() {
    if (resetTimer.current !== null) {
      window.clearTimeout(resetTimer.current);
    }
    resetTimer.current = window.setTimeout(() => {
      setStatus("idle");
      resetTimer.current = null;
    }, 1500);
  }

  async function copyToClipboard() {
    try {
      await uiStandardsClipboardWriter.write(fullText);
      setStatus("copied");
      scheduleReset();
    } catch {
      setStatus("failed");
      scheduleReset();
    }
  }

  return (
    <div
      className={cn("mt-1.5 border-t border-border pt-1.5", className)}
      data-testid="ui-standards-copy-command"
      data-standard={standardAttr}
      data-command={command}
      onClick={(event) => event.stopPropagation()}
      onPointerDown={(event) => event.stopPropagation()}
      onKeyDown={(event) => event.stopPropagation()}
    >
      <div className="flex min-w-0 items-start gap-2">
        <div className="min-w-0 flex-1">
          <div className="text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
            Cursor
          </div>
          <code className="mt-0.5 block break-words font-mono text-[length:var(--exits-text-xs)] leading-snug text-foreground">
            {command}
          </code>
          {context?.trim() ? (
            <code className="mt-0.5 block whitespace-pre-wrap break-words font-mono text-[length:var(--exits-text-xs)] leading-snug text-muted">
              {context.trim()}
            </code>
          ) : null}
          {status === "failed" ? (
            <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-[var(--exits-danger)]" role="status">
              Could not copy — select the command and copy manually.
            </p>
          ) : null}
          {expanded ? (
            <pre
              id={detailsId}
              className="m-0 mt-1.5 max-h-28 overflow-auto whitespace-pre-wrap rounded-[var(--exits-radius-sm)] border border-border bg-[var(--exits-surface)] p-2 font-mono text-[length:var(--exits-text-xs)] leading-relaxed text-muted"
            >
              {fullText}
            </pre>
          ) : null}
        </div>
        <div className="flex shrink-0 items-center gap-0.5">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            shape="round"
            className="size-8 min-h-8 min-w-8"
            aria-label="Copy Cursor command"
            title={status === "copied" ? "Copied" : status === "failed" ? "Copy failed" : "Copy"}
            data-testid="ui-standards-copy-command-btn"
            data-copy-status={status}
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
              void copyToClipboard();
            }}
          >
            {status === "copied" ? (
              <Check className="size-3.5 text-[var(--exits-success)] motion-reduce:transition-none" aria-hidden />
            ) : (
              <Copy className="size-3.5" aria-hidden />
            )}
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            shape="round"
            className="size-8 min-h-8 min-w-8"
            aria-label={expanded ? "Hide full Cursor command" : "View full Cursor command"}
            aria-expanded={expanded}
            aria-controls={detailsId}
            title={expanded ? "Hide" : "View command"}
            data-testid="ui-standards-copy-command-expand"
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
              setExpanded((value) => !value);
            }}
          >
            <ChevronDown
              className={cn(
                "size-3.5 transition-transform duration-[var(--exits-motion-fast)] motion-reduce:transition-none",
                expanded && "rotate-180",
              )}
              aria-hidden
            />
          </Button>
        </div>
      </div>
      {status === "copied" ? (
        <span className="sr-only" role="status">
          Copied
        </span>
      ) : null}
    </div>
  );
}

/** Optional wrapper props for sample cards that declare an explicit Cursor command. */
export type UiStandardsSampleCommandProps = {
  standard?: UiStandardsStandardName | UiStandardsStandardName[];
  command?: string;
  commandContext?: string;
};

export function UiStandardsSampleCommandFooter({
  standard,
  command,
  commandContext,
}: UiStandardsSampleCommandProps): ReactNode {
  if (!standard || !command?.trim()) {
    return null;
  }
  return <UiStandardsCopyCommand standard={standard} command={command} context={commandContext} />;
}
