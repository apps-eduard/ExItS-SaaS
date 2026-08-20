export function ErrorState({ title, detail }: { title: string; detail: string }) {
  return (
    <div
      role="alert"
      className="flex flex-col gap-1 rounded-[var(--exits-radius-md)] border border-destructive px-4 py-4"
    >
      <p className="m-0 font-semibold">{title}</p>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{detail}</p>
    </div>
  );
}
