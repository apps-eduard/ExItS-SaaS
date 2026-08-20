import { Fragment, type ReactNode } from "react";
import { cn } from "@/lib/utils";

export type AdminTableColumn<T> = {
  id: string;
  header: string;
  align?: "left" | "right";
  className?: string;
  cell: (row: T) => ReactNode;
};

export function AdminTable<T extends { id: string }>({
  columns,
  rows,
  empty,
  caption,
  expandedContent,
}: {
  columns: AdminTableColumn<T>[];
  rows: T[];
  empty: string;
  caption?: string;
  /** When non-null, renders a full-width detail row under the data row. */
  expandedContent?: (row: T) => ReactNode | null;
}) {
  if (rows.length === 0) {
    return <p className="text-[length:var(--exits-text-sm)] text-muted break-words">{empty}</p>;
  }

  return (
    <div className="min-w-0 overflow-x-auto">
      <table className="w-full min-w-[28rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
        {caption ? <caption className="sr-only">{caption}</caption> : null}
        <thead>
          <tr className="border-b border-border">
            {columns.map((column) => (
              <th
                key={column.id}
                scope="col"
                className={cn(
                  "py-2 pr-3 font-medium text-muted whitespace-nowrap first:pl-0 last:pr-0",
                  column.align === "right" && "text-right",
                  column.className,
                )}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const detail = expandedContent?.(row) ?? null;
            return (
              <Fragment key={row.id}>
                <tr className="border-b border-border/80 last:border-0 hover:bg-surface-muted/60">
                  {columns.map((column) => (
                    <td
                      key={column.id}
                      className={cn(
                        "h-10 py-1.5 pr-3 align-middle first:pl-0 last:pr-0",
                        column.align === "right" && "text-right tabular-nums",
                        column.className,
                      )}
                    >
                      {column.cell(row)}
                    </td>
                  ))}
                </tr>
                {detail ? (
                  <tr className="border-b border-border/80 bg-surface-muted/40">
                    <td colSpan={columns.length} className="px-0 py-2.5 first:pl-0 last:pr-0">
                      {detail}
                    </td>
                  </tr>
                ) : null}
              </Fragment>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
