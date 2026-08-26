export function PageHeader({ title, description }: { title: string; description?: string }) {
  return (
    <header className="flex flex-col gap-1">
      <h1 className="m-0 text-[length:var(--exits-text-xl)] font-bold tracking-tight">{title}</h1>
      {description ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
          {description}
        </p>
      ) : null}
    </header>
  );
}
