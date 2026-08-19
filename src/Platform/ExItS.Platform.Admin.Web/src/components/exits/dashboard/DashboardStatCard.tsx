export function DashboardStatCard({ value, detail }: { value: string; detail?: string }) {
  return (
    <div className="min-w-0">
      <p className="mt-1 font-[family-name:var(--exits-font-tabular)] text-[length:var(--exits-text-xl)] font-semibold tabular-nums leading-none">
        {value}
      </p>
      {detail ? (
        <p className="mt-1.5 text-[length:var(--exits-text-xs)] text-muted break-words">{detail}</p>
      ) : null}
    </div>
  );
}
