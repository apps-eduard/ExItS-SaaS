import { Inbox } from "lucide-react";

export function EmptyState({ title, body }: { title: string; body: string }) {
  return (
    <div className="flex flex-col items-start gap-2 py-2">
      <span className="flex size-10 items-center justify-center rounded-full bg-surface-muted text-muted">
        <Inbox className="size-5" aria-hidden="true" />
      </span>
      <h3 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">{title}</h3>
      <p className="m-0 max-w-prose text-muted">{body}</p>
    </div>
  );
}
