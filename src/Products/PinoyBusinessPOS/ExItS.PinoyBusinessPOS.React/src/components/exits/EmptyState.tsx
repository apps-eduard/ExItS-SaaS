export function EmptyState({ title, detail }: { title: string; detail: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-[var(--exits-radius-md)] border border-dashed border-border px-4 py-6">
      <p className="m-0 font-semibold">{title}</p>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{detail}</p>
    </div>
  );
}
