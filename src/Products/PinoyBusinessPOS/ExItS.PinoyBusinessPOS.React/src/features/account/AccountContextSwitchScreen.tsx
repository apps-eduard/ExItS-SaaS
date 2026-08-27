/** Full-viewport animated loader for account profile / workspace context switches. */
export function AccountContextSwitchScreen({ label }: { label: string }) {
  return (
    <div
      className="exits-context-switch flex min-h-[min(100dvh,36rem)] flex-1 flex-col items-center justify-center gap-5 py-16"
      data-testid="account-context-switch"
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <div className="exits-context-switch__visual" aria-hidden="true">
        <div className="exits-context-switch__ring" />
        <div className="exits-context-switch__core" />
      </div>
      <p className="exits-context-switch__label m-0 max-w-xs text-center text-[length:var(--exits-text-md)] font-medium text-foreground">
        {label}
      </p>
    </div>
  );
}
