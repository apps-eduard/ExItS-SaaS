export function LoadingState({ label }: { label: string }) {
  return (
    <p
      className="m-0 text-[length:var(--exits-text-sm)] text-muted"
      role="status"
      aria-live="polite"
    >
      {label}
    </p>
  );
}
