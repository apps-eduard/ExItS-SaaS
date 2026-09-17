import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { useExitsToast } from "@/components/exits/ToastProvider";
import { copyUiSnippet } from "@/features/ui-standards/copyUiSnippet";

export function UiStandardsSnippetBlock({
  snippet,
  testIdPrefix,
}: {
  snippet: string;
  testIdPrefix: string;
}) {
  const toast = useExitsToast();

  return (
    <div className="flex min-w-0 flex-col gap-1.5" data-testid={`${testIdPrefix}-snippet`}>
      <pre
        className="m-0 max-w-full overflow-x-auto rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)]/40 p-2 text-[length:var(--exits-text-xs)] leading-relaxed text-foreground"
        tabIndex={0}
      >
        <code>{snippet}</code>
      </pre>
      <div>
        <Button
          type="button"
          intent="neutral"
          appearance="outline"
          shape="soft"
          data-testid={`${testIdPrefix}-copy`}
          aria-label="Copy snippet"
          onClick={async () => {
            const ok = await copyUiSnippet(snippet);
            if (ok) {
              toast.success("Snippet copied");
            } else {
              toast.error("Copy failed", "Clipboard is not available in this browser.");
            }
          }}
        >
          Copy snippet
        </Button>
      </div>
    </div>
  );
}

export function PlaygroundSection({
  children,
  testId,
}: {
  children: ReactNode;
  testId?: string;
}) {
  return (
    <div
      className="flex min-w-0 flex-col gap-2 border-b border-border pb-3"
      data-testid={testId}
    >
      {children}
    </div>
  );
}

export function PlaygroundLabel({ children }: { children: ReactNode }) {
  return (
    <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
      {children}
    </p>
  );
}
