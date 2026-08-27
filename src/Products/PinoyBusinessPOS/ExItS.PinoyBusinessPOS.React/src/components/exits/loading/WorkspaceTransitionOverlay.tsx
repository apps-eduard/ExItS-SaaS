import { ExitsLoaderMark } from "@/components/exits/loading/ExitsLoaderMark";
import { useDeferredVisible } from "@/components/exits/loading/useDeferredVisible";

/**
 * Blocking overlay for workspace/account transitions.
 * Keeps the existing shell painted underneath; does not clear content.
 */
export function WorkspaceTransitionOverlay({
  active,
  label,
  detail,
  testId = "workspace-transition-overlay",
}: {
  active: boolean;
  label: string;
  detail?: string | null;
  testId?: string;
}) {
  const visible = useDeferredVisible(active);

  if (!active && !visible) {
    return null;
  }

  if (!visible) {
    return null;
  }

  return (
    <div
      className="exits-workspace-transition"
      data-testid={testId}
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="exits-workspace-transition__backdrop" aria-hidden="true" />
      <div className="exits-workspace-transition__panel">
        <ExitsLoaderMark size="md" />
        <p className="exits-workspace-transition__label m-0 text-center text-[length:var(--exits-text-md)] font-semibold text-foreground">
          {label}
        </p>
        {detail ? (
          <p className="exits-workspace-transition__detail m-0 text-center text-[length:var(--exits-text-sm)] text-muted">
            {detail}
          </p>
        ) : null}
      </div>
    </div>
  );
}
